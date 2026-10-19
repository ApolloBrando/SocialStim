using System;

namespace SocialStim.Rules.Council
{
    /// <summary>Explicit bounded workload and mistake policy shared by inference and decisions.</summary>
    public sealed class AiPolicy
    {
        /// <summary>Creates tuning; a positive sample override is bounded to 16–1024.</summary>
        public AiPolicy(AiDifficulty difficulty = AiDifficulty.Normal, int sampleBudget = 0)
        {
            if (!Enum.IsDefined(typeof(AiDifficulty), difficulty)) throw new ArgumentOutOfRangeException(nameof(difficulty));
            Difficulty = difficulty;
            Samples = sampleBudget == 0 ? new[] { 32, 80, 160, 256 }[(int)difficulty] : Math.Max(16, Math.Min(1024, sampleBudget));
        }
        /// <summary>Configured difficulty.</summary>
        public AiDifficulty Difficulty { get; }
        /// <summary>Maximum independently sampled assignments per inference refresh.</summary>
        public int Samples { get; }
        /// <summary>Percent chance of choosing a lower-scoring legal action.</summary>
        public int MistakePercent => new[] { 22, 12, 5, 1 }[(int)Difficulty];
        /// <summary>Confidence required for a limited Town shot.</summary>
        public double ShotThreshold => new[] { 0.94, 0.90, 0.86, 0.84 }[(int)Difficulty];
        /// <summary>How strongly deception and team coordination shape choices.</summary>
        public double Coordination => new[] { 0.35, 0.65, 0.85, 1.0 }[(int)Difficulty];
    }
}
