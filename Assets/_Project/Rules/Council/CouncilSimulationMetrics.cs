using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Council
{
    /// <summary>Aggregate completed-run diagnostics. These are balance observations, not ongoing-run saves.</summary>
    public sealed class CouncilSimulationMetrics
    {
        private readonly Dictionary<OutcomeKind, int> _outcomes = Enum.GetValues(typeof(OutcomeKind)).Cast<OutcomeKind>().ToDictionary(k => k, k => 0);
        private readonly Dictionary<RoleId, int[]> _roles = new Dictionary<RoleId, int[]>();
        private readonly List<string> _failures = new List<string>();
        internal int DayTotal { get; set; }
        internal int CorrectVotes { get; set; }
        internal int TownVotes { get; set; }
        internal int InfinitumRuns { get; set; }
        internal int StepTotal { get; set; }
        internal double Milliseconds { get; set; }
        /// <summary>Requested games, including failed/stalled runs.</summary>
        public int Games { get; internal set; }
        /// <summary>Completed outcomes.</summary>
        public IReadOnlyDictionary<OutcomeKind, int> Outcomes => _outcomes;
        /// <summary>Crashes or step-limit violations, with reproducible seeds.</summary>
        public IReadOnlyList<string> Failures => _failures;
        /// <summary>Average final day among completed games.</summary>
        public double AverageDays => DayTotal / (double)Math.Max(1, _outcomes.Values.Sum());
        /// <summary>Fraction of Town ballots against Evil/Anarchy, measured only after true roles are revealed.</summary>
        public double VoteAccuracy => CorrectVotes / (double)Math.Max(1, TownVotes);
        /// <summary>Steals divided by runs containing Infinitum.</summary>
        public double StealRate => _outcomes[OutcomeKind.InfinitumVictory] / (double)Math.Max(1, InfinitumRuns);
        /// <summary>Headless wall time per submitted player decision, including synchronous NPC resolution.</summary>
        public double MillisecondsPerStep => Milliseconds / Math.Max(1, StepTotal);
        internal void Complete(OutcomeKind outcome) { _outcomes[outcome]++; }
        internal void Fail(uint seed, string message) { _failures.Add(seed + ": " + message); }
        internal void Role(RoleId role, bool won)
        {
            if (!_roles.TryGetValue(role, out int[] entry)) { entry = new int[2]; _roles.Add(role, entry); }
            entry[0]++; if (won) entry[1]++;
        }
        /// <summary>Stable machine-readable summary emitted by batch tests.</summary>
        public string Summary(AiDifficulty difficulty, int budget)
        {
            string number(double value) => value.ToString("0.0000", CultureInfo.InvariantCulture);
            return "SIM_BASELINE difficulty=" + difficulty + " games=" + Games + " budget=" + budget
                + " avgDays=" + number(AverageDays) + " voteAccuracy=" + number(VoteAccuracy)
                + " stealRate=" + number(StealRate) + " msPerStep=" + number(MillisecondsPerStep)
                + " failures=" + Failures.Count + " outcomes=" + string.Join(",", _outcomes.Select(p => p.Key + ":" + p.Value))
                + " roleWins=" + string.Join(",", _roles.OrderBy(p => p.Key.Value, StringComparer.Ordinal).Select(p => p.Key.Value + ":" + p.Value[1] + "/" + p.Value[0]));
        }
    }
}
