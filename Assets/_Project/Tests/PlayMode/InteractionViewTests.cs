using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;
using SocialStim.Presentation.Text;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Actual TMP/uGUI first-press reveal and second-press advance; requires Unity and font resources.</summary>
    public sealed class InteractionViewTests
    {
        [UnityTest]
        public IEnumerator RevealDoesNotConsumeTheNextLineAndRootActionsStayShallow()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources.");
            var root = new GameObject("Interaction test");
            try
            {
                var role = new RoleSpec(new RoleId("town"), "Town", Faction.Town, RoleCategory.TownInvestigative, AbilityKind.Investigate, 0, 0, 0, -1, Faction.Town);
                var evil = new RoleSpec(new RoleId("evil"), "Evil", Faction.Antagonist, RoleCategory.AntagonistKilling, AbilityKind.Attack, 1, 0, 0, -1, Faction.Antagonist);
                var roles = new[] { role, evil };
                var lines = Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>().Select(intent => new DialogueLine(intent.ToString(), intent, "Authored line.")).ToArray();
                var run = RunFactory.CreateScenario(CopyFixture.Load(), new RunOptions(42), new[] { new CastMemberSpec("Self", role.Id), new CastMemberSpec("Other", role.Id), new CastMemberSpec("Enemy", evil.Id) }, roles, lines);
                var controller = new GameController(run, roles); var view = root.AddComponent<TextGameView>();
                view.Initialize(TMP_Settings.defaultFontAsset, initialSettings: new EffectsSettings(elementsPerSecond: 1));
                view.Render(controller.Request()); yield return null;
                var text = root.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "Current line");
                string first = text.text;
                Button advance = root.GetComponentsInChildren<Button>(true).Single(b => Label(b) == "Continue [Space]");
                advance.onClick.Invoke(); Assert.That(text.text, Is.EqualTo(first)); Assert.That(view.IsDisplaying, Is.True);
                yield return null; advance.onClick.Invoke(); Assert.That(text.text, Is.Not.EqualTo(first));
                for (int i = 0; view.IsDisplaying && i < 20; i++) { yield return null; advance.onClick.Invoke(); }
                Assert.That(view.IsDisplaying, Is.False); Assert.That(run.Revision, Is.Zero);
                var visible = root.GetComponentsInChildren<Button>().Select(Label).ToArray();
                Assert.That(visible, Has.Member("CHAT")); Assert.That(visible, Has.Member("ACCUSE")); Assert.That(visible, Has.Member("DEFEND"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static string Label(Button button) => button.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(t => t.name == "Label").Select(t => t.text.TrimStart('>', ' ')).FirstOrDefault() ?? "";
    }
}
