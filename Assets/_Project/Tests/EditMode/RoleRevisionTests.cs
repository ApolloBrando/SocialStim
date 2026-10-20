using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Tests.EditMode
{
    public sealed class RoleRevisionTests
    {
        private static CharacterId Id(int id) => new CharacterId(id);
        private static RoleSpec Role(string id) => DiscussionRevisionTests.AuthoredRoles().Single(r => r.Id.Value == id);
        private static NightActor Actor(int id, string role, int? charges = null, int guiltNight = 0)
            => new NightActor(Id(id), Role(role), charges ?? Role(role).Charges, guiltNight);
        private static NightOrder Order(int actor, int? target) => new NightOrder(Id(actor), target.HasValue ? Id(target.Value) : (CharacterId?)null);
        private static void Submit(RunSession run, GameCommand command) => Assert.That(run.TrySubmit(command, out string error), Is.True, error);

        [Test]
        public void AuthoredNamesAndReferenceDescriptionsUseTheRequestedNamesAndOneStructure()
        {
            var names = new[] { "Villager", "Sheriff", "Vigilante", "Lookout", "Forger", "Mafioso", "Strongarm", "Serial Killer", "Jester", "Ego", "Physician" };
            var roles = DiscussionRevisionTests.AuthoredRoles();
            Assert.That(roles.Select(r => r.Name), Is.EquivalentTo(names));
            foreach (var role in roles)
            {
                var lines = GameText.Default.Entries["role." + role.Id.Value].Split('\n');
                Assert.That(lines.Select(line => line.Split(':')[0]), Is.EqualTo(new[] { "Alignment", "Action", "Limits", "Goal", "Claim" }));
            }
        }
        [Test]
        public void VigilanteHasThreeShotsButCannotShootOnTheFirstNight()
        {
            var cast = new[] { Actor(0, "vigilante"), Actor(1, "citizen") };
            Assert.That(cast[0].Charges, Is.EqualTo(3));
            Assert.That(NightLegality.IsLegal(cast[0], Order(0, 1), cast, night: 1), Is.False);
            Assert.That(NightLegality.IsLegal(cast[0], Order(0, null), cast, night: 1), Is.True);
            Assert.That(NightLegality.IsLegal(cast[0], Order(0, 1), cast, night: 2), Is.True);
            Assert.Throws<ArgumentException>(() => new NightResolver().Resolve(cast, new[] { Order(0, 1) }, night: 1));
        }
        [TestCase("citizen", true)] [TestCase("physician", true)] [TestCase("assassin", false)]
        [TestCase("provocateur", false)] [TestCase("survivor", false)] [TestCase("lone-killer", false)]
        public void GuiltRequiresActuallyKillingATownParticipant(string target, bool guilty)
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, "vigilante"), Actor(1, target) }, new[] { Order(0, 1) }, night: 2);
            Assert.That(result.GuiltTriggered.Contains(Id(0)), Is.EqualTo(guilty));
            Assert.That(result.Reports.Any(r => r.Recipient == Id(0) && r.Kind == ReportKind.Guilt), Is.EqualTo(guilty));
        }
        [TestCase(false)] [TestCase(true)]
        public void BlockedOrProtectedTownShotDoesNotCauseGuilt(bool blocked)
        {
            var cast = new[] { Actor(0, "vigilante"), Actor(1, "citizen"), Actor(2, blocked ? "interrupter" : "physician") };
            var result = new NightResolver().Resolve(cast, new[] { Order(0, 1), Order(2, blocked ? 0 : 1) }, night: 2);
            Assert.That(result.GuiltTriggered, Is.Empty); Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.SpentCharges.Contains(Id(0)), Is.EqualTo(!blocked));
        }
        [Test]
        public void LastBulletAndForgedTownStillTriggerUnavoidableNextNightGuilt()
        {
            var cast = new[] { Actor(0, "vigilante", 1), Actor(1, "citizen"), Actor(2, "forger") };
            var result = new NightResolver().Resolve(cast, new[] { Order(0, 1), Order(2, 1) }, night: 2);
            Assert.That(result.GuiltTriggered, Does.Contain(Id(0)));
            var next = new[] { Actor(0, "vigilante", 0, guiltNight: 3), Actor(1, "physician"), Actor(2, "interrupter") };
            Assert.That(new NightResolver().Resolve(next, new[] { Order(0, null) }, night: 2).Deaths, Is.Empty);
            result = new NightResolver().Resolve(next, new[] { Order(0, null), Order(1, 0), Order(2, 0) }, night: 3);
            Assert.That(result.Deaths, Does.Contain(Id(0)));
            Assert.That(NightLegality.IsLegal(next[0], Order(0, 1), next, night: 3), Is.False);
        }
        [Test]
        public void RealSessionRemovesAllShotsWarnsPrivatelyAndKillsOnTheFollowingNight()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var members = new[] { "vigilante", "citizen", "physician", "assassin", "interrupter", "coordinator", "survivor", "forger", "lone-killer", "provocateur", "inspector" };
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                members.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))), roles, TestContent.AuthoredLines(),
                new ScenarioStrategy { Target = k => k.Day == 3 && (k.Self == Id(2) || k.Self == Id(4)) ? Id(0) : (CharacterId?)null });
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("vigilante"))); Submit(run, GameCommand.Nominate(null));
            Assert.That(run.TrySubmit(GameCommand.Night(Id(1)), out _), Is.False);
            Submit(run, GameCommand.Night(null)); Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(Id(1))); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Alive, Is.True); Assert.That(run.GetPlayerView().Charges, Is.Zero);
            Assert.That(run.GetPlayerView().Transcript.Any(o => o.Text.StartsWith("Private:", StringComparison.Ordinal) && o.Text.Contains("die from guilt")), Is.True);
            Submit(run, GameCommand.ReportLastAction());
            Assert.That(run.GetPlayerView().Transcript.Any(o => o.NightReport != null && o.Speaker == Id(0) && o.NightReport.Results.Any(r => r.Kind == ReportKind.Guilt)), Is.True);
            Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Alive, Is.False);
        }
        [TestCase("assassin")] [TestCase("interrupter")] [TestCase("forger")]
        [TestCase("lone-killer")] [TestCase("survivor")] [TestCase("provocateur")]
        public void EveryBluffingNpcPublishesOneCoverReportAndObserversRecordIt(string role)
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var cast = new[] { Role("citizen"), Role(role) }.Concat(roles.Where(r => r.Id.Value != "citizen" && r.Id.Value != role)).ToList();
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                cast.Select((r, i) => new CastMemberSpec("N" + i, r.Id)), roles, TestContent.AuthoredLines(), new BluffingNpc());
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null)); Submit(run, GameCommand.Continue());
            var reports = run.GetPlayerView().Transcript.Where(o => o.Speaker == Id(1) && o.NightReport != null).ToList();
            Assert.That(reports.Count, Is.EqualTo(1)); Assert.That(reports[0].NightReport.Role, Is.EqualTo(new RoleId("inspector")));
            Assert.That(reports[0].Text, Does.Contain("Sheriff report"));
            Assert.That(run.PlayerKnowledgeForDiagnostics().Evidence.Any(e => e.Actor == Id(1) && e.Kind == EvidenceKind.NightActionClaim), Is.True);
        }
        private sealed class BluffingNpc : INpcStrategy
        {
            public GameCommand Speak(KnowledgeView k, IRandomSource r) => k.Self == Id(1)
                ? GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector")) : GameCommand.Speak(SpeechIntent.StaySilent);
            public CharacterId? Nominate(KnowledgeView k, IRandomSource r) => null;
            public Verdict Judge(KnowledgeView k, CharacterId d, IRandomSource r) => Verdict.Innocent;
            public CharacterId? NightTarget(KnowledgeView k, IRandomSource r) => k.Self == Id(1) && k.OwnRole.Ability != AbilityKind.None ? Id(2) : (CharacterId?)null;
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void NightDeathOfCompetingClaimantCreatesSuspicionExceptWhenTheDeadClaimWasExposedAsFake(bool revealed, bool deadWasFake)
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var model = new BeliefState(Enumerable.Range(0, 5).Select(Id), Personality.None, new SeededRandom(42), Id(0), Role("citizen"),
                new SetupKnowledge(3, 1, 1), roles, Array.Empty<CharacterId>(), new ReasoningSettings());
            model.Observe(Id(1), GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector")), 1);
            model.Observe(Id(2), GameCommand.Speak(SpeechIntent.CounterClaim, role: new RoleId("inspector")), 1);
            model.ObserveDeath(Id(1), 1);
            if (revealed) model.Reveal(Id(1), Role(deadWasFake ? "assassin" : "inspector"), 1);
            float before = model.SuspicionValue(Id(2));
            model.ObserveNightClaimantDeath(Id(1), 1);
            Assert.That(model.SuspicionValue(Id(2)), deadWasFake ? Is.EqualTo(before) : Is.GreaterThan(before));
            float after = model.SuspicionValue(Id(2)); model.ObserveNightClaimantDeath(Id(1), 1);
            Assert.That(model.SuspicionValue(Id(2)), Is.EqualTo(after)); Assert.That(model.KnownFaction(Id(2)), Is.Null);
        }
        [Test]
        public void JesterFinalActWaitsForPlayerChoiceAndRejectsNonTownDeadAndSelfTargets()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial, lastWords: false),
                new[] { "provocateur", "citizen", "assassin", "physician", "inspector" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                roles, TestContent.AuthoredLines(), new ScenarioStrategy { Nominee = Id(0), Verdict = Verdict.Guilty });
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Speak(SpeechIntent.Defend)); Submit(run, GameCommand.Continue());
            var view = run.GetPlayerView(); Assert.That(view.Phase, Is.EqualTo(GamePhase.JesterRevenge)); Assert.That(view.Alive, Is.False);
            Assert.That(view.RevengeTargets, Is.EquivalentTo(new[] { Id(1), Id(3), Id(4) }));
            Assert.That(run.TrySubmit(GameCommand.Continue(), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Revenge(Id(2)), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Revenge(Id(0)), out _), Is.False);
            Assert.That(new GameController(run, roles).Request().Options.All(o => o.Kind == CommandKind.ChooseRevengeTarget), Is.True);
            Submit(run, GameCommand.Revenge(Id(3)));
            view = run.GetPlayerView(); Assert.That(view.Characters.Single(c => c.Id == Id(3)).Alive, Is.False);
            Assert.That(view.Phase, Is.EqualTo(GamePhase.NightSelection));
            int revenge = view.Transcript.Single(o => o.Target == Id(3) && o.Cue == GameCue.DeathReported).Sequence;
            Assert.That(revenge, Is.LessThan(view.Transcript.Last(o => o.Cue == GameCue.NightFalls).Sequence));
            Assert.That(run.TrySubmit(GameCommand.Revenge(Id(4)), out _), Is.False);
        }
        [Test]
        public void JesterRewardResolvesBeforeTerminalWinAndNoTownTargetsSkipsTheChoice()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            foreach (bool town in new[] { false, true })
            {
                var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial, lastWords: false),
                    new[] { "provocateur", town ? "citizen" : "assassin", town ? "assassin" : "interrupter" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                    roles, TestContent.AuthoredLines(), new ScenarioStrategy { Nominee = Id(0), Verdict = Verdict.Guilty });
                Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
                Submit(run, GameCommand.Speak(SpeechIntent.Defend)); Submit(run, GameCommand.Continue());
                if (town) { Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.JesterRevenge)); Submit(run, GameCommand.Revenge(Id(1))); }
                Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Ended)); Assert.That(run.GetPlayerView().Won, Is.True);
                Assert.That(run.GetPlayerView().Outcome, Is.EqualTo(OutcomeKind.AntagonistVictory));
            }
        }
    }
}
