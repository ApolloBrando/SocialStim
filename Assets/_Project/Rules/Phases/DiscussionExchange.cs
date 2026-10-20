using System;
using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Rules.Phases
{
    /// <summary>Bounded speaking order and response suspension, independent of cast secrets and views.</summary>
    public sealed class DiscussionExchange
    {
        private readonly DiscussionSchedule _schedule;
        private readonly CharacterId _player;
        private readonly Queue<CharacterId> _speakers = new Queue<CharacterId>();
        private readonly HashSet<CharacterId> _answered = new HashSet<CharacterId>();
        private bool _windowInProgress;

        /// <summary>Connects the public player identity and ordinary-window schedule.</summary>
        public DiscussionExchange(DiscussionSchedule schedule, CharacterId player)
        { _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule)); _player = player; }
        /// <summary>The public accuser whose statement currently awaits a player response.</summary>
        public CharacterId? RespondTo { get; private set; }
        /// <summary>Ends the previous exchange and advances after any accepted player discussion action.</summary>
        public void PlayerActed()
        {
            _speakers.Clear(); _answered.Clear(); RespondTo = null; _windowInProgress = false;
            _schedule.AdvanceRound();
        }
        /// <summary>Begins an ordinary window and answers a player's direct accusation at most once.</summary>
        public void Begin(IEnumerable<CharacterId> speakers, GameCommand playerSpeech, Action<CharacterId> defend)
        {
            if (_windowInProgress || RespondTo.HasValue || _schedule.Complete)
                throw new InvalidOperationException("An exchange is already active or discussion is complete.");
            _windowInProgress = true; _answered.Clear();
            foreach (CharacterId speaker in speakers) _speakers.Enqueue(speaker);
            if (IsPressure(playerSpeech) && playerSpeech.Target.HasValue && playerSpeech.Target != _player)
            { _answered.Add(playerSpeech.Target.Value); defend(playerSpeech.Target.Value); }
        }
        /// <summary>Allows an addressed opening statement to be answered without starting an ordinary window.</summary>
        public void ObserveOpening(CharacterId speaker, GameCommand speech)
        { if (IsPressure(speech) && speech.Target == _player) RespondTo = speaker; }
        /// <summary>Acknowledges one valid response; the host remains responsible for command legality and delivery.</summary>
        public void Answer()
        {
            if (!RespondTo.HasValue) throw new InvalidOperationException("No response is pending.");
            RespondTo = null;
        }
        /// <summary>Runs queued NPC statements until a player response is needed or the ordinary window completes.</summary>
        public void Resume(Func<CharacterId, GameCommand> speak, Action<CharacterId> defend, bool playerAlive, Func<bool> paused = null)
        {
            if (RespondTo.HasValue) throw new InvalidOperationException("Answer the pending accusation first.");
            while (_speakers.Count > 0)
            {
                if (paused != null && paused()) return;
                CharacterId speaker = _speakers.Dequeue();
                GameCommand speech = speak(speaker);
                if (paused != null && paused()) return;
                if (!IsPressure(speech) || !speech.Target.HasValue) continue;
                if (speech.Target == _player && playerAlive) { RespondTo = speaker; return; }
                if (speech.Target != _player && _answered.Add(speech.Target.Value)) defend(speech.Target.Value);
            }
            if (paused != null && paused()) return;
            if (!_windowInProgress) return;
            _windowInProgress = false;
        }
        /// <summary>Clears the old day's queue, responses and ordinary-window counters.</summary>
        public void Reset()
        { _speakers.Clear(); _answered.Clear(); RespondTo = null; _windowInProgress = false; _schedule.Reset(); }
        private static bool IsPressure(GameCommand speech) => speech != null && (speech.Intent == SpeechIntent.Accuse
            || speech.Intent == SpeechIntent.CallForTrial || speech.Intent == SpeechIntent.Deflect);
    }
}
