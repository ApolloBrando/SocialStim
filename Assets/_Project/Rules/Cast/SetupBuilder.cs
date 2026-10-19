using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Cast
{
    /// <summary>One setup validator/solver for presets, multiplicities, slots and player eligibility.</summary>
    public static class SetupBuilder
    {
        public static ResolvedSetup Build(SetupConfiguration configuration, IEnumerable<RoleSpec> definitions,
            int characterCapacity, IRandomSource random)
        {
            if (configuration == null || definitions == null || random == null) throw new ArgumentNullException(nameof(configuration));
            int count = configuration.Participants;
            if (count > characterCapacity) throw new ArgumentException("Not enough named characters.");
            var catalog = definitions.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList();
            if (catalog.Count == 0 || catalog.Select(r => r.Id).Distinct().Count() != catalog.Count) throw new ArgumentException("Invalid role catalog.");
            var limits = configuration.RoleCounts.ToDictionary(r => r.Role);
            foreach (var rule in limits.Values)
            {
                RoleSpec role = catalog.FirstOrDefault(r => r.Id == rule.Role) ?? throw new ArgumentException("Unknown role in custom limits.");
                if (role.Unique && rule.Minimum > 1) throw new ArgumentException("A unique role cannot have a minimum above one.");
            }
            var available = catalog.Where(r => !limits.TryGetValue(r.Id, out RoleCountRule rule) || rule.Maximum > 0).ToList();
            if (configuration.Preset == SetupPreset.Minimal)
                available = available.Where(r => r.Faction == Faction.Town && (r.Ability == AbilityKind.None || r.Ability == AbilityKind.Investigate)
                    || r.Faction == Faction.Antagonist && r.Ability == AbilityKind.Attack).ToList();
            if (!available.Any(r => r.Faction == Faction.Antagonist && r.Ability == AbilityKind.Attack))
                throw new ArgumentException("No evil killing role enabled.");
            RoleId? chosen = configuration.PlayerRole;
            if (chosen.HasValue && !available.Any(r => r.Id == chosen.Value)) throw new ArgumentException("Player role is disabled in this preset.");
            if (chosen.HasValue && configuration.PlayerFaction.HasValue
                && available.Single(r => r.Id == chosen.Value).Faction != configuration.PlayerFaction.Value)
                throw new ArgumentException("Player role does not match the chosen faction.");
            if (!chosen.HasValue && configuration.PlayerFaction.HasValue)
            {
                var candidates = available.Where(r => r.Faction == configuration.PlayerFaction.Value).ToList();
                if (candidates.Count == 0) throw new ArgumentException("No roles enabled for that faction.");
                chosen = candidates[random.Next(candidates.Count)].Id;
            }
            RoleSpec selected = chosen.HasValue ? available.Single(r => r.Id == chosen.Value) : null;
            int evil = configuration.Evil >= 0 ? configuration.Evil : configuration.Preset == SetupPreset.Minimal ? 1 : Math.Max(1, (count + 2) / 4);
            int neutral = configuration.Neutral >= 0 ? configuration.Neutral
                : configuration.Preset == SetupPreset.Minimal ? 0 : count >= 14 ? 2 : count >= 8 ? 1 : 0;
            if (configuration.Evil < 0 && selected != null && selected.Faction == Faction.Antagonist && selected.Ability != AbilityKind.Attack)
                evil = Math.Max(evil, 2);
            if (configuration.Neutral < 0 && selected != null && selected.Faction == Faction.Neutral) neutral = Math.Max(1, neutral);
            int town = count - evil - neutral;
            if (evil < 1 || neutral < 0 || town <= evil) throw new ArgumentException("Evil count too high for this player count.");
            var remaining = new Dictionary<Faction, int> { [Faction.Town] = town, [Faction.Antagonist] = evil, [Faction.Neutral] = neutral };
            var slots = new List<RoleSlot>();
            bool valid(IReadOnlyList<RoleSpec> roles) => roles.Any(r => r.Faction == Faction.Antagonist && r.Ability == AbilityKind.Attack)
                && roles.Count(r => r.Faction == Faction.Town) > roles.Count(r => r.Faction == Faction.Antagonist)
                && (configuration.Evil < 0 || roles.Count(r => r.Faction == Faction.Antagonist) == configuration.Evil)
                && (configuration.Neutral < 0 || roles.Count(r => r.Faction == Faction.Neutral) == configuration.Neutral);
            if (configuration.Preset == SetupPreset.Custom && configuration.Slots.Count > 0)
            {
                if (configuration.Slots.Count != count) throw new ArgumentException("Slot count must equal participant count.");
                var custom = new RoleList(configuration.Slots).Resolve(available, chosen, random,
                    limits.Values.Where(r => available.Any(a => a.Id == r.Role)), valid);
                return new ResolvedSetup(custom, chosen);
            }
            var forced = new List<RoleSpec>();
            void require(RoleSpec role)
            {
                if (--remaining[role.Faction] < 0) throw new ArgumentException("Role minimums exceed faction counts.");
                forced.Add(role); slots.Add(new RoleSlot(role.Id));
            }
            foreach (var rule in limits.Values.OrderBy(r => r.Role.Value, StringComparer.Ordinal))
            {
                if (rule.Minimum > 0 && !available.Any(r => r.Id == rule.Role)) throw new ArgumentException("Required role is disabled.");
                for (int i = 0; i < rule.Minimum; i++) require(available.Single(r => r.Id == rule.Role));
            }
            if (selected != null && !forced.Any(r => r.Id == selected.Id)) require(selected);
            if (!forced.Any(r => r.Faction == Faction.Antagonist && r.Ability == AbilityKind.Attack))
            {
                if (--remaining[Faction.Antagonist] < 0) throw new ArgumentException("Leave an evil slot for the Archon.");
                slots.Add(new RoleSlot(faction: Faction.Antagonist, category: RoleCategory.AntagonistKilling));
            }
            if (configuration.Preset == SetupPreset.Classic)
            {
                foreach (var category in new[] { RoleCategory.TownInvestigative, RoleCategory.TownProtective })
                {
                    int threshold = category == RoleCategory.TownInvestigative ? 3 : 4;
                    if (town >= threshold && remaining[Faction.Town] > 1 && !forced.Any(r => r.Category == category)
                        && available.Any(r => r.Category == category))
                    { slots.Add(new RoleSlot(faction: Faction.Town, category: category)); remaining[Faction.Town]--; }
                }
            }
            foreach (Faction faction in new[] { Faction.Town, Faction.Antagonist, Faction.Neutral })
                for (int i = 0; i < remaining[faction]; i++) slots.Add(new RoleSlot(faction: faction));
            var result = new RoleList(slots).Resolve(available, chosen, random,
                limits.Values.Where(r => available.Any(a => a.Id == r.Role)), valid);
            return new ResolvedSetup(result, chosen);
        }

        /// <summary>Preview validation uses an isolated stream and never consumes the run RNG.</summary>
        public static string Error(SetupConfiguration configuration, IEnumerable<RoleSpec> definitions, int capacity)
        {
            try { Build(configuration, definitions, capacity, new SeededRandom(1)); return null; }
            catch (ArgumentException exception) { return exception.Message; }
        }
    }
}
