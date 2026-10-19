using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Bounded importance sampling over observer-authorized worlds; no authoritative world reference is accepted.</summary>
    public sealed class CouncilInference
    {
        /// <summary>Infers role/faction marginals, credibility, affinity and exposure without consuming gameplay randomness.</summary>
        public CouncilInferenceResult Evaluate(CouncilKnowledge knowledge)
        {
            if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));
            if (knowledge.Results.Any(r => r.Recipient != knowledge.Self)) throw new ArgumentException("Foreign private result.");
            var social = new CouncilSocialEvidence(knowledge);
            uint seed = unchecked(knowledge.Seed ^ (uint)(knowledge.Self.Value + 1) * 2654435761u
                ^ (uint)knowledge.Events.Count * 2246822519u ^ (uint)knowledge.Results.Count * 3266489917u);
            var sampler = new CouncilHypothesisSampler(knowledge, new SeededRandom(seed));
            var characters = knowledge.Characters;
            var indices = characters.Select((c, i) => new { c.Id, Index = i }).ToDictionary(c => c.Id, c => c.Index);
            var catalog = knowledge.Catalog;
            var roleIndices = catalog.Select((r, i) => new { r.Id, Index = i }).ToDictionary(r => r.Id, r => r.Index);
            var unary = characters.Select(c => catalog.Select(r => social.LogLikelihood(c.Id, r)).ToArray()).ToArray();
            var reports = knowledge.Events.Where(e => e.Readout.HasValue && e.Actor.HasValue && e.Target.HasValue)
                .GroupBy(e => new { e.Actor, e.Target, e.Day }).Select(g => g.Last()).ToArray();
            var own = knowledge.Results.Where(r => r.Readout.HasValue && r.Target.HasValue).ToArray();
            var worlds = new List<Tuple<RoleSpec[], double>>();
            int attempts = 0;
            while (worlds.Count < knowledge.Policy.Samples && attempts++ < knowledge.Policy.Samples * 4)
            {
                var world = sampler.Next(); if (world == null) continue;
                double logWeight = 0;
                for (int i = 0; i < world.Length; i++) logWeight += unary[i][roleIndices[world[i].Id]];
                if (double.IsNegativeInfinity(logWeight)) continue;
                bool forgePossible = world.Any(r => r.Ability == AbilityKind.Forge);
                foreach (var report in own)
                    logWeight += Math.Log(ReadoutLikelihood(world[indices[report.Target.Value]], report.Readout.Value, forgePossible));
                foreach (var report in reports)
                {
                    var reporter = world[indices[report.Actor.Value]];
                    if (reporter.Ability == AbilityKind.Investigate)
                        logWeight += Math.Log(ReadoutLikelihood(world[indices[report.Target.Value]], report.Readout.Value, forgePossible));
                    else if (!reporter.CanBluff) logWeight += Math.Log(0.03);
                    // A plausible bluff is evidence about the reporter, not authenticated target information.
                }
                worlds.Add(Tuple.Create(world, logWeight));
            }
            if (worlds.Count == 0) throw new InvalidOperationException("Disclosed role constraints have no sampled hypothesis.");
            double max = worlds.Max(w => w.Item2);
            var weights = worlds.Select(w => Math.Exp(w.Item2 - max)).ToArray();
            double total = weights.Sum();
            double effective = total * total / weights.Sum(w => w * w);
            var beliefs = new Dictionary<CharacterId, CouncilBelief>();
            for (int i = 0; i < characters.Count; i++)
            {
                var roles = catalog.ToDictionary(r => r.Id, r => 0.0);
                for (int sample = 0; sample < worlds.Count; sample++) roles[worlds[sample].Item1[i].Id] += weights[sample] / total;
                double faction(Faction value) => catalog.Where(r => r.Faction == value).Sum(r => roles[r.Id]);
                double killer = catalog.Where(r => r.Objective == RoleObjective.LastSurvivor).Sum(r => roles[r.Id]);
                beliefs.Add(characters[i].Id, new CouncilBelief(new ReadOnlyDictionary<RoleId, double>(roles),
                    faction(Faction.Town), faction(Faction.Antagonist), faction(Faction.Neutral), killer,
                    social.Credibility(characters[i].Id), social.Affinity(characters[i].Id)));
            }
            return new CouncilInferenceResult(new ReadOnlyDictionary<CharacterId, CouncilBelief>(beliefs),
                worlds.Count, effective, social.Exposure, social.Tension);
        }
        private static double ReadoutLikelihood(RoleSpec target, Readout readout, bool forgePossible)
        {
            if (target.Readout == readout) return 0.985;
            // Anomaly is never inverted. Clear is not synonymous with Town.
            if (target.Readout == Readout.Anomaly || readout == Readout.Anomaly) return 0.0005;
            return forgePossible ? 0.18 : 0.002;
        }
    }
}
