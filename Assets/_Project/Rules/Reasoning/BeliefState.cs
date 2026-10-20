using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Observer-local attributed evidence, credibility, estimates, and visible self-exposure.</summary>
    internal sealed class BeliefState
    {
        private readonly Dictionary<CharacterId, float> _suspicion = new Dictionary<CharacterId, float>();
        private readonly Dictionary<CharacterId, float> _trust = new Dictionary<CharacterId, float>();
        private readonly Dictionary<CharacterId, RoleId> _claims = new Dictionary<CharacterId, RoleId>();
        private readonly List<ReportAssertion> _reports = new List<ReportAssertion>();
        private readonly List<NightReport> _privateReports = new List<NightReport>();
        private readonly List<EvidenceRecord> _evidence = new List<EvidenceRecord>();
        private readonly BeliefDistribution _distribution;
        private readonly Personality _personality;
        private readonly CharacterId _self;
        private readonly ReasoningSettings _settings;
        private readonly bool _deceptionPossible;
        private readonly RoleSpec _ownRole;
        private readonly HashSet<(int, CharacterId, CharacterId, bool)> _heard = new HashSet<(int, CharacterId, CharacterId, bool)>();
        private readonly Dictionary<(int, CharacterId, bool), float> _socialBudget = new Dictionary<(int, CharacterId, bool), float>();
        private readonly HashSet<(int, CharacterId, CharacterId, bool)> _stances = new HashSet<(int, CharacterId, CharacterId, bool)>();
        private readonly HashSet<CharacterId> _revealed = new HashSet<CharacterId>();
        private readonly List<(CharacterId speaker, NightActionClaim claim)> _actionReports = new List<(CharacterId, NightActionClaim)>();
        private readonly HashSet<(int, CharacterId, CharacterId)> _reportConflicts = new HashSet<(int, CharacterId, CharacterId)>();

        internal BeliefState(IEnumerable<CharacterId> characters, Personality personality, IRandomSource random,
            CharacterId self, RoleSpec ownRole, SetupKnowledge setup, IReadOnlyList<RoleSpec> catalog,
            IEnumerable<CharacterId> teammates, ReasoningSettings settings)
        {
            _self = self; _ownRole = ownRole; _personality = personality; _settings = settings;
            _deceptionPossible = catalog.Any(r => r.Ability == AbilityKind.Forge);
            var ids = characters.ToList();
            foreach (CharacterId id in ids)
            {
                _suspicion.Add(id, id == self ? 0 : random.Next(10001) / 1000f);
                _trust.Add(id, id == self ? 10 : random.Next(10001) / 1000f);
            }
            _distribution = new BeliefDistribution(ids, setup, catalog, self, ownRole, teammates);
        }

        internal int Exposure { get; private set; }
        internal IReadOnlyList<EvidenceRecord> Evidence => _evidence.ToList().AsReadOnly();
        internal int Suspicion(CharacterId target) => (int)Math.Round(_suspicion[target] * 10);
        internal float SuspicionValue(CharacterId target) => _suspicion[target];
        internal int Trust(CharacterId target) => (int)Math.Round(_trust[target] * 10);
        internal float TrustValue(CharacterId target) => _trust[target];
        internal Faction? KnownFaction(CharacterId target) => _distribution.KnownFaction(target);
        internal FactionEstimate Factions(CharacterId target) => _distribution.Factions(target);
        internal IReadOnlyList<RoleEstimate> Roles(CharacterId target) => _distribution.Roles(target);
        internal RoleId? Claim(CharacterId target) => _claims.TryGetValue(target, out RoleId claim) ? claim : (RoleId?)null;

        internal void Observe(CharacterId speaker, GameCommand speech, int day)
        {
            if (speech.NightReport != null) { ObserveActionReport(speaker, speech.NightReport, day); return; }
            int influence = Math.Max(0, (int)Math.Round(_settings.AssertionWeight * (_trust[speaker] * 10 + 25) / 75));
            if ((_personality == Personality.Anxious || _personality == Personality.Slow)) influence += _settings.GullibleBonus;
            if ((_personality == Personality.Anxious || _personality == Personality.Grim)) influence = Math.Max(0, influence - 2);
            EvidenceKind kind = speech.Intent == SpeechIntent.ClaimRole || speech.Intent == SpeechIntent.CounterClaim ? EvidenceKind.RoleClaim
                : speech.Intent == SpeechIntent.ReportInvestigation ? EvidenceKind.InvestigationClaim : EvidenceKind.Statement;
            Record(day, kind, speaker, speech.Target, speech.Intent, speech.ClaimedRole, speech.ReportedFaction, night: day - 1);
            if (speech.Intent == SpeechIntent.Accuse || speech.Intent == SpeechIntent.CallForTrial || speech.Intent == SpeechIntent.Deflect)
            {
                if (speech.Target.HasValue)
                {
                    Social(speaker, speaker, speech.Target.Value, day, true, influence, false);
                }
                if (speech.Intent == SpeechIntent.Deflect) Change(speaker, 4);
            }
            if (speech.Intent == SpeechIntent.Vouch && speech.Target.HasValue)
            {
                Social(speaker, speaker, speech.Target.Value, day, false, influence, false);
            }
            if (speech.Intent == SpeechIntent.Defend)
            { Social(speaker, speaker, speaker, day, false, influence, false); if (speaker == _self) Expose(-3); }
            if ((speech.Intent == SpeechIntent.Agree || speech.Intent == SpeechIntent.Disagree) && speech.Target.HasValue)
            {
                bool agrees = speech.Intent == SpeechIntent.Agree;
                if (speaker != _self && _stances.Add((day, speaker, speech.Target.Value, agrees)))
                    TrustChange(speech.Target.Value, agrees ? 1 : -1);
                EvidenceRecord statement = _evidence.LastOrDefault(e => e.Actor == speech.Target.Value
                    && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Deflect
                        || e.Intent == SpeechIntent.Vouch || e.Intent == SpeechIntent.Defend));
                if (statement != null)
                {
                    CharacterId subject = statement.Target ?? statement.Actor;
                    bool accusation = statement.Intent == SpeechIntent.Accuse || statement.Intent == SpeechIntent.Deflect;
                    bool pressure = (speech.Intent == SpeechIntent.Agree) == accusation;
                    Social(speaker, statement.Actor, subject, day, pressure, influence, true);
                }
            }
            if (speech.ClaimedRole.HasValue)
            {
                bool priorExists = _claims.TryGetValue(speaker, out RoleId prior);
                if (priorExists && prior != speech.ClaimedRole.Value) Penalize(speaker, _settings.ContradictionPenalty, day, true);
                if (!priorExists || prior != speech.ClaimedRole.Value) _distribution.Claim(speaker, speech.ClaimedRole.Value, influence);
                _claims[speaker] = speech.ClaimedRole.Value;
                if (speaker != _self && _ownRole.Unique && speech.ClaimedRole == _ownRole.Id
                    && !_evidence.Any(e => e.Kind == EvidenceKind.Contradiction && e.Actor == speaker))
                    Penalize(speaker, _settings.ContradictionPenalty, day, false);
            }
            if (speech.Intent == SpeechIntent.ReportInvestigation && speech.Target.HasValue && speech.ReportedFaction.HasValue)
                ObserveReport(speaker, speech.Target.Value, speech.ReportedFaction.Value, day, influence);
        }

        private void ObserveReport(CharacterId speaker, CharacterId target, Faction alignment, int day, int influence, int? reportedNight = null)
        {
            int night = reportedNight ?? day - 1;
            if (_reports.Any(r => r.Speaker == speaker && r.Target == target && r.Night == night && r.Alignment == alignment)) return;
            if (_reports.Any(r => r.Speaker == speaker && r.Target == target && r.Night == night && r.Alignment != alignment))
                Penalize(speaker, _settings.ContradictionPenalty, day, true);
            if (_reports.Any(r => r.Speaker == speaker && r.Target != target && r.Night == night))
                Penalize(speaker, _settings.ContradictionPenalty, day, true);
            foreach (CharacterId other in _reports.Where(r => r.Speaker != speaker && r.Target == target && r.Night == night && r.Alignment != alignment).Select(r => r.Speaker).Distinct())
            { Penalize(speaker, 5, day, true); Penalize(other, 5, day, true); }
            if (_privateReports.Any(r => r.Kind == ReportKind.Investigation && r.Target == target && r.Night == night && r.Alignment != alignment))
                Penalize(speaker, _settings.ContradictionPenalty, day, false);
            if (_privateReports.Any(r => r.Kind == ReportKind.VisitorSeen && r.Night == night
                && r.Target == speaker && r.Subject.HasValue && r.Subject != target))
                Penalize(speaker, _settings.ContradictionPenalty, day, false);
            _reports.Add(new ReportAssertion(speaker, target, night, alignment));
            if (speaker != _self)
            {
                Change(target, alignment == Faction.Antagonist ? influence + 6 : -influence);
                _distribution.AssertAlignment(target, alignment, influence * 3);
            }
        }

        private void ObserveActionReport(CharacterId speaker, NightActionClaim claim, int day)
        {
            if (_actionReports.Any(r => r.speaker == speaker && r.claim.Signature == claim.Signature)) return;
            Record(day, EvidenceKind.NightActionClaim, speaker, claim.Target, SpeechIntent.ReportAction, claim.Role, night: claim.Night, nightReport: claim);
            if (_actionReports.Any(r => r.speaker == speaker && r.claim.Night == claim.Night)
                || _claims.TryGetValue(speaker, out RoleId declared) && declared != claim.Role)
                Penalize(speaker, _settings.ContradictionPenalty, day, true);
            if (speaker != _self && claim.Target.HasValue && !claim.Blocked)
            {
                bool seenElsewhere = _privateReports.Any(r => r.Kind == ReportKind.VisitorSeen && r.Night == claim.Night
                    && r.Target == speaker && r.Subject != claim.Target);
                bool missingVisitor = _privateReports.Any(r => r.Kind == ReportKind.NoVisitors && r.Night == claim.Night && r.Target == claim.Target);
                if (seenElsewhere || missingVisitor) Penalize(speaker, _settings.ContradictionPenalty, day, false);
            }
            int credibility = Math.Max(1, (int)Math.Round(_settings.AssertionWeight * (_trust[speaker] + 2.5f) / 7.5f));
            foreach (ReportedNightResult result in claim.Results)
            {
                if (result.Kind == ReportKind.Investigation && result.Target.HasValue && result.Alignment.HasValue)
                {
                    Record(day, EvidenceKind.InvestigationClaim, speaker, result.Target, SpeechIntent.ReportInvestigation,
                        alignment: result.Alignment, night: claim.Night);
                    ObserveReport(speaker, result.Target.Value, result.Alignment.Value, day, credibility, claim.Night);
                }
                if (speaker != _self && result.Kind == ReportKind.VisitorSeen && result.Target.HasValue && result.Subject.HasValue
                    && _evidence.Any(e => e.Kind == EvidenceKind.Death && e.Target == result.Subject && e.Day == claim.Night))
                {
                    // A visit to a casualty is a lead, not proof of an attack or a hidden-role reveal.
                    Change(result.Target.Value, credibility * 0.6f);
                    _distribution.AssertAlignment(result.Target.Value, Faction.Antagonist, credibility);
                }
            }
            _actionReports.Add((speaker, claim));
            foreach (var first in _actionReports.Where(r => r.claim.Night == claim.Night))
                foreach (var second in _actionReports.Where(r => r.claim.Night == claim.Night && r.speaker.Value > first.speaker.Value))
                {
                    bool conflicts = first.claim.Results.Any(r => r.Kind == ReportKind.VisitorSeen && r.Target == second.speaker
                            && second.claim.Target.HasValue && !second.claim.Blocked && r.Subject != second.claim.Target)
                        || second.claim.Results.Any(r => r.Kind == ReportKind.VisitorSeen && r.Target == first.speaker
                            && first.claim.Target.HasValue && !first.claim.Blocked && r.Subject != first.claim.Target)
                        || first.claim.Results.Any(r => r.Kind == ReportKind.NoVisitors && r.Target == second.claim.Target && second.claim.Target.HasValue && !second.claim.Blocked)
                        || second.claim.Results.Any(r => r.Kind == ReportKind.NoVisitors && r.Target == first.claim.Target && first.claim.Target.HasValue && !first.claim.Blocked);
                    if (conflicts && _reportConflicts.Add((claim.Night, first.speaker, second.speaker)))
                    { Penalize(first.speaker, 5, day, true); Penalize(second.speaker, 5, day, true); }
                }
        }

        internal void Receive(NightReport report)
        {
            if (report.Recipient != _self) throw new ArgumentException("A belief model cannot read another observer's private report.");
            if (_privateReports.Any(r => r.Kind == report.Kind && r.Night == report.Night && r.Target == report.Target
                && r.Alignment == report.Alignment && r.Subject == report.Subject)) return;
            _privateReports.Add(report);
            if (report.Kind == ReportKind.VisitorSeen && report.Target.HasValue && report.Subject.HasValue
                && _evidence.Any(e => e.Kind == EvidenceKind.Death && e.Target == report.Subject && e.Day == report.Night))
            { Change(report.Target.Value, 20); _distribution.AssertAlignment(report.Target.Value, Faction.Antagonist, 35); }
            if (report.Kind != ReportKind.Investigation || !report.Target.HasValue || !report.Alignment.HasValue) return;
            Record(report.Night + 1, EvidenceKind.PrivateInvestigation, _self, report.Target, alignment: report.Alignment, night: report.Night);
            Change(report.Target.Value, report.Alignment == Faction.Antagonist ? 65 : -45);
            _distribution.AssertAlignment(report.Target.Value, report.Alignment.Value, 600);
        }

        internal void Reveal(CharacterId target, RoleSpec role, int day)
        {
            if (!_revealed.Add(target)) return;
            _distribution.Know(target, role.Faction, role.Id);
            Record(day, EvidenceKind.RoleReveal, target, target, role: role.Id, alignment: role.Faction);
            if (_claims.TryGetValue(target, out RoleId claim) && claim != role.Id) Penalize(target, _settings.ContradictionPenalty, day, true);
            foreach (CharacterId reporter in _reports.Where(r => !_deceptionPossible && r.Target == target && r.Alignment != role.Appearance).Select(r => r.Speaker).Distinct())
                Penalize(reporter, _settings.ContradictionPenalty, day, true);
            foreach (EvidenceRecord accusation in _evidence.Where(e => e.Kind == EvidenceKind.Statement && e.Target == target
                && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.CallForTrial || e.Intent == SpeechIntent.Deflect))
                .GroupBy(e => e.Actor).Select(g => g.First()).ToList())
            {
                Change(accusation.Actor, role.Faction == Faction.Antagonist ? -8 : 4);
                TrustChange(accusation.Actor, role.Faction == Faction.Antagonist ? 8 : -4);
            }
            foreach (EvidenceRecord ballot in _evidence.Where(e => e.Kind == EvidenceKind.Judgement && e.Target == target))
            {
                if (ballot.Verdict == Verdict.Abstain) continue;
                bool supportedExecution = ballot.Verdict == Verdict.Guilty;
                int credibility = role.Faction == Faction.Antagonist ? (supportedExecution ? 8 : -4) : (supportedExecution ? -3 : 2);
                TrustChange(ballot.Actor, credibility);
                Change(ballot.Actor, -credibility / 2);
                if (ballot.Actor == _self && credibility < 0) Expose(-credibility);
            }
        }

        internal void ObserveNomination(CharacterId voter, CharacterId? target, int day)
        {
            Record(day, EvidenceKind.Nomination, voter, target);
            if (target.HasValue && target.Value == _self && voter != _self) Expose(6);
        }

        internal void ObserveVote(CharacterId voter, CharacterId defendant, Verdict verdict, int day)
        {
            Record(day, EvidenceKind.Judgement, voter, defendant, verdict: verdict);
            if (Suspicion(defendant) >= 60 && verdict == Verdict.Innocent) Change(voter, 6);
            if (Suspicion(defendant) < 20 && verdict == Verdict.Guilty) Change(voter, 4);
            if (defendant == _self && voter != _self) Expose(verdict == Verdict.Guilty ? 8 : verdict == Verdict.Innocent ? -4 : 0);
        }

        internal void ObserveDeath(CharacterId target, int day) => Record(day, EvidenceKind.Death, target, target);

        private void Penalize(CharacterId speaker, int penalty, int day, bool publicContradiction)
        {
            Change(speaker, penalty); TrustChange(speaker, -penalty);
            Record(day, EvidenceKind.Contradiction, speaker, speaker);
            if (speaker == _self && publicContradiction) Expose(penalty);
        }

        private void Record(int day, EvidenceKind kind, CharacterId actor, CharacterId? target = null,
            SpeechIntent? intent = null, RoleId? role = null, Faction? alignment = null, Verdict? verdict = null, int night = 0, NightActionClaim nightReport = null)
            => _evidence.Add(new EvidenceRecord(_evidence.Count, day, kind, actor, target, intent, role, alignment, verdict, night, nightReport));
        private void Social(CharacterId listenerSpeaker, CharacterId origin, CharacterId target, int day, bool pressure, int influence, bool echo)
        {
            // The same source cannot manufacture corroboration by repeating itself or attracting echoes.
            if (listenerSpeaker == _self || !_heard.Add((day, listenerSpeaker, target, pressure))) return;
            var key = (day, target, pressure);
            _socialBudget.TryGetValue(key, out float spent);
            float credibility = Math.Max(0.1f, _trust[listenerSpeaker] / 10f);
            float amount = influence * credibility * (echo ? 0.12f : 0.3f);
            if (echo && _evidence.Count(e => e.Day == day && e.Actor != listenerSpeaker
                && e.Target == origin && e.Intent == SpeechIntent.Agree) > 1) amount *= 0.4f;
            amount = Math.Min(amount, Math.Max(0, 12 - spent));
            _socialBudget[key] = spent + amount;
            Change(target, pressure ? amount : -amount);
            _distribution.AssertAlignment(target, pressure ? Faction.Antagonist : Faction.Town, (int)Math.Round(amount));
            if (target == _self) Expose((int)Math.Ceiling(pressure ? amount : -amount));
            if (target == _self) TrustChange(listenerSpeaker, pressure ? -amount / 2 : amount / 2);
        }

        private void Change(CharacterId target, float amount) => _suspicion[target] = ClampValue(_suspicion[target] + amount / 10f);
        private void TrustChange(CharacterId target, float amount) => _trust[target] = ClampValue(_trust[target] + amount / 10f);
        private static float ClampValue(float value) => Math.Max(0, Math.Min(10, value));
        private void Expose(int amount) => Exposure = Clamp(Exposure + amount);
        private static int Clamp(int value) => Math.Max(0, Math.Min(100, value));
    }
}
