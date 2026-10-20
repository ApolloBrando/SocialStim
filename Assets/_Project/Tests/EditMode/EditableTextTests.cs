using System;
using System.IO;
using NUnit.Framework;
using SocialStim.Data.Dialogue;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Actual source editing must preserve eligibility metadata, unrelated text and reference compatibility.</summary>
    public sealed class EditableTextTests
    {
        private static string Read(string asset, string file)
        {
#if UNITY_5_3_OR_NEWER
            return File.ReadAllText(asset);
#else
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, file));
#endif
        }
        private static string Speech() => Read("Assets/_Project/Data/Dialogue/StarterDialogue.json", "StarterDialogue.json");
        private static string Copy() => Read("Assets/_Project/Data/Dialogue/Resources/SocialStimText.json", "SocialStimText.json");
        [Test]
        public void RewordPreservesEveryOtherLineConditionWeightAndMetadata()
        {
            var original = new EditableTextDocument(Speech()); string id = original.Ids[0];
            string text = "{target}, explain the discrepancy.\n<fear>We need an answer.</fear>";
            var changed = new EditableTextDocument(original.Reword(id, text)); changed.Validate();
            Assert.That(changed.Ids, Is.EqualTo(original.Ids));
            foreach (string other in original.Ids)
            {
                Assert.That(changed.Text(other), Is.EqualTo(other == id ? text : original.Text(other)));
                Assert.That(changed.OtherFields(other), Is.EqualTo(original.OtherFields(other)));
                Assert.That(changed.Group(other), Is.EqualTo(original.Group(other)));
            }
            Assert.That(original.Text(id), Is.Not.EqualTo(text), "Staging must not mutate the source document.");
        }
        [Test]
        public void InvalidStagedCopyCanBeRepairedButCannotPassSaveValidation()
        {
            var original = new EditableTextDocument(Copy());
            var invalid = new EditableTextDocument(original.Reword("run.identity", "Unknown {unavailable}."));
            Assert.Throws<ArgumentException>(() => invalid.Validate());
            var repaired = new EditableTextDocument(invalid.Reword("run.identity", "You are {name}."));
            Assert.DoesNotThrow(() => repaired.Validate());
            Assert.Throws<ArgumentException>(() => original.Reword("missing", "No such entry."));
        }
        [Test]
        public void NativeReferenceWriterMatchesTheCompleteCheckedInCatalogue()
        {
            string expected = Read("Docs/DIALOGUE_CATALOGUE.md", "DIALOGUE_CATALOGUE.md");
            Assert.That(DialogueCatalogue.Render(Speech(), Copy()), Is.EqualTo(expected));
        }
        [TestCase("{\"schemaVersion\":1,\"entries\":[{\"id\":\"x\",\"text\":\"A\"},{\"id\":\"x\",\"text\":\"B\"}]}")]
        [TestCase("{\"schemaVersion\":1,\"entries\":[{\"id\":\"x\",\"text\":3}]}")]
        [TestCase("[]")]
        public void UneditableStructureFailsWithoutLosingFields(string json)
            => Assert.Throws<ArgumentException>(() => new EditableTextDocument(json));
    }
}
