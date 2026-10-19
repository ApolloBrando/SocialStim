using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Declarative authoring conditions and fallback behavior using permitted speaker knowledge.</summary>
    public sealed class DialogueConditionTests
    {
        private static DialogueContext Context(GamePhase phase = GamePhase.Discussion, int day = 2, EvidenceRecord[] evidence = null)
            => new DialogueContext(new CharacterId(0), "Ada", "Bram", "", "", day, new RoleId("inspector"), Personality.Aggressive,
                phase, new CharacterId(1), evidence);

        /// <summary>Conditions are AND-combined and context changes select the generic fallback safely.</summary>
        [Test]
        public void PhaseAndObservedEvidenceMustBothMatch()
        {
            var conditions = new[] { new DialogueCondition(DialogueConditionKind.Phase, "Defense"), new DialogueCondition(DialogueConditionKind.TargetAccused) };
            var record = new EvidenceRecord(0, 2, EvidenceKind.Statement, new CharacterId(2), new CharacterId(1), SpeechIntent.Accuse);
            Assert.That(conditions.All(c => c.Matches(Context(GamePhase.Defense, evidence: new[] { record }))), Is.True);
            Assert.That(conditions.All(c => c.Matches(Context(GamePhase.Discussion, evidence: new[] { record }))), Is.False);
            Assert.That(conditions.All(c => c.Matches(Context(GamePhase.Defense))), Is.False);
        }

        /// <summary>Unknown predicates and contradictory day/semantic requirements fail before a run.</summary>
        [Test]
        public void InvalidConditionsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new DialogueCondition(DialogueConditionKind.HasTarget));
            Assert.Throws<ArgumentException>(() => new DialogueCondition(DialogueConditionKind.Phase, "secretPhase"));
            Assert.Throws<ArgumentException>(() => new DialogueCondition(DialogueConditionKind.SpeakerUnderPressure, number: 101));
            Assert.Throws<ArgumentException>(() => new DialogueLine("bad", SpeechIntent.Accuse, "x", conditions: new[] {
                new DialogueCondition(DialogueConditionKind.MinimumDay, number: 5), new DialogueCondition(DialogueConditionKind.MaximumDay, number: 2) }));
            Assert.Throws<ArgumentException>(() => new DialogueLine("bad", SpeechIntent.StaySilent, "x", conditions: new[] {
                new DialogueCondition(DialogueConditionKind.HasTarget, flag: true) }));
            Assert.Throws<ArgumentException>(() => new DialogueLine("bad", SpeechIntent.Accuse, "x", conditions: new[] {
                new DialogueCondition(DialogueConditionKind.Phase, "NightSelection") }));
            Assert.Throws<ArgumentException>(() => new DialogueLine("bad", SpeechIntent.ReportInvestigation, "x", conditions: new[] {
                new DialogueCondition(DialogueConditionKind.MaximumDay, number: 1) }));
        }

        /// <summary>Anti-repetition never relaxes contextual predicates to select an ineligible line.</summary>
        [Test]
        public void AntiRepetitionNeverBypassesConditions()
        {
            var lines = TestContent.Lines().Concat(new[] { new DialogueLine("restricted-condition", SpeechIntent.Accuse, "x", 1000,
                conditions: new[] { new DialogueCondition(DialogueConditionKind.MinimumDay, number: 20) }) });
            var selector = new DialogueSelector(lines, new SeededRandom(3));
            for (int i = 0; i < 20; i++) Assert.That(selector.Select(SpeechIntent.Accuse, Context()).Id, Is.Not.EqualTo("restricted-condition"));
        }

        /// <summary>New conditional lines are data-only extensions; schema 1 cannot silently accept schema 2 fields.</summary>
        [Test]
        public void VersionTwoConditionsLoadAndVersionOneRejectsThem()
        {
            string generic = string.Join(",", Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>().Select(intent =>
                "{\"id\":\"" + intent + "\",\"intent\":\"" + intent + "\",\"text\":\"PLACEHOLDER\",\"weight\":1}"));
            string extra = ",{\"id\":\"contextual\",\"intent\":\"Accuse\",\"text\":\"PLACEHOLDER {target}\",\"weight\":1,\"conditions\":[{\"kind\":\"hasTarget\",\"flag\":true}]}";
            string document = "{\"schemaVersion\":2,\"lines\":[" + generic + extra + "]}";
            Assert.That(DialogueDocument.Parse(document).Last().Conditions.Count, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(document.Replace("\"schemaVersion\":2", "\"schemaVersion\":1")));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(document.Replace("hasTarget", "targetTrueRole")));
        }

        /// <summary>Another NPC's private investigation is not visible to condition evaluation.</summary>
        [Test]
        public void ForeignPrivateEvidenceIsFilteredFromDialogueContext()
        {
            var foreign = new EvidenceRecord(0, 2, EvidenceKind.PrivateInvestigation, new CharacterId(2), new CharacterId(1), alignment: Faction.Antagonist);
            Assert.That(Context(evidence: new[] { foreign }).Evidence, Is.Empty);
        }
    }
}
