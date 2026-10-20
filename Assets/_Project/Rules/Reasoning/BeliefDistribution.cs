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
        private readonly Dictionary<CharacterId, FactionEstimate> _marginals = new Dictionary<CharacterId, FactionEstimate>();
        private bool _dirty = true;

        internal BeliefDistribution(IEnumerable<CharacterId> characters, SetupKnowledge setup, IReadOnlyList<RoleSpec> catalog,
            CharacterId self, RoleSpec ownRole, IEnumerable<CharacterId> teammates)
        {
            _setup = setup; _catalog = catalog.OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList().AsReadOnly();
            foreach (CharacterId id in characters) _weights.Add(id, new long[] { 100, 100, 100 });
            Know(self, ownRole.Faction, ownRole.Id);
            foreach (CharacterId id in teammates) Know(id, Faction.Antagonist);
        }

        internal void Know(CharacterId id, Faction faction, RoleId? role = null)
        { _known[id] = faction; if (role.HasValue) _knownRoles[id] = role.Value; _dirty = true; }

        internal void AssertAlignment(CharacterId target, Faction faction, int influence)
        {
            long current = _weights[target][(int)faction];
            _weights[target][(int)faction] = Math.Max(1, Math.Min(1000000, current * (100 + influence) / 100));
            _dirty = true;
        }

        internal void Claim(CharacterId speaker, RoleId role, int credibility)
        {
            _claims[speaker] = role;
            RoleSpec definition = _catalog.FirstOrDefault(r => r.Id == role);
            if (definition != null) AssertAlignment(speaker, definition.Faction, credibility);
        }

        internal Faction? KnownFaction(CharacterId target)
        {
            if (_known.TryGetValue(target, out Faction known)) return known;
            Faction? onlyRemaining = null;
            for (int i = 0; i < 3; i++)
            {
                if (_setup.For((Faction)i) <= _known.Values.Count(f => (int)f == i)) continue;
                if (onlyRemaining.HasValue) return null;
                onlyRemaining = (Faction)i;
            }
            return onlyRemaining;
        }

        internal FactionEstimate Factions(CharacterId target)
        {
            if (_known.TryGetValue(target, out Faction known))
                return new FactionEstimate(known == Faction.Town ? 10000 : 0, known == Faction.Antagonist ? 10000 : 0, known == Faction.Neutral ? 10000 : 0);
            if (_dirty) Recalculate();
            return _marginals[target];
        }

        private void Recalculate()
        {
            // Exact faction-count conditioning, not independent guesses or access to assigned roles.
            var ids = _weights.Keys.Where(id => !_known.ContainsKey(id)).OrderBy(id => id.Value).ToList();
            int town = _setup.Town - _known.Values.Count(f => f == Faction.Town);
            int evil = _setup.Antagonist - _known.Values.Count(f => f == Faction.Antagonist);
            int neutral = _setup.Neutral - _known.Values.Count(f => f == Faction.Neutral);
            if (town < 0 || evil < 0 || neutral < 0 || town + evil + neutral != ids.Count)
                throw new InvalidOperationException("Known alignments conflict with the disclosed setup.");
            int n = ids.Count;
            var likelihood = new double[n, 3];
            for (int i = 0; i < n; i++)
            {
                double sum = _weights[ids[i]].Sum();
                for (int faction = 0; faction < 3; faction++) likelihood[i, faction] = _weights[ids[i]][faction] / sum;
            }
            var forward = new double[n + 1, town + 1, evil + 1];
            var backward = new double[n + 1, town + 1, evil + 1];
            forward[0, 0, 0] = 1; backward[n, 0, 0] = 1;
            for (int i = 0; i < n; i++)
                for (int t = 0; t <= town; t++)
                    for (int e = 0; e <= evil; e++)
                    {
                        int z = i - t - e;
                        if (z < 0 || z > neutral) continue;
                        double prefix = forward[i, t, e];
                        if (t < town) forward[i + 1, t + 1, e] += prefix * likelihood[i, 0];
                        if (e < evil) forward[i + 1, t, e + 1] += prefix * likelihood[i, 1];
                        if (z < neutral) forward[i + 1, t, e] += prefix * likelihood[i, 2];
                    }
            for (int i = n - 1; i >= 0; i--)
                for (int t = 0; t <= town; t++)
                    for (int e = 0; e <= evil; e++)
                    {
                        int z = n - i - t - e;
                        if (z < 0 || z > neutral) continue;
                        if (t > 0) backward[i, t, e] += likelihood[i, 0] * backward[i + 1, t - 1, e];
                        if (e > 0) backward[i, t, e] += likelihood[i, 1] * backward[i + 1, t, e - 1];
                        if (z > 0) backward[i, t, e] += likelihood[i, 2] * backward[i + 1, t, e];
                    }
            _marginals.Clear();
            for (int i = 0; i < n; i++)
            {
                var mass = new double[3];
                for (int t = 0; t <= town; t++)
                    for (int e = 0; e <= evil; e++)
                    {
                        int z = i - t - e;
                        if (z < 0 || z > neutral) continue;
                        double prefix = forward[i, t, e];
                        if (t < town) mass[0] += prefix * likelihood[i, 0] * backward[i + 1, town - t - 1, evil - e];
                        if (e < evil) mass[1] += prefix * likelihood[i, 1] * backward[i + 1, town - t, evil - e - 1];
                        if (z < neutral) mass[2] += prefix * likelihood[i, 2] * backward[i + 1, town - t, evil - e];
                    }
                double sum = mass.Sum();
                if (sum <= 0) throw new InvalidOperationException("No consistent faction assignment remains.");
                var values = mass.Select(value => (int)Math.Floor(value / sum * 10000)).ToArray();
                foreach (int faction in Enumerable.Range(0, 3).OrderByDescending(f => mass[f] / sum * 10000 - values[f])
                    .ThenBy(f => f).Take(10000 - values.Sum())) values[faction]++;
                _marginals.Add(ids[i], new FactionEstimate(values[0], values[1], values[2]));
            }
            _dirty = false;
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
