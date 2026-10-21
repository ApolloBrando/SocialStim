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

namespace SocialStim.Tests.EditMode
{
    /// <summary>Authored role regressions for guilt, execution rewards and public claim evidence.</summary>
    public sealed class RoleRevisionTests
    {
        private static CharacterId Id(int id) => new CharacterId(id);
        private static RoleSpec Role(string id) => DiscussionRevisionTests.AuthoredRoles().Single(r => r.Id.Value == id);
        private static NightActor Actor(int id, string role, bool guilt = false) => new NightActor(Id(id), Role(role), Role(role).Charges, guilt);
        private static NightOrder Order(int id, int? target) => new NightOrder(Id(id), target.HasValue ? Id(target.Value) : (CharacterId?)null);
        private static void Submit(RunSession run, GameCommand command)
        { Assert.That(run.TrySubmit(command, out string error), Is.True, error); }
        private static RunSession Run(ScenarioStrategy strategy, params string[] roles) => RunFactory.CreateScenario(
            new RunOptions(47, discussionRounds: 1, votingMode: VotingMode.Trial),
            roles.Select((r, i) => new CastMemberSpec("Person" + i, new RoleId(r))),
            DiscussionRevisionTests.AuthoredRoles(), TestContent.AuthoredLines(), strategy);

        private sealed class ClaimingStrategy : INpcStrategy
        {
            public GameCommand Speak(KnowledgeView view, IRandomSource random) => GameCommand.Speak(SpeechIntent.ClaimRole, role: view.OwnRole.Id);
            public CharacterId? Nominate(KnowledgeView view, IRandomSource random) => null;
            public Verdict Judge(KnowledgeView view, CharacterId defendant, IRandomSource random) => Verdict.Innocent;
            public CharacterId? NightTarget(KnowledgeView view, IRandomSource random) => null;
        }

