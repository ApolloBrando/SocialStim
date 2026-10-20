using System.Linq;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Settings;
using SocialStim.Presentation.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Real uGUI display controls must hide together and reset the active preferences.</summary>
    public sealed class DisplayOptionsTests
    {
        [Test]
        public void OptionsIncludeSpeedAndResetWithoutChangingTheRun()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources.");
            var root = new GameObject("Display options test");
            try
            {
                var view = root.AddComponent<TextGameView>();
                view.Initialize(TMP_Settings.defaultFontAsset, initialSettings: new EffectsSettings(elementsPerSecond: 28));
                view.ApplyPreferences(new GamePreferences(new EffectsSettings(elementsPerSecond: 28)));
                var buttons = root.GetComponentsInChildren<Button>(true);
                Button toggle = buttons.Single(b => Label(b) == "Settings");
                var modal = root.transform.Find("Text Canvas/Settings Modal");
                Assert.That(modal.gameObject.activeInHierarchy, Is.False);
                toggle.onClick.Invoke(); Assert.That(modal.gameObject.activeInHierarchy, Is.True);
                GamePreferences saved = null; view.PreferencesChanged += p => saved = p;
                buttons.Single(b => Label(b) == "Data").onClick.Invoke();
                root.GetComponentsInChildren<Button>(true).Single(b => Label(b) == "Reset all defaults").onClick.Invoke();
                Assert.That(saved, Is.Not.Null); Assert.That(saved.Effects.ElementsPerSecond, Is.EqualTo(84));
                Assert.That(saved.Effects.ReducedMotion, Is.True); Assert.That(saved.Effects.Flashes, Is.False);
                buttons.Single(b => Label(b) == "Close settings").onClick.Invoke();
                Assert.That(modal.gameObject.activeInHierarchy, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static string Label(Button button) => button.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Single(label => label.name == "Label").text.TrimStart('>', ' ');
    }
}
