using System;

namespace SocialStim.Rules.Phases
{
    /// <summary>Guards transitions independently of session orchestration.</summary>
    public sealed class PhaseMachine
    {
        /// <summary>Starts at Day 1 discussion.</summary>
        public GamePhase Phase { get; private set; } = GamePhase.Discussion;
        /// <summary>Current day number.</summary>
        public int Day { get; private set; } = 1;
        /// <summary>Emitted after a legal phase transition.</summary>
        public event Action<GamePhase> Changed;

        internal void Restore(GamePhase phase, int day)
        {
            if (!Enum.IsDefined(typeof(GamePhase), phase) || phase == GamePhase.Execution || phase == GamePhase.NightResolution || day < 1)
                throw new ArgumentException("Invalid saved input phase.");
            Phase = phase; Day = day;
        }

        /// <summary>Rejects transitions that bypass the trial/night loop.</summary>
        public void MoveTo(GamePhase next)
        {
            bool legal = next == GamePhase.Ended && Phase != GamePhase.Ended;
            switch (Phase)
            {
                case GamePhase.Discussion: legal |= next == GamePhase.Nomination; break;
                case GamePhase.Nomination: legal |= next == GamePhase.Defense || next == GamePhase.NightSelection; break;
                case GamePhase.Defense: legal |= next == GamePhase.Judgement; break;
                case GamePhase.Judgement: legal |= next == GamePhase.LastWords || next == GamePhase.Nomination || next == GamePhase.NightSelection; break;
                case GamePhase.LastWords: legal |= next == GamePhase.Execution; break;
                case GamePhase.Execution: legal |= next == GamePhase.NightSelection; break;
                case GamePhase.NightSelection: legal |= next == GamePhase.NightResolution; break;
                case GamePhase.NightResolution: legal |= next == GamePhase.Dawn; break;
                case GamePhase.Dawn: legal |= next == GamePhase.Discussion; break;
            }
            if (!legal) throw new InvalidOperationException($"Illegal transition: {Phase} -> {next}.");
            if (Phase == GamePhase.NightResolution && next == GamePhase.Dawn) Day++;
            Phase = next;
            Changed?.Invoke(next);
        }
    }
}
