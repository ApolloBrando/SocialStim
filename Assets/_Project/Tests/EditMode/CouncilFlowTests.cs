using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Modern loop, response windows, voting and disclosure tested through public ports.</summary>
    public sealed class CouncilFlowTests
    {
        private static CouncilSession Run(ICouncilStrategy strategy = null, SetupVisibility visibility = SetupVisibility.FactionCounts)
            => CouncilFactory.CreateScenario(new CouncilOptions(147, rounds: 3, visibility: visibility),
                new[] { "solarian", "decryptor", "physician", "archon", "infinitum" }
                    .Select((role, i) => new CastMemberSpec("Person" + i, new RoleId(role))),
                MissionTestContent.Roles(), TestContent.Lines(), strategy ?? new QuietStrategy());

        private static void Submit(CouncilSession run, CouncilAction action)
        { bool accepted = run.TrySubmit(action, out string error); Assert.That(accepted, Is.True, error); }

        [Test]
        public void PassingUsesOneWindowPerRoundThenPluralityVoteRevealsEachBallot()
        {
            var run = Run();
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(run.View().Round, Is.EqualTo(i));
                Submit(run, new CouncilAction(CouncilActionKind.Pass));
            }
            Assert.That(run.View().Phase, Is.EqualTo(CouncilPhase.Vote));
            Submit(run, new CouncilAction(CouncilActionKind.Vote, new CharacterId(3)));
            var view = run.View();
            Assert.That(view.Phase, Is.EqualTo(CouncilPhase.Elimination));
            Assert.That(view.Events.Count(e => e.Kind == CouncilEventKind.Vote), Is.EqualTo(5));
            Assert.That(view.Knowledge.Characters.Single(c => c.Id.Value == 3).Alive, Is.False);
            Submit(run, new CouncilAction(CouncilActionKind.Continue));
            Assert.That(run.View().Outcome, Is.EqualTo(OutcomeKind.TownVictory));
            Assert.That(run.View().FinalRoles.Count, Is.EqualTo(5));
        }

        [Test]
        public void DirectAccusationGivesAPriorityResponseWithoutUsingTheNormalWindow()
        {
            var run = Run(new AccusingStrategy());
            Assert.That(run.View().Respond, Is.True);
            Assert.That(run.View().WindowsLeft, Is.EqualTo(1));
            Assert.That(run.TrySubmit(new CouncilAction(CouncilActionKind.Chat), out _), Is.False);
            Submit(run, new CouncilAction(CouncilActionKind.Pass));
            Assert.That(run.View().WindowsLeft, Is.EqualTo(1));
            Submit(run, new CouncilAction(CouncilActionKind.Pass));
            Assert.That(run.View().Respond, Is.True);
        }

        [Test]
        public void PreviewAndInvalidPhrasingDoNotAdvanceRulesOrChangeFutureTranscript()
        {
            var first = Run(); var second = Run();
            var action = new CouncilAction(CouncilActionKind.Chat);
            for (int i = 0; i < 20; i++) first.Phrases(action);
            Assert.That(first.View().Revision, Is.Zero);
            Assert.That(first.TrySubmit(action.WithLine("nonexistent"), out _), Is.False);
            Submit(first, action); Submit(second, action);
            Assert.That(first.View().Events.Select(e => e.Text), Is.EqualTo(second.View().Events.Select(e => e.Text)));
        }

        [Test]
        public void HiddenSetupAndInProgressAfterActionContainNoForeignSecrets()
        {
            var run = Run(visibility: SetupVisibility.Hidden);
            var view = run.View();
            Assert.That(view.Knowledge.Counts, Is.Null);
            Assert.That(view.Knowledge.RoleCounts, Is.Null);
            Assert.That(view.FinalRoles, Is.Empty);
            Assert.That(view.AfterAction, Is.Empty);
            Assert.That(view.Events.Select(e => e.Sequence), Is.EqualTo(Enumerable.Range(0, view.Events.Count)));
            Assert.That(view.Knowledge.Characters.All(c => !c.RevealedRole.HasValue), Is.True);
        }

        [Test]
        public void NoAbilityPlayerCannotSmuggleAClaimThroughChat()
        {
            var run = Run();
            Assert.That(run.TrySubmit(new CouncilAction(CouncilActionKind.Claim, claim: new RoleId("decryptor")), out _), Is.False);
            Assert.That(run.TrySubmit(new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.ClaimRole), out _), Is.False);
            Assert.That(run.View().Revision, Is.Zero);
        }

        [Test]
        public void SameSeedAndInputsProduceTheSameModernRun()
        {
            var roles = MissionTestContent.Roles();
            CouncilSession create() => CouncilFactory.Create(new CouncilOptions(32, rounds: 3),
                new SetupConfiguration(9), roles, TestContent.Names(), TestContent.Lines(), new QuietStrategy());
            var first = create(); var second = create();
            for (int i = 0; i < 3; i++)
            { Submit(first, new CouncilAction(CouncilActionKind.Pass)); Submit(second, new CouncilAction(CouncilActionKind.Pass)); }
            Assert.That(first.View().Events.Select(e => e.Text), Is.EqualTo(second.View().Events.Select(e => e.Text)));
        }

        private class QuietStrategy : ICouncilStrategy
        {
            public virtual CouncilAction Speak(CouncilKnowledge k, IRandomSource r) => new CouncilAction(CouncilActionKind.Pass);
            public CharacterId? Vote(CouncilKnowledge k, IRandomSource r) => new CharacterId(3);
            public CharacterId? NightTarget(CouncilKnowledge k, IRandomSource r) => null;
        }
        private sealed class AccusingStrategy : QuietStrategy
        {
            public override CouncilAction Speak(CouncilKnowledge k, IRandomSource r)
                => new CouncilAction(CouncilActionKind.Accuse, new CharacterId(0), ReasonType.GutFeeling);
        }
    }
}
