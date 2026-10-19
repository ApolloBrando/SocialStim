using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Real JSON authoring format compiled in both the Unity and headless test assemblies.</summary>
    public sealed class DialogueImportTests
    {
        private static string Document(string extra = "") => "{\"schemaVersion\":1,\"lines\":[" +
            string.Join(",", Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>().Select(intent =>
                "{\"id\":\"" + intent + "\",\"intent\":\"" + intent + "\",\"text\":\"PLACEHOLDER {speaker}\",\"weight\":1}")) + extra + "]}";

        /// <summary>Private DataMember fields deserialize while preserving public immutable runtime records.</summary>
        [Test]
        public void ParsesActualAuthoringContract()
        {
            var lines = DialogueDocument.Parse(Document());
            Assert.That(lines.Count, Is.EqualTo(Enum.GetValues(typeof(SpeechIntent)).Length));
            Assert.That(lines.First().Text, Is.EqualTo("PLACEHOLDER {speaker}"));
        }

        /// <summary>Unknown versions, missing required values, duplicate IDs and unknown intents are rejected.</summary>
        [Test]
        public void InvalidDocumentsFailEarly()
        {
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(Document().Replace("\"schemaVersion\":1", "\"schemaVersion\":999")));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse("{\"schemaVersion\":1}"));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(Document(",{\"id\":\"bad\",\"intent\":\"omniscience\",\"text\":\"x\",\"weight\":1}")));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(Document().Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1")));
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(Document().Replace("\"weight\":1", "\"weight\":1,\"conditions\":[]")));
        }

        /// <summary>The actual committed starter content uses the same importer as the game.</summary>
        [Test]
        public void CommittedStarterDialogueLoads()
        {
#if UNITY_EDITOR
            string path = Path.Combine(UnityEngine.Application.dataPath, "_Project/Data/Dialogue/StarterDialogue.json");
#else
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "StarterDialogue.json");
#endif
            var lines = DialogueDocument.Parse(File.ReadAllText(path));
            Assert.That(lines.Count, Is.GreaterThanOrEqualTo(64));
            Assert.That(lines.Any(line => line.Text.Contains("PLACEHOLDER")), Is.False);
            Assert.That(lines.Any(line => line.Intent == SpeechIntent.ReportAction && line.Text.Contains("{speaker}")), Is.False,
                "Speaker attribution is added once by the session.");
            foreach (SpeechIntent intent in Enum.GetValues(typeof(SpeechIntent)))
                Assert.That(lines.Where(l => l.Intent == intent && l.Conditions.Count == 0 && !l.SpeakerRole.HasValue)
                    .Select(l => l.Text).Distinct().Count(), Is.GreaterThanOrEqualTo(4), intent.ToString());
        }
    }
}
