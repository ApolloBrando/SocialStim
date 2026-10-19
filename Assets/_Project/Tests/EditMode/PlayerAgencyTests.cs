using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Player trial, death, spectator and private-result integration scenarios.</summary>
    public sealed class PlayerAgencyTests
    {
        private static RunSession Scenario(ScenarioStrategy strategy, bool reveal = true) => RunFactory.CreateScenario(
            new RunOptions(123, discussionRounds: 1, revealOnDeath: reveal), new[]
            {
                new CastMemberSpec("Player", new RoleId("interrupter")),
                new CastMemberSpec("Investigator", new RoleId("inspector")),
                new CastMemberSpec("Medic", new RoleId("physician")),
                new CastMemberSpec("Killer", new RoleId("assassin")),
                new CastMemberSpec("Neutral", new RoleId("survivor")),
            }, TestContent.Roles(), TestContent.Lines(), strategy);

        private static void Submit(RunSession run, GameCommand command) => Assert.That(run.TrySubmit(command, out string error), Is.True, error);

        /// <summary>The player may be convicted and executed, then loses all action choices.</summary>
        [Test]
        public void ExecutedPlayerCanOnlySpectate()
        {
            var run = Scenario(new ScenarioStrategy { Nominee = new CharacterId(0), Verdict = Verdict.Guilty });
            Submit(run, GameCommand.Speak(SpeechIntent.Defend));
            Submit(run, GameCommand.Nominate(null));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Defense));
            Submit(run, GameCommand.Speak(SpeechIntent.Defend));
            Assert.That(run.TrySubmit(GameCommand.Judge(Verdict.Innocent), out _), Is.False);
            Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.LastWords));
            Submit(run, GameCommand.Speak(SpeechIntent.LastWords));
            Assert.That(run.GetPlayerView().Alive, Is.False);
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.Defend), out _), Is.False);
            var options = new GameController(run, TestContent.Roles()).Request().Options;
            Assert.That(options.Count, Is.EqualTo(1));
            Assert.That(new GameController(run, TestContent.Roles()).TryChoose(options[0].Id, out _), Is.True);
        }

        /// <summary>A night kill makes the player a spectator without leaking private NPC results.</summary>
        [Test]
        public void NightDeathAndPrivateNpcResultsAreScoped()
        {
            var strategy = new ScenarioStrategy { Target = knowledge => knowledge.OwnRole.Ability == AbilityKind.Attack
                ? new CharacterId(0) : knowledge.OwnRole.Ability == AbilityKind.Investigate ? new CharacterId(3) : (CharacterId?)null };
            var run = Scenario(strategy, false);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null));
            var view = run.GetPlayerView();
            Assert.That(view.Phase, Is.EqualTo(GamePhase.Dawn));
            Assert.That(view.Alive, Is.False);
            Assert.That(view.Transcript.Any(o => o.Cue == GameCue.InvestigationResultReceived), Is.False);
            Assert.That(view.Characters.All(c => !c.RevealedRole.HasValue), Is.True);
            Assert.That(view.LegalNightTargets, Is.Empty);
        }

        /// <summary>Acquittals consume the configured trial count and then advance to night.</summary>
        [Test]
        public void TwoAcquittalsExhaustDailyTrialBudget()
        {
            var run = Scenario(new ScenarioStrategy { Nominee = new CharacterId(4), Verdict = Verdict.Innocent });
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            for (int trial = 0; trial < 2; trial++)
            {
                Submit(run, GameCommand.Nominate(new CharacterId(4)));
                Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Defense));
                Submit(run, GameCommand.Continue());
                Submit(run, GameCommand.Judge(Verdict.Abstain));
            }
            Assert.That(run.GetPlayerView().TrialsUsed, Is.EqualTo(2));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
        }

        /// <summary>Listeners treat claims as uncertain but penalize a speaker contradicting their prior claim.</summary>
        [Test]
        public void SwitchingRoleClaimsRaisesEachObserversSuspicion()
        {
            var observed = new Dictionary<CharacterId, int>();
            var strategy = new ScenarioStrategy
            {
                OnSpeech = knowledge => observed[knowledge.Self] = knowledge.Characters.Single(c => c.Id.Value == 0).Suspicion,
            };
            var run = RunFactory.CreateScenario(new RunOptions(123, discussionRounds: 3), new[]
            {
                new CastMemberSpec("Player", new RoleId("interrupter")),
                new CastMemberSpec("Listener1", new RoleId("inspector")),
                new CastMemberSpec("Listener2", new RoleId("physician")),
                new CastMemberSpec("Listener3", new RoleId("assassin")),
                new CastMemberSpec("Listener4", new RoleId("survivor")),
            }, TestContent.Roles(), TestContent.Lines(), strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector")));
            var before = observed.ToDictionary(pair => pair.Key, pair => pair.Value);
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("survivor")));
            Assert.That(observed.Count, Is.EqualTo(4));
            foreach (var pair in before) Assert.That(observed[pair.Key], Is.GreaterThan(pair.Value));
        }
    }
}
