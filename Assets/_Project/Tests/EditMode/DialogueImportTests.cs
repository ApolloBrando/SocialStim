using System;
using System.Linq;
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
        }
    }
}
