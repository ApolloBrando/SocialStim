using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Observer boundaries, hard constraints, late evidence and seeded skill/policy behavior.</summary>
    public sealed class CouncilInferenceTests
    {
        private static CouncilKnowledge Knowledge(IEnumerable<CouncilEvent> events = null, IEnumerable<NightReport> results = null,
            IReadOnlyDictionary<RoleId, int> roleCounts = null, int skill = 5)
        {
            var cast = Enumerable.Range(0, 5).Select(i => new KnownCharacter(new CharacterId(i), "P" + i, true, 35,
                revealedRole: i == 4 ? new RoleId("solarian") : (RoleId?)null, revealedFaction: i == 4 ? Faction.Town : (Faction?)null)).ToList();
            return new CouncilKnowledge(new CharacterId(0), MissionTestContent.Role("decryptor"), Personality.Cautious, 2, 3, -1, false,
                cast, (events ?? Enumerable.Empty<CouncilEvent>()).ToList(), (results ?? Enumerable.Empty<NightReport>()).ToList(),
                MissionTestContent.Roles(), new SetupKnowledge(3, 1, 1), roleCounts,
                stats: cast.ToDictionary(c => c.Id, c => new CharacterStats(skill, skill, skill, skill, skill, skill)),
                policy: new AiPolicy(AiDifficulty.Hard), seed: 147);
        }
        [Test]
        public void PosteriorNormalizesAndPinsOwnRoleAndPublicReveals()
        {
            var result = new CouncilInference().Evaluate(Knowledge());
            Assert.That(result.Samples, Is.EqualTo(160));
            foreach (var belief in result.Beliefs.Values)
                Assert.That(belief.Roles.Values.Sum(), Is.EqualTo(1).Within(0.000001));
            Assert.That(result.Beliefs[new CharacterId(0)].Roles[new RoleId("decryptor")], Is.EqualTo(1).Within(0.000001));
            Assert.That(result.Beliefs[new CharacterId(4)].Town, Is.EqualTo(1).Within(0.000001));
        }
        [Test]
        public void FullRoleCountsExcludeAbsentDefinitions()
        {
            var counts = new Dictionary<RoleId, int> { [new RoleId("decryptor")] = 1, [new RoleId("solarian")] = 2,
                [new RoleId("archon")] = 1, [new RoleId("infinitum")] = 1 };
            var result = new CouncilInference().Evaluate(Knowledge(roleCounts: counts));
            Assert.That(result.Beliefs.Values.All(b => b.Roles[new RoleId("aperture")] == 0), Is.True);
        }
        [Test]
        public void ClearResultDoesNotMeanTownAndAnomalyStronglyIdentifiesTheKiller()
        {
            var clear = new CouncilInference().Evaluate(Knowledge(results: new[] { new NightReport(new CharacterId(0),
                ReportKind.Investigation, new CharacterId(1), night: 1, readout: Readout.Clear) }));
            Assert.That(clear.Beliefs[new CharacterId(1)].Roles[new RoleId("infinitum")], Is.GreaterThan(0));
            var anomaly = new CouncilInference().Evaluate(Knowledge(results: new[] { new NightReport(new CharacterId(0),
                ReportKind.Investigation, new CharacterId(1), night: 1, readout: Readout.Anomaly) }));
            Assert.That(anomaly.Beliefs[new CharacterId(1)].Killer, Is.GreaterThan(0.9));
        }
        [Test]
        public void TheSameAuthorizedContextAlwaysHasTheSamePosterior()
        {
            var engine = new CouncilInference();
            var first = engine.Evaluate(Knowledge()); var second = engine.Evaluate(Knowledge());
            Assert.That(first.Beliefs.Select(b => b.Value.Evil), Is.EqualTo(second.Beliefs.Select(b => b.Value.Evil)));
            Assert.That(first.EffectiveSamples, Is.EqualTo(second.EffectiveSamples));
        }
        [Test]
        public void PersuasionDependsOnReasonStatsAndCredibilityRatherThanAFlatBonus()
        {
            var evidence = new[] { new CouncilEvent(0, 2, 2, CouncilEventKind.Speech, "A view.", new CharacterId(2),
                new CharacterId(1), SpeechIntent.Accuse, ReasonType.GutFeeling) };
            var low = new CouncilInference().Evaluate(Knowledge(evidence, skill: 1));
            var high = new CouncilInference().Evaluate(Knowledge(evidence, skill: 10));
            Assert.That(low.Beliefs[new CharacterId(1)].Hostile, Is.Not.EqualTo(high.Beliefs[new CharacterId(1)].Hostile));
            Assert.That(high.Beliefs[new CharacterId(2)].Affinity, Is.Zero);
        }
        [Test]
        public void TownNeverVotesForPubliclyConfirmedTown()
        {
            var raw = Knowledge(); var inference = new CouncilInference().Evaluate(raw);
            var k = new CouncilKnowledge(raw.Self, raw.OwnRole, raw.Traits, raw.Day, raw.Round, raw.Charges, raw.SelfHealUsed,
                raw.Characters, raw.Events, raw.Results, raw.Catalog, raw.Counts, raw.RoleCounts, stats: raw.Stats,
                policy: raw.Policy, seed: raw.Seed, inference: inference);
            for (uint seed = 1; seed <= 100; seed++)
                Assert.That(new CouncilNpcStrategy().Vote(k, new SeededRandom(seed)), Is.Not.EqualTo(new CharacterId(4)));
        }
        [Test]
        public void SkillsUseEqualBudgetsAndReproduceBySeed()
        {
            var first = CharacterStats.Generate(new SeededRandom(147)); var second = CharacterStats.Generate(new SeededRandom(147));
            Assert.That(first.Total, Is.EqualTo(30));
            foreach (StatKind kind in Enum.GetValues(typeof(StatKind)))
                Assert.That(first.Get(kind), Is.EqualTo(second.Get(kind)));
            Assert.That(new AiPolicy(AiDifficulty.Brutal).Samples, Is.GreaterThan(new AiPolicy(AiDifficulty.Easy).Samples));
        }
    }
}
