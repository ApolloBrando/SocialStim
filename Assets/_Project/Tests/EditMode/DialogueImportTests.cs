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
            Assert.Throws<ArgumentException>(() => DialogueDocument.Parse(Document().Replace("\"schemaVersion\":1", "\"schemaVersion\":2")));
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
            Assert.That(lines.Count, Is.EqualTo(45));
            Assert.That(lines.All(line => line.Text.StartsWith("PLACEHOLDER:", StringComparison.Ordinal)), Is.True);
        }
    }
}
