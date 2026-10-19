using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Commands
{
    /// <summary>Pure vote tally policies for nomination and judgement.</summary>
    public static class VoteResolver
    {
        /// <summary>Returns a strict-majority nominee; stable identity order breaks enumeration ties.</summary>
        public static CharacterId? Nominee(IEnumerable<CharacterId?> votes, int livingCount)
        {
            var group = votes.Where(v => v.HasValue).GroupBy(v => v.Value)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Value).FirstOrDefault();
            return group != null && group.Count() > livingCount / 2 ? group.Key : (CharacterId?)null;
        }

        /// <summary>Guilty must exceed innocent; ties and all-abstain acquit.</summary>
        public static bool IsGuilty(IEnumerable<Verdict> votes)
        {
            var list = votes.ToList();
            return list.Count(v => v == Verdict.Guilty) > list.Count(v => v == Verdict.Innocent);
        }
    }
}
