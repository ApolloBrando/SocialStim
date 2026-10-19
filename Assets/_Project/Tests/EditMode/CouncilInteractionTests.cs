using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Council;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Top-level actions and three-step navigation remain separate from gameplay commands.</summary>
    public sealed class CouncilInteractionTests
    {
        private static CouncilController Controller(bool abstain = true) => new CouncilController(CouncilFactory.CreateScenario(
            new CouncilOptions(147, rounds: 3, abstain: abstain),
            new[] { "solarian", "decryptor", "physician", "archon", "infinitum" }
                .Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
            MissionTestContent.Roles(), TestContent.Lines(), new QuietStrategy()));

        [Test]
        public void SolarianHasOnlyTheThreeSpeechActionsAndPass()
        {
            var controller = Controller();
            Assert.That(controller.Options().Select(o => o.Title), Is.EqualTo(new[] { "CHAT", "ACCUSE", "DEFEND", "Stay silent" }));
            Assert.That(controller.Snapshot.Revision, Is.Zero);
        }

        [Test]
        public void ActionTargetPhrasingAndBackPreserveRevisionUntilThePhraseIsChosen()
        {
            var controller = Controller();
            string accuse = controller.Options().Single(o => o.Title == "ACCUSE").Id;
            Assert.That(controller.Choose(accuse, out _), Is.True);
            Assert.That(controller.Step, Is.EqualTo(InteractionStep.Target));
            Assert.That(controller.ChooseTarget(new CharacterId(1), 0, out _), Is.True);
            Assert.That(controller.Step, Is.EqualTo(InteractionStep.Phrasing));
            Assert.That(controller.Options().Count(o => o.Action != null), Is.LessThanOrEqualTo(3));
            Assert.That(controller.Snapshot.Revision, Is.Zero);
            Assert.That(controller.Choose(controller.Options().Single(o => o.Title == "Back").Id, out _), Is.True);
            Assert.That(controller.Step, Is.EqualTo(InteractionStep.Actions));
            Assert.That(controller.Snapshot.Revision, Is.Zero);
            Assert.That(controller.Choose(accuse, out _), Is.False);
        }

        [Test]
        public void AbstainIsAbsentWhenDisabledAndQuickModeSkipsPhrasing()
        {
            var controller = Controller(false); controller.QuickMode = true;
            Assert.That(controller.Choose(controller.Options().Single(o => o.Title == "ACCUSE").Id, out _), Is.True);
            Assert.That(controller.ChooseTarget(new CharacterId(1), 0, out _), Is.True);
            Assert.That(controller.Snapshot.Revision, Is.EqualTo(1));
            for (int i = 0; i < 2; i++)
                Assert.That(controller.Choose(controller.Options().Single(o => o.Title == "Stay silent").Id, out _), Is.True);
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(CouncilPhase.Vote));
            Assert.That(controller.Options().Any(o => o.Title == "Abstain"), Is.False);
        }

        private sealed class QuietStrategy : ICouncilStrategy
        {
            public CouncilAction Speak(CouncilKnowledge k, IRandomSource r) => new CouncilAction(CouncilActionKind.Pass);
            public CharacterId? Vote(CouncilKnowledge k, IRandomSource r) => null;
            public CharacterId? NightTarget(CouncilKnowledge k, IRandomSource r) => null;
        }
    }
}
