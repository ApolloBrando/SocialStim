using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Public initial faction totals from the role setup; never recomputed from secret deaths.</summary>
    public sealed class SetupKnowledge
    {
        /// <summary>Creates publicly disclosed initial counts.</summary>
        public SetupKnowledge(int town, int antagonist, int neutral)
        {
            if (town < 0 || antagonist < 0 || neutral < 0 || town + antagonist + neutral == 0)
                throw new ArgumentOutOfRangeException(nameof(town));
            Town = town; Antagonist = antagonist; Neutral = neutral;
        }
        /// <summary>Initial Town slots.</summary>
        public int Town { get; }
        /// <summary>Initial Antagonist slots.</summary>
        public int Antagonist { get; }
        /// <summary>Initial Neutral slots.</summary>
        public int Neutral { get; }
        /// <summary>Returns the disclosed count for an alignment.</summary>
        public int For(Faction faction) => faction == Faction.Town ? Town : faction == Faction.Antagonist ? Antagonist : Neutral;
    }
}
