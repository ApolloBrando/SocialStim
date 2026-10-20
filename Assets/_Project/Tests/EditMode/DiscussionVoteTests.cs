using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Round windows, priority exchanges and the new daily plurality policy.</summary>
    public sealed class DiscussionVoteTests
    {
        private sealed class ScriptedConversation : INpcStrategy
        {
            internal Func<KnowledgeView, GameCommand> Speech = _ => GameCommand.Speak(SpeechIntent.StaySilent);
            internal Func<KnowledgeView, CharacterId?> Ballot = _ => null;
            public GameCommand Speak(KnowledgeView k, IRandomSource r) => Speech(k);
            public CharacterId? Nominate(KnowledgeView k, IRandomSource r) => Ballot(k);
            public Verdict Judge(KnowledgeView k, CharacterId d, IRandomSource r) => Verdict.Guilty;
            public CharacterId? NightTarget(KnowledgeView k, IRandomSource r) => null;
        }

        private static RunSession Run(ScriptedConversation strategy = null, int windows = 1, bool abstain = true,
            bool revote = true, bool lastWords = true, bool reveal = true) => RunFactory.CreateScenario(
            new RunOptions(42, discussionRounds: 3, playerWindowsPerRound: windows, allowAbstain: abstain,
                revoteTies: revote, lastWords: lastWords, revealOnDeath: reveal),
            new[] { "citizen", "inspector", "physician", "assassin", "citizen" }
                .Select((role, id) => new CastMemberSpec("Person" + id, new RoleId(role))),
            AdvancedRoleTests.Roles(), TestContent.Lines(), strategy ?? new ScriptedConversation());
        private static void Submit(RunSession run, GameCommand command)
        { Assert.That(run.TrySubmit(command, out string error), Is.True, error); }
        private static void DiscussToVote(RunSession run)
        {
            for (int count = 0; run.GetPlayerView().Phase == GamePhase.Discussion && count < 100; count++)
                Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Voting));
        }

        [Test]
        public void DefaultPolicyIsFiveRoundGnosiaAndTrialRemainsExplicit()
        {
            Assert.That(new RunOptions(1).VotingMode, Is.EqualTo(VotingMode.Gnosia));
            Assert.That(new RunOptions(1).DiscussionRounds, Is.EqualTo(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunOptions(1, discussionRounds: 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunOptions(1, discussionRounds: 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunOptions(1, playerWindowsPerRound: 3));
            Assert.DoesNotThrow(() => new RunOptions(1, discussionRounds: 1, votingMode: VotingMode.Trial));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ExactlyConfiguredOrdinaryWindowsPrecedeTheVote(int windows)
        {
            var run = Run(windows: windows);
            for (int window = 0; window < 3 * windows; window++)
            {
                var view = run.GetPlayerView();
                Assert.That(view.DiscussionRound, Is.EqualTo(window / windows + 1));
                Assert.That(view.PlayerWindow, Is.EqualTo(window % windows + 1));
                Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            }
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Voting));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.False);
        }

        [Test]
        public void AccusationPausesTheNpcQueueAndResponseDoesNotSpendAnExtraWindow()
        {
            var strategy = new ScriptedConversation { Speech = _ => GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(0)) };
            var run = Run(strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            var view = run.GetPlayerView();
            Assert.That(view.RespondTo.HasValue, Is.True);
            Assert.That(view.DiscussionRound, Is.EqualTo(1));
            Assert.That(view.Transcript.All(e => e.Intent != SpeechIntent.StaySilent), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ExpressFear), out _), Is.False);
            for (int n = 0; run.GetPlayerView().RespondTo.HasValue && n < 10; n++)
                Submit(run, GameCommand.Speak(SpeechIntent.Defend));
            Assert.That(run.GetPlayerView().RespondTo, Is.Null);
            Assert.That(run.GetPlayerView().DiscussionRound, Is.EqualTo(2));
        }

        [Test]
        public void YieldedOpenerCanBeAnsweredWithoutSpendingTheFirstWindow()
        {
            var strategy = new ScriptedConversation { Speech = _ => GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(0)) };
            var run = Run(strategy);
            Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().RespondTo.HasValue, Is.True);
            Submit(run, GameCommand.Speak(SpeechIntent.Defend));
            Assert.That(run.GetPlayerView().DiscussionRound, Is.EqualTo(1));
            Assert.That(run.GetPlayerView().PlayerWindow, Is.EqualTo(1));
            Assert.That(run.GetPlayerView().RespondTo, Is.Null);
        }

        [Test]
        public void NpcAccusationsReceiveBoundedVictimAnswers()
        {
            var strategy = new ScriptedConversation { Speech = k => k.Self.Value != 2
                ? GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(2)) : GameCommand.Speak(SpeechIntent.StaySilent) };
            var run = Run(strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            var lines = run.GetPlayerView().Transcript.Where(e => e.Cue == GameCue.SpeechDelivered).ToList();
            Assert.That(lines.Any(e => e.Intent == SpeechIntent.Accuse), Is.True);
            Assert.That(lines.Count(e => e.Speaker == new CharacterId(2) && e.Intent == SpeechIntent.Defend), Is.EqualTo(1));
            Assert.That(run.GetPlayerView().DiscussionRound, Is.EqualTo(2));
        }

        [Test]
        public void PluralityCanEliminateWithoutAMajorityAndPublishesEveryBallotBeforeDeath()
        {
            var strategy = new ScriptedConversation { Ballot = k => k.Self.Value == 1 || k.Self.Value == 2
                ? new CharacterId(4) : k.Self.Value == 3 ? new CharacterId(2) : (CharacterId?)null };
            var run = Run(strategy);
            DiscussToVote(run);
            Submit(run, GameCommand.Vote(new CharacterId(1)));
            var view = run.GetPlayerView();
            Assert.That(view.Phase, Is.EqualTo(GamePhase.VoteReveal));
            Assert.That(view.Defendant, Is.EqualTo(new CharacterId(4)));
            Assert.That(view.Characters.All(c => c.Alive && !c.RevealedRole.HasValue), Is.True);
            Assert.That(view.Transcript.Count(e => e.Cue == GameCue.BallotRevealed), Is.EqualTo(5));
            Assert.That(view.Transcript.Count(e => e.Cue == GameCue.VoteTallied), Is.EqualTo(1));
            Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Defense));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id.Value == 4).Alive, Is.True);
            Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.Judge(Verdict.Guilty));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.LastWords));
            Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id.Value == 4).Alive, Is.False);
            Assert.That(run.GetPlayerView().Characters.Count(c => !c.Alive), Is.EqualTo(1));
        }

        [Test]
        public void OneRestrictedRevoteThenNoEliminationEndsATie()
        {
            var strategy = new ScriptedConversation { Ballot = k => k.Self.Value == 1 ? new CharacterId(2)
                : k.Self.Value == 2 ? new CharacterId(1) : (CharacterId?)null };
            var run = Run(strategy);
            DiscussToVote(run); Submit(run, GameCommand.Vote(null)); Submit(run, GameCommand.Continue());
            var view = run.GetPlayerView();
            Assert.That(view.Phase, Is.EqualTo(GamePhase.Voting));
            Assert.That(view.Revote, Is.True);
            Assert.That(view.VoteTargets.Select(id => id.Value), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(run.TrySubmit(GameCommand.Vote(new CharacterId(3)), out _), Is.False);
            Submit(run, GameCommand.Vote(null)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
            Assert.That(run.GetPlayerView().Characters.All(c => c.Alive), Is.True);
        }

        [Test]
        public void AllAbstainDoesNotCreateAnUnboundedRevote()
        {
            var run = Run(); DiscussToVote(run); Submit(run, GameCommand.Vote(null)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
        }

        [Test]
        public void MandatoryVoteRejectsPassAndSelfBeforeChangingState()
        {
            var run = Run(abstain: false); DiscussToVote(run);
            int revision = run.GetPlayerView().Revision;
            Assert.That(run.TrySubmit(GameCommand.Vote(null), out _), Is.False);
            Assert.That(run.TrySubmit(GameCommand.Vote(new CharacterId(0)), out _), Is.False);
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(revision));
            var request = new GameController(run, AdvancedRoleTests.Roles()).Request();
            Assert.That(request.Options.Any(o => o.Label == "Abstain"), Is.False);
            Submit(run, GameCommand.Vote(new CharacterId(1)));
            Assert.That(run.GetPlayerView().Transcript.Where(e => e.Cue == GameCue.BallotRevealed)
                .All(e => e.Target.HasValue), Is.True);
        }

        [Test]
        public void LastWordsAndDeathRevealCanBeDisabledIndependently()
        {
            var strategy = new ScriptedConversation { Ballot = k => k.Self.Value == 4 ? new CharacterId(1) : new CharacterId(4) };
            var run = Run(strategy, lastWords: false, reveal: false); DiscussToVote(run);
            Submit(run, GameCommand.Vote(new CharacterId(4))); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Defense));
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(Verdict.Guilty));
            var view = run.GetPlayerView();
            Assert.That(view.Phase, Is.EqualTo(GamePhase.NightSelection));
            Assert.That(view.Transcript.Any(e => e.Intent == SpeechIntent.LastWords || e.Cue == GameCue.RoleRevealed), Is.False);
            Submit(run, GameCommand.Night(null)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().DiscussionRound, Is.EqualTo(1));
            Assert.That(run.GetPlayerView().PlayerWindow, Is.EqualTo(1));
        }

        [TestCase("inspector")]
        [TestCase("assassin")]
        [TestCase("survivor")]
        public void NewLoopTerminatesAcrossSeededPlayerFactions(string role)
        {
            for (uint seed = 1; seed <= 10; seed++)
            {
                string[] first = Play(seed, role);
                Assert.That(first.Last(), Does.Contain("Run ended:"));
                if (seed == 1) Assert.That(Play(seed, role), Is.EqualTo(first));
            }
        }

        private static string[] Play(uint seed, string role)
        {
            var roles = AdvancedRoleTests.Roles();
            var run = RunFactory.Create(new RunOptions(seed, new RoleId(role)), roles,
                TestContent.Names(), TestContent.Lines(), setup: RoleList.ForPopulation(9));
            var controller = new GameController(run, roles);
            for (int step = 0; step < 3000; step++)
            {
                var request = controller.Request();
                if (request.View.Phase == GamePhase.Ended) return request.View.Transcript.Select(e => e.Text).ToArray();
                Assert.That(request.Options.Count, Is.GreaterThan(0), request.View.Phase.ToString());
                Assert.That(controller.TryChoose(request.Options[0].Id, out string error), Is.True, error);
            }
            Assert.Fail("New loop did not terminate for " + seed + "/" + role);
            return Array.Empty<string>();
        }

        [Test]
        public void PluralityTiesNeverUseIdentityAsAnEliminationTiebreaker()
        {
            var result = VoteResolver.Plurality(new CharacterId?[] { new CharacterId(9), new CharacterId(1), null });
            Assert.That(result.Winner, Is.Null);
            Assert.That(result.Tied.Select(id => id.Value), Is.EqualTo(new[] { 1, 9 }));
            Assert.That(result.Abstentions, Is.EqualTo(1));
            var reversed = VoteResolver.Plurality(new CharacterId?[] { null, new CharacterId(1), new CharacterId(9) });
            Assert.That(reversed.Tied, Is.EqualTo(result.Tied));
            Assert.That(VoteResolver.Plurality(new CharacterId?[0]).Winner, Is.Null);
        }
    }
}
