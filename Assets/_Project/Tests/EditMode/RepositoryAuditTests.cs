using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Regressions for configurable populations, editable content, and atomic turn submission.</summary>
    public sealed class RepositoryAuditTests
    {
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(9)]
        [TestCase(12)]
        [TestCase(21)]
        [TestCase(32)]
        public void PopulationAndRoleAssignmentsScaleTogether(int count)
        {
            var roles = AdvancedRoleTests.Roles();
            var setup = RoleList.ForPopulation(count);
            var names = Enumerable.Range(0, count).Select(i => "Person" + i).ToArray();
            var options = new RunOptions(147, new RoleId("citizen"), votingMode: VotingMode.Trial);
            var run = RunFactory.Create(TestContent.Text, options, roles, names, TestContent.Lines(), setup: setup);
            var again = RunFactory.Create(TestContent.Text, options, roles, names, TestContent.Lines(), setup: setup);
            var assigned = setup.Resolve(roles, options.PlayerRole, new SeededRandom(147));
            Assert.That(run.GetPlayerView().Characters.Count, Is.EqualTo(count));
            Assert.That(run.GetPlayerView().OwnRole.Id.Value, Is.EqualTo("citizen"));
            Assert.That(run.GetPlayerView().Characters.Select(c => c.Name), Is.EqualTo(again.GetPlayerView().Characters.Select(c => c.Name)));
            Assert.That(assigned.Select(r => r.Id), Is.EqualTo(setup.Resolve(roles, options.PlayerRole, new SeededRandom(147)).Select(r => r.Id)));
            Assert.That(assigned.Count, Is.EqualTo(count));
            Assert.That(assigned.Any(r => r.Faction == Faction.Town), Is.True);
            Assert.That(assigned.Any(r => r.Faction == Faction.Antagonist && r.Ability == AbilityKind.Attack), Is.True);
            Assert.That(assigned.Where(r => r.Unique).Select(r => r.Id).Distinct().Count(), Is.EqualTo(assigned.Count(r => r.Unique)));
            Assert.Throws<ArgumentException>(() => RunFactory.Create(TestContent.Text, options, roles, names.Take(count - 1), TestContent.Lines(), setup: setup));
        }

        [Test]
        public void AuthoredSlotsAllowLargerCastsAndCatalogsWithoutAHiddenCap()
        {
            var roles = Enumerable.Range(0, 70).Select(i => TestContent.Role("citizen" + i,
                Faction.Town, RoleCategory.TownBenign, AbilityKind.None, charges: 0)).ToArray();
            var setup = new RoleList(Enumerable.Repeat(new RoleSlot(faction: Faction.Town), 25));
            Assert.That(setup.Resolve(roles, roles[69].Id, new SeededRandom(2)).Count, Is.EqualTo(25));
            Assert.That(setup.Resolve(roles, roles[69].Id, new SeededRandom(2)).Any(r => r.Id == roles[69].Id), Is.True);
        }

        [Test]
        public void LargeAuthoredListsDoNotDependOnRecursiveCallDepth()
        {
            var citizen = AdvancedRoleTests.Roles().Single(r => r.Id.Value == "citizen");
            var setup = new RoleList(Enumerable.Repeat(new RoleSlot(citizen.Id), 4096));
            Assert.That(setup.Resolve(new[] { citizen }, citizen.Id, new SeededRandom(1)).Count, Is.EqualTo(4096));
        }

        [Test]
        public void SharesAreAuthoredAndImpossibleSetupsFailClearly()
        {
            var roles = AdvancedRoleTests.Roles();
            var assigned = RoleList.ForPopulation(12, 3, 1, 0).Resolve(roles, null, new SeededRandom(1));
            Assert.That(assigned.Count(r => r.Faction == Faction.Town), Is.EqualTo(9));
            Assert.That(assigned.Count(r => r.Faction == Faction.Antagonist), Is.EqualTo(3));
            Assert.That(assigned.Any(r => r.Faction == Faction.Neutral), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => RoleList.ForPopulation(2));
            Assert.Throws<ArgumentException>(() => RoleList.ForPopulation(9, antagonistShare: 0));
            // Three participants need a team killer; a non-killing antagonist cannot replace that slot.
            Assert.Throws<ArgumentException>(() => RoleList.ForPopulation(3).Resolve(roles, new RoleId("forger"), new SeededRandom(1)));
        }

        [Test]
        public void ObservationCallbacksCannotSubmitAnotherTurn()
        {
            var run = TestContent.Scenario("inspector", "physician", "interrupter", "assassin", "survivor");
            int rejected = 0;
            run.Observation += observation => {
                bool accepted = run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out string error);
                Assert.That(accepted, Is.False);
                Assert.That(error, Is.Not.Empty);
                rejected++;
            };
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.True);
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(1));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Nomination));
        }

        [Test]
        public void InvalidScenarioNamesAndTraitsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new CastMemberSpec(" ", new RoleId("citizen")));
            Assert.Throws<ArgumentException>(() => new CastMemberSpec("Ada", new RoleId("citizen"), (Personality)128));
        }

        [TestCase(Faction.Town, RoleCategory.NeutralEvil)]
        [TestCase(Faction.Antagonist, RoleCategory.TownBenign)]
        [TestCase(Faction.Neutral, RoleCategory.AntagonistKilling)]
        public void MisconfiguredRoleCategoriesCannotSelectAnotherFactionsObjectives(Faction faction, RoleCategory category)
        {
            Assert.Throws<ArgumentException>(() => TestContent.Role("invalid", faction, category, AbilityKind.None));
        }

        [Test]
        public void MalformedMetadataAndVariableTyposFailAtImport()
        {
            string json = ReadDialogue();
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(json.Replace("{target}", "{target_name}")));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(json.Replace("{target}", "{}")));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(json.Replace("\"emotionKey\": \"tense\"", "\"emotionKey\": 42")));
        }

        [Test]
        public void ContradictoryOptionalTargetConditionsFailInsteadOfSilentlyNeverMatching()
        {
            Assert.Throws<ArgumentException>(() => new DialogueLine("bad", SpeechIntent.ReportAction, "{result}",
                conditions: new[] { new DialogueCondition(DialogueConditionKind.HasTarget, flag: true),
                    new DialogueCondition(DialogueConditionKind.HasTarget, flag: false) }));
        }

        [Test]
        public void RecordedReportUsesNaturalTextAndAttributesTheSpeakerOnce()
        {
            var run = RunFactory.CreateScenario(TestContent.Text, new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "physician", "inspector", "assassin", "citizen", "survivor" }
                    .Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
                AdvancedRoleTests.Roles(), DialogueDocument.Parse(ReadDialogue()), new ScenarioStrategy());
            foreach (var command in new[] { GameCommand.Speak(SpeechIntent.StaySilent), GameCommand.Nominate(null),
                GameCommand.Night(new CharacterId(1)), GameCommand.Continue(), GameCommand.ReportLastAction() })
                Assert.That(run.TrySubmit(command, out string error), Is.True, error);
            string text = run.GetPlayerView().Transcript.Last(o => o.Intent == SpeechIntent.ReportAction).Text;
            Assert.That(text.StartsWith("Person0: ", StringComparison.Ordinal), Is.True);
            Assert.That(text.Split(new[] { "Person0" }, StringSplitOptions.None).Length, Is.EqualTo(2));
            Assert.That(text, Does.Contain("tried to protect Person1"));
            Assert.That(text.Contains("PLACEHOLDER"), Is.False);
        }

        private static string ReadDialogue()
        {
#if UNITY_EDITOR
            return File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "_Project/Data/Dialogue/StarterDialogue.json"));
#else
            return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "StarterDialogue.json"));
#endif
        }
    }
}
