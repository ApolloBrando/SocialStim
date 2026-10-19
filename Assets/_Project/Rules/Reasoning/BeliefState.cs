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
        private readonly Dictionary<CharacterId, int> _suspicion = new Dictionary<CharacterId, int>();
        private readonly Dictionary<CharacterId, int> _trust = new Dictionary<CharacterId, int>();
        private readonly Dictionary<CharacterId, RoleId> _claims = new Dictionary<CharacterId, RoleId>();
        private readonly List<ReportAssertion> _reports = new List<ReportAssertion>();
        private readonly List<NightReport> _privateReports = new List<NightReport>();
        private readonly List<EvidenceRecord> _evidence = new List<EvidenceRecord>();
        private readonly BeliefDistribution _distribution;
        private readonly Personality _personality;
        private readonly CharacterId _self;
        private readonly ReasoningSettings _settings;

        internal BeliefState(IEnumerable<CharacterId> characters, Personality personality, IRandomSource random,
            CharacterId self, RoleSpec ownRole, SetupKnowledge setup, IReadOnlyList<RoleSpec> catalog,
            IEnumerable<CharacterId> teammates, ReasoningSettings settings)
        {
            _self = self; _personality = personality; _settings = settings;
            var ids = characters.ToList();
            foreach (CharacterId id in ids) { _suspicion.Add(id, 12 + random.Next(13)); _trust.Add(id, 45 + random.Next(11)); }
            _distribution = new BeliefDistribution(ids, setup, catalog, self, ownRole, teammates);
        }

        internal int Exposure { get; private set; }
        internal IReadOnlyList<EvidenceRecord> Evidence => _evidence.ToList().AsReadOnly();
        internal int Suspicion(CharacterId target) => _suspicion[target];
        internal int Trust(CharacterId target) => _trust[target];
        internal FactionEstimate Factions(CharacterId target) => _distribution.Factions(target);
        internal IReadOnlyList<RoleEstimate> Roles(CharacterId target) => _distribution.Roles(target);
        internal RoleId? Claim(CharacterId target) => _claims.TryGetValue(target, out RoleId claim) ? claim : (RoleId?)null;

        internal void Observe(CharacterId speaker, GameCommand speech, int day)
        {
            int influence = Math.Max(0, _settings.AssertionWeight * (_trust[speaker] + 25) / 75);
            if (_personality.HasFlag(Personality.Gullible)) influence += _settings.GullibleBonus;
            if (_personality.HasFlag(Personality.Cautious)) influence = Math.Max(0, influence - 2);
            EvidenceKind kind = speech.Intent == SpeechIntent.ClaimRole ? EvidenceKind.RoleClaim
                : speech.Intent == SpeechIntent.ReportInvestigation ? EvidenceKind.InvestigationClaim : EvidenceKind.Statement;
            Record(day, kind, speaker, speech.Target, speech.Intent, speech.ClaimedRole, speech.ReportedFaction, night: day - 1);
            if (speech.Intent == SpeechIntent.Accuse || speech.Intent == SpeechIntent.CallForTrial || speech.Intent == SpeechIntent.Deflect)
            {
                if (speech.Target.HasValue)
                {
                    Change(speech.Target.Value, influence);
                    _distribution.AssertAlignment(speech.Target.Value, Faction.Antagonist, influence * 2);
                    if (speech.Target.Value == _self && speaker != _self) Expose(influence);
                }
                if (speech.Intent == SpeechIntent.Deflect) Change(speaker, 4);
            }
            if (speech.Intent == SpeechIntent.Vouch && speech.Target.HasValue)
            {
                Change(speech.Target.Value, -influence);
                _distribution.AssertAlignment(speech.Target.Value, Faction.Town, influence);
                if (speech.Target.Value == _self && speaker != _self) Expose(-influence / 2);
            }
            if (speech.Intent == SpeechIntent.Defend)
            { Change(speaker, -3); if (speaker == _self) Expose(-3); }
            if (speech.Intent == SpeechIntent.Agree && speech.Target.HasValue) TrustChange(speech.Target.Value, 3);
            if (speech.Intent == SpeechIntent.Disagree && speech.Target.HasValue) TrustChange(speech.Target.Value, -3);
            if (speech.ClaimedRole.HasValue)
            {
                bool priorExists = _claims.TryGetValue(speaker, out RoleId prior);
                if (priorExists && prior != speech.ClaimedRole.Value) Penalize(speaker, _settings.ContradictionPenalty, day, true);
                if (!priorExists || prior != speech.ClaimedRole.Value) _distribution.Claim(speaker, speech.ClaimedRole.Value, influence);
                _claims[speaker] = speech.ClaimedRole.Value;
            }
            if (speech.Intent == SpeechIntent.ReportInvestigation && speech.Target.HasValue && speech.ReportedFaction.HasValue)
                ObserveReport(speaker, speech.Target.Value, speech.ReportedFaction.Value, day, influence);
        }

        private void ObserveReport(CharacterId speaker, CharacterId target, Faction alignment, int day, int influence)
        {
            int night = day - 1;
            if (_reports.Any(r => r.Speaker == speaker && r.Target == target && r.Night == night && r.Alignment == alignment)) return;
            if (_reports.Any(r => r.Speaker == speaker && r.Target == target && r.Night == night && r.Alignment != alignment))
                Penalize(speaker, _settings.ContradictionPenalty, day, true);
            foreach (CharacterId other in _reports.Where(r => r.Speaker != speaker && r.Target == target && r.Night == night && r.Alignment != alignment).Select(r => r.Speaker).Distinct())
            { Penalize(speaker, 5, day, true); Penalize(other, 5, day, true); }
            if (_privateReports.Any(r => r.Target == target && r.Night == night && r.Alignment != alignment))
                Penalize(speaker, _settings.ContradictionPenalty, day, false);
            _reports.Add(new ReportAssertion(speaker, target, night, alignment));
            Change(target, alignment == Faction.Antagonist ? influence + 6 : -influence);
            _distribution.AssertAlignment(target, alignment, influence * 3);
        }

        internal void Receive(NightReport report)
        {
            if (report.Recipient != _self) throw new ArgumentException("A belief model cannot read another observer's private report.");
            if (report.Kind != ReportKind.Investigation || !report.Target.HasValue || !report.Alignment.HasValue) return;
            _privateReports.Add(report);
            Record(report.Night + 1, EvidenceKind.PrivateInvestigation, _self, report.Target, alignment: report.Alignment, night: report.Night);
            Change(report.Target.Value, report.Alignment == Faction.Antagonist ? 65 : -45);
            _distribution.AssertAlignment(report.Target.Value, report.Alignment.Value, 600);
        }

        internal void Reveal(CharacterId target, RoleSpec role, int day)
        {
            _distribution.Know(target, role.Faction, role.Id);
            Record(day, EvidenceKind.RoleReveal, target, target, role: role.Id, alignment: role.Faction);
            if (_claims.TryGetValue(target, out RoleId claim) && claim != role.Id) Penalize(target, _settings.ContradictionPenalty, day, true);
            foreach (CharacterId reporter in _reports.Where(r => r.Target == target && r.Alignment != role.Appearance).Select(r => r.Speaker).Distinct())
                Penalize(reporter, _settings.ContradictionPenalty, day, true);
            foreach (EvidenceRecord accusation in _evidence.Where(e => e.Kind == EvidenceKind.Statement && e.Target == target
                && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.CallForTrial)).ToList())
            {
                Change(accusation.Actor, role.Faction == Faction.Antagonist ? -8 : 4);
                TrustChange(accusation.Actor, role.Faction == Faction.Antagonist ? 8 : -4);
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
            if (_suspicion[defendant] >= 60 && verdict == Verdict.Innocent) Change(voter, 6);
            if (_suspicion[defendant] < 20 && verdict == Verdict.Guilty) Change(voter, 4);
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
            SpeechIntent? intent = null, RoleId? role = null, Faction? alignment = null, Verdict? verdict = null, int night = 0)
            => _evidence.Add(new EvidenceRecord(_evidence.Count, day, kind, actor, target, intent, role, alignment, verdict, night));
        private void Change(CharacterId target, int amount) => _suspicion[target] = Clamp(_suspicion[target] + amount);
        private void TrustChange(CharacterId target, int amount) => _trust[target] = Clamp(_trust[target] + amount);
        private void Expose(int amount) => Exposure = Clamp(Exposure + amount);
        private static int Clamp(int value) => Math.Max(0, Math.Min(100, value));
    }
}
