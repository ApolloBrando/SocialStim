using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Council
{
    /// <summary>Engine-free three-step interaction state. Preview, Back and Cancel never spend a turn.</summary>
    public sealed class CouncilController
    {
        private readonly CouncilSession _session;
        private int _epoch;
        private CouncilActionKind _kind;
        private IReadOnlyList<CouncilOption> _phrases;
        public CouncilController(CouncilSession session) { _session = session ?? throw new ArgumentNullException(nameof(session)); }
        public InteractionStep Step { get; private set; }
        public bool QuickMode { get; set; }
        public CouncilSnapshot Snapshot => _session.View();

        public IReadOnlyList<CharacterId> Targets
        {
            get
            {
                var view = Snapshot; var self = view.Knowledge.Self;
                if (view.Phase == CouncilPhase.Vote) return view.VoteTargets;
                if (view.Phase == CouncilPhase.Night) return view.NightTargets;
                if (view.Phase == CouncilPhase.FreeTime) return view.Knowledge.Characters.Where(c => c.Alive && c.Id != self).Select(c => c.Id).ToList().AsReadOnly();
                if (view.Phase != CouncilPhase.Discussion || Step != InteractionStep.Target) return new List<CharacterId>().AsReadOnly();
                return view.Knowledge.Characters.Where(c => c.Alive && (_kind == CouncilActionKind.Defend || c.Id != self))
                    .Select(c => c.Id).ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<CouncilOption> Options()
        {
            var view = Snapshot; var list = new List<CouncilOption>();
            void add(string title, string detail = "", CouncilAction action = null, CouncilActionKind? navigate = null, bool back = false, bool claims = false)
                => list.Add(new CouncilOption(view.Revision + ":" + _epoch + ":" + list.Count, title, detail, action, navigate, back, claims));
            bool alive = view.Knowledge.Characters.Single(c => c.Id == view.Knowledge.Self).Alive;
            if (view.Phase == CouncilPhase.Ended) return list.AsReadOnly();
            if (Step == InteractionStep.Phrasing && _phrases != null)
            {
                add("Back", back: true);
                foreach (var phrase in _phrases) add(phrase.Title, phrase.Detail, phrase.Action);
                return list.AsReadOnly();
            }
            if (view.Phase == CouncilPhase.Vote)
            { if (view.CanAbstain) add("Abstain", action: new CouncilAction(CouncilActionKind.Vote)); return list.AsReadOnly(); }
            if (view.Phase == CouncilPhase.Night)
            { add(alive ? "Pass" : "Watch", action: new CouncilAction(alive ? CouncilActionKind.Night : CouncilActionKind.Continue)); return list.AsReadOnly(); }
            if (view.Phase == CouncilPhase.Morning)
            { add("Begin day", action: new CouncilAction(CouncilActionKind.Continue)); return list.AsReadOnly(); }
            if (view.Phase == CouncilPhase.Elimination)
            {
                if (view.Eliminated == view.Knowledge.Self)
                    foreach (var line in _session.Phrases(new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.LastWords)))
                        add("Last words", Plain(line.Text), new CouncilAction(CouncilActionKind.Chat, lineId: line.Id, intent: SpeechIntent.LastWords));
                add("Continue", action: new CouncilAction(CouncilActionKind.Continue)); return list.AsReadOnly();
            }
            if (view.Phase == CouncilPhase.FreeTime)
            { add("Pass", action: new CouncilAction(CouncilActionKind.Pass)); return list.AsReadOnly(); }
            if (!alive)
            { add("Watch", action: new CouncilAction(CouncilActionKind.Continue)); return list.AsReadOnly(); }
            if (Step == InteractionStep.Claims)
            {
                add("Back", back: true);
                foreach (var role in ClaimPermissions.Available(view.Knowledge.OwnRole, view.Knowledge.Catalog))
                {
                    add("Claim " + role.Name, role.Id == view.Knowledge.OwnRole.Id ? "Truthful" : "Bluff",
                        new CouncilAction(CouncilActionKind.Claim, claim: role.Id));
                    if (role.Id == view.Knowledge.OwnRole.Id && role.CanClaimTruthfully)
                        foreach (var report in view.Knowledge.Results.Where(r => r.Night == view.Day - 1
                            && (r.Readout.HasValue || r.Kind == Rules.Night.ReportKind.PatientAttacked || r.Kind == Rules.Night.ReportKind.PatientSafe)))
                        {
                            var action = new CouncilAction(CouncilActionKind.Claim, report.Target, ReasonType.Evidence, role.Id, report.Readout,
                                patientAttacked: report.Readout.HasValue ? (bool?)null : report.Kind == Rules.Night.ReportKind.PatientAttacked);
                            string target = view.Knowledge.Characters.Single(c => c.Id == report.Target.Value).Name;
                            add("Report " + target, report.Readout?.ToString().ToUpperInvariant()
                                ?? (report.Kind == Rules.Night.ReportKind.PatientAttacked ? "Attacked" : "Not attacked"), action);
                        }
                }
                return list.AsReadOnly();
            }
            if (Step == InteractionStep.Target)
            {
                add("Back", back: true);
                if (_kind == CouncilActionKind.Chat) add("Everyone", action: new CouncilAction(CouncilActionKind.Chat));
                return list.AsReadOnly();
            }
            if (view.Respond)
            {
                add("Defend yourself", action: new CouncilAction(CouncilActionKind.Defend, view.Knowledge.Self, ReasonType.Logic));
                add("Counter-accuse", action: new CouncilAction(CouncilActionKind.Accuse, view.RespondTo, ReasonType.Retaliation));
            }
            else
            {
                add("CHAT", navigate: CouncilActionKind.Chat);
                add("ACCUSE", navigate: CouncilActionKind.Accuse);
                add("DEFEND", navigate: CouncilActionKind.Defend);
            }
            if (ClaimPermissions.Available(view.Knowledge.OwnRole, view.Knowledge.Catalog).Count > 0) add("CLAIM ROLE", claims: true);
            add("Stay silent", action: new CouncilAction(CouncilActionKind.Pass));
            return list.AsReadOnly();
        }

        public bool Choose(string id, out string error)
        {
            var option = Options().FirstOrDefault(o => o.Id == id);
            if (option == null) { error = "That option expired."; return false; }
            error = null;
            if (option.Back) { Step = InteractionStep.Actions; _phrases = null; _epoch++; return true; }
            if (option.Claims) { Step = InteractionStep.Claims; _epoch++; return true; }
            if (option.Navigate.HasValue) { _kind = option.Navigate.Value; Step = InteractionStep.Target; _epoch++; return true; }
            if (option.Action != null && IsSpeech(option.Action) && Step != InteractionStep.Phrasing)
                return Prepare(option.Action, out error);
            return Submit(option.Action, out error);
        }

        public bool ChooseTarget(CharacterId target, int revision, out string error)
        {
            var view = Snapshot;
            if (revision != view.Revision || !Targets.Contains(target)) { error = "Choose a valid target."; return false; }
            if (view.Phase == CouncilPhase.Vote) return Submit(new CouncilAction(CouncilActionKind.Vote, target), out error);
            if (view.Phase == CouncilPhase.Night) return Submit(new CouncilAction(CouncilActionKind.Night, target), out error);
            if (view.Phase == CouncilPhase.FreeTime) return Submit(new CouncilAction(CouncilActionKind.FreeTime, target), out error);
            return Prepare(new CouncilAction(_kind, target), out error);
        }

        private bool Prepare(CouncilAction action, out string error)
        {
            var view = Snapshot; var variants = new List<CouncilAction>();
            if (action.Kind == CouncilActionKind.Accuse || action.Kind == CouncilActionKind.Defend)
            {
                var available = CouncilReasoning.Available(view.Knowledge, action.Target.Value);
                foreach (var reason in available.OrderByDescending(r => r == ReasonType.Evidence ? 100 : r == ReasonType.Contradiction ? 90
                    : r == ReasonType.ClaimConflict ? 80 : r == ReasonType.VoteHistory ? 60 : r == ReasonType.GutFeeling ? 30 : 10).Take(3))
                    variants.Add(new CouncilAction(action.Kind, action.Target, reason));
            }
            else if (action.Kind == CouncilActionKind.Chat && !action.Intent.HasValue)
            {
                variants.Add(new CouncilAction(CouncilActionKind.Chat, action.Target, intent: action.Target.HasValue ? SpeechIntent.Question : SpeechIntent.Chat));
                variants.Add(new CouncilAction(CouncilActionKind.Chat, action.Target, intent: SpeechIntent.Banter));
                variants.Add(new CouncilAction(CouncilActionKind.Chat, action.Target, intent: action.Target.HasValue ? SpeechIntent.Agree : SpeechIntent.ExpressFear));
            }
            else variants.Add(action);
            var phrases = new List<CouncilOption>();
            foreach (var variant in variants)
            {
                var options = _session.Phrases(variant);
                if (options.Count == 0) continue;
                foreach (var line in options.Take(variants.Count == 1 ? 3 : 1))
                    phrases.Add(new CouncilOption("", "[" + (variant.Reason != ReasonType.None ? variant.Reason.ToString()
                        : variant.Intent?.ToString() ?? "Claim") + "]", Plain(line.Text), variant.WithLine(line.Id)));
            }
            if (phrases.Count == 0) { error = "No matching phrasing."; return false; }
            if (QuickMode) return Submit(phrases[0].Action, out error);
            _phrases = phrases.Take(3).ToList().AsReadOnly(); Step = InteractionStep.Phrasing; _epoch++; error = null; return true;
        }
        private bool Submit(CouncilAction action, out string error)
        {
            if (!_session.TrySubmit(action, out error)) return false;
            Step = InteractionStep.Actions; _phrases = null; _epoch++; return true;
        }
        private static bool IsSpeech(CouncilAction action) => action.Kind == CouncilActionKind.Chat || action.Kind == CouncilActionKind.Accuse
            || action.Kind == CouncilActionKind.Defend || action.Kind == CouncilActionKind.Claim || action.Kind == CouncilActionKind.Report;
        private static string Plain(string text)
        { try { return SemanticMarkup.Parse(text).Text; } catch (ArgumentException) { return text; } }
    }
}
