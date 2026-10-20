using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Utility decisions and private team coordination from restricted observations.</summary>
    public sealed class SocialStrategyTests
    {
        private static KnowledgeView View(Personality traits = Personality.None, int exposure = 0, RoleSpec role = null,
            RoleId? claim = null, EvidenceRecord[] evidence = null) => new KnowledgeView(new CharacterId(0), role ?? TestContent.Roles()[0],
                traits, -1, 2, new[] {
                    new KnownCharacter(new CharacterId(0), "Self", true, 10, claim: claim),
                    new KnownCharacter(new CharacterId(1), "Other", true, 30, trust: 50, factionEstimate: new FactionEstimate(7000, 2000, 1000)),
                    new KnownCharacter(new CharacterId(2), "Ally", true, 10, teammate: true, trust: 65),
                }, Array.Empty<NightReport>(), evidence, exposure, TestContent.Roles());

        /// <summary>Visible public pressure makes an honest observer defend themselves.</summary>
        [Test]
        public void HighExposureSelectsDefense() => Assert.That(new SocialNpcStrategy().Speak(View(exposure: 90), new SeededRandom(5)).Intent, Is.EqualTo(SpeechIntent.Defend));

        /// <summary>A prior bluff is retained rather than silently switching to another role.</summary>
        [Test]
        public void AntagonistKeepsTheirExistingBluff()
        {
            var strategy = new SocialNpcStrategy();
            for (uint seed = 1; seed <= 50; seed++)
            {
                var command = strategy.Speak(View(role: TestContent.Roles()[3], claim: new RoleId("physician")), new SeededRandom(seed));
                if (command.ClaimedRole.HasValue) Assert.That(command.ClaimedRole.Value, Is.EqualTo(new RoleId("physician")));
                if (command.Intent == SpeechIntent.Accuse || command.Intent == SpeechIntent.CallForTrial || command.Intent == SpeechIntent.Deflect
                    || command.Intent == SpeechIntent.Disagree || command.Intent == SpeechIntent.ReportInvestigation)
                    Assert.That(command.Target, Is.Not.EqualTo(new CharacterId(2)));
            }
        }

        /// <summary>Already reported subjects are not given another fabricated result for the same night.</summary>
        [Test]
        public void FabricationDoesNotContradictSameNightHistory()
        {
            var prior = new EvidenceRecord(0, 2, EvidenceKind.InvestigationClaim, new CharacterId(0), new CharacterId(1),
                SpeechIntent.ReportInvestigation, alignment: Faction.Town, night: 1);
            var command = new SocialNpcStrategy().Speak(View(role: TestContent.Roles()[3], claim: new RoleId("inspector"), evidence: new[] { prior }), new SeededRandom(6));
            Assert.That(command.Intent == SpeechIntent.ReportInvestigation && command.Target == new CharacterId(1), Is.False);
        }

        /// <summary>Hard permitted knowledge overrides suspicion and historical grudges for Town.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TownDoesNotCondemnOrShootConfirmedTown(bool basic)
        {
            var role = TestContent.Role("vigilante", Faction.Town, RoleCategory.TownKilling, AbilityKind.Attack, attack: 1, charges: 2);
            var confirmed = new CharacterId(1);
            var view = new KnowledgeView(new CharacterId(0), role, Personality.Brave, 2, 2, new[] {
                new KnownCharacter(new CharacterId(0), "Self", true, 0),
                new KnownCharacter(confirmed, "Confirmed", true, 100, factionEstimate: new FactionEstimate(10000, 0, 0), knownFaction: Faction.Town),
            }, System.Array.Empty<NightReport>());
            INpcStrategy strategy = basic ? (INpcStrategy)new BasicNpcStrategy() : new SocialNpcStrategy();
            Assert.That(strategy.Nominate(view, new SeededRandom(4)), Is.Null);
            Assert.That(strategy.Judge(view, confirmed, new SeededRandom(4)), Is.EqualTo(SocialStim.Rules.Commands.Verdict.Innocent));
            Assert.That(strategy.NightTarget(view, new SeededRandom(4)), Is.Null);
        }

        /// <summary>Rounded heuristic confidence is not a hard alignment fact.</summary>
        [Test]
        public void RoundedEstimateDoesNotMasqueradeAsConfirmedTown()
        {
            var target = new KnownCharacter(new CharacterId(1), "Unconfirmed", true, 100,
                factionEstimate: new FactionEstimate(10000, 0, 0));
            Assert.That(target.KnownFaction, Is.Null);
            var view = new KnowledgeView(new CharacterId(0), TestContent.Roles()[0], Personality.None, -1, 2,
                new[] { new KnownCharacter(new CharacterId(0), "Self", true, 0), target }, System.Array.Empty<NightReport>());
            Assert.That(new SocialNpcStrategy().Nominate(view, new SeededRandom(4)), Is.EqualTo(target.Id));
        }

        /// <summary>Plan selection is input-order independent and preserves a player killer's explicit pass.</summary>
        [Test]
        public void TeamConsensusAndPlayerOverrideAreDeterministic()
        {
            var planner = new TeamNightPlanner();
            var proposals = new[] { new TeamProposal(new CharacterId(0), new CharacterId(3), 20),
                new TeamProposal(new CharacterId(1), new CharacterId(4), 50), new TeamProposal(new CharacterId(2), new CharacterId(3), 10) };
            Assert.That(planner.Plan(proposals).Target, Is.EqualTo(new CharacterId(3)));
            Assert.That(planner.Plan(proposals.Reverse()).Target, Is.EqualTo(new CharacterId(3)));
            Assert.That(planner.Plan(proposals, new CharacterId(1), null).Target, Is.Null);
            Assert.That(planner.Plan(proposals, new CharacterId(1), null).Killer, Is.EqualTo(new CharacterId(1)));
        }

        /// <summary>Team plans and foreign private evidence are removed from nonauthorized snapshots.</summary>
        [Test]
        public void TeamPlanCannotLeakToTown()
        {
            var plan = new TeamNightPlanner().Plan(new[] { new TeamProposal(new CharacterId(1), new CharacterId(2)) });
            var foreign = new EvidenceRecord(0, 2, EvidenceKind.PrivateInvestigation, new CharacterId(1), new CharacterId(2), alignment: Faction.Antagonist);
            var town = new KnowledgeView(new CharacterId(0), TestContent.Roles()[0], Personality.None, -1, 2,
                Array.Empty<KnownCharacter>(), Array.Empty<NightReport>(), new[] { foreign }, teamPlan: plan);
            Assert.That(town.TeamPlan, Is.Null); Assert.That(town.Evidence, Is.Empty);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void TownKillerTargetsStrongestSuspectAndPassesWithoutEnoughEvidence(bool basic)
        {
            var role = TestContent.Role("guard", Faction.Town, RoleCategory.TownKilling, AbilityKind.Attack, attack: 1, charges: 2);
            INpcStrategy strategy = basic ? (INpcStrategy)new BasicNpcStrategy() : new SocialNpcStrategy();
            foreach (int high in new[] { 30, 90 })
            {
                var view = new KnowledgeView(new CharacterId(0), role, Personality.None, 2, 2, new[] {
                    new KnownCharacter(new CharacterId(0), "Self", true, 0),
                    new KnownCharacter(new CharacterId(1), "Low suspicion", true, 5),
                    new KnownCharacter(new CharacterId(2), "High suspicion", true, high),
                }, Array.Empty<NightReport>());
                Assert.That(strategy.NightTarget(view, new SeededRandom(4)),
                    Is.EqualTo(high >= 60 ? new CharacterId(2) : (CharacterId?)null));
            }
        }

        [Test]
        public void BasicStrategyBluffsWithTheActualCustomCatalogue()
        {
            var town = TestContent.Role("custom-investigator", Faction.Town, RoleCategory.TownInvestigative, AbilityKind.Investigate);
            var evil = TestContent.Role("custom-killer", Faction.Antagonist, RoleCategory.AntagonistKilling, AbilityKind.Attack, attack: 1);
            var view = new KnowledgeView(new CharacterId(0), evil, Personality.None, -1, 1, new[] {
                new KnownCharacter(new CharacterId(0), "Self", true, 0),
                new KnownCharacter(new CharacterId(1), "Other", true, 0),
            }, Array.Empty<NightReport>(), roleCatalog: new[] { town, evil });
            int claims = 0;
            for (uint seed = 1; seed <= 50; seed++)
            {
                var command = new BasicNpcStrategy().Speak(view, new SeededRandom(seed));
                if (!command.ClaimedRole.HasValue) continue;
                claims++;
                Assert.That(command.ClaimedRole.Value, Is.EqualTo(town.Id));
            }
            Assert.That(claims, Is.GreaterThan(0));
        }
    }
}
