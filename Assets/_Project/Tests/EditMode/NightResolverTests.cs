using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Interaction invariants of the ordered night pipeline.</summary>
    public sealed class NightResolverTests
    {
        private static NightActor Actor(int id, string role) => new NightActor(new CharacterId(id), TestContent.Roles().First(r => r.Id.Value == role), role == "survivor" ? 2 : -1);
        private static NightOrder Order(int actor, int target) => new NightOrder(new CharacterId(actor), new CharacterId(target));

        /// <summary>A blocked killer neither visits nor kills.</summary>
        [Test]
        public void BlockPreventsAttackAndVisit()
        {
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "interrupter"), Actor(1, "assassin"), Actor(2, "inspector") }, new[] { Order(0, 1), Order(1, 2) });
            Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.Visits.Any(v => v.Visitor.Value == 1), Is.False);
            Assert.That(result.Reports.Count(r => r.Recipient.Value == 1 && r.Kind == ReportKind.Blocked), Is.EqualTo(1));
        }

        /// <summary>Protection wins when attack equals defense.</summary>
        [Test]
        public void ProtectionStopsEqualAttack()
        {
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "physician"), Actor(1, "assassin"), Actor(2, "inspector") }, new[] { Order(0, 2), Order(1, 2) });
            Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.Visits.Count, Is.EqualTo(2));
            Assert.That(result.Reports.Any(r => r.Recipient.Value == 2 && r.Kind == ReportKind.Targeted && !r.Target.HasValue), Is.True);
        }

        /// <summary>Blocking a protector removes their protection.</summary>
        [Test]
        public void BlockedProtectionDoesNotSaveTarget()
        {
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "physician"), Actor(1, "assassin"), Actor(2, "inspector"), Actor(3, "interrupter") }, new[] { Order(0, 2), Order(1, 2), Order(3, 0) });
            Assert.That(result.Deaths.Select(d => d.Value), Is.EqualTo(new[] { 2 }));
        }

        /// <summary>Mutual blocks both apply under the documented simultaneous policy.</summary>
        [Test]
        public void MutualBlocksBothApply()
        {
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "interrupter"), Actor(1, "interrupter") }, new[] { Order(0, 1), Order(1, 0) });
            Assert.That(result.Visits.Count, Is.EqualTo(2));
            Assert.That(result.Reports.Count(r => r.Kind == ReportKind.Blocked), Is.EqualTo(2));
        }

        /// <summary>Self-protection consumes a charge only when unblocked and executed.</summary>
        [Test]
        public void BlockedSurvivorKeepsCharge()
        {
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "survivor"), Actor(1, "interrupter") }, new[] { Order(0, 0), Order(1, 0) });
            Assert.That(result.SpentCharges, Is.Empty);
        }

        /// <summary>An executed vest prevents a basic attack and consumes one charge.</summary>
        [Test]
        public void SurvivorVestSpendsOneChargeAndSaves()
        {
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "survivor"), Actor(1, "assassin") }, new[] { Order(0, 0), Order(1, 0) });
            Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.SpentCharges.Select(c => c.Value), Is.EqualTo(new[] { 0 }));
        }

        /// <summary>Investigation observes appearance and still resolves if its actor dies.</summary>
        [Test]
        public void DyingInvestigatorStillResolvesApparentAlignment()
        {
            RoleSpec disguised = new RoleSpec(new RoleId("disguised"), "Disguised", Faction.Antagonist,
                RoleCategory.AntagonistKilling, AbilityKind.Attack, 1, 0, 0, -1, Faction.Town);
            NightResolution result = new NightResolver().Resolve(new[] { Actor(0, "inspector"), new NightActor(new CharacterId(1), disguised, -1) }, new[] { Order(0, 1), Order(1, 0) });
            Assert.That(result.Deaths.Select(d => d.Value), Is.EqualTo(new[] { 0 }));
            Assert.That(result.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(Faction.Town));
        }

        /// <summary>Duplicate actor commands cannot grant extra attacks.</summary>
        [Test]
        public void DuplicateOrdersAreRejected() => Assert.Throws<ArgumentException>(() => new NightResolver().Resolve(
            new[] { Actor(0, "assassin"), Actor(1, "inspector") }, new[] { Order(0, 1), Order(0, 1) }));

        /// <summary>Self-target restrictions and exhausted resources apply to every command source.</summary>
        [Test]
        public void IllegalSelfTargetAndExhaustedChargesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new NightResolver().Resolve(new[] { Actor(0, "physician") }, new[] { Order(0, 0) }));
            var survivor = new NightActor(new CharacterId(0), Actor(0, "survivor").Role, 0);
            Assert.That(NightLegality.IsLegal(survivor, Order(0, 0), new[] { survivor }), Is.False);
            Assert.That(NightLegality.IsLegal(survivor, new NightOrder(survivor.Id, null), new[] { survivor }), Is.True);
        }

        /// <summary>The faction cannot submit two independent team kills.</summary>
        [Test]
        public void MultipleTeamKillsAreRejected() => Assert.Throws<ArgumentException>(() => new NightResolver().Resolve(
            new[] { Actor(0, "assassin"), Actor(1, "assassin"), Actor(2, "inspector") }, new[] { Order(0, 2), Order(1, 2) }));
    }
}
