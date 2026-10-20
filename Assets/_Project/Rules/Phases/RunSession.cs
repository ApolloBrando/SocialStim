using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Phases
{
    /// <summary>Coordinates phase services, authoritative cast state, and player-scoped observations.</summary>
    public sealed class RunSession
    {
        private readonly RunOptions _options;
        private readonly TextTemplates _text;
        /// <summary>Immutable authored copy shared with the application view.</summary>
        public TextTemplates Text => _text;
        private readonly List<Participant> _cast;
        private readonly IReadOnlyList<RoleSpec> _catalog;
        private readonly PhaseMachine _phases = new PhaseMachine();
        private readonly NightResolver _night = new NightResolver();
        private readonly INpcStrategy _strategy;
        private readonly IWinCondition _winCondition;
        private readonly IRandomSource _decisions;
        private readonly DialogueSelector _dialogue;
        private readonly Dictionary<CharacterId, BeliefState> _beliefs = new Dictionary<CharacterId, BeliefState>();
        private readonly Dictionary<CharacterId, List<NightReport>> _results = new Dictionary<CharacterId, List<NightReport>>();
        private readonly Dictionary<CharacterId, RoleSpec> _revealed = new Dictionary<CharacterId, RoleSpec>();
        private readonly List<string> _actionReview = new List<string>();
        private readonly List<GameObservation> _transcript = new List<GameObservation>();
        private readonly DiscussionSchedule _schedule;
        private readonly DiscussionExchange _discussion;
        private readonly List<CharacterId> _voteCandidates = new List<CharacterId>();
        private PluralityResult _ballot;
        private bool _revoting;
        private int _discussionRounds;
        private int _trials;
        private int _revision;
        private CharacterId? _defendant;
        private OutcomeKind _outcome;
        private TeamNightPlan _lastTeamPlan;
        private bool _opening = true;
        private bool _submitting;
        private readonly Dictionary<CharacterId, NightOrder> _lastActions = new Dictionary<CharacterId, NightOrder>();
        private readonly HashSet<CharacterId> _executionObjectives = new HashSet<CharacterId>();

        internal RunSession(RunOptions options, List<Participant> cast, IReadOnlyList<RoleSpec> catalog,
            IEnumerable<DialogueLine> dialogue, INpcStrategy strategy, IWinCondition winCondition, ReasoningSettings reasoning, TextTemplates text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _schedule = options.VotingMode == VotingMode.Gnosia
                ? new DiscussionSchedule(options.DiscussionRounds, options.PlayerWindowsPerRound) : null;
            _discussion = _schedule == null ? null : new DiscussionExchange(_schedule, new CharacterId(0));
            _options = options; _cast = cast; _catalog = catalog; _strategy = strategy; _winCondition = winCondition;
            _decisions = new SeededRandom(options.Seed ^ 0xA341316Cu);
            _dialogue = new DialogueSelector(dialogue, new SeededRandom(options.Seed ^ 0xC8013EA4u));
            // The role list discloses these initial totals; hidden deaths never recalculate them.
            var setup = new SetupKnowledge(cast.Count(c => c.Role.Faction == Faction.Town), cast.Count(c => c.Role.Faction == Faction.Antagonist), cast.Count(c => c.Role.Faction == Faction.Neutral));
            foreach (Participant actor in _cast)
            {
                var teammates = actor.Role.Faction == Faction.Antagonist
                    ? _cast.Where(c => c.Id != actor.Id && c.Role.Faction == Faction.Antagonist).Select(c => c.Id) : Enumerable.Empty<CharacterId>();
                _beliefs.Add(actor.Id, new BeliefState(_cast.Select(c => c.Id), actor.Personality, _decisions,
                    actor.Id, actor.Role, setup, catalog, teammates, reasoning));
                _results.Add(actor.Id, new List<NightReport>());
            }
            Emit(GameCue.PhaseChanged, _text.Render("run.identity", ("name", Player.Name), ("role", Player.Role.Name), ("faction", _text.Render("faction." + Player.Role.Faction))));
            Emit(GameCue.PhaseChanged, _text.Render("run.cast", ("town", setup.Town), ("evil", setup.Antagonist), ("neutral", setup.Neutral)));
        }

        /// <summary>Only public/player-private observations are ever emitted on this event.</summary>
        public event Action<GameObservation> Observation;

        /// <summary>Returns a detached read-only player snapshot; no arbitrary-observer query exists.</summary>
        public PlayerView GetPlayerView()
        {
            KnowledgeView knowledge = Knowledge(Player);
            var actors = LivingNightActors();
            var legal = Player.Alive ? actors.Where(a => NightLegality.IsLegal(actors.First(p => p.Id == Player.Id),
                new NightOrder(Player.Id, a.Id), actors)).Select(a => a.Id).ToList() : new List<CharacterId>();
            return new PlayerView(_revision, _options.Seed, _phases.Day, _phases.Phase, _trials, _options.TrialsPerDay,
                Player.Alive, Player.Role, Player.Charges, _defendant, _outcome, PlayerWon(),
                knowledge.Characters, legal.AsReadOnly(), _transcript.ToList().AsReadOnly(), _opening, _lastActions.ContainsKey(Player.Id), _options.DiscussionRounds,
                _options.VotingMode, _schedule?.Round ?? _discussionRounds + 1, _schedule?.Window ?? 1,
                _options.PlayerWindowsPerRound, _discussion?.RespondTo,
                _voteCandidates.Where(id => id != Player.Id).ToList().AsReadOnly(), _options.AllowAbstain, _revoting, _results[Player.Id].ToList().AsReadOnly(),
                _phases.Phase == GamePhase.Ended ? _cast.Select(c => _text.Render("review.role", ("name", c.Name), ("role", c.Role.Name), ("faction", _text.Render("faction." + c.Role.Faction))))
                    .Concat(_actionReview).ToList().AsReadOnly() : new List<string>().AsReadOnly());
        }

        /// <summary>Applies one legal player command; invalid input leaves state and RNG untouched.</summary>
        public bool TrySubmit(GameCommand command, out string error)
        {
            if (_submitting) { error = _text.Render("error.resolving"); return false; }
            error = ValidatePlayerCommand(command);
            if (error != null) return false;
            if (command.DialogueLine != null && !_dialogue.Options(command.Intent, SpeechContext(Player, command, null)).Any(l => l.Id == command.DialogueLine))
            { error = _text.Render("error.phrasing"); return false; }
            _submitting = true;
            try { ApplyCommand(command); return true; }
            finally { _submitting = false; }
        }

        private void ApplyCommand(GameCommand command)
        {
            switch (_phases.Phase)
            {
                case GamePhase.Discussion:
                    if (_options.VotingMode == VotingMode.Gnosia) { Discuss(command); break; }
                    if (_opening && command.Kind == CommandKind.Continue)
                    {
                        _opening = false;
                        var speakers = Living().Where(c => c.Id != Player.Id).ToList();
                        if (speakers.Count > 0)
                        {
                            Participant speaker = speakers[_decisions.Next(speakers.Count)];
                            if (_beliefs[speaker.Id].Evidence.Any(e => e.Kind == EvidenceKind.Death && e.Day == _phases.Day - 1))
                                Say(speaker, GameCommand.Speak(SpeechIntent.ReactToDeath));
                            else SayNpc(speaker);
                        }
                        break;
                    }
                    _opening = false;
                    if (Player.Alive)
                    {
                        if (command.Kind == CommandKind.ReportLastAction) SayActionReport(Player);
                        else Say(Player, command);
                    }
                    foreach (Participant actor in Living().Where(c => c.Id != Player.Id).ToList()) SayNpc(actor);
                    if (++_discussionRounds >= _options.DiscussionRounds) Move(GamePhase.Nomination);
                    break;
                case GamePhase.Voting: CastDailyVote(command); break;
                case GamePhase.VoteReveal: CompleteVoteReveal(); break;
                case GamePhase.Nomination: Nominate(command); break;
                case GamePhase.Defense:
                    if (_defendant == Player.Id) Say(Player, command);
                    Move(GamePhase.Judgement); break;
                case GamePhase.Judgement: Judge(command); break;
                case GamePhase.LastWords:
                    if (_defendant == Player.Id) Say(Player, command);
                    Execute(); break;
                case GamePhase.NightSelection: ResolveNight(command); break;
                case GamePhase.Dawn:
                    _trials = 0; _discussionRounds = 0; _opening = true; _discussion?.Reset(); _revoting = false;
                    _voteCandidates.Clear(); Move(GamePhase.Discussion); MorningReports(); break;
            }
            _revision++;
        }

        private Participant Player => _cast[0];
        private IEnumerable<Participant> Living() => _cast.Where(c => c.Alive);
        private Participant Actor(CharacterId id) => _cast.First(c => c.Id == id);

        private string ValidatePlayerCommand(GameCommand command)
        {
            if (command == null || _phases.Phase == GamePhase.Ended) return _text.Render("error.noInput");
            if (!Player.Alive) return command.Kind == CommandKind.Continue ? null : _text.Render("error.spectator");
            switch (_phases.Phase)
            {
                case GamePhase.Discussion:
                    if (_discussion?.RespondTo != null)
                        return SpeechLegal(Player, command) && (command.Intent == SpeechIntent.Defend || command.Intent == SpeechIntent.Accuse
                            || command.Intent == SpeechIntent.ClaimRole || command.Intent == SpeechIntent.StaySilent)
                            ? null : _text.Render("error.response");
                    if (command.Kind == CommandKind.Continue && _opening) return null;
                    if (command.Kind == CommandKind.ReportLastAction) return _lastActions.ContainsKey(Player.Id) ? null : _text.Render("error.noAction");
                    return SpeechLegal(Player, command) ? null : _text.Render("error.discussion");
                case GamePhase.Voting:
                    return command.Kind == CommandKind.Vote && ((!command.Target.HasValue && _options.AllowAbstain)
                        || command.Target.HasValue && command.Target.Value != Player.Id && _voteCandidates.Contains(command.Target.Value))
                        ? null : _text.Render("error.ballot");
                case GamePhase.VoteReveal:
                    return command.Kind == CommandKind.Continue ? null : _text.Render("error.tally");
                case GamePhase.Nomination:
                    return command.Kind == CommandKind.Nominate && (!command.Target.HasValue || Living().Any(c => c.Id == command.Target && c.Id != Player.Id)) ? null : _text.Render("error.nominee");
                case GamePhase.Defense:
                    return _defendant == Player.Id ? (SpeechLegal(Player, command) && (command.Intent == SpeechIntent.Defend || command.Intent == SpeechIntent.ClaimRole) ? null : _text.Render("error.defense"))
                        : command.Kind == CommandKind.Continue ? null : _text.Render("error.hearDefense");
                case GamePhase.Judgement:
                    return _defendant == Player.Id ? (command.Kind == CommandKind.Continue ? null : _text.Render("error.selfJudge"))
                        : command.Kind == CommandKind.Judge && Enum.IsDefined(typeof(Verdict), command.Verdict) ? null : _text.Render("error.verdict");
                case GamePhase.LastWords:
                    return _defendant == Player.Id ? command.Kind == CommandKind.Speak && command.Intent == SpeechIntent.LastWords
                        && !command.Target.HasValue && !command.ClaimedRole.HasValue && !command.ReportedFaction.HasValue ? null : _text.Render("error.lastWords")
                        : command.Kind == CommandKind.Continue ? null : _text.Render("error.hearLastWords");
                case GamePhase.NightSelection:
                    var actors = LivingNightActors();
                    return command.Kind == CommandKind.ChooseNightTarget && NightLegality.IsLegal(actors.First(a => a.Id == Player.Id), new NightOrder(Player.Id, command.Target), actors) ? null : _text.Render("error.night");
                case GamePhase.Dawn: return command.Kind == CommandKind.Continue ? null : _text.Render("error.dawn");
                default: return _text.Render("error.phase");
            }
        }

        private bool SpeechLegal(Participant actor, GameCommand command)
        {
            if (command.Kind != CommandKind.Speak || !Enum.IsDefined(typeof(SpeechIntent), command.Intent)) return false;
            if (command.ClaimedRole.HasValue != (command.Intent == SpeechIntent.ClaimRole)
                || command.ReportedFaction.HasValue != (command.Intent == SpeechIntent.ReportInvestigation)) return false;
            bool needsTarget = SpeechContract.RequiresTarget(command.Intent);
            if (needsTarget != command.Target.HasValue) return false;
            if (needsTarget && (!command.Target.HasValue || !Living().Any(c => c.Id == command.Target && c.Id != actor.Id))) return false;
            if (command.Intent == SpeechIntent.ClaimRole && (!command.ClaimedRole.HasValue || !actor.Role.CanClaim(command.ClaimedRole.Value)
                || !_catalog.Any(r => r.Id == command.ClaimedRole.Value))) return false;
            if (command.Intent == SpeechIntent.ReportInvestigation && (_phases.Day <= 1 || !command.ReportedFaction.HasValue
                || !Enum.IsDefined(typeof(Faction), command.ReportedFaction.Value))) return false;
            if (command.Intent == SpeechIntent.ReactToDeath && !_beliefs[actor.Id].Evidence.Any(e => e.Kind == EvidenceKind.Death)) return false;
            if (command.Intent == SpeechIntent.ReactToRoleReveal && !_beliefs[actor.Id].Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal)) return false;
            if (command.Intent == SpeechIntent.ReportInvestigation && !actor.Role.CanBluff
                && !_results[actor.Id].Any(r => r.Kind == ReportKind.Investigation && r.Night == _phases.Day - 1
                    && r.Target == command.Target && r.Alignment == command.ReportedFaction)) return false;
            return command.Intent != SpeechIntent.LastWords && command.Intent != SpeechIntent.ReportAction;
        }

        private GameCommand SayNpc(Participant actor)
        {
            GameCommand speech = _strategy.Speak(Knowledge(actor), _decisions);
            if (speech == null || !SpeechLegal(actor, speech)) speech = GameCommand.Speak(SpeechIntent.StaySilent);
            Say(actor, speech);
            return speech;
        }

        private void Say(Participant actor, GameCommand speech, string actionReport = null)
        {
            DialogueContext context = SpeechContext(actor, speech, actionReport);
            DialogueSelection line = _dialogue.Select(speech.Intent, context, speech.DialogueLine);
            foreach (Participant observer in _cast) _beliefs[observer.Id].Observe(actor.Id, speech, _phases.Day);
            Emit(GameCue.SpeechDelivered, _text.Render("speech.attributed", ("name", actor.Name), ("line", line.Text)), actor.Id, line.Metadata, speech.Target, speech.Intent, speech.ClaimedRole, line.Text);
        }

        /// <summary>Public player revision used for safe request caching.</summary>
        public int Revision => _revision;
        /// <summary>Offers only eligible phrasings using the player's own authorized context.</summary>
        public IReadOnlyList<DialogueSelection> PlayerPhrasings(GameCommand command)
        {
            if (command.Kind != CommandKind.Speak || ValidatePlayerCommand(command) != null) return new List<DialogueSelection>().AsReadOnly();
            return _dialogue.Options(command.Intent, SpeechContext(Player, command, null));
        }
        private DialogueContext SpeechContext(Participant actor, GameCommand speech, string actionReport)
        {
            string claimed = speech.ClaimedRole.HasValue ? _catalog.First(r => r.Id == speech.ClaimedRole.Value).Name : "";
            return new DialogueContext(actor.Id, actor.Name, speech.Target.HasValue ? Actor(speech.Target.Value).Name : "",
                claimed, speech.ReportedFaction.HasValue ? _text.Render("faction." + speech.ReportedFaction.Value) : actionReport ?? "", _phases.Day, actor.Role.Id, actor.Personality,
                _phases.Phase, speech.Target, _beliefs[actor.Id].Evidence, _beliefs[actor.Id].Exposure);
        }

        private void SayActionReport(Participant actor)
        {
            NightOrder order = _lastActions[actor.Id];
            string report = NightActionText.Selection(_text, actor.Role.Ability, order.Target.HasValue ? Actor(order.Target.Value).Name : null);
            foreach (NightReport result in _results[actor.Id].Where(r => r.Night == _phases.Day - 1))
                report += " " + DescribeReport(result);
            Say(actor, GameCommand.Speak(SpeechIntent.ReportAction, order.Target), report);
        }

        private string DescribeReport(NightReport report)
        {
            switch (report.Kind)
            {
                case ReportKind.Investigation: return _text.Render("report.investigation", ("target", Actor(report.Target.Value).Name), ("result", _text.Render("faction." + report.Alignment)));
                case ReportKind.Blocked: return _text.Render("report.blocked");
                case ReportKind.AttackFailed: return _text.Render("report.attackFailed");
                case ReportKind.ProtectionApplied: return _text.Render("report.protection");
                case ReportKind.VisitorSeen: return _text.Render("report.visit", ("visitor", Actor(report.Target.Value).Name), ("target", Actor(report.Subject.Value).Name));
                case ReportKind.NoVisitors: return _text.Render("report.noVisitors", ("target", Actor(report.Target.Value).Name));
                case ReportKind.ForgeryApplied: return _text.Render("report.forged");
                default: return _text.Render("report.survived");
            }
        }

        private void MorningReports()
        {
            foreach (Participant actor in Living().Where(c => c.Id != Player.Id).ToList())
            {
                RoleId? claim = _beliefs[actor.Id].Claim(actor.Id);
                if (!claim.HasValue || !_lastActions.ContainsKey(actor.Id)) continue;
                RoleSpec asserted = _catalog.First(r => r.Id == claim.Value);
                if (asserted.Id == actor.Role.Id)
                {
                    NightReport finding = _results[actor.Id].LastOrDefault(r => r.Kind == ReportKind.Investigation && r.Night == _phases.Day - 1);
                    if (finding != null && Actor(finding.Target.Value).Alive && finding.Target != actor.Id)
                        Say(actor, GameCommand.Speak(SpeechIntent.ReportInvestigation, finding.Target, faction: finding.Alignment));
                    else SayActionReport(actor);
                }
                else
                {
                    // False reports use the public claim and visible cast, never another actor's private results.
                    var targets = Knowledge(actor).Characters.Where(c => c.Alive && c.Id != actor.Id && !c.IsTeammate).Select(c => c.Id).ToList();
                    if (asserted.Ability == AbilityKind.Investigate && targets.Count > 0)
                        Say(actor, GameCommand.Speak(SpeechIntent.ReportInvestigation, targets[_decisions.Next(targets.Count)], faction: Faction.Antagonist));
                    else
                        Say(actor, GameCommand.Speak(SpeechIntent.ReportAction),
                            NightActionText.Claimed(_text, asserted.Ability));
                }
            }
        }

        private void Discuss(GameCommand command)
        {
            if (_discussion.RespondTo.HasValue)
            {
                Say(Player, command); _discussion.Answer(); ContinueExchange(); return;
            }
            if (_opening && command.Kind == CommandKind.Continue && Player.Alive)
            {
                _opening = false;
                var speakers = Living().Where(c => c.Id != Player.Id).ToList();
                if (speakers.Count > 0)
                {
                    Participant speaker = speakers[_decisions.Next(speakers.Count)];
                    _discussion.ObserveOpening(speaker.Id, SayNpc(speaker));
                }
                return;
            }
            _opening = false;
            if (Player.Alive)
            {
                if (command.Kind == CommandKind.ReportLastAction) SayActionReport(Player);
                else Say(Player, command);
            }
            _discussion.Begin(Living().Where(c => c.Id != Player.Id).Select(c => c.Id), command,
                id => Say(Actor(id), GameCommand.Speak(SpeechIntent.Defend)));
            ContinueExchange();
        }

        private void ContinueExchange()
        {
            _discussion.Resume(id => SayNpc(Actor(id)), id => Say(Actor(id), GameCommand.Speak(SpeechIntent.Defend)), Player.Alive);
            if (!_schedule.Complete) return;
            _voteCandidates.Clear(); _voteCandidates.AddRange(Living().Select(c => c.Id));
            Move(GamePhase.Voting);
        }

        private void CastDailyVote(GameCommand command)
        {
            var ballots = new List<KeyValuePair<CharacterId, CharacterId?>>();
            foreach (Participant actor in Living())
            {
                CharacterId? vote = actor.Id == Player.Id ? command.Target : _strategy.Nominate(Knowledge(actor), _decisions);
                var targets = _voteCandidates.Where(id => id != actor.Id).ToList();
                if (!vote.HasValue || !targets.Contains(vote.Value))
                {
                    vote = null;
                    if (!_options.AllowAbstain)
                    {
                        KnowledgeView knowledge = Knowledge(actor);
                        var ranked = knowledge.Characters.Where(c => targets.Contains(c.Id))
                            .OrderBy(c => c.IsTeammate || actor.Role.Faction == Faction.Town && c.KnownFaction == Faction.Town)
                            .ThenByDescending(c => c.Suspicion).ThenBy(c => c.Id.Value).FirstOrDefault();
                        vote = ranked?.Id;
                    }
                }
                ballots.Add(new KeyValuePair<CharacterId, CharacterId?>(actor.Id, vote));
                if (vote.HasValue)
                {
                    KnownCharacter considered = Knowledge(actor).Characters.First(c => c.Id == vote.Value);
                    _actionReview.Add(_text.Render("review.vote", ("day", _phases.Day), ("name", actor.Name), ("target", considered.Name), ("suspicion", considered.Suspicion), ("credibility", considered.Trust)));
                }
            }
            // All ballots are chosen before any are published; there is no later-voter information advantage.
            foreach (var ballot in ballots)
            {
                foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveNomination(ballot.Key, ballot.Value, _phases.Day);
                Emit(GameCue.BallotRevealed, _text.Render("vote.ballot", ("name", Actor(ballot.Key).Name), ("target", ballot.Value.HasValue ? Actor(ballot.Value.Value).Name : _text.Render("vote.abstainTarget"))), ballot.Key, target: ballot.Value);
            }
            _ballot = VoteResolver.Plurality(ballots.Select(b => b.Value));
            string tally = string.Join(_text.Render("separator.tally"), _ballot.Counts.OrderBy(pair => pair.Key.Value).Select(pair => _text.Render("vote.count", ("name", Actor(pair.Key).Name), ("count", pair.Value))));
            Emit(GameCue.VoteTallied, _text.Render("vote.tally", ("tally", tally.Length > 0 ? tally : _text.Render("vote.empty")), ("count", _ballot.Abstentions)));
            _defendant = _ballot.Winner;
            if (!_defendant.HasValue) Emit(GameCue.VoteTied, _ballot.Tied.Count > 0 ? _text.Render("vote.tied") : _text.Render("vote.allAbstained"));
            Move(GamePhase.VoteReveal);
        }

        private void CompleteVoteReveal()
        {
            if (!_defendant.HasValue)
            {
                if (!_revoting && _options.RevoteTies && _ballot.Tied.Count > 1)
                {
                    _revoting = true; _voteCandidates.Clear(); _voteCandidates.AddRange(_ballot.Tied);
                    Emit(GameCue.RevoteStarted, _text.Render("vote.revoteBetween", ("names", string.Join(_text.Render("separator.names"), _voteCandidates.Select(id => Actor(id).Name)))));
                    Move(GamePhase.Voting);
                }
                else StartNight();
                return;
            }
            if (!_options.LastWords) { Execute(); return; }
            Move(GamePhase.LastWords);
            if (_defendant != Player.Id) Say(Actor(_defendant.Value), GameCommand.Speak(SpeechIntent.LastWords));
        }

        private void Nominate(GameCommand command)
        {
            var votes = new List<CharacterId?>();
            var ballots = new List<KeyValuePair<CharacterId, CharacterId?>>();
            foreach (Participant actor in Living())
            {
                CharacterId? vote = actor.Id == Player.Id ? command.Target : _strategy.Nominate(Knowledge(actor), _decisions);
                if (vote.HasValue && !Living().Any(c => c.Id == vote && c.Id != actor.Id)) vote = null;
                votes.Add(vote);
                ballots.Add(new KeyValuePair<CharacterId, CharacterId?>(actor.Id, vote));
            }
            // Choose all ballots before publishing any, so later voters cannot see this round's earlier votes.
            foreach (KeyValuePair<CharacterId, CharacterId?> ballot in ballots)
            {
                foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveNomination(ballot.Key, ballot.Value, _phases.Day);
                Emit(GameCue.NominationMade, _text.Render("trial.nomination", ("name", Actor(ballot.Key).Name), ("target", ballot.Value.HasValue ? Actor(ballot.Value.Value).Name : _text.Render("trial.nobody"))), ballot.Key, target: ballot.Value);
            }
            _defendant = VoteResolver.Nominee(votes, Living().Count());
            if (!_defendant.HasValue)
            { Emit(GameCue.VoteTied, _text.Render("trial.noMajority")); StartNight(); return; }
            _trials++;
            Move(GamePhase.Defense);
            if (_defendant != Player.Id) Say(Actor(_defendant.Value), GameCommand.Speak(SpeechIntent.Defend));
        }

        private void Judge(GameCommand command)
        {
            var ballots = new List<KeyValuePair<CharacterId, Verdict>>();
            foreach (Participant actor in Living().Where(c => c.Id != _defendant).ToList())
            {
                Verdict vote = actor.Id == Player.Id ? command.Verdict : _strategy.Judge(Knowledge(actor), _defendant.Value, _decisions);
                if (!Enum.IsDefined(typeof(Verdict), vote)) vote = Verdict.Abstain;
                ballots.Add(new KeyValuePair<CharacterId, Verdict>(actor.Id, vote));
            }
            foreach (KeyValuePair<CharacterId, Verdict> ballot in ballots)
            {
                foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveVote(ballot.Key, _defendant.Value, ballot.Value, _phases.Day);
                Emit(GameCue.PhaseChanged, _text.Render("trial.ballot", ("name", Actor(ballot.Key).Name), ("verdict", _text.Render("verdict." + ballot.Value))));
            }
            if (VoteResolver.IsGuilty(ballots.Select(b => b.Value)))
            {
                Emit(GameCue.VerdictGuilty, _text.Render("trial.guilty", ("name", Actor(_defendant.Value).Name)));
                Move(GamePhase.LastWords);
                if (_defendant != Player.Id) Say(Actor(_defendant.Value), GameCommand.Speak(SpeechIntent.LastWords));
            }
            else
            {
                Emit(GameCue.VerdictInnocent, _text.Render("trial.innocent", ("name", Actor(_defendant.Value).Name)));
                _defendant = null;
                if (_trials < _options.TrialsPerDay) Move(GamePhase.Nomination); else StartNight();
            }
        }

        private void Execute()
        {
            Move(GamePhase.Execution);
            Kill(Actor(_defendant.Value), true);
            if (!FinishIfTerminal()) StartNight();
        }

        private void StartNight() { _defendant = null; Move(GamePhase.NightSelection); }

        private void ResolveNight(GameCommand command)
        {
            var actors = LivingNightActors();
            var orders = new List<NightOrder>();
            foreach (Participant actor in Living())
            {
                CharacterId? target = actor.Id == Player.Id ? command.Target : _strategy.NightTarget(Knowledge(actor), _decisions);
                var order = new NightOrder(actor.Id, target);
                orders.Add(NightLegality.IsLegal(actors.First(a => a.Id == actor.Id), order, actors) ? order : new NightOrder(actor.Id, null));
            }
            var killers = orders.Where(o => Actor(o.Actor).Role.Faction == Faction.Antagonist && Actor(o.Actor).Role.Ability == AbilityKind.Attack && Actor(o.Actor).Charges != 0)
                .OrderBy(o => o.Actor.Value).ToList();
            var proposals = killers.Select(order =>
            {
                KnowledgeView view = Knowledge(Actor(order.Actor));
                KnownCharacter target = order.Target.HasValue ? view.Characters.First(c => c.Id == order.Target.Value) : null;
                return new TeamProposal(order.Actor, order.Target, target == null ? 0 : SocialNpcStrategy.PublicThreat(view, target));
            });
            bool playerKiller = killers.Any(o => o.Actor == Player.Id);
            _lastTeamPlan = new TeamNightPlanner().Plan(proposals, playerKiller ? Player.Id : (CharacterId?)null, playerKiller ? command.Target : null);
            foreach (NightOrder killer in killers)
            {
                orders.Remove(killer);
                orders.Add(new NightOrder(killer.Actor, _lastTeamPlan.Killer == killer.Actor ? _lastTeamPlan.Target : null));
            }
            if (Player.Alive && Player.Role.Faction == Faction.Antagonist && _lastTeamPlan != null)
                Emit(GameCue.PhaseChanged, _text.Render("night.team", ("name", Actor(_lastTeamPlan.Killer).Name), ("action", _lastTeamPlan.Target.HasValue ? _text.Render("night.teamAttack", ("target", Actor(_lastTeamPlan.Target.Value).Name)) : _text.Render("night.teamPass"))));
            _lastActions.Clear();
            foreach (NightOrder order in orders)
            {
                _lastActions.Add(order.Actor, order);
                _actionReview.Add(_text.Render("review.night", ("night", _phases.Day), ("name", Actor(order.Actor).Name), ("role", Actor(order.Actor).Role.Name), ("action", NightActionText.Selection(_text, Actor(order.Actor).Role.Ability, order.Target.HasValue ? Actor(order.Target.Value).Name : null))));
            }
            Move(GamePhase.NightResolution);
            NightResolution resolution = _night.Resolve(actors, orders);
            foreach (CharacterId id in resolution.SpentCharges) Actor(id).Charges--;
            foreach (CharacterId id in resolution.Deaths) Kill(Actor(id), false);
            foreach (NightReport report in resolution.Reports)
            {
                if (!Actor(report.Recipient).Alive) continue;
                var delivered = new NightReport(report.Recipient, report.Kind, report.Target, report.Alignment, _phases.Day, report.Subject);
                _results[report.Recipient].Add(delivered);
                _beliefs[report.Recipient].Receive(delivered);
                if (report.Recipient == Player.Id) DeliverReport(delivered);
            }
            if (resolution.Deaths.Count == 0) Emit(GameCue.DeathReported, _text.Render("death.none"));
            if (!FinishIfTerminal()) Move(GamePhase.Dawn);
        }

        private void DeliverReport(NightReport report)
        {
            GameCue cue;
            switch (report.Kind)
            {
                case ReportKind.Investigation: cue = GameCue.InvestigationResultReceived; break;
                case ReportKind.Blocked: cue = GameCue.AbilityBlocked; break;
                case ReportKind.Targeted: cue = GameCue.PlayerTargetedAtNight; break;
                case ReportKind.ProtectionApplied: cue = GameCue.ProtectionApplied; break;
                default: cue = GameCue.PhaseChanged; break;
            }
            Emit(cue, _text.Render("report.private", ("report", DescribeReport(report))));
        }

        private void Kill(Participant actor, bool execution)
        {
            actor.Alive = false;
            if (execution && actor.Role.Category == RoleCategory.NeutralEvil) _executionObjectives.Add(actor.Id);
            foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveDeath(actor.Id, _phases.Day);
            Emit(execution ? GameCue.ExecutionCarriedOut : GameCue.DeathReported, execution ? _text.Render("death.executed", ("name", actor.Name)) : _text.Render("death.night", ("name", actor.Name)), target: actor.Id);
            if (_options.RevealOnDeath)
            {
                _revealed[actor.Id] = actor.Role;
                foreach (Participant observer in _cast) _beliefs[observer.Id].Reveal(actor.Id, actor.Role, _phases.Day);
                Emit(GameCue.RoleRevealed, _text.Render("death.reveal", ("name", actor.Name), ("role", actor.Role.Name), ("faction", _text.Render("faction." + actor.Role.Faction))));
            }
            if (actor.Id == Player.Id) Emit(GameCue.PhaseChanged, _text.Render("death.spectating"));
        }

        private bool FinishIfTerminal()
        {
            _outcome = _winCondition is CapabilityWinCondition capability
                ? capability.Evaluate(LivingNightActors().AsReadOnly(), _phases.Day, _options.MaximumDays)
                : _winCondition.Evaluate(Living().Select(c => c.Role.Faction).ToList().AsReadOnly(), _phases.Day, _options.MaximumDays);
            if (_outcome == OutcomeKind.Ongoing) return false;
            Move(GamePhase.Ended);
            GameCue cue = _outcome == OutcomeKind.Draw ? GameCue.RunDrawn : PlayerWon() ? GameCue.VictoryAchieved : GameCue.DefeatSuffered;
            string result = _outcome == OutcomeKind.TownVictory ? _text.Render("outcome.TownVictory") : _outcome == OutcomeKind.AntagonistVictory
                ? _text.Render("outcome.AntagonistVictory") : _outcome == OutcomeKind.LoneKillerVictory ? _text.Render("outcome.LoneKillerVictory") : _text.Render("outcome.Draw");
            Emit(cue, _text.Render("run.ended", ("result", result), ("objective", _text.Render(PlayerWon() ? "objective.achieved" : "objective.missed"))));
            return true;
        }

        private bool PlayerWon() => _outcome != OutcomeKind.Ongoing && (Player.Role.Category == RoleCategory.NeutralEvil
            ? _executionObjectives.Contains(Player.Id) : Player.Role.Category == RoleCategory.NeutralKilling
            ? Player.Alive && _outcome == OutcomeKind.LoneKillerVictory : Player.Role.Faction == Faction.Neutral ? Player.Alive
            : Player.Role.Faction == Faction.Town ? _outcome == OutcomeKind.TownVictory : _outcome == OutcomeKind.AntagonistVictory);

        private KnowledgeView Knowledge(Participant observer)
        {
            var characters = _cast.Select(c => new KnownCharacter(c.Id, c.Name, c.Alive, _beliefs[observer.Id].Suspicion(c.Id),
                observer.Role.Faction == Faction.Antagonist && c.Role.Faction == Faction.Antagonist && c.Id != observer.Id,
                _revealed.TryGetValue(c.Id, out RoleSpec revealed) ? revealed.Id : (RoleId?)null,
                _revealed.TryGetValue(c.Id, out RoleSpec faction) ? faction.Faction : (Faction?)null, _beliefs[observer.Id].Claim(c.Id),
                _beliefs[observer.Id].Trust(c.Id), _beliefs[observer.Id].Factions(c.Id), _beliefs[observer.Id].Roles(c.Id), _beliefs[observer.Id].KnownFaction(c.Id)));
            return new KnowledgeView(observer.Id, observer.Role, observer.Personality, observer.Charges, _phases.Day,
                characters, _results[observer.Id], _beliefs[observer.Id].Evidence, _beliefs[observer.Id].Exposure, _catalog, _lastTeamPlan);
        }

        private List<NightActor> LivingNightActors() => Living().Select(c => new NightActor(c.Id, c.Role, c.Charges)).ToList();

        private void Move(GamePhase phase)
        {
            _phases.MoveTo(phase);
            if (phase == GamePhase.Execution || phase == GamePhase.NightResolution || phase == GamePhase.Ended) return;
            GameCue cue = phase == GamePhase.NightSelection ? GameCue.NightFalls : phase == GamePhase.Dawn ? GameCue.DawnBreaks
                : phase == GamePhase.Defense ? GameCue.TrialStarted : GameCue.PhaseChanged;
            string text;
            switch (phase)
            {
                case GamePhase.Defense: text = _text.Render("phase.trial", ("trial", _trials), ("maximum", _options.TrialsPerDay), ("name", Actor(_defendant.Value).Name)); break;
                case GamePhase.Discussion: text = _text.Render("phase.discussion"); break;
                case GamePhase.Nomination: text = _text.Render("phase.nomination"); break;
                case GamePhase.Voting: text = _revoting ? _text.Render("phase.revote") : _text.Render("phase.voting"); break;
                case GamePhase.VoteReveal: return;
                case GamePhase.Judgement: text = _text.Render("phase.judgement"); break;
                case GamePhase.LastWords: text = _text.Render("phase.lastWords"); break;
                case GamePhase.NightSelection: text = _text.Render("phase.night", ("day", _phases.Day)); break;
                case GamePhase.Dawn: text = _text.Render("phase.dawn", ("day", _phases.Day)); break;
                default: text = _text.Render("label.phase." + phase); break;
            }
            Emit(cue, text, target: phase == GamePhase.Defense ? _defendant : null);
        }

        private void Emit(GameCue cue, string text, CharacterId? speaker = null, DialogueMetadata metadata = null,
            CharacterId? target = null, SpeechIntent? intent = null, RoleId? claimedRole = null, string spokenText = null)
        {
            var observation = new GameObservation(_transcript.Count, _phases.Day, cue, text, speaker, metadata, target, intent, claimedRole, Math.Min(_options.DiscussionRounds, _schedule?.Round ?? _discussionRounds + 1), spokenText);
            _transcript.Add(observation); Observation?.Invoke(observation);
        }
    }
}
