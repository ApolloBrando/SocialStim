using System;
using System.Linq;
using NUnit.Framework;
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
                Slider speed = root.GetComponentsInChildren<Slider>(true).Single();
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
                Assert.That(speed.value, Is.EqualTo(100));
                Assert.That(speed.minValue, Is.Zero); Assert.That(speed.maxValue, Is.EqualTo(200));
                Assert.That(speed.wholeNumbers, Is.True);
                toggle.onClick.Invoke();
                Assert.That(speed.gameObject.activeInHierarchy, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ChangingTheActualSliderDoesNotSkipQueuedSpeech()
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
                var lines = Enum.GetValues(typeof(Personality)).Cast<Personality>()
                    .SelectMany(trait => Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>()
                        .Select(intent => new DialogueLine(trait + "." + intent, intent, "An authored test line.", requiredTrait: trait)));
                var run = RunFactory.CreateScenario(new RunOptions(42), new[] {
                    new CastMemberSpec("Player", town.Id), new CastMemberSpec("Town", town.Id),
                    new CastMemberSpec("Evil", evil.Id)
                }, roles, lines);
                var view = root.AddComponent<TextGameView>();
                view.Initialize(TMP_Settings.defaultFontAsset);
                view.Render(new GameController(run, roles).Request());
                Assert.That(view.IsDisplaying, Is.True);
                Slider speed = root.GetComponentsInChildren<Slider>(true).Single();
                speed.value = 150;
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

        [Test]
        public void DiscussionUsesOneRosterAndTwoStepAccuseAndDefendMenus()
        {
            Assert.That(TMP_Settings.defaultFontAsset, Is.Not.Null, "Import TMP Essential Resources.");
            var root = new GameObject("Discussion layout regression");
            try
            {
                var civilian = new RoleSpec(new RoleId("civilian"), "Solarian", Faction.Town,
                    RoleCategory.TownBenign, AbilityKind.None, 0, 0, 0, -1, Faction.Town);
                var physician = new RoleSpec(new RoleId("physician"), "Physician", Faction.Town,
                    RoleCategory.TownProtective, AbilityKind.Protect, 0, 0, 1, -1, Faction.Town, unique: true);
                var evil = new RoleSpec(new RoleId("evil"), "Archon", Faction.Antagonist,
                    RoleCategory.AntagonistKilling, AbilityKind.Attack, 1, 0, 0, -1, Faction.Antagonist);
                var roles = new[] { civilian, physician, evil };
                var lines = Enum.GetValues(typeof(Personality)).Cast<Personality>().SelectMany(trait =>
                    Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>().Select(intent =>
                        new DialogueLine(trait + "." + intent, intent, "Test speech.", requiredTrait: trait)));
                var run = RunFactory.CreateScenario(new RunOptions(42), new[] {
                    new CastMemberSpec("Ada", civilian.Id), new CastMemberSpec("Bram", physician.Id),
                    new CastMemberSpec("Celia", evil.Id)
                }, roles, lines);
                var view = root.AddComponent<TextGameView>();
                view.Initialize(TMP_Settings.defaultFontAsset, initialSettings: new EffectsSettings(enabled: false));
                view.ShowSetup(roles, "Test", _ => { });
                view.Render(new GameController(run, roles).Request());
                Assert.That(root.transform.Find("Text Canvas/Role Claims Board"), Is.Null);
                Assert.That(root.GetComponentsInChildren<ParticipantCard>().Length, Is.EqualTo(3));
                Transform choices = root.transform.Find("Text Canvas/Choices");
                Func<Button[]> active = () => choices.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy).ToArray();
                Assert.That(active().Select(Label), Is.EquivalentTo(new[] { "Wait", "Accuse", "Defend" }));
                active().Single(b => Label(b) == "Accuse").onClick.Invoke();
                Assert.That(active().Select(Label), Is.EquivalentTo(new[] { "Back" }));
                Assert.That(root.GetComponentsInChildren<ParticipantCard>().Where(c => c.Id.Value != 0)
                    .All(c => c.GetComponent<Button>().interactable), Is.True);
                Assert.That(run.GetPlayerView().Revision, Is.Zero, "Opening a target menu must not spend the turn.");
                active().Single(b => Label(b) == "Back").onClick.Invoke();
                active().Single(b => Label(b) == "Defend").onClick.Invoke();
                Assert.That(active().Select(Label), Is.EquivalentTo(new[] { "Back" }));
                Assert.That(root.GetComponentsInChildren<ParticipantCard>().Where(c => c.Id.Value != 0)
                    .All(c => c.GetComponent<Button>().interactable), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
