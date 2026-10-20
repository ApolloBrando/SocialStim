using System.Collections.Generic;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Commands
{
    /// <summary>Public ballot result without any hidden role or voter belief data.</summary>
    public sealed class PluralityResult
    {
        internal PluralityResult(CharacterId? winner, IReadOnlyList<CharacterId> tied,
            IReadOnlyDictionary<CharacterId, int> counts, int abstentions)
        { Winner = winner; Tied = tied; Counts = counts; Abstentions = abstentions; }
        /// <summary>Unique highest nonzero vote total, or no elimination.</summary>
        public CharacterId? Winner { get; }
        /// <summary>Highest tied candidates in stable identity order; empty for all abstentions.</summary>
        public IReadOnlyList<CharacterId> Tied { get; }
        /// <summary>Public totals for candidates receiving at least one vote.</summary>
        public IReadOnlyDictionary<CharacterId, int> Counts { get; }
        /// <summary>Number of voters choosing no target.</summary>
        public int Abstentions { get; }
    }
}
