using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Social evidence and presentation metadata must survive feature integration together.</summary>
    public sealed class MilestoneIntegrationTests
    {
        private static RunSession Create(ScenarioStrategy strategy) => RunFactory.CreateScenario(TestContent.Text,
            new RunOptions(123, discussionRounds: 1, revealOnDeath: false, votingMode: VotingMode.Trial),
            new[] { "inspector", "physician", "interrupter", "assassin", "survivor" }
                .Select((role, index) => new CastMemberSpec("Person" + index, new RoleId(role))),
            TestContent.Roles(), TestContent.Lines(), strategy);

        private static void Submit(RunSession run, GameCommand command)
        {
            bool accepted = run.TrySubmit(command, out string error);
            Assert.That(accepted, Is.True, error);
        }

        /// <summary>Effects receive one event per spoken line, with the public intent and target intact.</summary>
        [Test]
        public void EachSpokenLineIsPublishedOnceWithSemanticMetadata()
        {
            RunSession run = Create(new ScenarioStrategy());
            var delivered = new List<GameObservation>();
            run.Observation += delivered.Add;
            Submit(run, GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(1)));
            var speech = delivered.Where(e => e.Cue == GameCue.SpeechDelivered).ToList();
            Assert.That(speech.Count, Is.EqualTo(5));
            Assert.That(speech.GroupBy(e => e.Speaker).All(g => g.Count() == 1), Is.True);
            GameObservation player = speech.Single(e => e.Speaker == new CharacterId(0));
            Assert.That(player.Intent, Is.EqualTo(SpeechIntent.Accuse));
            Assert.That(player.Target, Is.EqualTo(new CharacterId(1)));
            Assert.That(speech.All(e => e.Intent.HasValue), Is.True);
        }

        /// <summary>Public nominations remain available to NPC reasoning after an acquittal and dawn.</summary>
        [Test]
        public void PublicNominationsRemainInLaterNpcKnowledge()
        {
            var strategy = new ScenarioStrategy { Nominee = new CharacterId(4), Verdict = Verdict.Innocent };
            RunSession run = Create(strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, GameCommand.Nominate(new CharacterId(4)));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Defense));
            Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.Judge(Verdict.Innocent));
            strategy.Nominee = null;
            Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null));
            Submit(run, GameCommand.Continue());
            var knowledge = new List<KnowledgeView>();
            strategy.OnSpeech = knowledge.Add;
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Assert.That(knowledge.Count, Is.GreaterThan(0));
            Assert.That(knowledge.All(k => k.Evidence.Any(e => e.Kind == EvidenceKind.Nomination
                && e.Actor == new CharacterId(0) && e.Target == new CharacterId(4))), Is.True);
            Assert.That(run.GetPlayerView().Transcript.Any(e => e.Cue == GameCue.NominationMade
                && e.Speaker == new CharacterId(0) && e.Target == new CharacterId(4)), Is.True);
        }

        /// <summary>A publicly visible death is evidence even when the victim's role stays secret.</summary>
        [Test]
        public void UnrevealedDeathStillReachesNpcEvidence()
        {
            var strategy = new ScenarioStrategy
            {
                Target = k => k.OwnRole.Ability == AbilityKind.Attack ? new CharacterId(4) : (CharacterId?)null
            };
            RunSession run = Create(strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Transcript.Any(e => e.Cue == GameCue.DeathReported
                && e.Target == new CharacterId(4)), Is.True);
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == new CharacterId(4)).Alive, Is.False);
            Submit(run, GameCommand.Continue());
            var knowledge = new List<KnowledgeView>();
            strategy.OnSpeech = knowledge.Add;
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Assert.That(knowledge.Count, Is.GreaterThan(0));
            Assert.That(knowledge.All(k => k.Evidence.Any(e => e.Kind == EvidenceKind.Death
                && e.Target == new CharacterId(4))), Is.True);
            Assert.That(knowledge.All(k => !k.Evidence.Any(e => e.Kind == EvidenceKind.RoleReveal
                && e.Target == new CharacterId(4))), Is.True);
        }
    }
}
