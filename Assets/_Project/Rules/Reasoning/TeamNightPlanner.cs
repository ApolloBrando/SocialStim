using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Deterministic team consensus; accepts proposals, never authoritative cast roles.</summary>
    public sealed class TeamNightPlanner
    {
        /// <summary>Chooses one kill; a living player killer explicitly controls their team's target/pass.</summary>
        public TeamNightPlan Plan(IEnumerable<TeamProposal> proposals, CharacterId? playerKiller = null, CharacterId? playerTarget = null)
        {
            var members = proposals.OrderBy(p => p.Actor.Value).ToList();
            if (members.Select(p => p.Actor).Distinct().Count() != members.Count) throw new ArgumentException("Duplicate team proposal.");
            if (playerKiller.HasValue)
            {
                if (!members.Any(p => p.Actor == playerKiller.Value)) throw new ArgumentException("Player killer is not in the authorized team.");
                return new TeamNightPlan(playerKiller.Value, playerTarget);
            }
            if (members.Count == 0) return null;
            var target = members.Where(p => p.Target.HasValue).GroupBy(p => p.Target.Value)
                .OrderByDescending(g => g.Count()).ThenByDescending(g => g.Sum(p => (long)Math.Max(0, Math.Min(1000, p.Priority))))
                .ThenBy(g => g.Key.Value).FirstOrDefault();
            return new TeamNightPlan(members[0].Actor, target == null ? (CharacterId?)null : target.Key);
        }
    }
}
