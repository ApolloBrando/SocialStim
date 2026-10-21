using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Presentation.Effects;
using SocialStim.Presentation.Text;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Tests.PlayMode
{
    public sealed class RelationChartLayoutTests
    {
        [Test]
        public void ChartUsesFourReadableCardsAndIndependentNativeFilledBarsPerPage()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources.");
            var root = new GameObject("Relation chart layout regression", typeof(RectTransform), typeof(Canvas));
            try
            {
                var town = new RoleSpec(new RoleId("town"), "Villager", Faction.Town, RoleCategory.TownBenign, AbilityKind.None, 0, 0, 0, -1, Faction.Town);
                var mafia = new RoleSpec(new RoleId("mafia"), "Mafioso", Faction.Antagonist, RoleCategory.AntagonistKilling, AbilityKind.Attack, 1, 0, 0, -1, Faction.Antagonist);
                var lines = Enum.GetValues(typeof(Personality)).Cast<Personality>().SelectMany(trait =>
                    Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>().Select(intent => new DialogueLine(trait + "." + intent, intent, "Test speech.", requiredTrait: trait)));
                var run = RunFactory.CreateScenario(new RunOptions(42), Enumerable.Range(0, 7).Select(i => new CastMemberSpec("N" + i, i == 6 ? mafia.Id : town.Id)), new[] { town, mafia }, lines);
                var modal = new GameObject("Information", typeof(RectTransform)); modal.transform.SetParent(root.transform, false);
                var panel = modal.AddComponent<InformationPanel>(); panel.Initialize(TMP_Settings.defaultFontAsset, "Close");
                var content = panel.OpenContent("Relations", new TextPalette(false), 720);
                var chart = content.gameObject.AddComponent<RelationChartPanel>(); chart.Initialize(TMP_Settings.defaultFontAsset, GameText.Default, new TextPalette(false), run.GetSocialDiagnostics());
                Canvas.ForceUpdateCanvases();
                var bars = chart.GetComponentsInChildren<Image>().Where(i => i.name == "Trust fill" || i.name == "Suspicion fill").ToArray();
                Assert.That(bars.Length, Is.EqualTo(8));
                Assert.That(bars.All(i => i.type == Image.Type.Filled && i.sprite != null && i.fillAmount >= 0 && i.fillAmount <= 1), Is.True);
                Assert.That(chart.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Relation ", StringComparison.Ordinal)), Is.EqualTo(4));
                Assert.That(chart.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text.StartsWith("VIEWPOINT", StringComparison.Ordinal)), Is.True);
                Assert.That(chart.GetComponentsInChildren<TextMeshProUGUI>().Count(t => t.name == "Trust score"), Is.EqualTo(4));
                Assert.That(chart.GetComponentsInChildren<TextMeshProUGUI>().Count(t => t.name == "Suspicion score"), Is.EqualTo(4));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
