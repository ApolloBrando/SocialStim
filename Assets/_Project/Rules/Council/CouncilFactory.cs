using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Creates isolated modern runs while preserving the original factory for optional trial play.</summary>
    public static class CouncilFactory
    {
        public static CouncilSession Create(CouncilOptions options, SetupConfiguration setup, IEnumerable<RoleSpec> roles,
            IEnumerable<string> names, IEnumerable<DialogueLine> dialogue, ICouncilStrategy strategy = null)
        {
            var catalog = roles?.ToList() ?? throw new ArgumentNullException(nameof(roles));
            var namePool = names?.ToList() ?? throw new ArgumentNullException(nameof(names));
            if (options == null || setup == null) throw new ArgumentNullException(nameof(options));
            if (namePool.Any(string.IsNullOrWhiteSpace) || namePool.Distinct(StringComparer.Ordinal).Count() != namePool.Count)
                throw new ArgumentException("Provide distinct, nonempty names.");
            var random = new SeededRandom(options.Seed);
            var resolved = SetupBuilder.Build(setup, catalog, namePool.Count, random);
            var assigned = resolved.Roles.ToList();
            Shuffle(assigned, random); Shuffle(namePool, random);
            if (resolved.PlayerRole.HasValue)
            {
                int index = assigned.FindIndex(r => r.Id == resolved.PlayerRole.Value);
                RoleSpec swap = assigned[0]; assigned[0] = assigned[index]; assigned[index] = swap;
            }
            return CreateScenario(options, assigned.Select((r, i) => new CastMemberSpec(namePool[i], r.Id,
                (Personality)(1 << random.Next(5)) | (Personality)(1 << random.Next(5)))), catalog, dialogue, strategy);
        }

        public static CouncilSession CreateScenario(CouncilOptions options, IEnumerable<CastMemberSpec> members,
            IEnumerable<RoleSpec> roles, IEnumerable<DialogueLine> dialogue, ICouncilStrategy strategy = null)
        {
            if (options == null || members == null || roles == null || dialogue == null) throw new ArgumentNullException(nameof(options));
            var catalog = roles.ToList(); var cast = members.ToList();
            if (catalog.Count == 0 || catalog.Any(r => r == null) || catalog.Select(r => r.Id).Distinct().Count() != catalog.Count)
                throw new ArgumentException("Invalid role catalog.");
            if (cast.Count < 3 || cast.Any(c => c == null) || cast.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count() != cast.Count)
                throw new ArgumentException("Invalid cast.");
            if (cast.Any(c => !catalog.Any(r => r.Id == c.Role))) throw new ArgumentException("Unknown cast role.");
            if (cast.GroupBy(c => c.Role).Any(g => g.Count() > 1 && catalog.Single(r => r.Id == g.Key).Unique))
                throw new ArgumentException("Unique role assigned twice.");
            return new CouncilSession(new CouncilWorld(options, cast, catalog), dialogue, strategy ?? new CouncilNpcStrategy());
        }
        private static void Shuffle<T>(IList<T> items, IRandomSource random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); T swap = items[i]; items[i] = items[j]; items[j] = swap; }
        }
    }
}
