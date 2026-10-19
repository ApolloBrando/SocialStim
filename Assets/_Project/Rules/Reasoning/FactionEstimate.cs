using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Normalized heuristic beliefs in basis points; not calibrated probabilities or secret truth.</summary>
    public sealed class FactionEstimate
    {
        /// <summary>Requires nonnegative estimates totaling exactly 10000.</summary>
        public FactionEstimate(int town, int antagonist, int neutral)
        {
            if (town < 0 || antagonist < 0 || neutral < 0 || town + antagonist + neutral != 10000)
                throw new ArgumentException("Faction estimates must sum to 10000 basis points.");
            Town = town; Antagonist = antagonist; Neutral = neutral;
        }
        /// <summary>Estimated Town likelihood.</summary>
        public int Town { get; }
        /// <summary>Estimated Antagonist likelihood.</summary>
        public int Antagonist { get; }
        /// <summary>Estimated Neutral likelihood.</summary>
        public int Neutral { get; }
        /// <summary>Returns this model's estimate for an alignment.</summary>
        public int For(Faction faction) => faction == Faction.Town ? Town : faction == Faction.Antagonist ? Antagonist : Neutral;
    }
}
