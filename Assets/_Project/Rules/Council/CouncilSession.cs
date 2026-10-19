using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Council
{
    /// <summary>Thin synchronous coordinator for council, vote and night services. Display timing never advances it.</summary>
    public sealed class CouncilSession
    {
        private readonly CouncilWorld _world;
        private readonly CouncilSpeechService _speech;
        private readonly CouncilDayDirector _day;
        private readonly CouncilVoteDirector _vote;
        private readonly CouncilNightDirector _night;
        private readonly MissionWinCondition _win = new MissionWinCondition();
        private bool _submitting;
        private int _freeTimeUsed;
        internal CouncilSession(CouncilWorld world, IEnumerable<DialogueLine> dialogue, ICouncilStrategy strategy)
        {
            _world = world; _speech = new CouncilSpeechService(world, dialogue);
            _day = new CouncilDayDirector(world, strategy, _speech); _vote = new CouncilVoteDirector(world, strategy);
            _night = new CouncilNightDirector(world, strategy); _day.Begin();
        }

        public CouncilSnapshot View()
        {
            var knowledge = _world.Knowledge(_world.Player);
            var actors = _world.Living.Select(a => a.Night()).ToList();
            var policy = new NightPolicy(true, _world.Options.SelfHeal, _world.Options.Backlash, _world.Options.NotifyLull);
            var nightTargets = !_world.Player.Alive ? new List<CharacterId>() : actors.Where(a => NightLegality.IsLegal(_world.Player.Night(),
                new NightOrder(_world.Player.Id, a.Id), actors, policy)).Select(a => a.Id).ToList();
            var votes = _world.Living.Where(a => a.Id != _world.Player.Id && (!_vote.IsRevote || _vote.Tied.Contains(a.Id))).Select(a => a.Id).ToList();
            bool ended = _world.Phase == CouncilPhase.Ended;
            return new CouncilSnapshot(_world.Revision, _world.Options.Seed, _world.Day, _world.Round, _world.Phase, knowledge,
                _world.Options.Rounds, Math.Max(0, _world.Options.PlayerWindows - _day.WindowsUsed), _day.Respond, _day.RespondTo,
                _world.Events.Where(e => e.Kind != CouncilEventKind.NightOrder && (e.Public || e.Recipient == _world.Player.Id)).Select((e, i) => e.Scoped(i)).ToList().AsReadOnly(),
                nightTargets.AsReadOnly(), votes.AsReadOnly(), _world.Eliminated, ended ? _world.Outcome : OutcomeKind.Ongoing,
                ended && _win.Won(_world.Player.Role, _world.Player.Alive, _world.Player.VotedOut, _world.Outcome),
                new ReadOnlyDictionary<CharacterId, RoleId>(ended ? _world.Actors.ToDictionary(a => a.Id, a => a.StartingRole.Id)
                    : new Dictionary<CharacterId, RoleId>()), ended ? _world.Events.ToList().AsReadOnly() : new List<CouncilEvent>().AsReadOnly());
        }

        /// <summary>Phrase preview is read-only and uses an isolated stream.</summary>
        public IReadOnlyList<DialogueSelection> Phrases(CouncilAction action)
        {
            string error = Validate(action);
            if (error != null) return new List<DialogueSelection>().AsReadOnly();
            return _speech.Preview(_world.Player, action);
        }

        public bool TrySubmit(CouncilAction action, out string error)
        {
            if (_submitting) { error = "A turn is resolving."; return false; }
            error = Validate(action); if (error != null) return false;
            _submitting = true;
            try { Apply(action); _world.Revision++; return true; }
            finally { _submitting = false; }
        }
        private string Validate(CouncilAction action)
        {
            if (_world.Phase == CouncilPhase.Ended) return "Run complete.";
            string error = CouncilActionValidator.Error(_world, _world.Player, action, true);
            if (error != null) return error;
            if (_world.Phase == CouncilPhase.Vote && action.Target.HasValue && _vote.IsRevote && !_vote.Tied.Contains(action.Target.Value))
                return "Choose a tied target.";
            if (_world.Phase == CouncilPhase.Discussion && _day.Respond && _world.Player.Alive)
            {
                bool response = action.Kind == CouncilActionKind.Pass || action.Kind == CouncilActionKind.Claim
                    || action.Kind == CouncilActionKind.Defend && action.Target == _world.Player.Id
                    || action.Kind == CouncilActionKind.Accuse && action.Target == _day.RespondTo;
                if (!response) return "Respond to the speaker first.";
            }
            bool speech = action.Kind == CouncilActionKind.Chat || action.Kind == CouncilActionKind.Accuse
                || action.Kind == CouncilActionKind.Defend || action.Kind == CouncilActionKind.Claim || action.Kind == CouncilActionKind.Report;
            return speech && !_speech.Eligible(_world.Player, action) ? "That phrasing is unavailable." : null;
        }

        private void Apply(CouncilAction action)
        {
            switch (_world.Phase)
            {
                case CouncilPhase.Discussion:
                    if (_world.Player.Alive && action.Kind != CouncilActionKind.Pass) _speech.Say(_world.Player, action);
                    _day.PlayerSpoke(_day.Respond, action); break;
                case CouncilPhase.Vote:
                    _vote.Resolve(action.Target);
                    if (_world.Phase == CouncilPhase.Elimination)
                    {
                        _night.Promote(); Evaluate();
                        if (_world.Eliminated.HasValue && _world.Eliminated != _world.Player.Id && _world.Options.LastWords)
                            _speech.Say(_world.Actor(_world.Eliminated.Value), new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.LastWords));
                    }
                    break;
                case CouncilPhase.Elimination:
                    if (action.Intent == SpeechIntent.LastWords) _speech.Say(_world.Player, action);
                    if (_world.Outcome != OutcomeKind.Ongoing) Finish();
                    else if (_world.Options.FreeTime) { _world.Phase = CouncilPhase.FreeTime; _freeTimeUsed = 0; }
                    else StartNight();
                    break;
                case CouncilPhase.FreeTime:
                    if (action.Target.HasValue) _world.Emit(CouncilEventKind.FreeTime,
                        "You spend time with " + _world.Actor(action.Target.Value).Name + ".", _world.Player.Id, action.Target,
                        recipient: _world.Player.Id);
                    if (++_freeTimeUsed >= _world.Options.FreeTimeActions) StartNight();
                    break;
                case CouncilPhase.Night:
                    _night.Resolve(action.Target);
                    if (Evaluate()) Finish();
                    break;
                case CouncilPhase.Morning:
                    _world.Day++; _world.Round = 1; _vote.Reset(); _world.Eliminated = null; MorningReports(); _day.Begin(); break;
            }
        }
        private void StartNight() { _world.Phase = CouncilPhase.Night; _world.Emit(CouncilEventKind.Phase, "Night " + _world.Day + "."); }
        private bool Evaluate()
        {
            _world.Outcome = _win.Evaluate(_world.Living.Select(a => a.Night()).ToList(), _world.Day, _world.Options.MaximumDays, _world.Options.Parity);
            return _world.Outcome != OutcomeKind.Ongoing;
        }
        private void Finish()
        {
            _world.Phase = CouncilPhase.Ended;
            string text = _world.Outcome == OutcomeKind.TownVictory ? "Town wins." : _world.Outcome == OutcomeKind.AntagonistVictory
                ? "Evil wins." : _world.Outcome == OutcomeKind.InfinitumVictory ? "Infinitum steals the win."
                : _world.Outcome == OutcomeKind.LoneKillerVictory ? "Anarchy is the last survivor." : "Draw.";
            _world.Emit(CouncilEventKind.GameOver, text);
        }
        private void MorningReports()
        {
            foreach (var actor in _world.Living.Where(a => a.Id != _world.Player.Id))
            {
                RoleId? claimed = _world.Claim(actor.Id);
                if (!claimed.HasValue) continue;
                var result = actor.Results.LastOrDefault(r => r.Night == _world.Day - 1 && (r.Readout.HasValue
                    || r.Kind == ReportKind.PatientAttacked || r.Kind == ReportKind.PatientSafe));
                if (claimed == actor.Role.Id && result != null)
                    _speech.Say(actor, new CouncilAction(CouncilActionKind.Report, result.Target, ReasonType.Evidence,
                        readout: result.Readout, patientAttacked: result.Readout.HasValue ? (bool?)null : result.Kind == ReportKind.PatientAttacked));
                else if (actor.Role.CanBluff)
                {
                    var role = _world.Catalog.Single(r => r.Id == claimed.Value);
                    var targets = _world.Knowledge(actor).Characters.Where(c => c.Alive && c.Id != actor.Id && !c.IsTeammate).ToList();
                    if (targets.Count == 0) continue;
                    var target = targets[_world.Decisions.Next(targets.Count)];
                    if (role.Ability == AbilityKind.Investigate)
                        _speech.Say(actor, new CouncilAction(CouncilActionKind.Report, target.Id, ReasonType.GutFeeling,
                            readout: Readout.Corrupt, declaredLie: true));
                    else if (role.Ability == AbilityKind.Protect)
                        _speech.Say(actor, new CouncilAction(CouncilActionKind.Report, target.Id, ReasonType.GutFeeling,
                            declaredLie: true, patientAttacked: _world.Decisions.Next(2) == 0));
                }
            }
        }
    }
}
