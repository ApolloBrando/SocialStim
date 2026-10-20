using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Dialogue selection and information filtering behavior.</summary>
    public sealed class DialogueAndKnowledgeTests
    {
        private static DialogueContext Context() => new DialogueContext(new CharacterId(0), "Ada", "Bram", "Physician", "Town", 2, new RoleId("inspector"), Personality.None);

        /// <summary>Repeated eligible lines cycle without violating trait eligibility.</summary>
        [Test]
        public void RecentLinesDoNotRepeatUntilPoolIsExhausted()
        {
            var lines = TestContent.Lines().Concat(new[] { new DialogueLine("restricted", SpeechIntent.Accuse, "secret", 1000, Personality.Brave) });
            var selector = new DialogueSelector(lines, new SeededRandom(55));
            string[] choices = Enumerable.Range(0, 4).Select(_ => selector.Select(SpeechIntent.Accuse, Context()).Id).ToArray();
            Assert.That(choices.Distinct().Count(), Is.EqualTo(4));
            Assert.That(choices, Has.No.Member("restricted"));
        }

        /// <summary>Invalid variables and missing fallbacks fail at content load, not mid-conversation.</summary>
        [Test]
        public void InvalidContentIsRejectedEarly()
        {
            Assert.Throws<ArgumentException>(() => new DialogueSelector(new DialogueLine[0], new SeededRandom(1)));
            Assert.Throws<ArgumentException>(() => new DialogueSelector(TestContent.Lines().Concat(new[] { new DialogueLine("bad", SpeechIntent.Accuse, "{trueRole}") }), new SeededRandom(1)));
            Assert.Throws<ArgumentException>(() => new DialogueSelector(TestContent.Lines().Concat(new[] { TestContent.Lines()[0] }), new SeededRandom(1)));
        }

        /// <summary>Substitution does not interpret effect markup or recursively replace variables.</summary>
        [Test]
        public void MarkupRemainsOpaque()
        {
            var lines = TestContent.Lines().Where(l => l.Intent != SpeechIntent.Accuse || l.RequiredTrait != Personality.None).Concat(new[] { new DialogueLine("opaque", SpeechIntent.Accuse, "<fear>{target}</fear> claims {role}") });
            string text = new DialogueSelector(lines, new SeededRandom(1)).Select(SpeechIntent.Accuse, Context()).Text;
            Assert.That(text, Is.EqualTo("<fear>Bram</fear> claims Physician"));
        }

        /// <summary>Private results belonging to another recipient are removed at the knowledge boundary.</summary>
        [Test]
        public void KnowledgeRejectsForeignPrivateResults()
        {
            var knowledge = new KnowledgeView(new CharacterId(0), TestContent.Roles()[0], Personality.None, -1, 2,
                new KnownCharacter[0], new[] { new NightReport(new CharacterId(1), ReportKind.Investigation, new CharacterId(2), Faction.Antagonist) });
            Assert.That(knowledge.PrivateResults, Is.Empty);
        }

        /// <summary>Public player snapshots do not disclose living hidden cast roles.</summary>
        [Test]
        public void PlayerSnapshotContainsNoHiddenAssignments()
        {
            var view = TestContent.Scenario("inspector", "assassin", "physician", "interrupter", "survivor").GetPlayerView();
            Assert.That(view.Characters.All(c => !c.RevealedRole.HasValue && !c.RevealedFaction.HasValue && !c.IsTeammate), Is.True);
            Assert.That(view.OwnRole.Id, Is.EqualTo(new RoleId("inspector")));
            Assert.That(typeof(KnownCharacter).GetProperties().Any(p => p.PropertyType == typeof(RoleSpec)), Is.False);
        }

        /// <summary>Antagonists learn only their granted teammate relationship.</summary>
        [Test]
        public void AntagonistSeesTeammateWithoutReceivingTheirRole()
        {
            var view = TestContent.Scenario("assassin", "assassin", "physician", "interrupter", "survivor").GetPlayerView();
            Assert.That(view.Characters.Single(c => c.Id.Value == 1).IsTeammate, Is.True);
            Assert.That(view.Characters.Single(c => c.Id.Value == 1).RevealedRole, Is.Null);
            Assert.That(view.LegalNightTargets, Has.No.Member(new CharacterId(1)));
        }

        /// <summary>A saved RNG state reproduces the next values exactly.</summary>
        [Test]
        public void RandomStreamCanResume()
        {
            var first = new SeededRandom(42);
            for (int i = 0; i < 17; i++) first.Next(100);
            var resumed = new SeededRandom(first.State);
            Assert.That(Enumerable.Range(0, 100).Select(_ => first.Next(97)).ToArray(),
                Is.EqualTo(Enumerable.Range(0, 100).Select(_ => resumed.Next(97)).ToArray()));
        }
    }
}
