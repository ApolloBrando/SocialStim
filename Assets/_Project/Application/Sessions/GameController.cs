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
        private readonly InteractionLabels _copy;
        private readonly IReadOnlyList<RoleSpec> _catalog;
        private readonly GameText _text;

        /// <summary>Wires an isolated session to the public role catalog used for claims.</summary>
        public GameController(RunSession session, IEnumerable<RoleSpec> catalog, InteractionLabels copy = null, GameText text = null)
        { _text = text ?? GameText.Default; _copy = copy ?? InteractionLabels.Default; _session = session ?? throw new ArgumentNullException(nameof(session)); _catalog = catalog.ToList().AsReadOnly(); }

        /// <summary>Builds choices without consulting hidden assignments or NPC scores.</summary>
        public PlayerInputRequest Request()
        {
            PlayerView view = _session.GetPlayerView();
            var options = new List<PlayerOption>();
            Action<string, string, GameCommand> add = (label, interpretation, command) =>
                options.Add(new PlayerOption($"{view.Revision}:{options.Count}", label, interpretation, command));
            if (view.Phase == GamePhase.Ended) return new PlayerInputRequest(view, options.AsReadOnly());
            if (view.Phase == GamePhase.JesterRevenge)
            {
                foreach (var target in view.Characters.Where(c => view.RevengeTargets.Contains(c.Id)))
                    add(_text.Format("ui.revengeTarget", ("name", target.Name)), _text.Format("ui.revengeHelp"), GameCommand.Revenge(target.Id));
                return new PlayerInputRequest(view, options.AsReadOnly());
            }
            if (!view.Alive)
            { add("Watch", "Continue the run.", GameCommand.Continue()); return new PlayerInputRequest(view, options.AsReadOnly()); }
            if (view.CounterClaimRole.HasValue)
            {
                RoleSpec disputed = _catalog.First(r => r.Id == view.CounterClaimRole.Value);
                add(_copy.CounterClaim + " " + disputed.Name, "", GameCommand.Speak(SpeechIntent.CounterClaim, role: disputed.Id));
                add(_copy.Wait, "", GameCommand.Speak(SpeechIntent.StaySilent));
                return new PlayerInputRequest(view, options.AsReadOnly());
            }
            switch (view.Phase)
            {
                case GamePhase.Discussion:
                    add(_copy.Wait, "", GameCommand.Speak(SpeechIntent.StaySilent));
                    if (view.RespondTo.HasValue)
                    {
                        add(_copy.DefendSelf, "", GameCommand.Speak(SpeechIntent.Defend));
                        add(_copy.Refute, "", GameCommand.Speak(SpeechIntent.Deflect, view.RespondTo));
                    }
                    else
                        foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                        {
                            add(target.Name, "", GameCommand.Speak(SpeechIntent.Accuse, target.Id));
                            add(target.Name, "", GameCommand.Speak(SpeechIntent.Vouch, target.Id));
                        }
                    if (view.FollowUpTo.HasValue && view.FollowUpTo != view.PlayerId)
                    {
                        add(_copy.Agree, "", GameCommand.Speak(SpeechIntent.Agree, view.FollowUpTo));
                        add(_copy.Disagree, "", GameCommand.Speak(SpeechIntent.Disagree, view.FollowUpTo));
                    }
                    AddClaims(add, view);
                    break;
                case GamePhase.Voting:
                    if (view.AllowAbstain) add("Abstain", "Cast no target vote.", GameCommand.Vote(null));
                    foreach (var target in view.Characters.Where(c => view.VoteTargets.Contains(c.Id)))
                        add("Vote " + target.Name, "The largest vote total selects the defendant.", GameCommand.Vote(target.Id));
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
                    { add(_copy.DefendSelf, "Make your case.", GameCommand.Speak(SpeechIntent.Defend)); AddClaims(add, view); }
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
            if (view.CounterClaimRole.HasValue) return;
            foreach (RoleSpec role in _catalog.Where(r => view.OwnRole.CanClaim(r)
                && !view.Characters.Any(c => c.Alive && c.ClaimedRole == r.Id)).OrderBy(r => r.Name, StringComparer.Ordinal))
                if (!view.Characters.First(c => c.Id == view.PlayerId).ClaimedRole.HasValue)
                    add(role.Name, "", GameCommand.Speak(SpeechIntent.ClaimRole, role: role.Id));
            if (view.Phase == GamePhase.Discussion && view.CanReportAction)
            {
                RoleSpec asserted = _catalog.First(r => r.Id == view.Characters.First(c => c.Id == view.PlayerId).ClaimedRole);
                if (view.OwnRole.CanBluff && asserted.Ability == AbilityKind.Investigate && view.LastNightAction?.Target != null)
                    foreach (Faction faction in Enum.GetValues(typeof(Faction)))
                        add(_text.Format("ui.giveReport") + " — " + faction, "", GameCommand.ReportLastAction(faction));
                else add(_text.Format("ui.giveReport"), "", GameCommand.ReportLastAction());
            }
        }
    }
}
