using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Input;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Actual legal navigation, selected authored speech, claim permissions and terminal information scope.</summary>
    public sealed class InteractionTests
    {
        private static RunSession Run(string role = "inspector", int days = 30) => RunFactory.CreateScenario(TestContent.Text,
            new RunOptions(123, discussionRounds: 3, maximumDays: days),
            new[] { role, "citizen", "assassin", "physician", "citizen" }.Select((r, i) => new CastMemberSpec("Person" + i, new RoleId(r))),
            AdvancedRoleTests.Roles(), TestContent.Lines(), new ScenarioStrategy());
        [Test]
        public void NavigationAndPreviewAreReadOnlyAndActualSelectedWordsAreDelivered()
        {
            var run = Run(); var controller = new GameController(run, AdvancedRoleTests.Roles());
            var request = controller.Request();
            var targets = InteractionMenu.Targets(request, InteractionAction.Accuse);
            Assert.That(targets.Any(id => id.Value == 0), Is.False);
            var option = InteractionMenu.Options(request, InteractionAction.Accuse, targets[0]).First();
            Assert.That(option.Phrasings.Count, Is.InRange(1, 3));
            for (int i = 0; i < 10; i++) Assert.That(controller.Request(), Is.SameAs(request));
            Assert.That(run.Revision, Is.Zero);
            var selected = option.Phrasings.Last();
            Assert.That(controller.TryChoose(option.Id + "|" + selected.Id, out string error), Is.True, error);
            Assert.That(run.GetPlayerView().Transcript.First(o => o.Speaker == new CharacterId(0)).Text,
                Is.EqualTo("Person0: " + selected.Text));
            int revision = run.Revision;
            Assert.That(controller.TryChoose(option.Id + "|" + selected.Id, out _), Is.False);
            Assert.That(run.Revision, Is.EqualTo(revision));
        }
        [Test]
        public void ForgedPhrasingDoesNotMutateStateOrSelectAnotherIntentsLine()
        {
            var run = Run(); var controller = new GameController(run, AdvancedRoleTests.Roles());
            var option = controller.Request().Options.First(o => o.Intent == SpeechIntent.Accuse);
            Assert.That(controller.TryChoose(option.Id + "|ClaimRole.0", out _), Is.False);
            Assert.That(run.Revision, Is.Zero);
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(1)).WithDialogue("unknown"), out _), Is.False);
            Assert.That(run.Revision, Is.Zero);
        }
        [TestCase("citizen", 0)]
        [TestCase("inspector", 1)]
        public void HonestRolesOnlyReceiveTheirPermittedTruthfulClaims(string role, int count)
        {
            var run = Run(role); var request = new GameController(run, AdvancedRoleTests.Roles()).Request();
            var claims = InteractionMenu.Options(request, InteractionAction.Claim, null);
            Assert.That(claims.Count, Is.EqualTo(count));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")), out _), Is.False);
            Assert.That(run.GetPlayerView().OwnRole.CanBluff, Is.False);
        }
        [Test]
        public void EvilCanBluffAndInvalidNpcBluffsUseTheSamePermissionCheck()
        {
            var run = Run("assassin"); var controller = new GameController(run, AdvancedRoleTests.Roles());
            Assert.That(InteractionMenu.Options(controller.Request(), InteractionAction.Claim, null).Count, Is.GreaterThan(1));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector")), out _), Is.True);
            var spy = new InvalidTownBluffer();
            var townRun = RunFactory.CreateScenario(TestContent.Text, new RunOptions(99),
                new[] { "citizen", "inspector", "assassin" }.Select((r, i) => new CastMemberSpec("Person" + i, new RoleId(r))),
                AdvancedRoleTests.Roles(), TestContent.Lines(), spy);
            Assert.That(townRun.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.True);
            Assert.That(townRun.GetPlayerView().Transcript.Any(o => o.Speaker == new CharacterId(1) && o.Intent == SpeechIntent.ClaimRole), Is.False);
        }
        [Test]
        public void TrueRoleReviewIsAbsentUntilTerminalAndIncludesRolesAfterward()
        {
            var run = Run(days: 1); var controller = new GameController(run, AdvancedRoleTests.Roles());
            for (int i = 0; i < 100 && run.GetPlayerView().Phase != GamePhase.Ended; i++)
            {
                Assert.That(run.GetPlayerView().AfterAction, Is.Empty);
                var request = controller.Request();
                Assert.That(controller.TryChoose(request.Options[0].Id, out string error), Is.True, error);
            }
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Ended));
            Assert.That(run.GetPlayerView().AfterAction.Any(line => line.Contains("Person2: assassin")), Is.True);
        }
        [Test]
        public void HonestResultOptionsComeOnlyFromThePlayersActualReadout()
        {
            var run = Run();
            for (int i = 0; i < 3; i++) Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Vote(null), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Continue(), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Night(new CharacterId(2)), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Continue(), out _), Is.True);
            var request = new GameController(run, AdvancedRoleTests.Roles()).Request();
            Assert.That(request.Options.Where(o => o.Intent == SpeechIntent.ReportInvestigation).Count(), Is.EqualTo(1));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ReportInvestigation, new CharacterId(2), faction: Faction.Town), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ReportInvestigation, new CharacterId(2), faction: Faction.Antagonist), out _), Is.True);
        }
        [Test]
        public void ObservationRoundDescribesTheSpokenWindowRatherThanTheNextWindow()
        {
            var run = Run(); Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.True);
            Assert.That(run.GetPlayerView().DiscussionRound, Is.EqualTo(2));
            Assert.That(run.GetPlayerView().Transcript.Where(o => o.Cue == GameCue.SpeechDelivered).All(o => o.Round == 1), Is.True);
        }
        private sealed class InvalidTownBluffer : SocialStim.Rules.Reasoning.INpcStrategy
        {
            public GameCommand Speak(SocialStim.Rules.Reasoning.KnowledgeView k, SocialStim.Rules.Randomness.IRandomSource r)
                => GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician"));
            public CharacterId? Nominate(SocialStim.Rules.Reasoning.KnowledgeView k, SocialStim.Rules.Randomness.IRandomSource r) => null;
            public Verdict Judge(SocialStim.Rules.Reasoning.KnowledgeView k, CharacterId d, SocialStim.Rules.Randomness.IRandomSource r) => Verdict.Innocent;
            public CharacterId? NightTarget(SocialStim.Rules.Reasoning.KnowledgeView k, SocialStim.Rules.Randomness.IRandomSource r) => null;
        }
    }
}
