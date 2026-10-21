using System;
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
    /// <summary>Spectator exceptions remain separate from public reveals and NPC knowledge.</summary>
    public sealed class SpectatorAndRelationsTests
    {
        private static CharacterId Id(int value) => new CharacterId(value);
        private static void Submit(RunSession run, GameCommand command)
        { Assert.That(run.TrySubmit(command, out string error), Is.True, error); }

        [Test]
        public void TrueRolesAreGrantedOnlyAfterPlayerDeathAndNeverAsPublicReveals()
        {
            KnowledgeView npcKnowledge = null;
            var strategy = new ScenarioStrategy { Target = k => k.OwnRole.Ability == AbilityKind.Attack ? Id(0) : (CharacterId?)null,
                OnSpeech = k => { if (k.Self == Id(1)) npcKnowledge = k; } };
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial, revealOnDeath: false),
                new[] { "citizen", "inspector", "assassin", "physician", "citizen" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                roles, TestContent.AuthoredLines(), strategy);
            Assert.That(run.GetPlayerView().SpectatorRoles, Is.Empty);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            var spectator = run.GetPlayerView(); Assert.That(spectator.Alive, Is.False);
            Assert.That(spectator.SpectatorRoles.Count, Is.EqualTo(5));
            Assert.That(spectator.SpectatorRoles[Id(2)], Is.EqualTo(new RoleId("assassin")));
            Assert.That(spectator.Characters.All(c => !c.RevealedRole.HasValue), Is.True);
            Assert.That(spectator.Transcript.Any(o => o.Cue == GameCue.RoleRevealed), Is.False);
            Assert.That(((IDictionary<CharacterId, RoleId>)spectator.SpectatorRoles).IsReadOnly, Is.True);
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Continue());
            Assert.That(npcKnowledge.Characters.All(c => !c.RevealedRole.HasValue), Is.True);
            Assert.That(npcKnowledge.Characters.Single(c => c.Id == Id(2)).KnownFaction, Is.Null);
            Assert.That(run.PlayerKnowledgeForDiagnostics().Characters.All(c => !c.RevealedRole.HasValue), Is.True);
        }

        [Test]
        public void RelationSnapshotsContainDirectedNpcOpinionsWithoutChangingGameplay()
        {
            var run = TestContent.Scenario("inspector", "physician", "assassin", "survivor");
            var before = run.GetPlayerView(); var knowledge = run.PlayerKnowledgeForDiagnostics();
            var edges = run.GetSocialDiagnostics();
            Assert.That(edges.Count, Is.EqualTo(9));
            Assert.That(edges.All(e => e.Observer != Id(0) && e.Observer != e.Subject), Is.True);
            Assert.That(edges.Any(e => e.Subject == Id(0)), Is.True);
            Assert.That(edges.All(e => e.Suspicion >= 0 && e.Suspicion <= 100 && e.Trust >= 0 && e.Trust <= 100), Is.True);
            Assert.That(edges.All(e => e.Estimate.Town + e.Estimate.Antagonist + e.Estimate.Neutral == 10000), Is.True);
            Assert.That(((ICollection<SocialDebugRecord>)edges).IsReadOnly, Is.True);
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(before.Revision));
            Assert.That(run.GetPlayerView().Transcript, Is.EqualTo(before.Transcript));
            Assert.That(run.PlayerKnowledgeForDiagnostics().Evidence, Is.EqualTo(knowledge.Evidence));
            int suspicion = edges[0].Suspicion;
            Submit(run, GameCommand.Speak(SpeechIntent.Accuse, Id(2)));
            Assert.That(edges[0].Suspicion, Is.EqualTo(suspicion), "Snapshots cannot mutate when the game advances.");
            Assert.That(before.SpectatorRoles, Is.Empty);
        }
    }
}
