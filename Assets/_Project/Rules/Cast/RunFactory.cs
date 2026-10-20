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
        /// <summary>Creates the requested setup; the legacy seven-person template is used when none is supplied.</summary>
        public static RunSession Create(RunOptions options, IEnumerable<RoleSpec> roles, IEnumerable<string> names,
            IEnumerable<DialogueLine> dialogue, INpcStrategy strategy = null, ReasoningSettings reasoning = null, RoleList setup = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var catalog = ValidateCatalog(roles);
            var availableNames = names?.ToList() ?? throw new ArgumentNullException(nameof(names));
            int castSize = setup?.Slots.Count ?? 7;
            if (availableNames.Count < castSize || availableNames.Any(string.IsNullOrWhiteSpace) || availableNames.Distinct(StringComparer.Ordinal).Count() != availableNames.Count)
                throw new ArgumentException("Provide enough distinct, nonempty names for the role list.");
            var random = new SeededRandom(options.Seed);
            var roleList = setup ?? new RoleList(new[] {
                new RoleSlot(category: RoleCategory.TownInvestigative), new RoleSlot(category: RoleCategory.TownProtective),
                new RoleSlot(category: RoleCategory.TownSupport), new RoleSlot(category: RoleCategory.AntagonistKilling),
                new RoleSlot(category: RoleCategory.NeutralBenign), new RoleSlot(faction: Faction.Town), new RoleSlot(faction: Faction.Town)
            });
            var assigned = roleList.Resolve(catalog, options.PlayerRole, random).ToList();
            Shuffle(assigned, random); Shuffle(availableNames, random);
            if (options.PlayerRole.HasValue)
            {
                int index = assigned.FindIndex(r => r.Id == options.PlayerRole.Value);
                RoleSpec swap = assigned[0]; assigned[0] = assigned[index]; assigned[index] = swap;
            }
            var members = new List<CastMemberSpec>();
            for (int i = 0; i < assigned.Count; i++)
                members.Add(new CastMemberSpec(availableNames[i], assigned[i].Id));
            return CreateScenario(options, members, catalog, dialogue, strategy, reasoning: reasoning);
        }

        /// <summary>Builds an explicit starting scenario without exposing session secrets afterward.</summary>
        public static RunSession CreateScenario(RunOptions options, IEnumerable<CastMemberSpec> members,
            IEnumerable<RoleSpec> roles, IEnumerable<DialogueLine> dialogue, INpcStrategy strategy = null, IWinCondition winCondition = null,
            ReasoningSettings reasoning = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var catalog = ValidateCatalog(roles);
            var specifications = members?.ToList() ?? throw new ArgumentNullException(nameof(members));
            if (specifications.Any(m => m == null)) throw new ArgumentException("Scenario members cannot be null.");
            var traits = Enum.GetValues(typeof(Personality)).Cast<Personality>().Where(trait => trait != Personality.None).ToList();
            var remaining = new List<Personality>();
            // A separate existing seeded stream makes voice independent of role/faction
            // assignment and does not consume either the rules or dialogue stream.
            var traitRandom = new SeededRandom(options.Seed ^ 0x93D76A4Bu);
            var cast = new List<Participant>();
            for (int index = 0; index < specifications.Count; index++)
            {
                CastMemberSpec member = specifications[index];
                Personality trait = Personality.None;
                if (index == 0)
                {
                    if (member.Personality != Personality.None)
                        throw new ArgumentException("The player does not have an NPC trait.");
                }
                else
                {
                    if (remaining.Count == 0) { remaining.AddRange(traits); Shuffle(remaining, traitRandom); }
                    trait = member.Personality == Personality.None ? remaining[0] : member.Personality;
                    if (!remaining.Remove(trait))
                        throw new ArgumentException("Use every NPC trait before repeating one in a scenario.");
                }
                cast.Add(new Participant(new CharacterId(index), member.Name,
                    catalog.FirstOrDefault(role => role.Id == member.Role) ?? throw new ArgumentException($"Unknown role: {member.Role}."), trait));
            }
            if (cast.Count < 3 || cast.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count() != cast.Count)
                throw new ArgumentException("A scenario needs at least three uniquely named participants.");
            foreach (var group in cast.GroupBy(c => c.Role.Id))
                if (group.First().Role.Unique && group.Count() > 1) throw new ArgumentException("Unique role occurs more than once.");
            if (winCondition == null && catalog.Any(r => r.Ability == AbilityKind.Attack && r.Faction != Faction.Antagonist
                && r.Category != RoleCategory.TownKilling && r.Category != RoleCategory.NeutralKilling))
                throw new ArgumentException("Independent attacks require an explicit killing category or custom win condition.");
            if (winCondition == null && catalog.Any(r => (int)r.Category > (int)RoleCategory.NeutralBenign))
                winCondition = new CapabilityWinCondition();
            var settings = reasoning ?? new ReasoningSettings();
            return new RunSession(options, cast, catalog, dialogue, strategy ?? new SocialNpcStrategy(settings), winCondition ?? new StarterWinCondition(), settings);
        }

        private static List<RoleSpec> ValidateCatalog(IEnumerable<RoleSpec> roles)
        {
            var catalog = roles?.ToList() ?? throw new ArgumentNullException(nameof(roles));
            if (catalog.Count == 0 || catalog.Any(r => r == null) || catalog.Select(r => r.Id).Distinct().Count() != catalog.Count)
                throw new ArgumentException("Catalog must have unique role IDs.");
            return catalog.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList();
        }

        private static void Shuffle<T>(IList<T> values, IRandomSource random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            { int other = random.Next(i + 1); T swap = values[i]; values[i] = values[other]; values[other] = swap; }
        }
    }
}
