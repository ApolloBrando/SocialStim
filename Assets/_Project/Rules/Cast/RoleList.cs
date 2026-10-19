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
            if (Slots.Count < 3 || Slots.Count > 20 || Slots.Any(s => s == null))
                throw new ArgumentException("A role list needs 3 to 20 non-null slots.");
        }
        public IReadOnlyList<RoleSlot> Slots { get; }

        public IReadOnlyList<RoleSpec> Resolve(IEnumerable<RoleSpec> roles, RoleId? chosen, IRandomSource random)
        {
            var catalog = roles.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList();
            if (catalog.Count > 64 || catalog.Select(r => r.Id).Distinct().Count() != catalog.Count)
                throw new ArgumentException("Role catalog needs at most 64 distinct definitions.");
            if (chosen.HasValue && !catalog.Any(r => r.Id == chosen.Value)) throw new ArgumentException("Chosen role is missing.");
            var pools = Slots.Select(s => catalog.Where(s.Accepts).ToList()).ToList();
            foreach (var pool in pools)
            {
                if (pool.Count == 0) throw new ArgumentException("A role slot has no eligible definitions.");
                for (int i = pool.Count - 1; i > 0; i--)
                { int j = random.Next(i + 1); RoleSpec swap = pool[i]; pool[i] = pool[j]; pool[j] = swap; }
            }
            var assigned = new List<RoleSpec>();
            int attempts = 0;
            if (!Fill(0, pools, assigned, chosen, ref attempts))
                throw new ArgumentException("Role list cannot satisfy chosen role and uniqueness constraints.");
            return assigned.AsReadOnly();
        }

        private static bool Fill(int slot, List<List<RoleSpec>> pools, List<RoleSpec> assigned, RoleId? chosen, ref int attempts)
        {
            if (++attempts > 100000) throw new ArgumentException("Role list search limit exceeded; narrow its slot constraints.");
            if (slot == pools.Count) return !chosen.HasValue || assigned.Any(r => r.Id == chosen.Value);
            if (chosen.HasValue && !assigned.Any(r => r.Id == chosen.Value)
                && !pools.Skip(slot).Any(p => p.Any(r => r.Id == chosen.Value))) return false;
            foreach (RoleSpec role in pools[slot])
            {
                if (role.Unique && assigned.Any(r => r.Id == role.Id)) continue;
                assigned.Add(role);
                if (Fill(slot + 1, pools, assigned, chosen, ref attempts)) return true;
                assigned.RemoveAt(assigned.Count - 1);
            }
            return false;
        }
    }
}
