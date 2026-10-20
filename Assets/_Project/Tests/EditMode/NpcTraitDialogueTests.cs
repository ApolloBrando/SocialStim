using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Single-trait assignment, voice isolation and the actual authored content contract.</summary>
    public sealed class NpcTraitDialogueTests
    {
        private static readonly Personality[] Traits = Enum.GetValues(typeof(Personality)).Cast<Personality>()
            .Where(trait => trait != Personality.None).ToArray();
        private static readonly SpeechIntent[] Frequent = { SpeechIntent.Accuse, SpeechIntent.Defend,
            SpeechIntent.ClaimRole, SpeechIntent.Agree, SpeechIntent.Disagree, SpeechIntent.ReactToDeath,
            SpeechIntent.VoteJustification };

        [TestCase(2)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(17)]
        public void AssignmentUsesEveryTraitBeforeRepeatingAndIsSeeded(int npcCount)
        {
            Assert.That(Traits.Length, Is.EqualTo(7));
            for (uint seed = 1; seed <= 10; seed++)
            {
                Personality[] first = Assigned(npcCount, seed, false);
                Assert.That(Assigned(npcCount, seed, false), Is.EqualTo(first));
                Assert.That(Assigned(npcCount, seed, true), Is.EqualTo(first), "Changing roles/factions cannot select voices.");
                Assert.That(first, Has.No.Member(Personality.None));
                for (int start = 0; start < first.Length; start += 7)
                {
                    var cycle = first.Skip(start).Take(7).ToArray();
                    Assert.That(cycle.Distinct().Count(), Is.EqualTo(cycle.Length));
                    if (cycle.Length == 7) Assert.That(cycle, Is.EquivalentTo(Traits));
                }
            }
        }

        private static Personality[] Assigned(int npcCount, uint seed, bool reverseRoles)
        {
            var observed = new SortedDictionary<int, Personality>();
            var roles = AdvancedRoleTests.Roles();
            string[] catalogue = { "citizen", "assassin", "physician", "survivor" };
            var members = Enumerable.Range(0, npcCount + 1).Select(index => new CastMemberSpec("Person" + index,
                new RoleId(catalogue[(reverseRoles ? npcCount - index : index) % catalogue.Length])));
            var run = RunFactory.CreateScenario(new RunOptions(seed, discussionRounds: 1, votingMode: VotingMode.Trial),
                members, roles, TestContent.Lines(), new ScenarioStrategy {
                    OnSpeech = view => observed[view.Self.Value] = view.Personality
                });
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out string error), Is.True, error);
            Assert.That(observed.Count, Is.EqualTo(npcCount));
            return observed.Values.ToArray();
        }

        [Test]
        public void PlayerCannotReceiveATraitAndCombinedTraitsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new CastMemberSpec("Invalid", new RoleId("citizen"), Personality.Anxious | Personality.Brave));
            Assert.Throws<ArgumentException>(() => new DialogueLine("mixed", SpeechIntent.Accuse, "Test", requiredTrait: Personality.Anxious | Personality.Brave));
            var roles = AdvancedRoleTests.Roles();
            Assert.Throws<ArgumentException>(() => RunFactory.CreateScenario(new RunOptions(1), new[] {
                new CastMemberSpec("Player", new RoleId("citizen"), Personality.Brave),
                new CastMemberSpec("Other", new RoleId("physician")),
                new CastMemberSpec("Enemy", new RoleId("assassin"))
            }, roles, TestContent.Lines()));
        }

        [Test]
        public void ExplicitScenarioTraitsCannotBreakFairDistribution()
        {
            Assert.Throws<ArgumentException>(() => RunFactory.CreateScenario(new RunOptions(1), new[] {
                new CastMemberSpec("Player", new RoleId("citizen")),
                new CastMemberSpec("First", new RoleId("physician"), Personality.Brave),
                new CastMemberSpec("Second", new RoleId("assassin"), Personality.Brave)
            }, AdvancedRoleTests.Roles(), TestContent.Lines()));
        }

        [Test]
        public void EveryAuthoredTraitHasMinimumVolumeAndEveryActionHasSafeFallbacks()
        {
            var lines = TestContent.AuthoredLines();
            foreach (Personality trait in Traits)
            {
                var voice = lines.Where(line => line.RequiredTrait == trait).ToArray();
                Assert.That(voice.Length, Is.GreaterThanOrEqualTo(100), trait.ToString());
                foreach (SpeechIntent intent in Enum.GetValues(typeof(SpeechIntent)))
                {
                    var pool = voice.Where(line => line.Intent == intent).ToArray();
                    Assert.That(pool.Length, Is.GreaterThanOrEqualTo(Frequent.Contains(intent) ? 8 : 3), trait + "/" + intent);
                    Assert.That(pool.Any(line => !line.SpeakerRole.HasValue && line.Conditions.Count == 0), Is.True);
                }
                Assert.That(voice.All(line => Enum.IsDefined(typeof(Personality), line.RequiredTrait)), Is.True);
            }
        }

        [Test]
        public void NoAuthoredTextsAreDuplicatesOrLexicalNearDuplicates()
        {
            var lines = TestContent.AuthoredLines();
            var normalized = lines.Select(line => Normalize(line.Text)).ToArray();
            Assert.That(normalized.Distinct().Count(), Is.EqualTo(normalized.Length));
            var pairs = normalized.Select(text => {
                string[] words = text.Split(' ');
                return new HashSet<string>(Enumerable.Range(0, Math.Max(0, words.Length - 1)).Select(i => words[i] + " " + words[i + 1]));
            }).ToArray();
            for (int i = 0; i < pairs.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    if (Math.Min(pairs[i].Count, pairs[j].Count) < 3) continue;
                    int shared = pairs[i].Count(pair => pairs[j].Contains(pair));
                    double similarity = (double)shared / (pairs[i].Count + pairs[j].Count - shared);
                    if (similarity >= 0.8) Assert.Fail("Near duplicate: " + lines[i].Id + " / " + lines[j].Id);
                }
        }

        private static string Normalize(string text) => string.Join(" ", Regex.Matches(
            Regex.Replace(text, "<[^>]*>", "").ToLowerInvariant(), "[a-z0-9_{}']+")
            .Cast<Match>().Select(match => match.Value));

        [Test]
        public void SelectionAndFallbackNeverLeaveTheSpeakersPool()
        {
            var lines = TestContent.AuthoredLines();
            var identities = lines.ToDictionary(line => line.Id);
            var selector = new DialogueSelector(lines, new SeededRandom(47));
            foreach (Personality trait in Enum.GetValues(typeof(Personality)))
                foreach (SpeechIntent intent in Enum.GetValues(typeof(SpeechIntent)))
                    foreach (int day in new[] { 1, 2, 9 })
                        for (int turn = 0; turn < 8; turn++)
                        {
                            var context = Context(trait, day);
                            var selection = selector.Select(intent, context);
                            var line = identities[selection.Id];
                            Assert.That(line.RequiredTrait, Is.EqualTo(trait));
                            Assert.That(line.Conditions.All(condition => condition.Matches(context)), Is.True);
                            Assert.That(selection.Text, Does.Not.Contain("{"));
                            Assert.That(selection.Text, Does.Not.Contain("}"));
                        }
        }

        [Test]
        public void MissingOwnVoiceFailsInsteadOfSelectingPlayerOrAnotherNpc()
        {
            var playerOnly = TestContent.AuthoredLines().Where(line => line.RequiredTrait == Personality.None);
            var selector = new DialogueSelector(playerOnly, new SeededRandom(2));
            Assert.Throws<InvalidOperationException>(() => selector.Select(SpeechIntent.Accuse, Context(Personality.Anxious, 2)));
            Assert.Throws<ArgumentException>(() => new DialogueLine("conflict", SpeechIntent.Accuse, "Test", requiredTrait: Personality.Anxious,
                conditions: new[] { new DialogueCondition(DialogueConditionKind.SpeakerTrait, "Brave") }));
        }

        [Test]
        public void SpecificContextFallsBackWithinTraitWithoutDroppingItsGates()
        {
            var lines = TestContent.AuthoredLines().Concat(new[] {
                new DialogueLine("specific-grim", SpeechIntent.Accuse, "A permitted later-day statement about {target}.", requiredTrait: Personality.Grim,
                    conditions: new[] { new DialogueCondition(DialogueConditionKind.MinimumDay, number: 8) })
            });
            var selector = new DialogueSelector(lines, new SeededRandom(3));
            Assert.That(selector.Select(SpeechIntent.Accuse, Context(Personality.Grim, 9)).Id, Is.EqualTo("specific-grim"));
            var fallback = selector.Select(SpeechIntent.Accuse, Context(Personality.Grim, 2));
            Assert.That(fallback.Id.StartsWith("npc.grim.", StringComparison.Ordinal), Is.True);
        }

        [Test]
        public void PlayerTemplatesStayNeutralAndPlaceholdersAreRegistered()
        {
            var lines = TestContent.AuthoredLines();
            var replacements = new Dictionary<string, string> {
                ["speaker"] = "Ada", ["target"] = "Admiral Waffle", ["role"] = "Physician", ["result"] = "CLEAR", ["day"] = "2"
            };
            foreach (DialogueLine line in lines)
            {
                string text = Regex.Replace(line.Text, @"\{([^{}]+)\}", match => replacements[match.Groups[1].Value]);
                Assert.That(text.Contains("{") || text.Contains("}"), Is.False, line.Id);
                if (line.RequiredTrait != Personality.None)
                {
                    Assert.That(line.Text.Split(' ').Length, Is.LessThanOrEqualTo(40), line.Id);
                    continue;
                }
                Assert.That(line.Text.Split(' ').Length, Is.LessThanOrEqualTo(14), line.Id);
                Assert.That(line.Text.IndexOfAny(new[] { '<', '>', '!', '?' }), Is.EqualTo(-1), line.Id);
                if (line.Intent != SpeechIntent.ReportAction)
                    Assert.That(text.Split(' ').Length, Is.LessThanOrEqualTo(14), line.Id);
            }
        }

        [Test]
        public void OriginalIdsAndConditionalMetadataRemainAccessible()
        {
            var lines = TestContent.AuthoredLines().ToDictionary(line => line.Id);
            Assert.That(lines.Keys.Count(id => !id.StartsWith("npc.", StringComparison.Ordinal) && !id.StartsWith("player.counterclaim.", StringComparison.Ordinal)), Is.EqualTo(70));
            var pressure = lines["starter.social.defend.pressure"];
            Assert.That(pressure.Weight, Is.EqualTo(3));
            Assert.That(pressure.Metadata.Emotion, Is.EqualTo("tense"));
            Assert.That(pressure.Conditions.Single().Number, Is.EqualTo(45));
            var legacy = lines["starter.social.accuse.aggressive"];
            Assert.That(legacy.RequiredTrait, Is.EqualTo(Personality.Brave));
            Assert.That(legacy.Conditions.Any(condition => condition.Kind == DialogueConditionKind.TargetAccused), Is.True);
        }

        [Test]
        public void ActualNamePoolRetainsOriginalsAndHasFortyShortDistinctNames()
        {
#if UNITY_EDITOR
            string path = Path.Combine(UnityEngine.Application.dataPath, "_Project/ScriptableObjects/Catalogs/StarterCatalog.asset");
#else
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "StarterCatalog.asset");
#endif
            string names = File.ReadAllText(path).Split(new[] { "  _names:\n" }, StringSplitOptions.None)[1]
                .Split(new[] { "  _reasoning:" }, StringSplitOptions.None)[0];
            var values = Regex.Matches(names, @"^  - (.+)$", RegexOptions.Multiline).Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
            Assert.That(values.Length, Is.GreaterThanOrEqualTo(40));
            Assert.That(values.Distinct().Count(), Is.EqualTo(values.Length));
            Assert.That(values.All(value => value.Length <= 16 && Regex.IsMatch(value, "^[A-Za-z]+$")), Is.True);
            foreach (string original in "Ada Bram Celia Dorian Elena Felix Greta Hector Iris Jonas Kira Lucian Mara Nico Orla Petra Quinn Radu Sabina Tomas".Split(' '))
                Assert.That(values, Has.Member(original));
        }

        private static DialogueContext Context(Personality trait, int day) => new DialogueContext(new CharacterId(1), "Ada",
            "Admiral Waffle", "Physician", "CLEAR", day, new RoleId("inspector"), trait);
    }
}
