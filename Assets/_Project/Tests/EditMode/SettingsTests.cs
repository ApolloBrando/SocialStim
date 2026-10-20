using System;
using System.IO;
using NUnit.Framework;
using SocialStim.Application.Settings;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Phases;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Actual preference serialization, migration, field isolation and atomic file persistence.</summary>
    public sealed class SettingsTests
    {
        [Test]
        public void RoundTripPreservesEverySupportedGroupWithoutRunState()
        {
            var p = new GamePreferences(new EffectsSettings(true, false, true, 0.7f, true, 120),
                new NewRunSettings(16, 7, 2, VotingMode.Trial, false, false, false, false, 4, 0),
                30, 1.25f, true, true, 2, true);
            string json = SettingsCodec.Encode(p); var restored = SettingsCodec.Decode(json);
            foreach (PreferenceKey key in Enum.GetValues(typeof(PreferenceKey)))
                Assert.That(PreferencesEditor.Value(restored, key), Is.EqualTo(PreferencesEditor.Value(p, key)).Within(0.001), key.ToString());
            Assert.That(restored.Game.Seed, Is.EqualTo(0));
            Assert.That(json.Contains("transcript") || json.Contains("beliefs") || json.Contains("ownRole"), Is.False);
        }
        [Test]
        public void BadNumericFieldsClampIndependentlyAndMissingFieldsUseDefaults()
        {
            var p = SettingsCodec.Decode("{\"version\":2,\"speed\":900,\"intensity\":-1,\"textSize\":1,\"playerCount\":99,\"rounds\":1,\"windows\":99,\"flashes\":true,\"mode\":99}");
            Assert.That(p.Effects.ElementsPerSecond, Is.EqualTo(240)); Assert.That(p.Effects.Intensity, Is.Zero);
            Assert.That(p.Effects.Flashes, Is.True); Assert.That(p.Effects.ReducedMotion, Is.True);
            Assert.That(p.Game.PlayerCount, Is.EqualTo(20)); Assert.That(p.Game.Rounds, Is.EqualTo(3));
            Assert.That(p.Game.Windows, Is.EqualTo(2)); Assert.That(p.Game.Mode, Is.EqualTo(VotingMode.Gnosia));
            Assert.That(p.TextSize, Is.EqualTo(16)); Assert.That(p.UiScale, Is.EqualTo(1));
        }
        [Test]
        public void VersionOneDisplayPreferencesMigrateWithSafeNewDefaults()
        {
            var p = SettingsCodec.Decode("{\"version\":1,\"speed\":28,\"reducedMotion\":false}");
            Assert.That(p.Effects.ElementsPerSecond, Is.EqualTo(28)); Assert.That(p.Effects.ReducedMotion, Is.False);
            Assert.That(p.Effects.Flashes, Is.False); Assert.That(p.Game.Rounds, Is.EqualTo(5));
            Assert.That(SettingsCodec.Encode(p), Does.Contain("\"version\":2"));
            Assert.Throws<ArgumentException>(() => SettingsCodec.Decode("{\"version\":999}"));
            Assert.Throws<ArgumentException>(() => SettingsCodec.Decode("broken"));
        }
        [Test]
        public void ResetChangesOnlyTheSelectedFieldAndRejectsInvalidEdits()
        {
            var p = new GamePreferences(new EffectsSettings(reducedMotion: false, elementsPerSecond: 200), textSize: 30);
            var changed = PreferencesEditor.Reset(p, PreferenceKey.Speed);
            Assert.That(changed.Effects.ElementsPerSecond, Is.EqualTo(84));
            Assert.That(changed.TextSize, Is.EqualTo(30)); Assert.That(changed.Effects.ReducedMotion, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => PreferencesEditor.Apply(p, PreferenceKey.Speed, float.NaN));
        }
        [Test]
        public void StoreReplacesPreferencesAndPreservesMalformedFileOnLoad()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SocialStim-settings-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "settings.json");
            try
            {
                var store = new SettingsStore(path); Assert.That(store.Exists, Is.False);
                Assert.That(store.Load(out string warning).Effects.Flashes, Is.False); Assert.That(warning, Is.Null);
                store.Save(new GamePreferences());
                store.Save(new GamePreferences(textSize: 30));
                Assert.That(store.Load(out warning).TextSize, Is.EqualTo(30)); Assert.That(warning, Is.Null);
                Assert.That(File.Exists(path + ".tmp"), Is.False);
                File.WriteAllText(path, "{\"version\":999}");
                store.Load(out warning); Assert.That(warning, Is.Not.Empty);
                Assert.That(File.ReadAllText(path), Is.EqualTo("{\"version\":999}"));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test]
        public void LivePolicyChangePreservesRevealPositionAndDoesNotSkipTheLine()
        {
            var playback = new DisplayPlayback(); int calls = 0;
            playback.Begin(1, SemanticText.Literal("abcdefghij"), EffectProfile.None,
                new EffectsSettings(elementsPerSecond: 2), _ => calls++);
            playback.Tick(2); int visible = playback.VisibleUtf16;
            playback.UpdateSettings(new EffectsSettings(elementsPerSecond: 1)); playback.Tick(0.1f);
            Assert.That(playback.VisibleUtf16, Is.GreaterThanOrEqualTo(visible));
            Assert.That(playback.Busy, Is.True); Assert.That(calls, Is.Zero);
            playback.UpdateSettings(new EffectsSettings(instantText: true));
            Assert.That(playback.VisibleUtf16, Is.EqualTo(10));
        }
    }
}
