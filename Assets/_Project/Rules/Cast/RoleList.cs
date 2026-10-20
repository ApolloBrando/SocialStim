using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>Validated authored role slots, filled with deterministic constrained search.</summary>
    public sealed class RoleList
    {
        public RoleList(IEnumerable<RoleSlot> slots)
        {
            Slots = slots?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(slots));
            if (Slots.Count < 3 || Slots.Any(s => s == null))
                throw new ArgumentException("A role list needs at least three non-null slots.");
        }
        public IReadOnlyList<RoleSlot> Slots { get; }

        public IReadOnlyList<RoleSpec> Resolve(IEnumerable<RoleSpec> roles, RoleId? chosen, IRandomSource random)
        {
            if (roles == null) throw new ArgumentNullException(nameof(roles));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var catalog = roles.ToList();
            if (catalog.Count == 0 || catalog.Any(r => r == null) || catalog.Select(r => r.Id).Distinct().Count() != catalog.Count)
                throw new ArgumentException("Role catalog needs distinct, non-null definitions.");
            catalog = catalog.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList();
            if (chosen.HasValue && !catalog.Any(r => r.Id == chosen.Value)) throw new ArgumentException("Chosen role is missing.");
            var pools = Slots.Select(s => catalog.Where(s.Accepts).ToList()).ToList();
            foreach (var pool in pools)
            {
                if (pool.Count == 0) throw new ArgumentException("A role slot has no eligible definitions.");
                for (int i = pool.Count - 1; i > 0; i--)
                { int j = random.Next(i + 1); RoleSpec swap = pool[i]; pool[i] = pool[j]; pool[j] = swap; }
            }
            var assigned = new List<RoleSpec>();
            if (!Fill(pools, assigned, chosen))
                throw new ArgumentException("Role list cannot satisfy chosen role and uniqueness constraints.");
            return assigned.AsReadOnly();
        }

        /// <summary>Scales faction shares to a population; exact authored slots remain available through the constructor.</summary>
        public static RoleList ForPopulation(int participants, int townShare = 6, int antagonistShare = 2, int neutralShare = 1)
        {
            if (participants < 3) throw new ArgumentOutOfRangeException(nameof(participants), "At least three participants are required.");
            if (townShare < 1 || antagonistShare < 1 || neutralShare < 0)
                throw new ArgumentException("Town and Antagonist shares must be positive; Neutral may be zero.");
            long total = (long)townShare + antagonistShare + neutralShare;
            int neutrals = neutralShare == 0 || participants < 5 ? 0 : Math.Max(1, (int)((long)participants * neutralShare / total));
            neutrals = Math.Min(neutrals, participants - 2);
            int antagonists = Math.Min(participants - neutrals - 1, Math.Max(1, (int)(((long)participants * antagonistShare + total - 1) / total)));
            int town = participants - antagonists - neutrals;
            var slots = new List<RoleSlot>();
            // Keep one team killer and a specialist core when there is room for flexible Town slots.
            slots.Add(new RoleSlot(faction: Faction.Antagonist, category: RoleCategory.AntagonistKilling));
            for (int i = 1; i < antagonists; i++) slots.Add(new RoleSlot(faction: Faction.Antagonist));
            if (town >= 4)
            {
                slots.Add(new RoleSlot(faction: Faction.Town, category: RoleCategory.TownInvestigative));
                slots.Add(new RoleSlot(faction: Faction.Town, category: RoleCategory.TownProtective));
                slots.Add(new RoleSlot(faction: Faction.Town, category: RoleCategory.TownKilling));
                town -= 3;
            }
            for (int i = 0; i < town; i++) slots.Add(new RoleSlot(faction: Faction.Town));
            for (int i = 0; i < neutrals; i++) slots.Add(new RoleSlot(faction: Faction.Neutral));
            return new RoleList(slots);
        }

        private static bool Fill(List<List<RoleSpec>> pools, List<RoleSpec> assigned, RoleId? chosen)
        {
            // Iterative backtracking keeps large authored casts off the call stack.
            var next = new int[pools.Count];
            var usedUnique = new HashSet<RoleId>();
            var canChoose = new bool[pools.Count + 1];
            for (int i = pools.Count - 1; i >= 0; i--)
                canChoose[i] = canChoose[i + 1] || chosen.HasValue && pools[i].Any(r => r.Id == chosen.Value);
            int slot = 0, attempts = 0, chosenCount = 0;
            while (slot >= 0)
            {
                if (++attempts > 100000) throw new ArgumentException("Role list search limit exceeded; narrow its slot constraints.");
                if (slot == pools.Count && (!chosen.HasValue || chosenCount > 0)) return true;
                bool exhausted = slot == pools.Count || next[slot] >= pools[slot].Count
                    || chosen.HasValue && chosenCount == 0 && !canChoose[slot];
                if (exhausted)
                {
                    if (slot < pools.Count) next[slot] = 0;
                    if (--slot < 0) return false;
                    RoleSpec removed = assigned[assigned.Count - 1];
                    assigned.RemoveAt(assigned.Count - 1);
                    if (removed.Unique) usedUnique.Remove(removed.Id);
                    if (chosen.HasValue && removed.Id == chosen.Value) chosenCount--;
                    continue;
                }
                RoleSpec role = pools[slot][next[slot]++];
                if (role.Unique && !usedUnique.Add(role.Id)) continue;
                assigned.Add(role);
                if (chosen.HasValue && role.Id == chosen.Value) chosenCount++;
                slot++;
            }
            return false;
        }
    }
}
