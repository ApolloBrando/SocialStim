using System;

namespace SocialStim.Rules.Phases
{
    /// <summary>Counts ordinary player windows separately from priority responses.</summary>
    public sealed class DiscussionSchedule
    {
        private readonly int _rounds;
        private readonly int _windows;
        private int _completed;

        /// <summary>Creates a bounded daily discussion schedule.</summary>
        public DiscussionSchedule(int rounds, int windows)
        {
            if (rounds < 3 || rounds > 7 || windows < 1 || windows > 2)
                throw new ArgumentOutOfRangeException(nameof(rounds));
            _rounds = rounds; _windows = windows;
        }
        /// <summary>Current one-based round, including the final completed round.</summary>
        public int Round => Math.Min(_rounds, _completed / _windows + 1);
        /// <summary>Current one-based ordinary player window.</summary>
        public int Window => Complete ? _windows : _completed % _windows + 1;
        /// <summary>All configured ordinary windows have been consumed.</summary>
        public bool Complete => _completed == _rounds * _windows;
        /// <summary>Completes an ordinary window after its NPC exchanges, never a response prompt.</summary>
        public void Advance()
        {
            if (Complete) throw new InvalidOperationException("Discussion is complete.");
            _completed++;
        }
        /// <summary>Starts a fresh day's schedule.</summary>
        public void Reset() => _completed = 0;
    }
}
