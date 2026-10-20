using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Recorded reports, scoped deductions, roster targets and privileged diagnostic isolation.</summary>
    public sealed class ReportsAndRosterTests
    {
        private static CharacterId Id(int value) => new CharacterId(value);
        private static void Submit(RunSession run, GameCommand command) => Assert.That(run.TrySubmit(command, out string error), Is.True, error);

        [Test]
        public void WatcherCanSelectALivingCounterclaimantAndReportTheirActualVisitors()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles(); RunSession run = null;
            for (uint seed = 1; seed <= 100; seed++)
            {
                var attempt = RunFactory.CreateScenario(new RunOptions(seed, discussionRounds: 3),
                    new[] { "coordinator", "citizen", "inspector", "physician", "vigilante", "survivor", "assassin" }
                        .Select((role, i) => new CastMemberSpec("N" + i, new RoleId(role))), roles, TestContent.AuthoredLines(),
                    new ScenarioStrategy { Target = k => k.Self == Id(3) ? Id(6) : (CharacterId?)null });
                Submit(attempt, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("coordinator")));
                if (attempt.GetPlayerView().Transcript.Any(o => o.Intent == SpeechIntent.CounterClaim && o.Speaker == Id(6)))
                { run = attempt; break; }
            }
            Assert.That(run, Is.Not.Null, "Seeded counterclaim fixture must find the legal bluff response.");
            while (run.GetPlayerView().Phase == GamePhase.Discussion) Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, GameCommand.Vote(null)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
            Assert.That(run.GetPlayerView().LegalNightTargets, Does.Contain(Id(6)));
            var request = new GameController(run, roles).Request();
            Assert.That(request.Options.Count, Is.GreaterThan(6), "The former six-entry pager hid a legal night choice.");
            Assert.That(RosterInteractions.ForTarget(request, Id(6)).Count, Is.EqualTo(1));
            Submit(run, GameCommand.Night(Id(6))); Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.ReportLastAction());
            var published = run.GetPlayerView().Transcript.Last(o => o.Speaker == Id(0) && o.NightReport != null);
            Assert.That(published.Text, Does.Contain("chose to watch N6"));
            Assert.That(published.Text, Does.Contain("saw N3 visit N6"));
            Assert.That(published.Text, Does.Contain("could not see which ability"));
            Assert.That(published.NightReport.Results.Any(r => r.Kind == ReportKind.VisitorSeen && r.Target == Id(3) && r.Subject == Id(6)), Is.True);
            Assert.That(run.TrySubmit(GameCommand.ReportLastAction(), out _), Is.False);
        }

        [Test]
        public void ClaimedPlayerGetsGiveReportForPreviousNightAndNotAnotherClaim()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var run = RunFactory.CreateScenario(new RunOptions(5, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "physician", "citizen", "assassin", "survivor" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                roles, TestContent.AuthoredLines(), new ScenarioStrategy());
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")));
            Assert.That(run.TrySubmit(GameCommand.ReportLastAction(), out _), Is.False);
            Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(Id(1))); Submit(run, GameCommand.Continue());
            var request = new GameController(run, roles).Request();
            Assert.That(request.Options.Count(o => o.Label == "Give report"), Is.EqualTo(1));
            Assert.That(request.Options.Any(o => o.Intent == SpeechIntent.ClaimRole), Is.False);
            Submit(run, GameCommand.ReportLastAction());
            var report = run.GetPlayerView().Transcript.Last(o => o.NightReport != null && o.Speaker == Id(0));
            Assert.That(report.NightReport.Night, Is.EqualTo(1));
            Assert.That(report.NightReport.Target, Is.EqualTo(Id(1)));
            Assert.That(report.Text, Does.Contain("protection was applied to N1"));
            Assert.That(report.Text, Does.Contain("does not tell me whether anyone attacked"));
            Assert.That(new GameController(run, roles).Request().Options.Any(o => o.Kind == CommandKind.ReportLastAction), Is.False);
        }

        [Test]
        public void BlockedHonestReportPreservesSelectedTargetAndDoesNotInventAResult()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var run = RunFactory.CreateScenario(new RunOptions(5, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "physician", "citizen", "interrupter", "survivor" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                roles, TestContent.AuthoredLines(), new ScenarioStrategy { Target = k => k.Self == Id(2) ? Id(0) : (CharacterId?)null });
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")));
            Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(Id(1))); Submit(run, GameCommand.Continue());
            Assert.That(run.TrySubmit(GameCommand.ReportLastAction(Faction.Antagonist), out _), Is.False);
            Submit(run, GameCommand.ReportLastAction());
            var report = run.GetPlayerView().Transcript.Last(o => o.NightReport != null && o.Speaker == Id(0));
            Assert.That(report.Text, Does.Contain("chose to protect N1"));
            Assert.That(report.Text, Does.Contain("I was blocked"));
            Assert.That(report.NightReport.Blocked, Is.True);
            Assert.That(report.NightReport.Results.Any(r => r.Kind == ReportKind.ProtectionApplied), Is.False);
            Assert.That(report.Text, Does.Not.Contain("N2"), "Blocking actor identity is private.");
        }

        [Test]
        public void PublicReportFormatterSeparatesPassProtectionAndVisitorKnowledge()
        {
            string Name(CharacterId id) => "N" + id.Value;
            var blocked = new NightActionClaim(2, new RoleId("interrupter"), AbilityKind.Block, Id(3), new[] { new ReportedNightResult(ReportKind.Blocked) });
            Assert.That(NightReportFormatter.Format(blocked, "Singer", Name, GameText.Default), Does.Contain("my own selected block still took effect"));
            var passed = new NightActionClaim(2, new RoleId("coordinator"), AbilityKind.Watch, null);
            Assert.That(NightReportFormatter.Format(passed, "Watcher", Name, GameText.Default), Does.Contain("chose not to use my ability"));
            var watch = new NightActionClaim(2, new RoleId("coordinator"), AbilityKind.Watch, Id(3), new[] { new ReportedNightResult(ReportKind.NoVisitors, Id(3)) });
            Assert.That(NightReportFormatter.Format(watch, "Watcher", Name, GameText.Default), Does.Contain("no other visitors to N3"));
        }

        [Test]
        public void InvestigationAndConflictingVisitReportsChangeObserverBeliefsWithoutCertifyingTruth()
        {
            var model = new BeliefState(Enumerable.Range(0, 5).Select(Id), Personality.None, new SeededRandom(42), Id(0),
                TestContent.Roles()[0], new SetupKnowledge(3, 1, 1), TestContent.Roles(), Array.Empty<CharacterId>(), new ReasoningSettings());
            float before = model.SuspicionValue(Id(3));
            var report = new NightActionClaim(1, new RoleId("inspector"), AbilityKind.Investigate, Id(3),
                new[] { new ReportedNightResult(ReportKind.Investigation, Id(3), Faction.Antagonist) });
            model.Observe(Id(1), GameCommand.PublishReport(report), 2);
            Assert.That(model.SuspicionValue(Id(3)), Is.GreaterThan(before));
            Assert.That(model.KnownFaction(Id(3)), Is.Null);
            int evidenceCount = model.Evidence.Count; float suspicion = model.SuspicionValue(Id(3));
            model.Observe(Id(1), GameCommand.PublishReport(report), 2);
            Assert.That(model.Evidence.Count, Is.EqualTo(evidenceCount)); Assert.That(model.SuspicionValue(Id(3)), Is.EqualTo(suspicion));
            model.Receive(new NightReport(Id(0), ReportKind.VisitorSeen, Id(2), night: 1, subject: Id(4)));
            float trust = model.TrustValue(Id(2));
            model.Observe(Id(2), GameCommand.PublishReport(new NightActionClaim(1, new RoleId("physician"), AbilityKind.Protect, Id(3))), 2);
            Assert.That(model.TrustValue(Id(2)), Is.LessThan(trust));
            Assert.That(model.Evidence.Any(e => e.Kind == EvidenceKind.Contradiction && e.Actor == Id(2)), Is.True);
        }

        [Test]
        public void NightDiagnosticsAreReadOnlyAndDoNotEnterPlayerEvidenceOrChangeRevision()
        {
            var run = TestContent.Scenario("inspector", "physician", "assassin", "survivor");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(Id(1)));
            var before = run.GetPlayerView(); var evidence = run.PlayerKnowledgeForDiagnostics().Evidence.Count;
            var records = run.GetNightDiagnostics(); Assert.That(records.Count, Is.EqualTo(4));
            Assert.That(records.Single(r => r.Actor == Id(0)).VisitTarget, Is.EqualTo(Id(1)));
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(before.Revision));
            Assert.That(run.GetPlayerView().Transcript.Select(o => o.Text), Is.EqualTo(before.Transcript.Select(o => o.Text)));
            Assert.That(run.PlayerKnowledgeForDiagnostics().Evidence.Count, Is.EqualTo(evidence));
            Assert.That(((System.Collections.Generic.ICollection<NightDebugRecord>)records).IsReadOnly, Is.True);
            Assert.That(before.Characters.All(c => !c.RevealedRole.HasValue), Is.True);
        }

        [Test]
        public void NameHighlightingPreservesTextAndHandlesWholeNamesPossessivesAndExistingMarkup()
        {
            var text = SemanticMarkup.Parse("<fear>Anna asks Ann: Ann's claim concerns N3.</fear>");
            var marked = ParticipantNameStyler.Highlight(text, new[] { "Ann", "Anna", "N3" });
            Assert.That(marked.Text, Is.EqualTo(text.Text));
            Assert.That(marked.StyleAt(0), Is.EqualTo(SemanticStyle.Participant));
            Assert.That(marked.StyleAt(5), Is.EqualTo(SemanticStyle.Fear));
            Assert.That(marked.Spans.Count(s => s.Style == SemanticStyle.Participant), Is.EqualTo(4));
            Assert.That(ParticipantNameStyler.Highlight(SemanticText.Literal("Annex"), new[] { "Ann" }).Spans, Is.Empty);
        }

        [Test]
        public void AuthoredGameTextAndRoleReferencesLoadAndRequiredReportFieldsCannotDisappear()
        {
#if UNITY_EDITOR
            string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Project/Data/Dialogue/GameText.json");
#else
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "GameText.json");
#endif
            var text = GameTextDocument.Parse(File.ReadAllText(path));
            foreach (var role in DiscussionRevisionTests.AuthoredRoles()) Assert.That(text.Entries.ContainsKey("role." + role.Id.Value), Is.True);
            Assert.That(text.Format("ui.giveReport"), Is.EqualTo("Give report"));
            var broken = text.Entries.ToDictionary(p => p.Key, p => p.Value); broken["report.watch"] = "I watched someone.";
            Assert.Throws<ArgumentException>(() => new GameText(broken));
        }

        [Test]
        public void PublicRecordIncludesAttributedJudgementVotes()
        {
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "inspector", "physician", "assassin", "survivor" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                TestContent.Roles(), TestContent.Lines(), new ScenarioStrategy { Nominee = Id(1), Verdict = Verdict.Innocent });
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(Id(1)));
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(Verdict.Abstain));
            var record = RosterInteractions.PublicRecord(run.GetPlayerView(), Id(1));
            Assert.That(record.Any(o => o.Cue == GameCue.BallotRevealed && o.Speaker == Id(2) && o.Target == Id(1)
                && o.Text.Contains("Innocent")), Is.True);
        }

        [Test]
        public void ActionLabelsDistinguishClaimsCounterclaimsAndReportsWithoutChangingAuthoredWords()
        {
            var original = SemanticMarkup.Parse("<fear>N1 contradicts N2.</fear>");
            var counter = ActionAppearance.Label(original, SpeechIntent.CounterClaim, GameText.Default);
            Assert.That(counter.Text, Is.EqualTo("[COUNTERCLAIM] " + original.Text));
            Assert.That(counter.StyleAt(0), Is.EqualTo(SemanticStyle.ActionChallenge));
            Assert.That(counter.StyleAt(counter.Text.Length - 1), Is.EqualTo(SemanticStyle.Fear));
            Assert.That(ActionAppearance.Label(original, SpeechIntent.ReportAction, GameText.Default).Text, Does.StartWith("[REPORT]"));
            Assert.That(ActionAppearance.Label(original, SpeechIntent.ClaimRole, GameText.Default).Text, Does.StartWith("[CLAIM]"));
            Assert.That(ActionAppearance.Tone(SpeechIntent.Accuse), Is.EqualTo(EffectTone.Pressure));
            Assert.That(ActionAppearance.Tone(SpeechIntent.Vouch), Is.EqualTo(EffectTone.Reassurance));
            Assert.That(ActionAppearance.Tone(SpeechIntent.Deflect), Is.EqualTo(EffectTone.Challenge));
            Assert.That(ActionAppearance.Label(original, null, GameText.Default).Text, Is.EqualTo(original.Text));
        }

        [Test]
        public void DaylightTracksRoundsRatherThanNpcVerbosity()
        {
            Assert.That(Daylight.DiscussionProgress(1, 5), Is.Zero);
            Assert.That(Daylight.DiscussionProgress(3, 5), Is.EqualTo(0.3f).Within(0.001));
            Assert.That(Daylight.DiscussionProgress(5, 5), Is.EqualTo(0.6f).Within(0.001));
        }
    }
}
