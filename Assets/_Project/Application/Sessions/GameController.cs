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
            { add("Continue spectating", "Advance the remaining participants without acting.", GameCommand.Continue()); return new PlayerInputRequest(view, options.AsReadOnly()); }
            switch (view.Phase)
            {
                case GamePhase.Discussion:
                    if (view.Opening) add("Let someone else open", "Yield the opening to one random participant without spending a discussion round.", GameCommand.Continue());
                    if (view.CanReportAction) add("Report last night's action", "Share your recorded selection and legitimate results as your own public statement.", GameCommand.ReportLastAction());
                    add("Defend yourself", "A modest denial can reduce suspicion; it is not proof.", GameCommand.Speak(SpeechIntent.Defend));
                    add("Express fear", "Adds a social stance without asserting evidence.", GameCommand.Speak(SpeechIntent.ExpressFear));
                    add("Stay silent", "Uses this discussion turn without making a claim.", GameCommand.Speak(SpeechIntent.StaySilent));
                    AddClaims(add, view);
                    foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                    {
                        add($"Accuse {target.Name}", "May increase suspicion on this person; repeated false accusations can backfire after a reveal.", GameCommand.Speak(SpeechIntent.Accuse, target.Id));
                        add($"Vouch for {target.Name}", "May lower suspicion on this person; your credibility affects the response.", GameCommand.Speak(SpeechIntent.Vouch, target.Id));
                        add($"Call for a trial of {target.Name}", "Escalates public pressure before nomination.", GameCommand.Speak(SpeechIntent.CallForTrial, target.Id));
                        add($"Agree with {target.Name}", "Expresses support and may modestly improve this person's credibility.", GameCommand.Speak(SpeechIntent.Agree, target.Id));
                        add($"Disagree with {target.Name}", "Challenges this person's credibility without claiming secret evidence.", GameCommand.Speak(SpeechIntent.Disagree, target.Id));
                        add($"Deflect toward {target.Name}", "Redirects suspicion while making you appear evasive.", GameCommand.Speak(SpeechIntent.Deflect, target.Id));
                        if (view.Day > 1)
                            foreach (Faction faction in Enum.GetValues(typeof(Faction)))
                                add($"Report {target.Name} as {faction}", "An attributed assertion that may be true or fabricated; contradicting your own reports damages credibility.",
                                    GameCommand.Speak(SpeechIntent.ReportInvestigation, target.Id, faction: faction));
                    }
                    break;
                case GamePhase.Nomination:
                    add("Abstain from nomination", "A strict majority is required to start a trial.", GameCommand.Nominate(null));
                    foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                        add($"Nominate {target.Name}", "Your nomination is public.", GameCommand.Nominate(target.Id));
                    break;
                case GamePhase.Defense:
                    if (view.Defendant == view.PlayerId)
                    { add("Defend yourself", "Your defense is heard before judgement.", GameCommand.Speak(SpeechIntent.Defend)); AddClaims(add, view); }
                    else add("Proceed to judgement", "The defendant has finished their defense.", GameCommand.Continue());
                    break;
                case GamePhase.Judgement:
                    if (view.Defendant == view.PlayerId) add("Await the verdict", "Defendants cannot vote on themselves.", GameCommand.Continue());
                    else foreach (Verdict verdict in Enum.GetValues(typeof(Verdict)))
                        add(verdict.ToString(), "Votes become public together; Guilty must exceed Innocent.", GameCommand.Judge(verdict));
                    break;
                case GamePhase.LastWords:
                    add(view.Defendant == view.PlayerId ? "Speak last words" : "Continue after last words", "Execution follows, then the run checks its outcome.",
                        view.Defendant == view.PlayerId ? GameCommand.Speak(SpeechIntent.LastWords) : GameCommand.Continue());
                    break;
                case GamePhase.NightSelection:
                    add("Pass tonight", "Preserves limited resources and makes no visit.", GameCommand.Night(null));
                    foreach (var target in view.Characters.Where(c => view.LegalNightTargets.Contains(c.Id)))
                        add($"{ActionLabels.Ability(view.OwnRole.Ability)}: {target.Name}", "Your selection is private. Blocks resolve before protection, attacks, and investigation.", GameCommand.Night(target.Id));
                    break;
                case GamePhase.Dawn: add("Begin discussion", "Review the public deaths and your private results first.", GameCommand.Continue()); break;
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
                    ? "Truthful claim. Listeners still treat it as uncertain evidence."
                    : "Deceptive claim. Switching claims or later contradiction can expose the lie.", GameCommand.Speak(SpeechIntent.ClaimRole, role: role.Id));
        }
    }
}
