using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Cast
{
    /// <summary>Resolves the initial constrained role list and builds isolated run instances.</summary>
    public static class RunFactory
    {
        /// <summary>Creates the seven-character setup; rejects unsupported chosen roles and exhausted categories.</summary>
        public static RunSession Create(RunOptions options, IEnumerable<RoleSpec> roles, IEnumerable<string> names,
            IEnumerable<DialogueLine> dialogue, INpcStrategy strategy = null)
        {
            var catalog = ValidateCatalog(roles);
            var availableNames = names.ToList();
            if (availableNames.Count < 7 || availableNames.Any(string.IsNullOrWhiteSpace) || availableNames.Distinct(StringComparer.Ordinal).Count() != availableNames.Count)
                throw new ArgumentException("Provide at least seven distinct, nonempty names.");
            var random = new SeededRandom(options.Seed);
            var assigned = new List<RoleSpec>();
            foreach (RoleCategory category in new[] { RoleCategory.TownInvestigative, RoleCategory.TownProtective,
                RoleCategory.TownSupport, RoleCategory.AntagonistKilling, RoleCategory.NeutralBenign })
                assigned.Add(Pick(catalog.Where(r => r.Category == category), assigned, random));
            for (int i = 0; i < 2; i++) assigned.Add(Pick(catalog.Where(r => r.Faction == Faction.Town), assigned, random));
            if (options.PlayerRole.HasValue)
            {
                RoleSpec chosen = catalog.FirstOrDefault(r => r.Id == options.PlayerRole.Value)
                    ?? throw new ArgumentException("Chosen player role is not in the catalog.");
                if (assigned.All(r => r.Id != chosen.Id))
                {
                    int slot = assigned.FindIndex(r => r.Category == chosen.Category);
                    if (slot < 0) throw new ArgumentException("Chosen role cannot fit this role list.");
                    assigned[slot] = chosen;
                }
            }
            Shuffle(assigned, random); Shuffle(availableNames, random);
            if (options.PlayerRole.HasValue)
            {
                int index = assigned.FindIndex(r => r.Id == options.PlayerRole.Value);
                RoleSpec swap = assigned[0]; assigned[0] = assigned[index]; assigned[index] = swap;
            }
            var members = new List<CastMemberSpec>();
            for (int i = 0; i < assigned.Count; i++)
                members.Add(new CastMemberSpec(availableNames[i], assigned[i].Id, (Personality)(1 << random.Next(5)) | (Personality)(1 << random.Next(5))));
            return CreateScenario(options, members, catalog, dialogue, strategy);
        }

        /// <summary>Builds an explicit starting scenario without exposing session secrets afterward.</summary>
        public static RunSession CreateScenario(RunOptions options, IEnumerable<CastMemberSpec> members,
            IEnumerable<RoleSpec> roles, IEnumerable<DialogueLine> dialogue, INpcStrategy strategy = null, IWinCondition winCondition = null)
        {
            var catalog = ValidateCatalog(roles);
            var cast = members.Select((m, index) => new Participant(new CharacterId(index), m.Name,
                catalog.FirstOrDefault(r => r.Id == m.Role) ?? throw new ArgumentException($"Unknown role: {m.Role}."), m.Personality)).ToList();
            if (cast.Count < 3 || cast.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count() != cast.Count)
                throw new ArgumentException("A scenario needs at least three uniquely named participants.");
            foreach (var group in cast.GroupBy(c => c.Role.Id))
                if (group.First().Role.Unique && group.Count() > 1) throw new ArgumentException("Unique role occurs more than once.");
            if (winCondition == null && catalog.Any(r => r.Ability == AbilityKind.Attack && r.Faction != Faction.Antagonist))
                throw new ArgumentException("Starter parity does not support independent lethal roles; supply a different win condition.");
            return new RunSession(options, cast, catalog, dialogue, strategy ?? new BasicNpcStrategy(), winCondition ?? new StarterWinCondition());
        }

        private static List<RoleSpec> ValidateCatalog(IEnumerable<RoleSpec> roles)
        {
            var catalog = roles.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList();
            if (catalog.Count == 0 || catalog.Select(r => r.Id).Distinct().Count() != catalog.Count)
                throw new ArgumentException("Catalog must have unique role IDs.");
            return catalog;
        }

        private static RoleSpec Pick(IEnumerable<RoleSpec> candidates, IReadOnlyList<RoleSpec> assigned, IRandomSource random)
        {
            var eligible = candidates.Where(r => !r.Unique || assigned.All(a => a.Id != r.Id)).ToList();
            if (eligible.Count == 0) throw new ArgumentException("Role list cannot satisfy a category or uniqueness constraint.");
            return eligible[random.Next(eligible.Count)];
        }

        private static void Shuffle<T>(IList<T> values, IRandomSource random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            { int other = random.Next(i + 1); T swap = values[i]; values[i] = values[other]; values[other] = swap; }
        }
    }
}
