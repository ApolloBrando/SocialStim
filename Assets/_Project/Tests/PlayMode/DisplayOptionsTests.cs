using System.Linq;
using NUnit.Framework;
using System;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;
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
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ChangingSpeedThroughTheActualButtonDoesNotSkipQueuedSpeech()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources.");
            var root = new GameObject("Live settings regression");
            try
            {
                var town = new RoleSpec(new RoleId("town"), "Town", Faction.Town,
                    RoleCategory.TownInvestigative, AbilityKind.Investigate, 0, 0, 0, -1, Faction.Town);
                var evil = new RoleSpec(new RoleId("evil"), "Evil", Faction.Antagonist,
                    RoleCategory.AntagonistKilling, AbilityKind.Attack, 1, 0, 0, -1, Faction.Antagonist);
                var roles = new[] { town, evil };
                var lines = Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>()
                    .Select(intent => new DialogueLine(intent.ToString(), intent, "An authored test line."));
                var run = RunFactory.CreateScenario(new RunOptions(42), new[] {
                    new CastMemberSpec("Player", town.Id), new CastMemberSpec("Town", town.Id),
                    new CastMemberSpec("Evil", evil.Id)
                }, roles, lines);
                var view = root.AddComponent<TextGameView>();
                view.Initialize(TMP_Settings.defaultFontAsset);
                view.Render(new GameController(run, roles).Request());
                Assert.That(view.IsDisplaying, Is.True);
                Button speed = root.GetComponentsInChildren<Button>(true)
                    .Single(button => Label(button).StartsWith("Speed:", StringComparison.Ordinal));
                speed.onClick.Invoke();
                Assert.That(view.IsDisplaying, Is.True, "A display setting cannot consume queued observations.");
                Assert.That(run.GetPlayerView().Revision, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void CompositionReusesOneCompatibleEventSystem()
        {
            var root = new GameObject("Event system ownership test");
            try
            {
                var first = UiEventSystem.Ensure(root.transform);
                Assert.That(UiEventSystem.Ensure(root.transform), Is.SameAs(first));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static string Label(Button button) => button.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Single(label => label.name == "Label").text.TrimStart('>', ' ');
    }
}
