using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Samples only disclosed constraints. Claims and forgeable results remain likelihood evidence.</summary>
    internal sealed class CouncilHypothesisSampler
    {
        private readonly CouncilKnowledge _knowledge;
        private readonly RoleSpec[] _catalog;
        private readonly IRandomSource _random;
        internal CouncilHypothesisSampler(CouncilKnowledge knowledge, IRandomSource random)
        { _knowledge = knowledge; _catalog = knowledge.Catalog.ToArray(); _random = random; }

        internal RoleSpec[] Next()
        {
            var cast = _knowledge.Characters;
            var world = new RoleSpec[cast.Count];
            var bag = _knowledge.RoleCounts == null ? null : _knowledge.RoleCounts
                .SelectMany(pair => Enumerable.Repeat(_catalog.Single(r => r.Id == pair.Key), pair.Value)).ToList();
            var remaining = _knowledge.Counts == null ? null : new[] { _knowledge.Counts.Town, _knowledge.Counts.Antagonist, _knowledge.Counts.Neutral };
            var unique = new HashSet<RoleId>();
            for (int i = 0; i < cast.Count; i++)
            {
                RoleId? known = cast[i].Id == _knowledge.Self ? _knowledge.OwnRole.Id : cast[i].RevealedRole;
                if (!known.HasValue) continue;
                RoleSpec role = _catalog.FirstOrDefault(r => r.Id == known.Value);
                if (role == null || role.Unique && !unique.Add(role.Id)) return null;
                if (bag != null && !bag.Remove(role)) return null;
                if (remaining != null && --remaining[(int)role.Faction] < 0) return null;
                world[i] = role;
            }
            // Teammate permissions are exact faction information, never a foreign role assignment.
            var order = Enumerable.Range(0, cast.Count).Where(i => world[i] == null).ToList();
            Shuffle(order);
            order = order.OrderByDescending(i => cast[i].IsTeammate || cast[i].RevealedFaction.HasValue).ToList();
            foreach (int i in order)
            {
                Faction? knownFaction = cast[i].RevealedFaction ?? (cast[i].IsTeammate ? Faction.Antagonist : (Faction?)null);
                var candidates = (bag ?? _catalog.ToList()).Where(r => (!knownFaction.HasValue || r.Faction == knownFaction.Value)
                    && !(_knowledge.OwnRole.Faction == Faction.Antagonist && !cast[i].IsTeammate && r.Faction == Faction.Antagonist)
                    && (!r.Unique || !unique.Contains(r.Id)) && (remaining == null || remaining[(int)r.Faction] > 0)).ToList();
                if (candidates.Count == 0) return null;
                if (remaining != null && !knownFaction.HasValue && bag == null)
                {
                    // Draw faction slots proportionally, not proportionally to catalog definition count.
                    int total = candidates.Select(r => r.Faction).Distinct().Sum(f => remaining[(int)f]);
                    int ticket = _random.Next(total);
                    Faction faction = candidates[0].Faction;
                    foreach (var candidateFaction in candidates.Select(r => r.Faction).Distinct())
                    { ticket -= remaining[(int)candidateFaction]; if (ticket < 0) { faction = candidateFaction; break; } }
                    candidates = candidates.Where(r => r.Faction == faction).ToList();
                }
                if (remaining == null && bag == null && !knownFaction.HasValue)
                {
                    // Hidden lists use a broad declared prior, never the actual setup's undisclosed totals.
                    int ticket = _random.Next(100);
                    Faction prior = ticket < 65 ? Faction.Town : ticket < 90 ? Faction.Antagonist : Faction.Neutral;
                    var pool = candidates.Where(r => r.Faction == prior).ToList();
                    if (pool.Count > 0) candidates = pool;
                }
                RoleSpec selected = candidates[_random.Next(candidates.Count)];
                world[i] = selected; if (selected.Unique) unique.Add(selected.Id);
                bag?.Remove(selected);
                if (remaining != null) remaining[(int)selected.Faction]--;
            }
            return world;
        }
        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            { int j = _random.Next(i + 1); T swap = list[i]; list[i] = list[j]; list[j] = swap; }
        }
    }
}
