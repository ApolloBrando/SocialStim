using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        /// <summary>Tallies a plurality without requiring a majority or breaking ties by identity.</summary>
        public static PluralityResult Plurality(IEnumerable<CharacterId?> votes)
        {
            if (votes == null) throw new ArgumentNullException(nameof(votes));
            var ballots = votes.ToList();
            var counts = ballots.Where(v => v.HasValue).GroupBy(v => v.Value).OrderBy(g => g.Key.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            int maximum = counts.Values.DefaultIfEmpty(0).Max();
            var leaders = counts.Where(p => p.Value == maximum).Select(p => p.Key).OrderBy(id => id.Value).ToList().AsReadOnly();
            return new PluralityResult(leaders.Count == 1 ? leaders[0] : (CharacterId?)null,
                leaders.Count > 1 ? leaders : new List<CharacterId>().AsReadOnly(),
                new ReadOnlyDictionary<CharacterId, int>(counts), ballots.Count(v => !v.HasValue));
        }

        /// <summary>Guilty must exceed innocent; ties and all-abstain acquit.</summary>
        public static bool IsGuilty(IEnumerable<Verdict> votes)
        {
            var list = votes.ToList();
            return list.Count(v => v == Verdict.Guilty) > list.Count(v => v == Verdict.Innocent);
        }
    }
}
