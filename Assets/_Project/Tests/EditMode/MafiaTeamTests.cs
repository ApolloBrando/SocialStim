using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    public sealed class MafiaTeamTests
    {
        private static CharacterId Id(int id) => new CharacterId(id);
        private static void Submit(RunSession run, GameCommand command) => Assert.That(run.TrySubmit(command, out string error), Is.True, error);
        private static RunSession Run(ScenarioStrategy strategy, params string[] cast)
            => RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial, revealOnDeath: false),
                cast.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))), DiscussionRevisionTests.AuthoredRoles(), TestContent.AuthoredLines(), strategy);
        [Test]
        public void PlayerAndNpcMafiaShareAlliedRolesAndRecordedActionsButTownCannotReadThem()
        {
            KnowledgeView ally = null, town = null;
            var strategy = new ScenarioStrategy { Target = k => k.Self == Id(1) ? Id(4) : k.Self == Id(2) ? Id(3) : (CharacterId?)null,
                OnSpeech = k => { if (k.Day == 2 && k.Self == Id(1)) ally = k; if (k.Day == 2 && k.Self == Id(4)) town = k; } };
            var run = Run(strategy, "assassin", "forger", "interrupter", "physician", "inspector", "vigilante", "citizen");
            var initial = run.GetPlayerView();
            Assert.That(initial.Characters.Single(c => c.Id == Id(1)).TeamRole, Is.EqualTo(new RoleId("forger")));
            Assert.That(initial.Transcript.Any(o => o.Text.Contains("Private Mafia teammates")), Is.True);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(Id(6)));
            var records = run.GetPlayerView().TeamReports;
            Assert.That(records.Count, Is.EqualTo(3));
            Assert.That(records.Single(r => r.Actor == Id(1)).Action.Target, Is.EqualTo(Id(4)));
            Assert.That(records.Single(r => r.Actor == Id(2)).Action.Target, Is.EqualTo(Id(3)));
            var privateChat = run.GetPlayerView().Transcript.Where(o => o.Text.StartsWith("Private Mafia action", System.StringComparison.Ordinal)).ToList();
            Assert.That(privateChat.Count, Is.EqualTo(3));
            Assert.That(privateChat.Any(o => o.Text.Contains("N1") && o.Text.Contains("N4") && o.Text.Contains("Forger report")), Is.True);
            Assert.That(privateChat.Any(o => o.Text.Contains("N2") && o.Text.Contains("N3") && o.Text.Contains("Strongarm report")), Is.True);
            Assert.That(((ICollection<TeamNightRecord>)records).IsReadOnly, Is.True);
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Assert.That(ally.TeamReports.Count, Is.EqualTo(3));
            Assert.That(ally.Characters.Single(c => c.Id == Id(2)).TeamRole, Is.EqualTo(new RoleId("interrupter")));
            Assert.That(town.TeamReports, Is.Empty); Assert.That(town.Characters.All(c => !c.TeamRole.HasValue), Is.True);
        }
        [Test]
        public void PlayerSupportPromotesAfterNightDeathAndItsPreviousNightRecordKeepsTheOldRole()
        {
            var run = Run(new ScenarioStrategy { Target = k => k.Self == Id(2) ? Id(1) : (CharacterId?)null },
                "forger", "assassin", "vigilante", "physician", "citizen", "survivor");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(Id(4)));
            var view = run.GetPlayerView();
            Assert.That(view.OwnRole.Id, Is.EqualTo(new RoleId("assassin"))); Assert.That(view.Charges, Is.EqualTo(-1));
            Assert.That(view.LastNightAction.Role, Is.EqualTo(new RoleId("forger")));
            Assert.That(view.LastNightAction.Ability, Is.EqualTo(AbilityKind.Forge)); Assert.That(view.LastNightAction.Target, Is.EqualTo(Id(4)));
            Assert.That(view.TeamReports.Single(r => r.Actor == Id(0)).Action.Role, Is.EqualTo(new RoleId("forger")));
            Assert.That(view.Transcript.Any(o => o.Text.Contains("Private Mafia update")), Is.True);
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(Id(4)));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(4)).Alive, Is.False);
        }
        [Test]
        public void NpcSupportPromotesAfterExecutionWithoutPublishingTheNewKillerToTown()
        {
            KnowledgeView killer = null;
            var run = Run(new ScenarioStrategy { Nominee = Id(1), Verdict = Verdict.Guilty,
                Target = k => { if (k.Self == Id(2)) killer = k; return k.Self == Id(2) ? Id(4) : (CharacterId?)null; } },
                "citizen", "assassin", "interrupter", "physician", "inspector", "survivor");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(Id(1)));
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(Verdict.Guilty)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Transcript.Any(o => o.Text.Contains("Private Mafia")), Is.False);
            Assert.That(run.GetPlayerView().Characters.All(c => !c.TeamRole.HasValue), Is.True);
            Submit(run, GameCommand.Night(null));
            Assert.That(killer.OwnRole.Id, Is.EqualTo(new RoleId("assassin")));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(4)).Alive, Is.False);
            Assert.That(run.GetPlayerView().TeamReports, Is.Empty);
            Assert.That(run.GetPlayerView().Transcript.Any(o => o.Text.StartsWith("Private Mafia action", System.StringComparison.Ordinal)), Is.False);
        }
        [Test]
        public void SimultaneousCasualtiesAreRemovedBeforeExactlyOneLivingMafiaInheritsTheKill()
        {
            var run = Run(new ScenarioStrategy { Target = k => k.Self == Id(4) ? Id(1) : k.Self == Id(5) ? Id(2) : (CharacterId?)null },
                "physician", "assassin", "interrupter", "forger", "vigilante", "lone-killer", "survivor", "citizen");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(1)).Alive, Is.False);
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(2)).Alive, Is.False);
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null));
            Assert.That(run.GetNightDiagnostics().Single(r => r.Actor == Id(3)).Action.Role, Is.EqualTo(new RoleId("assassin")));
            Assert.That(run.GetNightDiagnostics().Count(r => r.Action.Role == new RoleId("assassin")), Is.EqualTo(1));
        }
        [Test]
        public void BlockedTeamActionRetainsTheAttemptedTargetWithoutInventingAVisit()
        {
            var run = Run(new ScenarioStrategy { Target = k => k.Self == Id(1) ? Id(3) : k.Self == Id(2) ? Id(1) : (CharacterId?)null },
                "assassin", "forger", "interrupter", "physician", "inspector", "citizen");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            var record = run.GetPlayerView().TeamReports.Single(r => r.Actor == Id(1));
            Assert.That(record.Action.Target, Is.EqualTo(Id(3))); Assert.That(record.Action.Blocked, Is.True);
            Assert.That(record.VisitTarget, Is.Null);
        }
        [Test]
        public void NoSurvivingMafiaMeansNoPromotionOrResurrection()
        {
            var run = Run(new ScenarioStrategy { Target = k => k.Self == Id(3) ? Id(1) : k.Self == Id(4) ? Id(2) : (CharacterId?)null },
                "physician", "assassin", "forger", "vigilante", "lone-killer", "survivor");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Characters.Where(c => c.Id == Id(1) || c.Id == Id(2)).All(c => !c.Alive), Is.True);
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            Assert.That(run.GetNightDiagnostics().Any(r => r.Action.Role == new RoleId("assassin") || r.Action.Role == new RoleId("forger")), Is.False);
        }
        [Test]
        public void TeamVisitRecordsLetMafiaNoticeAConflictingLookoutStoryWithoutKnowingTheLiarsRole()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles(); var own = roles.Single(r => r.Id.Value == "assassin");
            var model = new BeliefState(Enumerable.Range(0, 6).Select(Id), Personality.Brave, new SeededRandom(42),
                Id(0), own, new SetupKnowledge(3, 2, 1), roles, new[] { Id(1) }, new ReasoningSettings());
            model.RememberTeamAction(new TeamNightRecord(Id(1), "N1", new NightActionClaim(1, new RoleId("forger"), AbilityKind.Forge, Id(4)), Id(4)));
            float trust = model.TrustValue(Id(3));
            model.Observe(Id(3), GameCommand.PublishReport(new NightActionClaim(1, new RoleId("coordinator"), AbilityKind.Watch, Id(4),
                new[] { new ReportedNightResult(ReportKind.NoVisitors, Id(4)) })), 2);
            Assert.That(model.TrustValue(Id(3)), Is.LessThan(trust));
            Assert.That(model.Evidence.Any(e => e.Kind == EvidenceKind.Contradiction && e.Actor == Id(3)), Is.True);
            Assert.That(model.KnownFaction(Id(3)), Is.Null);
        }
    }
}
