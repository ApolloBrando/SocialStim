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
        private readonly TextTemplates _text;
        private readonly RunSession _session;
        private readonly IReadOnlyList<RoleSpec> _catalog;

        /// <summary>Wires an isolated session to the public role catalog used for claims.</summary>
        public GameController(RunSession session, IEnumerable<RoleSpec> catalog)
        { _session = session ?? throw new ArgumentNullException(nameof(session)); _text = session.Text; _catalog = catalog.ToList().AsReadOnly(); }

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
            { add(_text.Render("choice.watch"), _text.Render("choice.watchHint"), GameCommand.Continue()); return _cached = new PlayerInputRequest(view, options.AsReadOnly()); }
            switch (view.Phase)
            {
                case GamePhase.Discussion:
                    if (view.RespondTo.HasValue)
                    {
                        add(_text.Render("choice.defendResponse"), _text.Render("choice.defendResponseHint"), GameCommand.Speak(SpeechIntent.Defend));
                        add(_text.Render("choice.counterAccuse"), _text.Render("choice.counterAccuseHint"), GameCommand.Speak(SpeechIntent.Accuse, view.RespondTo));
                        AddClaims(add, view);
                        add(_text.Render("choice.silentResponse"), _text.Render("choice.silentResponseHint"), GameCommand.Speak(SpeechIntent.StaySilent));
                        break;
                    }
                    if (view.Opening) add(_text.Render("choice.yieldOpening"), _text.Render("choice.yieldOpeningHint"), GameCommand.Continue());
                    if (view.CanReportAction && view.OwnRole.Ability != AbilityKind.None) add(_text.Render("choice.reportAction"), _text.Render("choice.reportActionHint"), GameCommand.ReportLastAction());
                    add(_text.Render("choice.defend"), _text.Render("choice.defendHint"), GameCommand.Speak(SpeechIntent.Defend));
                    add(_text.Render("choice.fear"), _text.Render("choice.fearHint"), GameCommand.Speak(SpeechIntent.ExpressFear));
                    add(_text.Render("choice.silent"), _text.Render("choice.silentHint"), GameCommand.Speak(SpeechIntent.StaySilent));
                    AddClaims(add, view);
                    foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                    {
                        add(_text.Render("choice.accuse", ("target", target.Name)), _text.Render("choice.accuseHint"), GameCommand.Speak(SpeechIntent.Accuse, target.Id));
                        add(_text.Render("choice.vouch", ("target", target.Name)), _text.Render("choice.vouchHint"), GameCommand.Speak(SpeechIntent.Vouch, target.Id));
                        add(_text.Render("choice.trialTarget", ("target", target.Name)), _text.Render("choice.trialTargetHint"), GameCommand.Speak(SpeechIntent.CallForTrial, target.Id));
                        add(_text.Render("choice.agree", ("target", target.Name)), _text.Render("choice.agreeHint"), GameCommand.Speak(SpeechIntent.Agree, target.Id));
                        add(_text.Render("choice.disagree", ("target", target.Name)), _text.Render("choice.disagreeHint"), GameCommand.Speak(SpeechIntent.Disagree, target.Id));
                        add(_text.Render("choice.deflect", ("target", target.Name)), _text.Render("choice.deflectHint"), GameCommand.Speak(SpeechIntent.Deflect, target.Id));
                        if (view.Day > 1 && view.OwnRole.CanBluff)
                            foreach (Faction faction in Enum.GetValues(typeof(Faction)))
                                add(_text.Render("choice.reportBluff", ("target", target.Name), ("result", _text.Render("faction." + faction))), _text.Render("choice.reportBluffHint"),
                                    GameCommand.Speak(SpeechIntent.ReportInvestigation, target.Id, faction: faction));
                    }
                    if (!view.OwnRole.CanBluff)
                        foreach (var report in view.PrivateResults.Where(r => r.Kind == ReportKind.Investigation && r.Night == view.Day - 1
                            && r.Target.HasValue && view.Characters.Any(c => c.Id == r.Target && c.Alive)))
                            add(_text.Render("choice.reportHonest", ("target", view.Characters.First(c => c.Id == report.Target).Name), ("result", _text.Render("faction." + report.Alignment))),
                                _text.Render("choice.reportHonestHint"), GameCommand.Speak(SpeechIntent.ReportInvestigation, report.Target, faction: report.Alignment));
                    break;
                case GamePhase.Voting:
                    if (view.AllowAbstain) add(_text.Render("choice.abstain"), _text.Render("choice.abstainHint"), GameCommand.Vote(null));
                    foreach (var target in view.Characters.Where(c => view.VoteTargets.Contains(c.Id)))
                        add(_text.Render("choice.voteTarget", ("target", target.Name)), _text.Render("choice.voteTargetHint"), GameCommand.Vote(target.Id));
                    break;
                case GamePhase.VoteReveal:
                    add(_text.Render("choice.continue"), _text.Render("choice.continueTallyHint"), GameCommand.Continue());
                    break;
                case GamePhase.Nomination:
                    add(_text.Render("choice.abstainTrial"), _text.Render("choice.abstainTrialHint"), GameCommand.Nominate(null));
                    foreach (var target in view.Characters.Where(c => c.Alive && c.Id != view.PlayerId))
                        add(_text.Render("choice.nominate", ("target", target.Name)), _text.Render("choice.nominateHint"), GameCommand.Nominate(target.Id));
                    break;
                case GamePhase.Defense:
                    if (view.Defendant == view.PlayerId)
                    { add(_text.Render("choice.trialDefend"), _text.Render("choice.trialDefendHint"), GameCommand.Speak(SpeechIntent.Defend)); AddClaims(add, view); }
                    else add(_text.Render("choice.judge"), _text.Render("choice.judgeHint"), GameCommand.Continue());
                    break;
                case GamePhase.Judgement:
                    if (view.Defendant == view.PlayerId) add(_text.Render("choice.awaitVerdict"), _text.Render("choice.awaitVerdictHint"), GameCommand.Continue());
                    else foreach (Verdict verdict in Enum.GetValues(typeof(Verdict)))
                        add(_text.Render("verdict." + verdict), _text.Render("choice.verdictHint"), GameCommand.Judge(verdict));
                    break;
                case GamePhase.LastWords:
                    add(view.Defendant == view.PlayerId ? _text.Render("choice.lastWords") : _text.Render("choice.lastWordsContinue"), _text.Render("choice.lastWordsHint"),
                        view.Defendant == view.PlayerId ? GameCommand.Speak(SpeechIntent.LastWords) : GameCommand.Continue());
                    break;
                case GamePhase.NightSelection:
                    add(_text.Render("choice.nightPass"), _text.Render("choice.nightPassHint"), GameCommand.Night(null));
                    foreach (var target in view.Characters.Where(c => view.LegalNightTargets.Contains(c.Id)))
                        add(_text.Render("choice.nightTarget", ("ability", ActionLabels.Ability(_text, view.OwnRole.Ability)), ("target", target.Name)), _text.Render("choice.nightTargetHint"), GameCommand.Night(target.Id));
                    break;
                case GamePhase.Dawn: add(_text.Render("choice.beginDiscussion"), _text.Render("choice.beginDiscussionHint"), GameCommand.Continue()); break;
            }
            return _cached = new PlayerInputRequest(view, options.AsReadOnly());
        }

        /// <summary>Rejects stale or forged option IDs before touching session state.</summary>
        public bool TryChoose(string optionId, out string error)
        {
            string[] parts = (optionId ?? "").Split(new[] { '|' }, 2);
            PlayerOption option = Request().Options.FirstOrDefault(o => string.Equals(o.Id, parts[0], StringComparison.Ordinal));
            if (option == null) { error = _text.Render("choice.errorOption"); return false; }
            if (parts.Length == 1) return _session.TrySubmit(option.Command, out error);
            if (!option.Phrasings.Any(line => line.Id == parts[1])) { error = _text.Render("choice.errorPhrasing"); return false; }
            return _session.TrySubmit(option.Command.WithDialogue(parts[1]), out error);
        }

        private void AddClaims(Action<string, string, GameCommand> add, PlayerView view)
        {
            foreach (RoleSpec role in _catalog.Where(r => view.OwnRole.CanClaim(r.Id)).OrderBy(r => r.Name, StringComparer.Ordinal))
                add(_text.Render("choice.claim", ("role", role.Name)), role.Id == view.OwnRole.Id
                    ? _text.Render("choice.truthHint")
                    : _text.Render("choice.bluffHint"), GameCommand.Speak(SpeechIntent.ClaimRole, role: role.Id));
        }
    }
}
