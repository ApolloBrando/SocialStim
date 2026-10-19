using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Normalizes evidence weights against disclosed setup constraints, without hidden assignments.</summary>
    internal sealed class BeliefDistribution
    {
        private readonly SetupKnowledge _setup;
        private readonly IReadOnlyList<RoleSpec> _catalog;
        private readonly Dictionary<CharacterId, Faction> _known = new Dictionary<CharacterId, Faction>();
        private readonly Dictionary<CharacterId, RoleId> _knownRoles = new Dictionary<CharacterId, RoleId>();
        private readonly Dictionary<CharacterId, long[]> _weights = new Dictionary<CharacterId, long[]>();
        private readonly Dictionary<CharacterId, RoleId> _claims = new Dictionary<CharacterId, RoleId>();

        internal BeliefDistribution(IEnumerable<CharacterId> characters, SetupKnowledge setup, IReadOnlyList<RoleSpec> catalog,
            CharacterId self, RoleSpec ownRole, IEnumerable<CharacterId> teammates)
        {
            _setup = setup; _catalog = catalog.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList().AsReadOnly();
            foreach (CharacterId id in characters) _weights.Add(id, new long[] { 100, 100, 100 });
            Know(self, ownRole.Faction, ownRole.Id);
            foreach (CharacterId id in teammates) Know(id, Faction.Antagonist);
        }

        internal void Know(CharacterId id, Faction faction, RoleId? role = null)
        { _known[id] = faction; if (role.HasValue) _knownRoles[id] = role.Value; }

        internal void AssertAlignment(CharacterId target, Faction faction, int influence)
        {
            long current = _weights[target][(int)faction];
            _weights[target][(int)faction] = Math.Max(1, Math.Min(1000000, current * (100 + influence) / 100));
        }

        internal void Claim(CharacterId speaker, RoleId role, int credibility)
        {
            _claims[speaker] = role;
            RoleSpec definition = _catalog.FirstOrDefault(r => r.Id == role);
            if (definition != null) AssertAlignment(speaker, definition.Faction, credibility);
        }

        internal FactionEstimate Factions(CharacterId target)
        {
            if (_known.TryGetValue(target, out Faction known))
                return new FactionEstimate(known == Faction.Town ? 10000 : 0, known == Faction.Antagonist ? 10000 : 0, known == Faction.Neutral ? 10000 : 0);
            var weights = new long[3];
            for (int i = 0; i < 3; i++)
            {
                int remaining = Math.Max(0, _setup.For((Faction)i) - _known.Values.Count(f => (int)f == i));
                weights[i] = remaining * _weights[target][i];
            }
            int[] values = Normalize(weights, 10000);
            return new FactionEstimate(values[0], values[1], values[2]);
        }

        internal IReadOnlyList<RoleEstimate> Roles(CharacterId target)
        {
            FactionEstimate factions = Factions(target);
            if (_knownRoles.TryGetValue(target, out RoleId knownRole))
                return _catalog.Select(r => new RoleEstimate(r.Id, r.Id == knownRole ? 10000 : 0)).ToList().AsReadOnly();
            var output = new Dictionary<RoleId, int>();
            for (int i = 0; i < 3; i++)
            {
                var candidates = _catalog.Where(r => (int)r.Faction == i).ToList();
                if (candidates.Count == 0)
                { if (factions.For((Faction)i) != 0) throw new InvalidOperationException("Public setup has no matching catalog role."); continue; }
                long[] weights = candidates.Select(r => r.Unique && _knownRoles.Values.Contains(r.Id) ? 0L
                    : _claims.TryGetValue(target, out RoleId claim) && r.Id == claim ? 150L : 100L).ToArray();
                int[] values = Normalize(weights, factions.For((Faction)i));
                for (int index = 0; index < candidates.Count; index++) output.Add(candidates[index].Id, values[index]);
            }
            return _catalog.Select(r => new RoleEstimate(r.Id, output[r.Id])).ToList().AsReadOnly();
        }

        private static int[] Normalize(long[] weights, int total)
        {
            long sum = weights.Sum();
            if (sum == 0 && total != 0) throw new InvalidOperationException("Public setup constraints leave no candidate alignment.");
            var values = new int[weights.Length];
            if (sum == 0) return values;
            for (int i = 0; i < weights.Length; i++) values[i] = (int)(weights[i] * total / sum);
            int remainder = total - values.Sum();
            foreach (int index in Enumerable.Range(0, weights.Length).OrderByDescending(i => weights[i] * total % sum).ThenBy(i => i).Take(remainder)) values[index]++;
            return values;
        }
    }
}
