using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.Input;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Sessions
{
    /// <summary>Application port for text or future visual clients; owns no scene references.</summary>
    public sealed class GameController
    {
        private readonly RunSession _session;
        private readonly IReadOnlyList<RoleSpec> _catalog;

        /// <summary>Wires an isolated session to the public role catalog used for claims.</summary>
        public GameController(RunSession session, IEnumerable<RoleSpec> catalog)
        { _session = session ?? throw new ArgumentNullException(nameof(session)); _catalog = catalog.ToList().AsReadOnly(); }

        /// <summary>Builds choices without consulting hidden assignments or NPC scores.</summary>
        public PlayerInputRequest Request()
        {
            PlayerView view = _session.GetPlayerView();
            var options = new List<PlayerOption>();
            Action<string, string, GameCommand> add = (label, interpretation, command) =>
                options.Add(new PlayerOption($"{view.Revision}:{options.Count}", label, interpretation, command));
            if (view.Phase == GamePhase.Ended) return new PlayerInputRequest(view, options.AsReadOnly());
            if (!view.Alive)
            { add("Watch", "Continue the run.", GameCommand.Continue()); return new PlayerInputRequest(view, options.AsReadOnly()); }
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
                    if (view.CanReportAction) add("Report last night's action", "Share your action and results.", GameCommand.ReportLastAction());
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
                        if (view.Day > 1)
                            foreach (Faction faction in Enum.GetValues(typeof(Faction)))
                                add($"Report {target.Name} as {faction}", "State a finding. You may bluff.",
                                    GameCommand.Speak(SpeechIntent.ReportInvestigation, target.Id, faction: faction));
                    }
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
            return new PlayerInputRequest(view, options.AsReadOnly());
        }

        /// <summary>Rejects stale or forged option IDs before touching session state.</summary>
        public bool TryChoose(string optionId, out string error)
        {
            PlayerOption option = Request().Options.FirstOrDefault(o => string.Equals(o.Id, optionId, StringComparison.Ordinal));
            if (option == null) { error = "That option is no longer available."; return false; }
            return _session.TrySubmit(option.Command, out error);
        }

        private void AddClaims(Action<string, string, GameCommand> add, PlayerView view)
        {
            foreach (RoleSpec role in _catalog.OrderBy(r => r.Name, StringComparer.Ordinal))
                add($"Claim {role.Name}", role.Id == view.OwnRole.Id
                    ? "Tell the truth."
                    : "Bluff. Contradictions may expose you.", GameCommand.Speak(SpeechIntent.ClaimRole, role: role.Id));
        }
    }
}
