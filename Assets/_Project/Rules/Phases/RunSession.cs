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
    public sealed partial class RunSession
    {
        private readonly RunOptions _options;
        private readonly ReasoningSettings _reasoning;
        private bool _applyingCommand;
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
        private readonly List<GameObservation> _transcript = new List<GameObservation>();
        private int _discussionRounds;
        private int _trials;
        private int _revision;
        private CharacterId? _defendant;
        private OutcomeKind _outcome;
        private TeamNightPlan _lastTeamPlan;
        private bool _opening = true;
        private readonly Dictionary<CharacterId, NightOrder> _lastActions = new Dictionary<CharacterId, NightOrder>();
        private readonly HashSet<CharacterId> _executionObjectives = new HashSet<CharacterId>();

        internal RunSession(RunOptions options, List<Participant> cast, IReadOnlyList<RoleSpec> catalog,
            IEnumerable<DialogueLine> dialogue, INpcStrategy strategy, IWinCondition winCondition, ReasoningSettings reasoning)
        {
            _reasoning = reasoning;
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
            Emit(GameCue.PhaseChanged, $"Day 1. You are {Player.Name}, the {Player.Role.Name} ({Player.Role.Faction}). Seed: {options.Seed}.");
            Emit(GameCue.PhaseChanged, $"Public setup: {setup.Town} Town, {setup.Antagonist} Antagonist, {setup.Neutral} Neutral. These are initial counts, not surviving secret alignments.");
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
                knowledge.Characters, legal.AsReadOnly(), _transcript.ToList().AsReadOnly(), _opening, _lastActions.ContainsKey(Player.Id));
        }

        /// <summary>Applies one legal player command; invalid input leaves state and RNG untouched.</summary>
        public bool TrySubmit(GameCommand command, out string error)
        {
            if (_applyingCommand) { error = "A command is already being applied."; return false; }
            error = ValidatePlayerCommand(command);
            if (error != null) return false;
            _applyingCommand = true;
            try
            {
                switch (_phases.Phase)
                {
                    case GamePhase.Discussion:
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
                        _trials = 0; _discussionRounds = 0; _opening = true; Move(GamePhase.Discussion); MorningReports(); break;
                }
                _revision++;
                return true;
            }
            finally { _applyingCommand = false; }
        }

        private Participant Player => _cast[0];
        private IEnumerable<Participant> Living() => _cast.Where(c => c.Alive);
        private Participant Actor(CharacterId id) => _cast.First(c => c.Id == id);

        private string ValidatePlayerCommand(GameCommand command)
        {
            if (command == null || _phases.Phase == GamePhase.Ended) return "No active input request.";
            if (!Player.Alive) return command.Kind == CommandKind.Continue ? null : "Dead participants may only spectate.";
            switch (_phases.Phase)
            {
                case GamePhase.Discussion:
                    if (command.Kind == CommandKind.Continue && _opening) return null;
                    if (command.Kind == CommandKind.ReportLastAction) return _lastActions.ContainsKey(Player.Id) ? null : "There is no previous night action.";
                    return SpeechLegal(Player, command) ? null : "Invalid discussion statement.";
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
            if (command.ClaimedRole.HasValue != (command.Intent == SpeechIntent.ClaimRole)
                || command.ReportedFaction.HasValue != (command.Intent == SpeechIntent.ReportInvestigation)) return false;
            bool needsTarget = SpeechContract.RequiresTarget(command.Intent);
            if (needsTarget != command.Target.HasValue) return false;
            if (needsTarget && (!command.Target.HasValue || !Living().Any(c => c.Id == command.Target && c.Id != actor.Id))) return false;
            if (command.Intent == SpeechIntent.ClaimRole && (!command.ClaimedRole.HasValue || !_catalog.Any(r => r.Id == command.ClaimedRole.Value))) return false;
            if (command.Intent == SpeechIntent.ReportInvestigation && (_phases.Day <= 1 || !command.ReportedFaction.HasValue
                || !Enum.IsDefined(typeof(Faction), command.ReportedFaction.Value))) return false;
            if (command.Intent == SpeechIntent.ReactToDeath && !_beliefs[actor.Id].Evidence.Any(e => e.Kind == EvidenceKind.Death)) return false;
            if (command.Intent == SpeechIntent.ReactToRoleReveal && !_beliefs[actor.Id].Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal)) return false;
            return command.Intent != SpeechIntent.LastWords && command.Intent != SpeechIntent.ReportAction;
        }

        private void SayNpc(Participant actor)
        {
            GameCommand speech = _strategy.Speak(Knowledge(actor), _decisions);
            Say(actor, speech != null && SpeechLegal(actor, speech) ? speech : GameCommand.Speak(SpeechIntent.StaySilent));
        }

        private void Say(Participant actor, GameCommand speech, string actionReport = null)
        {
            string claimed = speech.ClaimedRole.HasValue ? _catalog.First(r => r.Id == speech.ClaimedRole.Value).Name : "";
            var context = new DialogueContext(actor.Id, actor.Name, speech.Target.HasValue ? Actor(speech.Target.Value).Name : "",
                claimed, speech.ReportedFaction?.ToString() ?? actionReport ?? "", _phases.Day, actor.Role.Id, actor.Personality,
                _phases.Phase, speech.Target, _beliefs[actor.Id].Evidence, _beliefs[actor.Id].Exposure);
            DialogueSelection line = _dialogue.Select(speech.Intent, context);
            foreach (Participant observer in _cast) _beliefs[observer.Id].Observe(actor.Id, speech, _phases.Day);
            Emit(GameCue.SpeechDelivered, $"{actor.Name}: {line.Text}", actor.Id, line.Metadata, speech.Target, speech.Intent, speech.ClaimedRole);
        }

        private void SayActionReport(Participant actor)
        {
            NightOrder order = _lastActions[actor.Id];
            string report = !order.Target.HasValue ? "I passed last night."
                : "I selected " + actor.Role.Ability + " on " + Actor(order.Target.Value).Name + " last night.";
            foreach (NightReport result in _results[actor.Id].Where(r => r.Night == _phases.Day - 1))
                report += " " + DescribeReport(result);
            Say(actor, GameCommand.Speak(SpeechIntent.ReportAction, order.Target), report);
        }

        private string DescribeReport(NightReport report)
        {
            switch (report.Kind)
            {
                case ReportKind.Investigation: return Actor(report.Target.Value).Name + " appeared " + report.Alignment + ".";
                case ReportKind.Blocked: return "My ability was blocked; I do not know who blocked it.";
                case ReportKind.AttackFailed: return "My attack did not kill the target.";
                case ReportKind.ProtectionApplied: return "Protection was applied, which does not prove an attack.";
                case ReportKind.VisitorSeen: return Actor(report.Target.Value).Name + " visited " + Actor(report.Subject.Value).Name + ".";
                case ReportKind.NoVisitors: return "I saw no other visitors to " + Actor(report.Target.Value).Name + ".";
                case ReportKind.ForgeryApplied: return "I changed the target's investigative appearance for that night.";
                default: return "I was attacked and survived; I do not know the attacker.";
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
                    var targets = Living().Where(c => c.Id != actor.Id).Select(c => c.Id).ToList();
                    if (asserted.Ability == AbilityKind.Investigate && targets.Count > 0)
                        Say(actor, GameCommand.Speak(SpeechIntent.ReportInvestigation, targets[_decisions.Next(targets.Count)], faction: Faction.Antagonist));
                    else
                        Say(actor, GameCommand.Speak(SpeechIntent.ReportAction),
                            asserted.Ability == AbilityKind.None ? "I had no night action."
                                : "I claim I used " + asserted.Ability + " last night.");
                }
            }
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
            { Emit(GameCue.VoteTied, "No nominee reached a strict majority. The day ends without a trial."); StartNight(); return; }
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
                Emit(GameCue.PhaseChanged, $"{Actor(ballot.Key).Name}: {ballot.Value}.");
            }
            if (VoteResolver.IsGuilty(ballots.Select(b => b.Value)))
            {
                Emit(GameCue.VerdictGuilty, $"{Actor(_defendant.Value).Name} is found guilty.");
                Move(GamePhase.LastWords);
                if (_defendant != Player.Id) Say(Actor(_defendant.Value), GameCommand.Speak(SpeechIntent.LastWords));
            }
            else
            {
                Emit(GameCue.VerdictInnocent, $"{Actor(_defendant.Value).Name} is acquitted. Guilty must exceed Innocent; abstentions are excluded.");
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
            var killers = orders.Where(o => Actor(o.Actor).Role.Faction == Faction.Antagonist && Actor(o.Actor).Role.Ability == AbilityKind.Attack)
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
                Emit(GameCue.PhaseChanged, $"PRIVATE TEAM: {Actor(_lastTeamPlan.Killer).Name} will {( _lastTeamPlan.Target.HasValue ? "attack " + Actor(_lastTeamPlan.Target.Value).Name : "pass tonight")}.");
            _lastActions.Clear();
            foreach (NightOrder order in orders) _lastActions.Add(order.Actor, order);
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
            if (resolution.Deaths.Count == 0) Emit(GameCue.DeathReported, "Nobody died during the night.");
            if (!FinishIfTerminal()) Move(GamePhase.Dawn);
        }

        private void DeliverReport(NightReport report)
        {
            switch (report.Kind)
            {
                case ReportKind.VisitorSeen:
                case ReportKind.NoVisitors:
                case ReportKind.ForgeryApplied:
                    Emit(GameCue.PhaseChanged, "PRIVATE: " + DescribeReport(report)); break;
                case ReportKind.Investigation:
                    Emit(GameCue.InvestigationResultReceived, $"PRIVATE: {Actor(report.Target.Value).Name} appeared {report.Alignment} to your investigation."); break;
                case ReportKind.Blocked: Emit(GameCue.AbilityBlocked, "PRIVATE: Your night ability was blocked. The visitor is unknown."); break;
                case ReportKind.Targeted: Emit(GameCue.PlayerTargetedAtNight, "PRIVATE: You were attacked and survived. The attacker is unknown."); break;
                case ReportKind.AttackFailed: Emit(GameCue.PhaseChanged, $"PRIVATE: Your attack on {Actor(report.Target.Value).Name} failed."); break;
                case ReportKind.ProtectionApplied: Emit(GameCue.ProtectionApplied, $"PRIVATE: You applied protection to {Actor(report.Target.Value).Name}. This does not prove an attack occurred."); break;
            }
        }

        private void Kill(Participant actor, bool execution)
        {
            actor.Alive = false;
            if (execution && actor.Role.Category == RoleCategory.NeutralEvil) _executionObjectives.Add(actor.Id);
            foreach (Participant observer in _cast) _beliefs[observer.Id].ObserveDeath(actor.Id, _phases.Day);
            Emit(execution ? GameCue.ExecutionCarriedOut : GameCue.DeathReported, execution ? $"{actor.Name} was executed." : $"{actor.Name} died during the night.", target: actor.Id);
            if (_options.RevealOnDeath)
            {
                _revealed[actor.Id] = actor.Role;
                foreach (Participant observer in _cast) _beliefs[observer.Id].Reveal(actor.Id, actor.Role, _phases.Day);
                Emit(GameCue.RoleRevealed, $"Revealed: {actor.Name} was {actor.Role.Name} ({actor.Role.Faction}).");
            }
            if (actor.Id == Player.Id) Emit(GameCue.PhaseChanged, "You are dead. Continue to spectate; you can no longer speak, vote, or target anyone.");
        }

        private bool FinishIfTerminal()
        {
            _outcome = _winCondition is CapabilityWinCondition capability
                ? capability.Evaluate(LivingNightActors().AsReadOnly(), _phases.Day, _options.MaximumDays)
                : _winCondition.Evaluate(Living().Select(c => c.Role.Faction).ToList().AsReadOnly(), _phases.Day, _options.MaximumDays);
            if (_outcome == OutcomeKind.Ongoing) return false;
            Move(GamePhase.Ended);
            GameCue cue = _outcome == OutcomeKind.Draw ? GameCue.RunDrawn : PlayerWon() ? GameCue.VictoryAchieved : GameCue.DefeatSuffered;
            Emit(cue, $"Run ended: {_outcome}. Your objective: {(PlayerWon() ? "achieved" : "not achieved")}. Survival: {(Player.Alive ? "alive" : "dead")}.");
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
                _beliefs[observer.Id].Trust(c.Id), _beliefs[observer.Id].Factions(c.Id), _beliefs[observer.Id].Roles(c.Id)));
            return new KnowledgeView(observer.Id, observer.Role, observer.Personality, observer.Charges, _phases.Day,
                characters, _results[observer.Id], _beliefs[observer.Id].Evidence, _beliefs[observer.Id].Exposure, _catalog, _lastTeamPlan);
        }

        private List<NightActor> LivingNightActors() => Living().Select(c => new NightActor(c.Id, c.Role, c.Charges)).ToList();

        private void Move(GamePhase phase)
        {
            _phases.MoveTo(phase);
            GameCue cue = phase == GamePhase.NightSelection ? GameCue.NightFalls : phase == GamePhase.Dawn ? GameCue.DawnBreaks
                : phase == GamePhase.Defense ? GameCue.TrialStarted : GameCue.PhaseChanged;
            Emit(cue, phase == GamePhase.Defense ? $"Trial {_trials}/{_options.TrialsPerDay}: {Actor(_defendant.Value).Name} may defend."
                : $"Day {_phases.Day}: {phase}.", target: phase == GamePhase.Defense ? _defendant : null);
        }

        private void Emit(GameCue cue, string text, CharacterId? speaker = null, DialogueMetadata metadata = null,
            CharacterId? target = null, SpeechIntent? intent = null, RoleId? claimedRole = null)
        {
            var observation = new GameObservation(_transcript.Count, _phases.Day, cue, text, speaker, metadata, target, intent, claimedRole);
            _transcript.Add(observation); Observation?.Invoke(observation);
        }
    }
}
