using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Editable data changes actual displayed narration, choices and results; invalid content has no prose fallback.</summary>
    public sealed class TextCatalogTests
    {
        [Test]
        public void EditingDataChangesActualRunAndChoiceWordingWithoutChangingCommandIdentity()
        {
            var changed = new TextTemplates(TestContent.Text.Keys.Select(k => new KeyValuePair<string, string>(k,
                k == "run.identity" ? "Identity {name}." : k == "choice.accuse" ? "Challenge {target}."
                : k == "speech.attributed" ? "{line} — {name}" : RenderOriginal(k))));
            var members = new[] { new CastMemberSpec("Self", new RoleId("inspector")), new CastMemberSpec("Other", new RoleId("physician")), new CastMemberSpec("Enemy", new RoleId("assassin")) };
            var run = RunFactory.CreateScenario(changed, new RunOptions(42), members, TestContent.Roles(), TestContent.Lines(), new ScenarioStrategy());
            Assert.That(run.GetPlayerView().Transcript[0].Text, Is.EqualTo("Identity Self."));
            var controller = new GameController(run, TestContent.Roles());
            var option = controller.Request().Options.First(o => o.Intent == SpeechIntent.Accuse);
            Assert.That(option.Label, Is.EqualTo("Challenge Other.")); Assert.That(option.Id.StartsWith("0:"), Is.True);
            var phrase = option.Phrasings[0];
            Assert.That(controller.TryChoose(option.Id + "|" + phrase.Id, out string error), Is.True, error);
            var delivered = run.GetPlayerView().Transcript.First(o => o.Speaker == new CharacterId(0));
            Assert.That(delivered.Text, Is.EqualTo(phrase.Text + " — Self")); Assert.That(delivered.SpokenText, Is.EqualTo(phrase.Text));
        }
        private static string RenderOriginal(string key)
        {
            // Supply sentinel tokens that reconstruct the authored template, without exposing mutable production state.
            string[] tokens = { "name", "role", "faction", "town", "evil", "neutral", "day", "target", "result", "visitor", "suspicion", "credibility", "tally", "count", "names", "verdict", "action", "night", "report", "objective", "trial", "maximum", "seed", "reason", "phase", "round", "alive", "tone", "line", "self", "state", "activity", "mark", "claim", "uses", "title", "value", "ability" };
            return TestContent.Text.Render(key, tokens.Select(t => (t, (object)("{" + t + "}"))).ToArray());
        }
        [Test]
        public void NightReportsUseEditableTemplatesAndNeverCertifySuccess()
        {
            Assert.That(NightActionText.Selection(TestContent.Text, AbilityKind.Attack, "Someone"), Is.EqualTo("I tried to attack Someone last night."));
            var edited = new TextTemplates(new[] { new KeyValuePair<string, string>("night.selection.Attack", "My chosen target was {target}.") });
            Assert.That(NightActionText.Selection(edited, AbilityKind.Attack, "Someone"), Is.EqualTo("My chosen target was Someone."));
            Assert.Throws<ArgumentException>(() => NightActionText.Claimed(edited, AbilityKind.Attack));
        }
        [Test]
        public void MissingRequiredEntriesAreRejectedBeforeRunCreation()
        {
            Assert.Throws<ArgumentException>(() => TextCatalogContract.Validate(new TextTemplates(new[] { new KeyValuePair<string, string>("one", "Only one.") })));
            Assert.Throws<ArgumentException>(() => TestContent.Text.Render("missing"));
            Assert.Throws<ArgumentException>(() => TestContent.Text.Render("run.identity"));
        }
        [TestCase("{\"schemaVersion\":2,\"entries\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"entries\":[],\"extra\":0}")]
        [TestCase("{\"schemaVersion\":1,\"entries\":[{\"id\":\"x\",\"text\":\"{bad token}\"}]}")]
        [TestCase("{\"schemaVersion\":1,\"entries\":[{\"id\":\"x\",\"text\":\"A\"},{\"id\":\"x\",\"text\":\"B\"}]}")]
        [TestCase("{\"schemaVersion\":1,\"entries\":[{\"id\":\"x\",\"id\":\"y\",\"text\":\"A\"}]}")]
        [TestCase("{\"schemaVersion\":1,\"entries\":[{\"id\":\"x\",\"text\":3}]}")]
        public void InvalidEditableCopyFailsExplicitly(string json) => Assert.Throws<ArgumentException>(() => TextDocument.Parse(json));
    }
}
