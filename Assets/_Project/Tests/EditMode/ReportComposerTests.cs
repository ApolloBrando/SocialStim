using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Application.Input;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    public sealed class ReportComposerTests
    {
        private static CharacterId Id(int id) => new CharacterId(id);
        private static void Submit(RunSession run, GameCommand command) => Assert.That(run.TrySubmit(command, out string error), Is.True, error);
        private static RunSession Ready(string claim)
        {
            var run = RunFactory.CreateScenario(new RunOptions(5, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "survivor", "citizen", "inspector", "physician", "assassin" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                DiscussionRevisionTests.AuthoredRoles(), TestContent.AuthoredLines(), new ScenarioStrategy());
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId(claim)));
            Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null)); Submit(run, GameCommand.Continue());
            return run;
        }
        private static void Choose(GameController controller, string label)
            => Assert.That(controller.TryChoose(controller.Request().Options.Single(o => o.Label == label).Id, out string error), Is.True, error);
        [TestCase("inspector", "Report Mafia", ReportKind.Investigation)]
        [TestCase("physician", "Report protection applied", ReportKind.ProtectionApplied)]
        [TestCase("vigilante", "Report that the attack failed", ReportKind.AttackFailed)]
        [TestCase("coordinator", "Report no visitors", ReportKind.NoVisitors)]
        public void BluffCanNameADifferentTargetAndRoleSpecificResultWithoutSpendingNavigationTurns(string claim, string result, ReportKind expected)
        {
            var run = Ready(claim); var controller = new GameController(run, DiscussionRevisionTests.AuthoredRoles());
            var before = run.GetPlayerView(); var old = controller.Request().Options.Single(o => o.Label == "Fabricate report").Id;
            Choose(controller, "Fabricate report");
            Assert.That(controller.TryChoose(old, out _), Is.False, "Navigation IDs must also reject stale selections.");
            Assert.That(controller.Request().CompositionPrompt, Does.Contain("Night 1"));
            var target = controller.Request().Options.Single(o => o.Target == Id(2));
            Assert.That(controller.TryChoose(target.Id, out _), Is.True);
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(before.Revision));
            Assert.That(run.GetPlayerView().Transcript, Is.EqualTo(before.Transcript));
            Choose(controller, result);
            var report = run.GetPlayerView().Transcript.Last(o => o.Speaker == Id(0) && o.NightReport != null).NightReport;
            Assert.That(report.Target, Is.EqualTo(Id(2))); Assert.That(report.Role, Is.EqualTo(new RoleId(claim)));
            Assert.That(report.Results.Single().Kind, Is.EqualTo(expected));
            Assert.That(run.GetPlayerView().LastNightAction.Target, Is.Null, "Fabricating cannot rewrite the real night action.");
            Assert.That(run.PlayerKnowledgeForDiagnostics().Evidence.Any(e => e.Kind == SocialStim.Rules.Reasoning.EvidenceKind.NightActionClaim && e.Actor == Id(0)), Is.True);
        }
        [Test]
        public void LookoutCanToggleMultipleVisitorsAndCancelWithoutPublishing()
        {
            var run = Ready("coordinator"); var controller = new GameController(run, DiscussionRevisionTests.AuthoredRoles());
            int revision = run.GetPlayerView().Revision;
            Choose(controller, "Fabricate report"); Choose(controller, "Back");
            Assert.That(controller.Request().CompositionPrompt, Is.Null); Assert.That(run.GetPlayerView().Revision, Is.EqualTo(revision));
            Choose(controller, "Fabricate report");
            Assert.That(controller.TryChoose(controller.Request().Options.Single(o => o.Target == Id(1)).Id, out _), Is.True);
            Choose(controller, "Choose the visitors I claim to have seen");
            foreach (var id in new[] { Id(2), Id(3), Id(2), Id(4) })
                Assert.That(controller.TryChoose(controller.Request().Options.Single(o => o.Target == id).Id, out _), Is.True);
            Assert.That(controller.Request().CompositionPrompt, Does.Contain("N3, N4"));
            Choose(controller, "Publish report · 2 visitors");
            var report = run.GetPlayerView().Transcript.Last(o => o.Speaker == Id(0) && o.NightReport != null).NightReport;
            Assert.That(report.Results.Select(r => r.Target), Is.EquivalentTo(new[] { (CharacterId?)Id(3), Id(4) }));
            Assert.That(report.Results.All(r => r.Subject == Id(1)), Is.True);
        }
        [Test]
        public void RosterShowsClaimsAndCounterclaimsAtTheirDialogueBoundary()
        {
            var view = Ready("inspector").GetPlayerView();
            foreach (var line in view.Transcript.Where(o => o.Intent == SpeechIntent.ClaimRole || o.Intent == SpeechIntent.CounterClaim))
            {
                int index = view.Transcript.ToList().IndexOf(line);
                Assert.That(RosterInteractions.VisibleParticipant(view, line.Speaker.Value, index).ClaimedRole, Is.Null);
                Assert.That(RosterInteractions.VisibleParticipant(view, line.Speaker.Value, index + 1).ClaimedRole, Is.EqualTo(line.ClaimedRole));
            }
            Assert.That(view.Transcript.Any(o => o.Intent == SpeechIntent.CounterClaim), Is.True);
        }
        [Test]
        public void RosterDoesNotRevealAnUpcomingNightDeathWhileEarlierSpeechIsPlaying()
        {
            var run = RunFactory.CreateScenario(new RunOptions(5, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "citizen", "inspector", "assassin", "physician", "survivor" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                DiscussionRevisionTests.AuthoredRoles(), TestContent.AuthoredLines(), new ScenarioStrategy { Target = k => k.Self == Id(2) ? Id(1) : (CharacterId?)null });
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            var view = run.GetPlayerView(); int index = view.Transcript.ToList().FindIndex(o => o.Cue == GameCue.DeathReported && o.Target == Id(1));
            Assert.That(index, Is.GreaterThan(0));
            Assert.That(RosterInteractions.VisibleParticipant(view, Id(1), index).Alive, Is.True);
            Assert.That(RosterInteractions.VisibleParticipant(view, Id(1), index + 1).Alive, Is.False);
        }
        [Test]
        public void HonestTownCannotReplaceARecordedReportWithAnAuthoredLie()
        {
            var run = RunFactory.CreateScenario(new RunOptions(5, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "physician", "citizen", "inspector", "assassin", "survivor" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                DiscussionRevisionTests.AuthoredRoles(), TestContent.AuthoredLines(), new ScenarioStrategy());
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")));
            Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(Id(1))); Submit(run, GameCommand.Continue());
            var lie = new NightActionClaim(1, new RoleId("physician"), AbilityKind.Protect, Id(2), new[] { new ReportedNightResult(ReportKind.ProtectionApplied, Id(2)) });
            Assert.That(run.TrySubmit(GameCommand.FabricateReport(lie), out _), Is.False);
            Assert.That(new GameController(run, DiscussionRevisionTests.AuthoredRoles()).Request().Options.Any(o => o.Label == "Fabricate report"), Is.False);
            Submit(run, GameCommand.ReportLastAction());
            Assert.That(run.GetPlayerView().Transcript.Last(o => o.Speaker == Id(0) && o.NightReport != null).NightReport.Target, Is.EqualTo(Id(1)));
        }
        [Test]
        public void WrongNightRoleAndResultShapesAreRejectedWithoutChangingTheRun()
        {
            var run = Ready("inspector"); var before = run.GetPlayerView();
            var invalid = new[] {
                new NightActionClaim(2, new RoleId("inspector"), AbilityKind.Investigate, Id(1), new[] { new ReportedNightResult(ReportKind.Investigation, Id(1), Faction.Town) }),
                new NightActionClaim(1, new RoleId("physician"), AbilityKind.Protect, Id(1)),
                new NightActionClaim(1, new RoleId("inspector"), AbilityKind.Investigate, Id(99)),
                new NightActionClaim(1, new RoleId("inspector"), AbilityKind.Investigate, Id(0)),
                new NightActionClaim(1, new RoleId("inspector"), AbilityKind.Investigate, Id(1), new[] { new ReportedNightResult(ReportKind.ProtectionApplied, Id(1)) }) };
            foreach (var report in invalid) Assert.That(run.TrySubmit(GameCommand.FabricateReport(report), out _), Is.False);
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(before.Revision));
            Assert.That(run.GetPlayerView().Transcript, Is.EqualTo(before.Transcript));
        }
    }
}