        [Test]
        public void PlayerCanCounterclaimOnlyOnceAcrossDifferentClaimWindows()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var run = RunFactory.CreateScenario(new RunOptions(42),
                new[] { "assassin", "physician", "inspector", "coordinator", "citizen" }
                    .Select((r, i) => new CastMemberSpec("Person" + i, new RoleId(r))), roles, TestContent.AuthoredLines(), new ClaimingStrategy());
            Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().CounterClaimRole.HasValue, Is.True);
            Submit(run, GameCommand.Speak(SpeechIntent.CounterClaim, role: run.GetPlayerView().CounterClaimRole));
            for (int i = 0; run.GetPlayerView().Phase == GamePhase.Discussion && i < 20; i++)
            {
                Assert.That(run.GetPlayerView().CounterClaimRole, Is.Null);
                Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            }
            Assert.That(run.GetPlayerView().Transcript.Count(o => o.Intent == SpeechIntent.CounterClaim && o.Speaker == Id(0)), Is.EqualTo(1));
            Assert.That(run.GetPlayerView().Transcript.Count(o => o.Intent == SpeechIntent.ClaimRole), Is.GreaterThan(1));
        }

        [Test]
        public void ExecutionsSometimesGetOneLivingNpcReactionBeforeNight()
        {
            int reactions = 0;
            for (uint seed = 1; seed <= 20; seed++)
            {
                var roles = DiscussionRevisionTests.AuthoredRoles();
                var run = RunFactory.CreateScenario(new RunOptions(seed, discussionRounds: 1, votingMode: VotingMode.Trial),
                    new[] { "citizen", "citizen", "physician", "assassin", "forger" }
                        .Select((r, i) => new CastMemberSpec("Person" + i, new RoleId(r))), roles, TestContent.AuthoredLines(),
                    new ScenarioStrategy { Nominee = Id(1), Verdict = Verdict.Guilty });
                Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(Id(1)));
                Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(Verdict.Guilty)); Submit(run, GameCommand.Continue());
                var transcript = run.GetPlayerView().Transcript;
                int execution = transcript.Single(o => o.Cue == GameCue.ExecutionCarriedOut).Sequence;
                var replies = transcript.Where(o => o.Sequence > execution && o.Cue == GameCue.SpeechDelivered).ToList();
                Assert.That(replies.Count, Is.LessThanOrEqualTo(1));
                Assert.That(replies.All(o => o.Speaker != Id(0) && o.Speaker != Id(1)), Is.True);
                reactions += replies.Count;
            }
            Assert.That(reactions, Is.GreaterThan(0)); Assert.That(reactions, Is.LessThan(20));
        }

        [TestCase("citizen", true)] [TestCase("physician", true)] [TestCase("inspector", true)]
        [TestCase("coordinator", true)] [TestCase("assassin", false)] [TestCase("interrupter", false)]
        [TestCase("forger", false)] [TestCase("lone-killer", false)] [TestCase("survivor", false)]
        [TestCase("provocateur", false)]
        public void VigilanteGuiltRequiresASuccessfulKillOfActualTown(string role, bool guilty)
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, "vigilante"), Actor(1, role) }, new[] { Order(0, 1) });
            Assert.That(result.TownKillers.Contains(Id(0)), Is.EqualTo(guilty));
            Assert.That(result.Deaths, Has.No.Member(Id(0)), "Guilt must not resolve on the shooting night.");
        }

        [TestCase(false)] [TestCase(true)]
        public void ProtectedOrBlockedTownShotsNeverScheduleGuilt(bool blocked)
        {
            var cast = new[] { Actor(0, "vigilante"), Actor(1, "citizen"), Actor(2, "physician"), Actor(3, "interrupter") };
            var result = new NightResolver().Resolve(cast, new[] { Order(0, 1), blocked ? Order(3, 0) : Order(2, 1) });
            Assert.That(result.TownKillers, Is.Empty); Assert.That(result.Deaths, Is.Empty);
        }

        [Test]
        public void DueGuiltCannotBeProtectedBlockedOrUsedForAnotherShot()
        {
            var cast = new[] { Actor(0, "vigilante", true), Actor(1, "citizen"), Actor(2, "physician"), Actor(3, "interrupter") };
            Assert.That(NightLegality.IsLegal(cast[0], Order(0, 1), cast), Is.False);
            var result = new NightResolver().Resolve(cast, new[] { Order(0, null), Order(2, 0), Order(3, 0) });
            Assert.That(result.Deaths, Is.EqualTo(new[] { Id(0) }));
            Assert.That(result.SpentCharges, Has.No.Member(Id(0)));
        }

        [Test]
        public void GuiltSurvivesDawnAndKillsAtTheEndOfTheFollowingNight()
        {
            var run = Run(new ScenarioStrategy(), "vigilante", "citizen", "physician", "assassin", "interrupter", "citizen");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(Id(1)));
            Assert.That(run.GetPlayerView().Alive, Is.True);
            Assert.That(run.GetPlayerView().Day, Is.EqualTo(2));
            Assert.That(run.GetPlayerView().Transcript.Any(o => o.Text.Contains("Guilt prevents")), Is.True);
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, GameCommand.Nominate(null));
            Assert.That(run.GetPlayerView().LegalNightTargets, Is.Empty);
            Assert.That(run.TrySubmit(GameCommand.Night(Id(3)), out _), Is.False);
            Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Alive, Is.False);
        }

        [TestCase(Verdict.Guilty, true)] [TestCase(Verdict.Innocent, false)] [TestCase(Verdict.Abstain, false)]
        public void NpcJesterCanOnlyKillALivingTownGuiltyVoter(Verdict playerVote, bool playerDies)
        {
            var strategy = new ScenarioStrategy { Nominee = Id(1), Verdict = Verdict.Guilty };
            var run = Run(strategy, "citizen", "provocateur", "assassin", "citizen", "physician");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(Id(1)));
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(playerVote));
            Submit(run, GameCommand.Continue());
            var view = run.GetPlayerView();
            var revenge = view.Transcript.Single(o => o.Text.Contains("taken by the executed Jester"));
            if (!playerDies) Assert.That(revenge.Target, Is.Not.EqualTo(Id(0)));
            Assert.That(playerDies ? new[] { Id(0), Id(3), Id(4) } : new[] { Id(3), Id(4) }, Does.Contain(revenge.Target.Value));
            Assert.That(revenge.Target, Is.Not.EqualTo(Id(2)), "Mafia guilty voters are ineligible.");
            Assert.That(view.Transcript.Single(o => o.Cue == GameCue.NightFalls).Sequence, Is.GreaterThan(revenge.Sequence));
        }

        [Test]
        public void PlayerJesterChoosesBeforeNightAndCannotHauntMafiaOrChooseTwice()
        {
            var run = Run(new ScenarioStrategy { Nominee = Id(0), Verdict = Verdict.Guilty },
                "provocateur", "citizen", "assassin", "physician", "citizen");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Speak(SpeechIntent.Defend)); Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.Speak(SpeechIntent.LastWords));
            var view = run.GetPlayerView();
            Assert.That(view.Phase, Is.EqualTo(GamePhase.JesterRevenge)); Assert.That(view.Alive, Is.False);
            Assert.That(view.Transcript.Any(o => o.Cue == GameCue.NightFalls), Is.False);
            Assert.That(view.RevengeTargets, Is.EquivalentTo(new[] { Id(1), Id(3), Id(4) }));
            Assert.That(run.TrySubmit(GameCommand.Continue(), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Revenge(Id(2)), out _), Is.False);
            var controller = new GameController(run, DiscussionRevisionTests.AuthoredRoles());
            var choice = controller.Request().Options.Single(o => o.Target == Id(3));
            Assert.That(controller.TryChoose(choice.Id, out _), Is.True);
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(3)).Alive, Is.False);
            Assert.That(controller.TryChoose(choice.Id, out _), Is.False);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
        }

        [Test]
        public void JesterWithNoEligibleTownVoterDoesNotStallOrKillAnInnocentVoter()
        {
            var run = Run(new ScenarioStrategy { Nominee = Id(1), Verdict = Verdict.Guilty },
                "citizen", "provocateur", "assassin", "forger", "interrupter");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(Id(1)));
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(Verdict.Innocent)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.Not.EqualTo(GamePhase.JesterRevenge));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(0)).Alive, Is.True);
        }

        [TestCase(false, false, true)] [TestCase(true, false, true)] [TestCase(true, true, false)]
        public void NightClaimantDeathIsWeightedEvidenceWithoutHiddenRoleKnowledge(bool reveal, bool deadLiar, bool suspicious)
        {
            var catalog = DiscussionRevisionTests.AuthoredRoles();
            var model = new BeliefState(Enumerable.Range(0, 5).Select(Id), Personality.Grim, new SeededRandom(13),
                Id(0), Role("citizen"), new SetupKnowledge(3, 1, 1), catalog, Array.Empty<CharacterId>(), new ReasoningSettings());
            model.Observe(Id(1), GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector")), 1);
            model.Observe(Id(2), GameCommand.Speak(SpeechIntent.CounterClaim, role: new RoleId("inspector")), 1);
            model.ObserveDeath(Id(1), 1, true);
            if (reveal) model.Reveal(Id(1), Role(deadLiar ? "assassin" : "inspector"), 1);
            int before = model.Suspicion(Id(2));
            model.ConsiderNightClaimDeaths(1);
            Assert.That(model.Suspicion(Id(2)) > before, Is.EqualTo(suspicious));
            Assert.That(model.Evidence.Any(e => e.Kind == EvidenceKind.ClaimantLostAtNight), Is.EqualTo(suspicious));
            Assert.That(model.KnownFaction(Id(2)), Is.Null);
            int after = model.Suspicion(Id(2)); model.ConsiderNightClaimDeaths(1);
            Assert.That(model.Suspicion(Id(2)), Is.EqualTo(after));
        }
    }
}
