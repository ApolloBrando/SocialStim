using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.Input;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Night;

namespace SocialStim.Application.Sessions
{
    /// <summary>Application port for text or future visual clients; owns no scene references.</summary>
    public sealed class GameController
    {
        private PlayerInputRequest _cached;
        private readonly RunSession _session;
        private readonly IReadOnlyList<RoleSpec> _catalog;

        /// <summary>Wires an isolated session to the public role catalog used for claims.</summary>
        public GameController(RunSession session, IEnumerable<RoleSpec> catalog)
        { _session = session ?? throw new ArgumentNullException(nameof(session)); _catalog = catalog.ToList().AsReadOnly(); }

        /// <summary>Builds choices without consulting hidden assignments or NPC scores.</summary>
        public PlayerInputRequest Request()
        {
            if (_cached != null && _cached.View.Revision == _session.Revision) return _cached;
            PlayerView view = _session.GetPlayerView();
            var options = new List<PlayerOption>();
            Action<string, string, GameCommand> add = (label, interpretation, command) =>
                options.Add(new PlayerOption($"{view.Revision}:{options.Count}", label, interpretation, command, _session.PlayerPhrasings(command)));
            if (view.Phase == GamePhase.Ended) return _cached = new PlayerInputRequest(view, options.AsReadOnly());
            if (!view.Alive)
            { add("Watch", "Continue the run.", GameCommand.Continue()); return _cached = new PlayerInputRequest(view, options.AsReadOnly()); }
            switch (view.Phase)
            {
                case GamePhase.Discussion:
                    if (view.RespondTo.HasValue)
                    {
                        add("Defend yourself", "Answer the accusation.", GameCommand.Speak(SpeechIntent.Defend));
                        add("Counter-accuse", "Challenge your accuser.", GameCommand.Speak(SpeechIntent.Accuse, view.RespondTo));
                        AddClaims(add, view);
                        add("Stay silent", "Decline to answer.", GameCommand.Speak(SpeechIntent.StaySilent));
                        break;
                    }
                    if (view.Opening) add("Let someone else open", "Listen first. Keeps your turn.", GameCommand.Continue());
                    if (view.CanReportAction && view.OwnRole.Ability != AbilityKind.None) add("Report last night's action", "Share your action and results.", GameCommand.ReportLastAction());
                    add("Defend yourself", "Push back on suspicion.", GameCommand.Speak(SpeechIntent.Defend));
                    add("Express fear", "Share your unease.", GameCommand.Speak(SpeechIntent.ExpressFear));
                    add("Stay silent", "Listen this turn.", GameCommand.Speak(SpeechIntent.StaySilent));
                    AddClaims(add, view);
                    foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                    {
                        add($"Accuse {target.Name}", "Raise suspicion.", GameCommand.Speak(SpeechIntent.Accuse, target.Id));
                        add($"Vouch for {target.Name}", "Offer your support.", GameCommand.Speak(SpeechIntent.Vouch, target.Id));
                        add($"Put {target.Name} on trial", "Ask for a trial.", GameCommand.Speak(SpeechIntent.CallForTrial, target.Id));
                        add($"Agree with {target.Name}", "Support their view.", GameCommand.Speak(SpeechIntent.Agree, target.Id));
                        add($"Disagree with {target.Name}", "Challenge their view.", GameCommand.Speak(SpeechIntent.Disagree, target.Id));
                        add($"Deflect toward {target.Name}", "Shift attention away from you.", GameCommand.Speak(SpeechIntent.Deflect, target.Id));
                        if (view.Day > 1 && view.OwnRole.CanBluff)
                            foreach (Faction faction in Enum.GetValues(typeof(Faction)))
                                add($"Report {target.Name} as {faction}", "State a finding. You may bluff.",
                                    GameCommand.Speak(SpeechIntent.ReportInvestigation, target.Id, faction: faction));
                    }
                    if (!view.OwnRole.CanBluff)
                        foreach (var report in view.PrivateResults.Where(r => r.Kind == ReportKind.Investigation && r.Night == view.Day - 1
                            && r.Target.HasValue && view.Characters.Any(c => c.Id == r.Target && c.Alive)))
                            add("Report " + view.Characters.First(c => c.Id == report.Target).Name + " as " + report.Alignment,
                                "Share your actual readout.", GameCommand.Speak(SpeechIntent.ReportInvestigation, report.Target, faction: report.Alignment));
                    break;
                case GamePhase.Voting:
                    if (view.AllowAbstain) add("Abstain", "Cast no target vote.", GameCommand.Vote(null));
                    foreach (var target in view.Characters.Where(c => view.VoteTargets.Contains(c.Id)))
                        add("Vote " + target.Name, "The largest vote total determines elimination.", GameCommand.Vote(target.Id));
                    break;
                case GamePhase.VoteReveal:
                    add("Continue", "Acknowledge the public tally.", GameCommand.Continue());
                    break;
                case GamePhase.Nomination:
                    add("Abstain", "A majority starts a trial.", GameCommand.Nominate(null));
                    foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                        add($"Nominate {target.Name}", "Public nomination.", GameCommand.Nominate(target.Id));
                    break;
                case GamePhase.Defense:
                    if (view.Defendant == view.PlayerId)
                    { add("Defend yourself", "Make your case.", GameCommand.Speak(SpeechIntent.Defend)); AddClaims(add, view); }
                    else add("Vote", "Hear the verdict.", GameCommand.Continue());
                    break;
                case GamePhase.Judgement:
                    if (view.Defendant == view.PlayerId) add("Await the verdict", "You cannot vote in your own trial.", GameCommand.Continue());
                    else foreach (Verdict verdict in Enum.GetValues(typeof(Verdict)))
                        add(verdict.ToString(), "Guilty must exceed Innocent.", GameCommand.Judge(verdict));
                    break;
                case GamePhase.LastWords:
                    add(view.Defendant == view.PlayerId ? "Speak last words" : "Continue", "Execution follows.",
                        view.Defendant == view.PlayerId ? GameCommand.Speak(SpeechIntent.LastWords) : GameCommand.Continue());
                    break;
                case GamePhase.NightSelection:
                    add("Pass tonight", "No visit. Keeps your charges.", GameCommand.Night(null));
                    foreach (var target in view.Characters.Where(c => view.LegalNightTargets.Contains(c.Id)))
                        add($"{ActionLabels.Ability(view.OwnRole.Ability)}: {target.Name}", "Private night action.", GameCommand.Night(target.Id));
                    break;
                case GamePhase.Dawn: add("Begin discussion", "Start the day.", GameCommand.Continue()); break;
            }
            return _cached = new PlayerInputRequest(view, options.AsReadOnly());
        }

        /// <summary>Rejects stale or forged option IDs before touching session state.</summary>
        public bool TryChoose(string optionId, out string error)
        {
            string[] parts = (optionId ?? "").Split(new[] { '|' }, 2);
            PlayerOption option = Request().Options.FirstOrDefault(o => string.Equals(o.Id, parts[0], StringComparison.Ordinal));
            if (option == null) { error = "That option is no longer available."; return false; }
            if (parts.Length == 1) return _session.TrySubmit(option.Command, out error);
            if (!option.Phrasings.Any(line => line.Id == parts[1])) { error = "That phrasing is no longer available."; return false; }
            return _session.TrySubmit(option.Command.WithDialogue(parts[1]), out error);
        }

        private void AddClaims(Action<string, string, GameCommand> add, PlayerView view)
        {
            foreach (RoleSpec role in _catalog.Where(r => view.OwnRole.CanClaim(r.Id)).OrderBy(r => r.Name, StringComparer.Ordinal))
                add($"Claim {role.Name}", role.Id == view.OwnRole.Id
                    ? "Tell the truth."
                    : "Bluff. Contradictions may expose you.", GameCommand.Speak(SpeechIntent.ClaimRole, role: role.Id));
        }
    }
}
