using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Actual core assets, mission pipeline, neutral precedence and configuration contracts.</summary>
    public sealed class MissionCoreRoleTests
    {
        private static NightActor Actor(int id, string role, bool selfHealUsed = false, bool backlashDue = false)
        { var spec = MissionTestContent.Role(role); return new NightActor(new CharacterId(id), spec, spec.Charges, selfHealUsed, backlashDue); }
        private static NightOrder Order(int actor, int? target) => new NightOrder(new CharacterId(actor), target.HasValue ? new CharacterId(target.Value) : (CharacterId?)null);
        private static NightResolver Resolver(bool backlash = true) => new NightResolver(policy: new NightPolicy(mission: true, backlash: backlash));

        [Test]
        public void ActualCoreAssetsDeclareNineRolesAndClaimPermissions()
        {
            var roles = MissionTestContent.Roles();
            Assert.That(roles.Count, Is.EqualTo(9));
            Assert.That(roles.Select(r => r.Id).Distinct().Count(), Is.EqualTo(9));
            var solarian = roles.Single(r => r.Id.Value == "solarian");
            var decryptor = roles.Single(r => r.Id.Value == "decryptor");
            var physician = roles.Single(r => r.Id.Value == "physician");
            var archon = roles.Single(r => r.Id.Value == "archon");
            Assert.That(ClaimPermissions.Available(solarian, roles), Is.Empty);
            Assert.That(ClaimPermissions.CanClaim(decryptor, decryptor), Is.True);
            Assert.That(ClaimPermissions.CanClaim(decryptor, physician), Is.False);
            Assert.That(ClaimPermissions.CanClaim(archon, decryptor), Is.True);
            Assert.That(ClaimPermissions.CanClaim(roles.Single(r => r.Id.Value == "infinitum"), decryptor), Is.True);
            Assert.That(ClaimPermissions.CanReport(decryptor, false), Is.False);
            Assert.That(ClaimPermissions.CanReport(archon, false), Is.True);
        }

        [TestCase("solarian", Readout.Clear)]
        [TestCase("archon", Readout.Corrupt)]
        [TestCase("aperture", Readout.Corrupt)]
        [TestCase("infinitum", Readout.Clear)]
        [TestCase("anarchy", Readout.Anomaly)]
        public void ReadoutsDiscloseOnlyApparentEvidence(string role, Readout expected)
        {
            var result = Resolver().Resolve(new[] { Actor(0, "decryptor"), Actor(1, role) }, new[] { Order(0, 1) });
            var report = result.Reports.Single(r => r.Kind == ReportKind.Investigation);
            Assert.That(report.Readout, Is.EqualTo(expected));
            Assert.That(report.Alignment, Is.Null);
        }

        [TestCase("solarian", Readout.Corrupt)]
        [TestCase("archon", Readout.Clear)]
        [TestCase("anarchy", Readout.Anomaly)]
        public void ForgeInvertsOnlyClearAndCorrupt(string role, Readout expected)
        {
            var result = Resolver().Resolve(new[] { Actor(0, "aperture"), Actor(1, "decryptor"), Actor(2, role) },
                new[] { Order(0, 2), Order(1, 2) });
            Assert.That(result.Reports.Single(r => r.Kind == ReportKind.Investigation).Readout, Is.EqualTo(expected));
        }

        [Test]
        public void PhysicianCancelsOneAttackAndReceivesAnAttackNotice()
        {
            var cast = new[] { Actor(0, "physician"), Actor(1, "archon"), Actor(2, "anarchy"), Actor(3, "solarian") };
            var one = Resolver().Resolve(cast, new[] { Order(0, 3), Order(1, 3) });
            Assert.That(one.Deaths, Is.Empty);
            Assert.That(one.Reports.Any(r => r.Recipient.Value == 0 && r.Kind == ReportKind.PatientAttacked), Is.True);
            var two = Resolver().Resolve(cast, new[] { Order(0, 3), Order(1, 3), Order(2, 3) });
            Assert.That(two.Deaths.Select(d => d.Value), Is.EquivalentTo(new[] { 3 }));
            var safe = Resolver().Resolve(cast, new[] { Order(0, 3) });
            Assert.That(safe.Reports.Any(r => r.Kind == ReportKind.PatientSafe), Is.True);
        }

        [Test]
        public void SelfHealIsOncePerRunAndBlockPreservesIt()
        {
            var cast = new[] { Actor(0, "physician"), Actor(1, "singer"), Actor(2, "archon") };
            var healed = Resolver().Resolve(cast, new[] { Order(0, 0), Order(2, 0) });
            Assert.That(healed.Deaths, Is.Empty);
            Assert.That(healed.SelfHealsSpent.Select(c => c.Value), Is.EquivalentTo(new[] { 0 }));
            var blocked = Resolver().Resolve(cast, new[] { Order(0, 0), Order(1, 0) });
            Assert.That(blocked.SelfHealsSpent, Is.Empty);
            var spent = Actor(0, "physician", selfHealUsed: true);
            Assert.That(NightLegality.IsLegal(spent, Order(0, 0), new[] { spent }, new NightPolicy(mission: true)), Is.False);
        }

        [Test]
        public void InfinitumSurvivesMultipleAttackersAndLullStopsASelectedKill()
        {
            var result = Resolver().Resolve(new[] { Actor(0, "archon"), Actor(1, "anarchy"), Actor(2, "infinitum") },
                new[] { Order(0, 2), Order(1, 2) });
            Assert.That(result.Deaths, Is.Empty);
            result = Resolver().Resolve(new[] { Actor(0, "singer"), Actor(1, "anarchy"), Actor(2, "solarian") },
                new[] { Order(0, 1), Order(1, 2) });
            Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.Visits.Any(v => v.Visitor.Value == 1), Is.False);
        }

        [Test]
        public void BacklashIsScheduledOnlyByAFatalTownShotAndResolvesTheFollowingNight()
        {
            var result = Resolver().Resolve(new[] { Actor(0, "nullifier"), Actor(1, "solarian") }, new[] { Order(0, 1) });
            Assert.That(result.Deaths.Select(c => c.Value), Is.EquivalentTo(new[] { 1 }));
            Assert.That(result.BacklashScheduled.Select(c => c.Value), Is.EquivalentTo(new[] { 0 }));
            var next = Resolver().Resolve(new[] { Actor(0, "nullifier", backlashDue: true), Actor(1, "singer") }, new[] { Order(1, 0) });
            Assert.That(next.Deaths.Select(c => c.Value), Is.EquivalentTo(new[] { 0 }));
            result = Resolver(false).Resolve(new[] { Actor(0, "nullifier"), Actor(1, "solarian") }, new[] { Order(0, 1) });
            Assert.That(result.BacklashScheduled, Is.Empty);
        }

        [Test]
        public void ForgeDoesNotInventOrChangePhysicianAttackNotices()
        {
            var result = Resolver().Resolve(new[] { Actor(0, "aperture"), Actor(1, "physician"), Actor(2, "solarian") },
                new[] { Order(0, 2), Order(1, 2) });
            Assert.That(result.Reports.Single(r => r.Recipient.Value == 1).Kind, Is.EqualTo(ReportKind.PatientSafe));
        }

        [Test]
        public void OutcomesHandleStealTownVictoryLastSurvivorAndStalemate()
        {
            var win = new MissionWinCondition();
            Assert.That(win.Evaluate(new[] { Actor(0, "archon"), Actor(1, "solarian"), Actor(2, "infinitum") }), Is.EqualTo(OutcomeKind.InfinitumVictory));
            Assert.That(win.Evaluate(new[] { Actor(0, "solarian"), Actor(1, "infinitum") }), Is.EqualTo(OutcomeKind.TownVictory));
            Assert.That(win.Evaluate(new[] { Actor(0, "anarchy") }), Is.EqualTo(OutcomeKind.LoneKillerVictory));
            Assert.That(win.Evaluate(new[] { Actor(0, "anarchy"), Actor(1, "infinitum") }), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(win.Evaluate(new[] { Actor(0, "infinitum") }), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(win.Evaluate(new NightActor[0]), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(win.Evaluate(new[] { Actor(0, "archon"), Actor(1, "solarian"), Actor(2, "anarchy") }), Is.EqualTo(OutcomeKind.Ongoing));
            Assert.That(win.Evaluate(new[] { Actor(0, "archon"), Actor(1, "solarian"), Actor(2, "infinitum") },
                parityMode: NeutralParityMode.CountAsTown), Is.EqualTo(OutcomeKind.Ongoing));
            Assert.That(win.Won(MissionTestContent.Role("infinitum"), true, false, OutcomeKind.TownVictory), Is.False);
        }

        [Test]
        public void SuccessionSelectsOneLivingEligibleEvilBeforeAnOutcomeCheck()
        {
            var living = new[] { Actor(0, "singer"), Actor(1, "aperture"), Actor(2, "solarian") };
            Assert.That(EvilSuccession.Successor(living, true), Is.EqualTo(new CharacterId(0)));
            Assert.That(EvilSuccession.Successor(living, false), Is.Null);
            Assert.That(EvilSuccession.Successor(living.Concat(new[] { Actor(3, "archon") }).ToList(), true), Is.Null);
        }

        [TestCase(5)]
        [TestCase(9)]
        [TestCase(16)]
        [TestCase(20)]
        public void PresetsAndChosenRolesResolveWithoutBreakingPopulationOrUniqueRoles(int count)
        {
            var roles = MissionTestContent.Roles();
            foreach (var role in roles)
            {
                var config = new SetupConfiguration(count, playerRole: role.Id);
                var first = SetupBuilder.Build(config, roles, 20, new SeededRandom(123));
                var again = SetupBuilder.Build(config, roles, 20, new SeededRandom(123));
                Assert.That(first.Roles.Count, Is.EqualTo(count));
                Assert.That(first.Roles.Any(r => r.Id == role.Id), Is.True);
                Assert.That(first.Roles.Select(r => r.Id), Is.EqualTo(again.Roles.Select(r => r.Id)));
                Assert.That(first.Town, Is.GreaterThan(first.Evil));
                Assert.That(first.Roles.Where(r => r.Unique).GroupBy(r => r.Id).Any(g => g.Count() > 1), Is.False);
            }
        }

        [Test]
        public void DisabledRolesLimitsAndCustomSlotsAreEnforced()
        {
            var roles = MissionTestContent.Roles();
            var disabled = new SetupConfiguration(9, roleCounts: new[] { new RoleCountRule(new RoleId("archon"), maximum: 0) });
            Assert.That(SetupBuilder.Error(disabled, roles, 20), Does.Contain("No evil killing role"));
            var custom = new SetupConfiguration(5, SetupPreset.Custom, evil: 1, neutral: 0, slots: new[] {
                new RoleSlot(new RoleId("archon")), new RoleSlot(new RoleId("decryptor")),
                new RoleSlot(new RoleId("solarian")), new RoleSlot(new RoleId("solarian")), new RoleSlot(new RoleId("solarian"))
            }, roleCounts: new[] { new RoleCountRule(new RoleId("solarian"), 3, 3) });
            Assert.That(SetupBuilder.Build(custom, roles, 20, new SeededRandom(1)).Roles.Count(r => r.Id.Value == "solarian"), Is.EqualTo(3));
            Assert.That(SetupBuilder.Error(new SetupConfiguration(9, evil: 5), roles, 20), Does.Contain("Evil count too high"));
            Assert.That(SetupBuilder.Error(new SetupConfiguration(21), roles, 20), Does.Contain("Not enough named"));
        }
    }
}
