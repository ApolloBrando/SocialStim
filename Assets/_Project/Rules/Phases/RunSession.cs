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
        private readonly List<Participant> _cast;
        private readonly IReadOnlyList<RoleSpec> _catalog;
        private readonly PhaseMachine _phases = new PhaseMachine();
        private readonly NightResolver _night = new NightResolver();
        private readonly INpcStrategy _strategy;
        private readonly IWinCondition _winCondition;
        private readonly IRandomSource _decisions;
        private readonly DialogueSelector _dialogue;
        private readonly GameText _text;
        private int _lastNight;
        private readonly Dictionary<(CharacterId actor, int night), NightActionClaim> _givenReports = new Dictionary<(CharacterId, int), NightActionClaim>();
        private IReadOnlyList<NightDebugRecord> _nightDiagnostics = Array.Empty<NightDebugRecord>();
        private readonly Dictionary<CharacterId, BeliefState> _beliefs = new Dictionary<CharacterId, BeliefState>();
        private readonly Dictionary<CharacterId, List<NightReport>> _results = new Dictionary<CharacterId, List<NightReport>>();
        private readonly Dictionary<CharacterId, RoleSpec> _revealed = new Dictionary<CharacterId, RoleSpec>();
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
        private RoleId? _counterClaimRole, _activeClaimRole;
        private CharacterId? _claimSpeaker, _followUpTo;
        private bool _resolvingClaim;
        private int _reactionBudget = 2;
        private readonly HashSet<CharacterId> _reacted = new HashSet<CharacterId>();
        private readonly Dictionary<CharacterId, NightOrder> _lastActions = new Dictionary<CharacterId, NightOrder>();
        private readonly HashSet<CharacterId> _counterClaimed = new HashSet<CharacterId>();
        private readonly Dictionary<CharacterId, int> _guiltDue = new Dictionary<CharacterId, int>();
        private readonly HashSet<CharacterId> _guiltyVoters = new HashSet<CharacterId>();
        private readonly List<CharacterId> _revengeTargets = new List<CharacterId>();
        private readonly HashSet<CharacterId> _executionObjectives = new HashSet<CharacterId>();

        internal RunSession(RunOptions options, List<Participant> cast, IReadOnlyList<RoleSpec> catalog,
            IEnumerable<DialogueLine> dialogue, INpcStrategy strategy, IWinCondition winCondition, ReasoningSettings reasoning, GameText text = null)
        {
            _schedule = options.VotingMode == VotingMode.Gnosia
                ? new DiscussionSchedule(options.DiscussionRounds, options.PlayerWindowsPerRound) : null;
            _discussion = _schedule == null ? null : new DiscussionExchange(_schedule, new CharacterId(0));
            _options = options; _cast = cast; _catalog = catalog; _strategy = strategy; _winCondition = winCondition;
            _text = text ?? GameText.Default;
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
            Emit(GameCue.PhaseChanged, $"You are {Player.Name}, the {Player.Role.Name} ({Player.Role.Faction}).");
            Emit(GameCue.PhaseChanged, $"Starting cast: {setup.Town} Town · {setup.Antagonist} Antagonist · {setup.Neutral} Neutral.");
        }

        /// <summary>Only public/player-private observations are ever emitted on this event.</summary>
        public event Action<GameObservation> Observation;
        /// <summary>Privileged read-only diagnostics for developer tools; intentionally reveals secret night actions.</summary>
        public IReadOnlyList<NightDebugRecord> GetNightDiagnostics() => _nightDiagnostics;

        /// <summary>Privileged detached NPC opinions for developer tools; never routed through player or NPC knowledge.</summary>
        public IReadOnlyList<SocialDebugRecord> GetSocialDiagnostics() => _cast.Where(c => c.Id != Player.Id)
            .SelectMany(observer => _cast.Where(c => c.Id != observer.Id).Select(subject =>
                new SocialDebugRecord(observer.Id, observer.Name, observer.Alive, subject.Id, subject.Name, subject.Alive,
                    _beliefs[observer.Id].Suspicion(subject.Id), _beliefs[observer.Id].Trust(subject.Id), _beliefs[observer.Id].Factions(subject.Id),
                    _beliefs[observer.Id].SuspicionValue(subject.Id), _beliefs[observer.Id].TrustValue(subject.Id))))
            .ToList().AsReadOnly();

        /// <summary>Returns a detached read-only player snapshot; no arbitrary-observer query exists.</summary>
        public PlayerView GetPlayerView()
        {
            KnowledgeView knowledge = Knowledge(Player);
            var actors = LivingNightActors();
            var legal = Player.Alive ? actors.Where(a => NightLegality.IsLegal(actors.First(p => p.Id == Player.Id),
                new NightOrder(Player.Id, a.Id), actors)).Select(a => a.Id).ToList() : new List<CharacterId>();
            return new PlayerView(_revision, _options.Seed, _phases.Day, _phases.Phase, _trials, _options.TrialsPerDay,
                Player.Alive, Player.Role, Player.Charges, _defendant, _outcome, PlayerWon(),
                knowledge.Characters, legal.AsReadOnly(), _transcript.ToList().AsReadOnly(), _opening, CanGiveReport(Player), _options.DiscussionRounds,
                _options.VotingMode, _schedule?.Round ?? _discussionRounds + 1, 1,
                1, _discussion?.RespondTo,
                _voteCandidates.Where(id => id != Player.Id).ToList().AsReadOnly(), _options.AllowAbstain, _revoting,
                _counterClaimRole, _followUpTo, OwnLastNight(Player), _revengeTargets.ToList().AsReadOnly(),
                !Player.Alive && _phases.Phase != GamePhase.JesterRevenge
                    ? _cast.ToDictionary(c => c.Id, c => c.Role.Id) : null);
        }

        /// <summary>Applies one legal player command; invalid input leaves state and RNG untouched.</summary>
        public bool TrySubmit(GameCommand command, out string error)
        {
            if (_submitting) { error = "A turn is already resolving."; return false; }
            error = ValidatePlayerCommand(command);
            if (error != null) return false;
            _submitting = true;
            try { ApplyCommand(command); return true; }
            finally { _submitting = false; }
        }

        private void ApplyCommand(GameCommand command)
        {
            if (_phases.Phase == GamePhase.JesterRevenge)
            { ResolveRevenge(command.Target.Value); _revision++; return; }
            if (_counterClaimRole.HasValue)
            {
                _counterClaimRole = null; _followUpTo = null;
                if (_phases.Phase == GamePhase.Discussion) { _discussion.PlayerActed(); _reactionBudget = 2; _reacted.Clear(); }
                if (command.Intent == SpeechIntent.CounterClaim) Say(Player, command);
                _activeClaimRole = null;
                if (_phases.Phase == GamePhase.Defense) Move(GamePhase.Judgement);
                else if (_discussion != null) BeginNextExchange(command);
                _revision++; return;
            }
            _followUpTo = null;
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
                        if (command.Kind == CommandKind.ReportLastAction) SayActionReport(Player, command.ReportedFaction);
                        else Say(Player, command);
                    }
                    foreach (Participant actor in Living().Where(c => c.Id != Player.Id).ToList()) SayNpc(actor);
                    if (++_discussionRounds >= _options.DiscussionRounds) Move(GamePhase.Nomination);
                    break;
                case GamePhase.Voting: CastDailyVote(command); break;
                case GamePhase.VoteReveal: CompleteVoteReveal(); break;
                case GamePhase.Nomination: Nominate(command); break;
                case GamePhase.Defense:
                    if (_defendant == Player.Id) { Say(Player, command); FollowUps(Player, command); }
                    if (!_counterClaimRole.HasValue) Move(GamePhase.Judgement); break;
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
            if (command == null || _phases.Phase == GamePhase.Ended) return "No active input request.";
            if (_phases.Phase == GamePhase.JesterRevenge)
                return command.Kind == CommandKind.Revenge && command.Target.HasValue && _revengeTargets.Contains(command.Target.Value)
                    ? null : "Choose a living Town participant who voted guilty.";
            if (!Player.Alive) return command.Kind == CommandKind.Continue ? null : "Dead participants may only spectate.";
            if (_counterClaimRole.HasValue)
                return command.Kind == CommandKind.Speak && (command.Intent == SpeechIntent.StaySilent
                    || command.Intent == SpeechIntent.CounterClaim && SpeechLegal(Player, command))
                    ? null : "Counterclaim the role just claimed or wait.";
            switch (_phases.Phase)
            {
                case GamePhase.Discussion:
                    if (command.Kind == CommandKind.ReportLastAction)
                        return CanGiveReport(Player) && (!command.ReportedFaction.HasValue || Player.Role.CanBluff
                            && Enum.IsDefined(typeof(Faction), command.ReportedFaction.Value)
                            && _catalog.Any(r => r.Id == _beliefs[Player.Id].Claim(Player.Id) && r.Ability == AbilityKind.Investigate))
                            ? null : "Claim a role and report the previous night once.";
                    if (_discussion?.RespondTo != null)
                        return SpeechLegal(Player, command) && (command.Intent == SpeechIntent.Defend || command.Intent == SpeechIntent.Accuse
                            || command.Intent == SpeechIntent.Deflect || command.Intent == SpeechIntent.Agree || command.Intent == SpeechIntent.Disagree
                            || command.Intent == SpeechIntent.ClaimRole || command.Intent == SpeechIntent.StaySilent)
                            ? null : "Respond to the public accusation first.";
                    if (command.Kind == CommandKind.Continue && _opening) return null;
                    return SpeechLegal(Player, command) ? null : "Invalid discussion statement.";
                case GamePhase.Voting:
                    return command.Kind == CommandKind.Vote && ((!command.Target.HasValue && _options.AllowAbstain)
                        || command.Target.HasValue && command.Target.Value != Player.Id && _voteCandidates.Contains(command.Target.Value))
                        ? null : "Choose a valid ballot target.";
                case GamePhase.VoteReveal:
                    return command.Kind == CommandKind.Continue ? null : "Acknowledge the public ballot result.";
                case GamePhase.Nomination:
                    return command.Kind == CommandKind.Nominate && (!command.Target.HasValue || Living().Any(c => c.Id == command.Target && c.Id != Player.Id)) ? null : "Invalid nominee.";
                case GamePhase.Defense:
                    return _defendant == Player.Id ? (SpeechLegal(Player, command) && (command.Intent == SpeechIntent.Defend || command.Intent == SpeechIntent.ClaimRole) ? null : "Choose a defense or role claim.")
                        : command.Kind == CommandKind.Continue ? null : "Acknowledge the defense.";
                case GamePhase.Judgement:
                    return _defendant == Player.Id ? (command.Kind == CommandKind.Continue ? null : "Defendants cannot judge themselves.")
                        : command.Kind == CommandKind.Judge && Enum.IsDefined(typeof(Verdict), command.Verdict) ? null : "Choose a verdict.";
                case GamePhase.LastWords:
                    return _defendant == Player.Id ? command.Kind == CommandKind.Speak && command.Intent == SpeechIntent.LastWords
                        && !command.Target.HasValue && !command.ClaimedRole.HasValue && !command.ReportedFaction.HasValue ? null : "Choose last words."
                        : command.Kind == CommandKind.Continue ? null : "Acknowledge last words.";
                case GamePhase.NightSelection:
                    var actors = LivingNightActors();
                    return command.Kind == CommandKind.ChooseNightTarget && NightLegality.IsLegal(actors.First(a => a.Id == Player.Id), new NightOrder(Player.Id, command.Target), actors) ? null : "Invalid night target or exhausted ability.";
                case GamePhase.Dawn: return command.Kind == CommandKind.Continue ? null : "Acknowledge dawn.";
                default: return "This phase does not accept participant input.";
            }
        }

        private bool SpeechLegal(Participant actor, GameCommand command)
        {
            if (command.Kind != CommandKind.Speak || !Enum.IsDefined(typeof(SpeechIntent), command.Intent)) return false;
            if (command.ClaimedRole.HasValue != (command.Intent == SpeechIntent.ClaimRole || command.Intent == SpeechIntent.CounterClaim)
                || command.ReportedFaction.HasValue != (command.Intent == SpeechIntent.ReportInvestigation)) return false;
            bool needsTarget = SpeechContract.RequiresTarget(command.Intent);
            if (needsTarget != command.Target.HasValue) return false;
            if (needsTarget && (!command.Target.HasValue || !Living().Any(c => c.Id == command.Target && c.Id != actor.Id))) return false;
            if (command.Intent == SpeechIntent.ClaimRole || command.Intent == SpeechIntent.CounterClaim)
            {
                RoleSpec role = _catalog.FirstOrDefault(r => r.Id == command.ClaimedRole);
                if (!actor.Role.CanClaim(role)) return false;
                if (command.Intent == SpeechIntent.CounterClaim)
                { if (_counterClaimed.Contains(actor.Id) || _activeClaimRole != role.Id || _claimSpeaker == actor.Id) return false; }
                else if (_beliefs[actor.Id].Claim(actor.Id).HasValue
                    || Living().Any(c => c.Id != actor.Id && _beliefs[actor.Id].Claim(c.Id) == role.Id)) return false;
            }
            if (command.Intent == SpeechIntent.ReportInvestigation && (_phases.Day <= 1 || !command.ReportedFaction.HasValue
                || !Enum.IsDefined(typeof(Faction), command.ReportedFaction.Value))) return false;
            if (command.Intent == SpeechIntent.ReportInvestigation && (actor.Role.Faction == Faction.Town
                ? !_results[actor.Id].Any(r => r.Kind == ReportKind.Investigation && r.Target == command.Target
                    && r.Alignment == command.ReportedFaction && r.Night == _phases.Day - 1)
                : !_catalog.Any(r => r.Id == _beliefs[actor.Id].Claim(actor.Id) && r.Ability == AbilityKind.Investigate))) return false;
            if (command.Intent == SpeechIntent.Agree || command.Intent == SpeechIntent.Disagree)
                if (!_beliefs[actor.Id].Evidence.Any(e => e.Actor == command.Target
                    && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Deflect
                        || e.Intent == SpeechIntent.Defend || e.Intent == SpeechIntent.Vouch))) return false;
            if (command.Intent == SpeechIntent.ReactToDeath && !_beliefs[actor.Id].Evidence.Any(e => e.Kind == EvidenceKind.Death)) return false;
            if (command.Intent == SpeechIntent.ReactToRoleReveal && !_beliefs[actor.Id].Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal)) return false;
            return command.Intent != SpeechIntent.LastWords && command.Intent != SpeechIntent.ReportAction;
        }

        private GameCommand SayNpc(Participant actor)
        {
            GameCommand speech = _strategy.Speak(Knowledge(actor), _decisions);
            if (speech?.Kind == CommandKind.ReportLastAction && CanGiveReport(actor))
            { var report = BuildPublicReport(actor, speech.ReportedFaction); PublishReport(actor, report); return GameCommand.PublishReport(report); }
            if (speech == null || !SpeechLegal(actor, speech)) speech = GameCommand.Speak(SpeechIntent.StaySilent);
            Say(actor, speech);
            FollowUps(actor, speech);
            return speech;
        }

        private void Say(Participant actor, GameCommand speech, string actionReport = null)
        {
            if (speech.Intent == SpeechIntent.StaySilent) return;
            if (speech.Intent == SpeechIntent.CounterClaim) _counterClaimed.Add(actor.Id);
            string claimed = speech.ClaimedRole.HasValue ? _catalog.First(r => r.Id == speech.ClaimedRole.Value).Name : "";
            var context = new DialogueContext(actor.Id, actor.Name, speech.Target.HasValue ? Actor(speech.Target.Value).Name : "",
                claimed, speech.ReportedFaction?.ToString() ?? actionReport ?? "", _phases.Day, actor.Role.Id, actor.Personality,
                _phases.Phase, speech.Target, _beliefs[actor.Id].Evidence, _beliefs[actor.Id].Exposure);
            DialogueSelection line = _dialogue.Select(speech.Intent, context);
            foreach (Participant observer in _cast) _beliefs[observer.Id].Observe(actor.Id, speech, _phases.Day);
            Emit(GameCue.SpeechDelivered, $"{actor.Name}: {line.Text}", actor.Id, line.Metadata, speech.Target, speech.Intent, speech.ClaimedRole);
            if (speech.Intent == SpeechIntent.ClaimRole) CounterClaims(actor, speech.ClaimedRole.Value);
        }

        private void CounterClaims(Participant speaker, RoleId roleId)
        {
            if (_resolvingClaim) return;
            _resolvingClaim = true; _activeClaimRole = roleId; _claimSpeaker = speaker.Id;
            RoleSpec role = _catalog.First(r => r.Id == roleId);
            bool bluffUsed = false;
            // Each respondent decides from their own role and public claims. No target's secret role is read.
            foreach (Participant actor in Living().Where(c => c.Id != speaker.Id && c.Id != Player.Id && !_counterClaimed.Contains(c.Id)))
            {
                KnowledgeView knowledge = Knowledge(actor);
                bool truthful = actor.Role.Id == roleId && actor.Role.CanClaimTruthfully;
                bool teammateClaim = knowledge.Characters.Any(c => c.IsTeammate && c.ClaimedRole == roleId);
                bool bluff = actor.Role.CanBluff && !bluffUsed && !teammateClaim
                    && !knowledge.Characters.First(c => c.Id == actor.Id).ClaimedRole.HasValue && _decisions.Next(100) < 25;
                if (!actor.Role.CanClaim(role) || !truthful && !bluff) continue;
                Say(actor, GameCommand.Speak(SpeechIntent.CounterClaim, role: roleId));
                if (!truthful) bluffUsed = true;
            }
            if (speaker.Id != Player.Id && Player.Alive && !_counterClaimed.Contains(Player.Id) && Player.Role.CanClaim(role)
                && (_phases.Phase == GamePhase.Defense || _phases.Phase == GamePhase.Discussion && _discussion != null && !_schedule.Complete)
                && (!Player.Role.CanBluff || !Knowledge(Player).Characters.Any(c => c.IsTeammate && c.ClaimedRole == roleId)))
                _counterClaimRole = roleId;
            if (!_counterClaimRole.HasValue) _activeClaimRole = null;
            _resolvingClaim = false;
        }

        private void FollowUps(Participant speaker, GameCommand speech)
        {
            bool accusation = speech.Intent == SpeechIntent.Accuse || speech.Intent == SpeechIntent.Deflect;
            if (!accusation && speech.Intent != SpeechIntent.Defend && speech.Intent != SpeechIntent.Vouch) return;
            CharacterId subject = speech.Target ?? speaker.Id;
            _followUpTo = speaker.Id == Player.Id ? (CharacterId?)null : speaker.Id;
            // A bounded, seeded group joins before the addressed participant receives the floor.
            int limit = Math.Min(_reactionBudget, _decisions.Next(3)), spoken = 0;
            var listeners = Living().Where(c => c.Id != Player.Id && !_reacted.Contains(c.Id)).OrderBy(c => c.Id.Value).ToList();
            for (int i = listeners.Count - 1; i > 0; i--)
            { int j = _decisions.Next(i + 1); Participant swap = listeners[i]; listeners[i] = listeners[j]; listeners[j] = swap; }
            foreach (Participant listener in listeners)
            {
                if (spoken >= limit) break;
                GameCommand reaction = SocialNpcStrategy.Reaction(Knowledge(listener), speaker.Id, subject, accusation, _decisions);
                if (reaction == null) continue;
                Say(listener, reaction); spoken++; _reactionBudget--; _reacted.Add(listener.Id);
            }
        }

        private void SayActionReport(Participant actor, Faction? chosenResult = null)
        {
            PublishReport(actor, BuildPublicReport(actor, chosenResult));
        }
        private bool CanGiveReport(Participant actor) => actor.Alive && _lastNight > 0 && _lastActions.ContainsKey(actor.Id)
            && _beliefs[actor.Id].Claim(actor.Id).HasValue && !_givenReports.ContainsKey((actor.Id, _lastNight));

        private NightActionClaim OwnLastNight(Participant actor) => actor.Alive && _lastNight > 0 && _lastActions.TryGetValue(actor.Id, out NightOrder order)
            ? new NightActionClaim(_lastNight, actor.Role.Id, actor.Role.Ability, order.Target,
                _results[actor.Id].Where(r => r.Night == _lastNight).Select(r => new ReportedNightResult(r.Kind, r.Target, r.Alignment, r.Subject))) : null;

        private NightActionClaim BuildPublicReport(Participant actor, Faction? chosenResult = null)
        {
            NightActionClaim own = OwnLastNight(actor);
            RoleSpec asserted = _catalog.First(r => r.Id == _beliefs[actor.Id].Claim(actor.Id).Value);
            if (asserted.Id == actor.Role.Id) return own;
            // A cover story can use the liar's own selected target, not another participant's hidden orders/results.
            var results = new List<ReportedNightResult>();
            if (own.Blocked) results.Add(new ReportedNightResult(ReportKind.Blocked));
            else if (own.Target.HasValue)
            {
                if (asserted.Ability == AbilityKind.Investigate)
                    results.Add(new ReportedNightResult(ReportKind.Investigation, own.Target, chosenResult ?? Faction.Town));
                if (asserted.Ability == AbilityKind.Watch)
                    results.Add(new ReportedNightResult(ReportKind.NoVisitors, own.Target, subject: own.Target));
                if (asserted.Ability == AbilityKind.Protect)
                    results.Add(new ReportedNightResult(ReportKind.ProtectionApplied, own.Target));
            }
            return new NightActionClaim(_lastNight, asserted.Id, asserted.Ability, own.Target, results);
        }

        private void PublishReport(Participant actor, NightActionClaim report)
        {
            _givenReports[(actor.Id, report.Night)] = report;
            var command = GameCommand.PublishReport(report);
            string prose = NightReportFormatter.Format(report, _catalog.First(r => r.Id == report.Role).Name, id => Actor(id).Name, _text);
            foreach (Participant observer in _cast) _beliefs[observer.Id].Observe(actor.Id, command, _phases.Day);
            Emit(GameCue.SpeechDelivered, actor.Name + ": " + prose, actor.Id, target: report.Target, intent: SpeechIntent.ReportAction, nightReport: report);
        }

        private string DescribeReport(NightReport report)
        {
            switch (report.Kind)
            {
                case ReportKind.Investigation: return Actor(report.Target.Value).Name + " appeared " + report.Alignment + ".";
                case ReportKind.Blocked: return "Someone blocked me. I don\'t know who.";
                case ReportKind.AttackFailed: return "My attack failed.";
                case ReportKind.ProtectionApplied: return "My protection took effect; I can\'t tell if anyone attacked.";
                case ReportKind.VisitorSeen: return Actor(report.Target.Value).Name + " visited " + Actor(report.Subject.Value).Name + ".";
                case ReportKind.NoVisitors: return "I saw no other visitors to " + Actor(report.Target.Value).Name + ".";
                case ReportKind.ForgeryApplied: return "I made the target look like an Antagonist for the night.";
                default: return "I survived an attack. I don\'t know who it was.";
            }
        }

        private void MorningReports()
        {
            foreach (Participant actor in Living().Where(c => c.Id != Player.Id).ToList())
            {
                if (CanGiveReport(actor)) PublishReport(actor, BuildPublicReport(actor));
            }
        }

        private void Discuss(GameCommand command)
        {
            if (_opening && command.Kind == CommandKind.Continue && Player.Alive && !_discussion.RespondTo.HasValue)
            {
                _opening = false;
                var speakers = Living().Where(c => c.Id != Player.Id).ToList();
                if (speakers.Count > 0)
                {
                    int count = 1 + _decisions.Next(Math.Min(3, speakers.Count));
                    for (int i = 0; i < count; i++)
                    {
                        int index = _decisions.Next(speakers.Count);
                        Participant speaker = speakers[index]; speakers.RemoveAt(index);
                        _discussion.ObserveOpening(speaker.Id, SayNpc(speaker));
                        if (_discussion.RespondTo.HasValue || _counterClaimRole.HasValue) break;
                    }
                }
                return;
            }
            _opening = false;
            _discussion.PlayerActed(); _reactionBudget = 2; _reacted.Clear();
            if (Player.Alive)
            {
                if (command.Kind == CommandKind.ReportLastAction) SayActionReport(Player, command.ReportedFaction);
                else { Say(Player, command); FollowUps(Player, command); }
            }
            BeginNextExchange(command);
        }

        private void BeginNextExchange(GameCommand command)
        {
            if (_schedule.Complete)
            {
                if ((command.Intent == SpeechIntent.Accuse || command.Intent == SpeechIntent.Deflect) && command.Target.HasValue)
                    DefendNpc(command.Target.Value);
                StartDiscussionVote(); return;
            }
            var queued = Living().Where(c => c.Id != Player.Id).OrderBy(c => c.Id.Value).ToList();
            // Shuffle a bounded subset; not every NPC speaks in every window.
            for (int i = queued.Count - 1; i > 0; i--)
            { int j = _decisions.Next(i + 1); Participant swap = queued[i]; queued[i] = queued[j]; queued[j] = swap; }
            int queuedCount = queued.Count == 0 ? 0 : 1 + _decisions.Next(queued.Count);
            _discussion.Begin(queued.Take(queuedCount).Select(c => c.Id), command,
                DefendNpc);
            ContinueExchange();
        }

        private void ContinueExchange()
        {
            if (_counterClaimRole.HasValue) return;
            _discussion.Resume(id => SayNpc(Actor(id)), DefendNpc, Player.Alive, () => _counterClaimRole.HasValue);
            if (_counterClaimRole.HasValue) return;
        }

        private void StartDiscussionVote()
        {
            _followUpTo = null;
            _voteCandidates.Clear(); _voteCandidates.AddRange(Living().Select(c => c.Id));
            Move(GamePhase.Voting);
        }

        private void DefendNpc(CharacterId id)
        {
            Participant actor = Actor(id);
            GameCommand choice = _strategy.Speak(Knowledge(actor), _decisions);
            var speech = choice != null && choice.Intent == SpeechIntent.ClaimRole && SpeechLegal(actor, choice)
                ? choice : GameCommand.Speak(SpeechIntent.Defend);
            Say(actor, speech); FollowUps(actor, speech);
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
                            .ThenByDescending(c => SocialAssessment.Threat(knowledge, c)).ThenBy(c => _decisions.Next(1000)).FirstOrDefault();
                        vote = ranked?.Id;
                    }
                }
                ballots.Add(new KeyValuePair<CharacterId, CharacterId?>(actor.Id, vote));
            }
            // All ballots are chosen before any are published; there is no later-voter information advantage.
            foreach (var ballot in ballots)
            {
                foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveNomination(ballot.Key, ballot.Value, _phases.Day);
                Emit(GameCue.BallotRevealed, Actor(ballot.Key).Name + " votes "
                    + (ballot.Value.HasValue ? Actor(ballot.Value.Value).Name : "to abstain") + ".", ballot.Key, target: ballot.Value);
            }
            _ballot = VoteResolver.Plurality(ballots.Select(b => b.Value));
            string tally = string.Join(" · ", _ballot.Counts.OrderBy(pair => pair.Key.Value).Select(pair => Actor(pair.Key).Name + ": " + pair.Value));
            Emit(GameCue.VoteTallied, (tally.Length > 0 ? tally : "No votes cast") + " · Abstentions: " + _ballot.Abstentions + ".");
            _defendant = _ballot.Winner;
            if (!_defendant.HasValue) Emit(GameCue.VoteTied, _ballot.Tied.Count > 0 ? "The vote is tied." : "Everyone abstained.");
            Move(GamePhase.VoteReveal);
        }

        private void CompleteVoteReveal()
        {
            if (!_defendant.HasValue)
            {
                if (!_revoting && _options.RevoteTies && _ballot.Tied.Count > 1)
                {
                    _revoting = true; _voteCandidates.Clear(); _voteCandidates.AddRange(_ballot.Tied);
                    Emit(GameCue.RevoteStarted, "Revote between " + string.Join(", ", _voteCandidates.Select(id => Actor(id).Name)) + ".");
                    Move(GamePhase.Voting);
                }
                else StartNight();
                return;
            }
            _trials++;
            Move(GamePhase.Defense);
            if (_defendant != Player.Id) DefendNpc(_defendant.Value);
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
                Emit(GameCue.NominationMade, $"{Actor(ballot.Key).Name} nominates {(ballot.Value.HasValue ? Actor(ballot.Value.Value).Name : "nobody")}.", ballot.Key, target: ballot.Value);
            }
            _defendant = VoteResolver.Nominee(votes, Living().Count());
            if (!_defendant.HasValue)
            { Emit(GameCue.VoteTied, "No majority. No trial today."); StartNight(); return; }
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
                Emit(GameCue.BallotRevealed, $"{Actor(ballot.Key).Name}: {ballot.Value}.", ballot.Key, target: _defendant);
            }
            _guiltyVoters.Clear();
            foreach (var ballot in ballots.Where(b => b.Value == Verdict.Guilty)) _guiltyVoters.Add(ballot.Key);
            if (VoteResolver.IsGuilty(ballots.Select(b => b.Value)))
            {
                Emit(GameCue.VerdictGuilty, $"{Actor(_defendant.Value).Name} is found guilty.");
                if (!_options.LastWords) { Execute(); return; }
                Move(GamePhase.LastWords);
                if (_defendant != Player.Id) Say(Actor(_defendant.Value), GameCommand.Speak(SpeechIntent.LastWords));
            }
            else
            {
                Emit(GameCue.VerdictInnocent, $"{Actor(_defendant.Value).Name} is acquitted.");
                _defendant = null;
                if (_options.VotingMode == VotingMode.Trial && _trials < _options.TrialsPerDay) Move(GamePhase.Nomination); else StartNight();
            }
        }

        private void Execute()
        {
            Move(GamePhase.Execution);
            Participant executed = Actor(_defendant.Value);
            Kill(executed, true);
            var listeners = Living().Where(c => c.Id != Player.Id).ToList();
            if (listeners.Count > 0 && _decisions.Next(100) < 45)
                Say(listeners[_decisions.Next(listeners.Count)], GameCommand.Speak(
                    _options.RevealOnDeath ? SpeechIntent.ReactToRoleReveal : SpeechIntent.ReactToDeath));
            _revengeTargets.Clear();
            if (executed.Role.ExecutionRevenge)
                _revengeTargets.AddRange(Living().Where(c => c.Role.Faction == Faction.Town && _guiltyVoters.Contains(c.Id)).Select(c => c.Id));
            if (_revengeTargets.Count > 0)
            {
                if (executed.Id == Player.Id)
                { Move(GamePhase.JesterRevenge); return; }
                // Revenge grants the eligible identities; preference uses only the Jester's own recorded opinions.
                CharacterId target = _revengeTargets.OrderByDescending(id => _beliefs[executed.Id].Suspicion(id))
                    .ThenBy(id => _beliefs[executed.Id].Trust(id)).ThenBy(id => id.Value).First();
                ResolveRevenge(target); return;
            }
            if (!FinishIfTerminal()) StartNight();
        }

        private void ResolveRevenge(CharacterId target)
        {
            Kill(Actor(target), false, _text.Format("event.revenge", ("name", Actor(target).Name)));
            _revengeTargets.Clear();
            if (!FinishIfTerminal()) StartNight();
        }

        private void StartNight() { _defendant = null; Move(GamePhase.NightSelection); }

        private void ResolveNight(GameCommand command)
        {
            var actors = LivingNightActors();
            var orders = new List<NightOrder>();
            foreach (Participant actor in Living())
            {
                CharacterId? target = _guiltDue.TryGetValue(actor.Id, out int due) && due <= _phases.Day ? null
                    : actor.Id == Player.Id ? command.Target : _strategy.NightTarget(Knowledge(actor), _decisions);
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
                Emit(GameCue.PhaseChanged, $"Team: {Actor(_lastTeamPlan.Killer).Name} will {( _lastTeamPlan.Target.HasValue ? "attack " + Actor(_lastTeamPlan.Target.Value).Name : "pass tonight")}.");
            _lastActions.Clear();
            _lastNight = _phases.Day;
            foreach (NightOrder order in orders) _lastActions.Add(order.Actor, order);
            Move(GamePhase.NightResolution);
            NightResolution resolution = _night.Resolve(actors, orders);
            _nightDiagnostics = orders.OrderBy(o => o.Actor.Value).Select(order => {
                Participant actor = Actor(order.Actor);
                var report = new NightActionClaim(_lastNight, actor.Role.Id, actor.Role.Ability, order.Target,
                    resolution.Reports.Where(r => r.Recipient == actor.Id).Select(r => new ReportedNightResult(r.Kind, r.Target, r.Alignment, r.Subject)));
                return new NightDebugRecord(actor.Id, actor.Name, report,
                    resolution.Visits.FirstOrDefault(v => v.Visitor == actor.Id)?.Target, resolution.SpentCharges.Contains(actor.Id));
            }).ToList().AsReadOnly();
            foreach (CharacterId id in resolution.SpentCharges) Actor(id).Charges--;
            foreach (CharacterId id in resolution.TownKillers)
            {
                _guiltDue[id] = _phases.Day + 1;
                if (id == Player.Id && !resolution.Deaths.Contains(id))
                    Emit(GameCue.PhaseChanged, _text.Format("event.guiltQueued"));
            }
            foreach (CharacterId id in resolution.Deaths) Kill(Actor(id), false);
            foreach (Participant survivor in Living()) _beliefs[survivor.Id].ConsiderNightClaimDeaths(_phases.Day);
            foreach (NightReport report in resolution.Reports)
            {
                if (!Actor(report.Recipient).Alive) continue;
                var delivered = new NightReport(report.Recipient, report.Kind, report.Target, report.Alignment, _phases.Day, report.Subject);
                _results[report.Recipient].Add(delivered);
                _beliefs[report.Recipient].Receive(delivered);
                if (report.Recipient == Player.Id) DeliverReport(delivered);
            }
            if (resolution.Deaths.Count == 0) Emit(GameCue.DeathReported, "Nobody died during the night.");
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
            Emit(cue, "Private: " + DescribeReport(report));
        }

        private void Kill(Participant actor, bool execution, string cause = null)
        {
            actor.Alive = false;
            if (execution && actor.Role.Category == RoleCategory.NeutralEvil) _executionObjectives.Add(actor.Id);
            foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveDeath(actor.Id, _phases.Day, !execution && _phases.Phase == GamePhase.NightResolution);
            Emit(execution ? GameCue.ExecutionCarriedOut : GameCue.DeathReported, cause ?? (execution ? $"{actor.Name} was executed." : $"{actor.Name} died during the night."), target: actor.Id);
            if (_options.RevealOnDeath)
            {
                _revealed[actor.Id] = actor.Role;
                foreach (Participant observer in _cast) _beliefs[observer.Id].Reveal(actor.Id, actor.Role, _phases.Day);
                Emit(GameCue.RoleRevealed, $"Revealed: {actor.Name} was {actor.Role.Name} ({actor.Role.Faction}).", target: actor.Id);
            }
            if (actor.Id == Player.Id) Emit(GameCue.PhaseChanged, "You died. You can keep watching.");
        }

        private bool FinishIfTerminal()
        {
            _outcome = _winCondition is CapabilityWinCondition capability
                ? capability.Evaluate(LivingNightActors().AsReadOnly(), _phases.Day, _options.MaximumDays)
                : _winCondition.Evaluate(Living().Select(c => c.Role.Faction).ToList().AsReadOnly(), _phases.Day, _options.MaximumDays);
            if (_outcome == OutcomeKind.Ongoing) return false;
            Move(GamePhase.Ended);
            GameCue cue = _outcome == OutcomeKind.Draw ? GameCue.RunDrawn : PlayerWon() ? GameCue.VictoryAchieved : GameCue.DefeatSuffered;
            string result = _outcome == OutcomeKind.TownVictory ? "Town wins." : _outcome == OutcomeKind.AntagonistVictory
                ? "Antagonists win." : _outcome == OutcomeKind.LoneKillerVictory ? _text.Format("event.serialKillerWins")
                : _outcome == OutcomeKind.InfinitumVictory ? _text.Format("event.egoWins") : "Draw.";
            Emit(cue, $"Run ended: {result} Objective {(PlayerWon() ? "achieved" : "missed")}.");
            return true;
        }

        private bool PlayerWon() => _outcome != OutcomeKind.Ongoing && (Player.Role.StealsParityWin
            ? Player.Alive && _outcome == OutcomeKind.InfinitumVictory : Player.Role.Category == RoleCategory.NeutralEvil
            ? _executionObjectives.Contains(Player.Id) : Player.Role.Category == RoleCategory.NeutralKilling
            ? Player.Alive && _outcome == OutcomeKind.LoneKillerVictory : Player.Role.Faction == Faction.Neutral ? Player.Alive
            : Player.Role.Faction == Faction.Town ? _outcome == OutcomeKind.TownVictory : _outcome == OutcomeKind.AntagonistVictory);

        private KnowledgeView Knowledge(Participant observer)
        {
            var characters = _cast.Select(c => new KnownCharacter(c.Id, c.Name, c.Alive, _beliefs[observer.Id].Suspicion(c.Id),
                observer.Role.Faction == Faction.Antagonist && c.Role.Faction == Faction.Antagonist && c.Id != observer.Id,
                _revealed.TryGetValue(c.Id, out RoleSpec revealed) ? revealed.Id : (RoleId?)null,
                _revealed.TryGetValue(c.Id, out RoleSpec faction) ? faction.Faction : (Faction?)null, _beliefs[observer.Id].Claim(c.Id),
                _beliefs[observer.Id].Trust(c.Id), _beliefs[observer.Id].Factions(c.Id), _beliefs[observer.Id].Roles(c.Id), _beliefs[observer.Id].KnownFaction(c.Id),
                _beliefs[observer.Id].SuspicionValue(c.Id), _beliefs[observer.Id].TrustValue(c.Id)));
            return new KnowledgeView(observer.Id, observer.Role, observer.Personality,
                _guiltDue.TryGetValue(observer.Id, out int due) && due <= _phases.Day ? 0 : observer.Charges, _phases.Day,
                characters, _results[observer.Id], _beliefs[observer.Id].Evidence, _beliefs[observer.Id].Exposure, _catalog, _lastTeamPlan,
                _phases.Phase == GamePhase.Voting ? _voteCandidates : null, OwnLastNight(observer), CanGiveReport(observer));
        }

        /// <summary>Scoped player snapshot for portable policy evaluation; never exposes another observer's mind.</summary>
        internal KnowledgeView PlayerKnowledgeForDiagnostics() => Knowledge(Player);

        private List<NightActor> LivingNightActors() => Living().Select(c => new NightActor(c.Id, c.Role, c.Charges, _guiltDue.TryGetValue(c.Id, out int due) && due <= _phases.Day)).ToList();

        private void Move(GamePhase phase)
        {
            _phases.MoveTo(phase);
            if (phase == GamePhase.Execution || phase == GamePhase.NightResolution || phase == GamePhase.Ended) return;
            GameCue cue = phase == GamePhase.NightSelection ? GameCue.NightFalls : phase == GamePhase.Dawn ? GameCue.DawnBreaks
                : phase == GamePhase.Defense ? GameCue.TrialStarted : GameCue.PhaseChanged;
            string text;
            switch (phase)
            {
                case GamePhase.Defense: text = $"Trial {_trials}/{_options.TrialsPerDay}: {Actor(_defendant.Value).Name}."; break;
                case GamePhase.Discussion: text = "Discussion begins."; break;
                case GamePhase.Nomination: text = "Nominations open."; break;
                case GamePhase.Voting: text = _revoting ? "Revote." : "Vote."; break;
                case GamePhase.VoteReveal: return;
                case GamePhase.Judgement: text = "Time to vote."; break;
                case GamePhase.LastWords: text = "Last words."; break;
                case GamePhase.JesterRevenge: text = _text.Format("ui.revengePrompt"); break;
                case GamePhase.NightSelection: text = $"Night {_phases.Day}."; break;
                case GamePhase.Dawn: text = $"Day {_phases.Day}."; break;
                default: text = phase.ToString(); break;
            }
            Emit(cue, text, target: phase == GamePhase.Defense ? _defendant : null);
        }

        private void Emit(GameCue cue, string text, CharacterId? speaker = null, DialogueMetadata metadata = null,
            CharacterId? target = null, SpeechIntent? intent = null, RoleId? claimedRole = null, NightActionClaim nightReport = null)
        {
            var observation = new GameObservation(_transcript.Count, _phases.Day, cue, text, speaker, metadata, target, intent, claimedRole, nightReport);
            _transcript.Add(observation); Observation?.Invoke(observation);
        }
    }
}
