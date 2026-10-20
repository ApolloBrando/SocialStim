using System.Linq;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
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
                var buttons = root.GetComponentsInChildren<Button>(true);
                Button speed = buttons.Single(b => Label(b).StartsWith("Speed:", System.StringComparison.Ordinal));
                Button toggle = buttons.Single(b => Label(b) == "Display options");
                Button reset = buttons.Single(b => Label(b) == "Reset display defaults");
                Assert.That(speed.gameObject.activeInHierarchy, Is.False);
                Assert.That(reset.gameObject.activeInHierarchy, Is.False);
                toggle.onClick.Invoke();
                Assert.That(speed.gameObject.activeInHierarchy, Is.True);
                Assert.That(reset.gameObject.activeInHierarchy, Is.True);
                EffectsSettings saved = null;
                view.SettingsChanged += settings => saved = settings;
                reset.onClick.Invoke();
                Assert.That(saved, Is.Not.Null);
                Assert.That(saved.ElementsPerSecond, Is.EqualTo(84));
                Assert.That(saved.ReducedMotion, Is.True);
                Assert.That(saved.Flashes, Is.False);
                Assert.That(Label(speed), Does.Contain("84"));
                toggle.onClick.Invoke();
                Assert.That(speed.gameObject.activeInHierarchy, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static string Label(Button button) => button.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Single(label => label.name == "Label").text.TrimStart('>', ' ');
    }
}
