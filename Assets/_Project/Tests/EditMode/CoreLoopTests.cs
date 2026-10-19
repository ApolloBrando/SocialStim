using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Victory;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Phase, command, vote, outcome and input-generation contracts.</summary>
    public sealed class CoreLoopTests
    {
        /// <summary>Execution cannot be entered directly from discussion.</summary>
        [Test]
        public void PhaseMachineRejectsSkippedTrialStages() => Assert.Throws<InvalidOperationException>(() => new PhaseMachine().MoveTo(GamePhase.Execution));

        /// <summary>A tie does not nominate or convict; abstentions do not grant guilt.</summary>
        [Test]
        public void VotingRequiresStrictMajorityAndGuiltyExcess()
        {
            Assert.That(VoteResolver.Nominee(new CharacterId?[] { new CharacterId(1), new CharacterId(1), null, null }, 4), Is.Null);
            Assert.That(VoteResolver.Nominee(new CharacterId?[] { new CharacterId(1), new CharacterId(1), new CharacterId(1), null }, 4), Is.EqualTo(new CharacterId(1)));
            Assert.That(VoteResolver.IsGuilty(new[] { Verdict.Guilty, Verdict.Innocent, Verdict.Abstain }), Is.False);
            Assert.That(VoteResolver.IsGuilty(new[] { Verdict.Abstain, Verdict.Abstain }), Is.False);
            Assert.That(VoteResolver.IsGuilty(new[] { Verdict.Guilty, Verdict.Abstain, Verdict.Abstain }), Is.True);
        }

        /// <summary>An illegal command neither changes revision nor consumes the speech round.</summary>
        [Test]
        public void IllegalInputLeavesStateUnchanged()
        {
            RunSession run = TestContent.Scenario("inspector", "physician", "interrupter", "assassin", "survivor");
            Assert.That(run.TrySubmit(GameCommand.Night(new CharacterId(1)), out _), Is.False);
            Assert.That(run.GetPlayerView().Revision, Is.Zero);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Discussion));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(99)), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.True);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Nomination));
        }

        /// <summary>Old input options cannot act in the next request.</summary>
        [Test]
        public void StaleOptionIdIsRejected()
        {
            var controller = new GameController(TestContent.Scenario("inspector", "physician", "interrupter", "assassin", "survivor"), TestContent.Roles());
            string id = controller.Request().Options.First().Id;
            Assert.That(controller.TryChoose(id, out _), Is.True);
            int revision = controller.Request().View.Revision;
            Assert.That(controller.TryChoose(id, out _), Is.False);
            Assert.That(controller.Request().View.Revision, Is.EqualTo(revision));
        }

        /// <summary>Faction victory, draws, and the turn cap have distinct outcomes.</summary>
        [Test]
        public void StarterOutcomesHandleNeutralAndAllDeadCases()
        {
            var condition = new StarterWinCondition();
            Assert.That(condition.Evaluate(new[] { Faction.Town, Faction.Neutral }, 1, 30), Is.EqualTo(OutcomeKind.TownVictory));
            Assert.That(condition.Evaluate(new[] { Faction.Antagonist, Faction.Town, Faction.Neutral }, 1, 30), Is.EqualTo(OutcomeKind.AntagonistVictory));
            Assert.That(condition.Evaluate(new[] { Faction.Neutral }, 1, 30), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(condition.Evaluate(new Faction[0], 1, 30), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(condition.Evaluate(new[] { Faction.Town, Faction.Town, Faction.Antagonist }, 30, 30), Is.EqualTo(OutcomeKind.Draw));
        }

        /// <summary>Only consistent speech payloads may introduce claims.</summary>
        [Test]
        public void DisguisedClaimPayloadIsRejected()
        {
            RunSession run = TestContent.Scenario("inspector", "physician", "interrupter", "assassin", "survivor");
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.Defend, role: new RoleId("assassin")), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ReportInvestigation, new CharacterId(1), faction: Faction.Town), out _), Is.False);
        }
    }
}
