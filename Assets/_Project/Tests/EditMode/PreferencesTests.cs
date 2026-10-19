using System;
using System.IO;
using NUnit.Framework;
using SocialStim.Application.Settings;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Preference corruption, migration, draft semantics and actual file round trips.</summary>
    public sealed class PreferencesTests
    {
        [Test]
        public void NewDefaultsAreThreeTimesTheOldSpeedAndClampFieldsIndependently()
        {
            var settings = PreferencesCodec.Read("{\"version\":2,\"display\":{\"Speed\":1000,\"Intensity\":-4,\"FontSize\":28,\"ReducedMotion\":0},\"game\":{\"PlayerCount\":80,\"Rounds\":99}}", 20, out _);
            Assert.That(new DisplayPreferences().Get(DisplaySetting.Speed), Is.EqualTo(84));
            Assert.That(settings.Display.Get(DisplaySetting.Speed), Is.EqualTo(240));
            Assert.That(settings.Display.Get(DisplaySetting.Intensity), Is.Zero);
            Assert.That(settings.Display.Get(DisplaySetting.FontSize), Is.EqualTo(28));
            Assert.That(settings.Display.On(DisplaySetting.ReducedMotion), Is.False);
            Assert.That(settings.Game.Get(GameSetting.PlayerCount), Is.EqualTo(20));
            Assert.That(settings.Game.Get(GameSetting.Rounds), Is.EqualTo(7));
        }

        [Test]
        public void VersionOneMigratesDefaultSpeedAndPreservesAccessibility()
        {
            var settings = PreferencesCodec.Read("{\"version\":1,\"textSpeed\":28,\"intensity\":0.75,\"reducedMotion\":false,\"flashes\":true}", 20, out _);
            Assert.That(settings.Display.Get(DisplaySetting.Speed), Is.EqualTo(84));
            Assert.That(settings.Display.Get(DisplaySetting.Intensity), Is.EqualTo(0.75f));
            Assert.That(settings.Display.On(DisplaySetting.ReducedMotion), Is.False);
            Assert.That(settings.Display.On(DisplaySetting.Flashes), Is.True);
        }

        [Test]
        public void UnknownVersionsAndMalformedFilesRecoverWithoutThrowing()
        {
            var future = PreferencesCodec.Read("{\"version\":999}", 20, out string warning);
            Assert.That(warning, Is.Not.Empty);
            Assert.That(future.Display.Get(DisplaySetting.Speed), Is.EqualTo(84));
            Assert.DoesNotThrow(() => PreferencesCodec.Read("{not json", 20, out _));
            Assert.That(new DisplayPreferences().With(DisplaySetting.Speed, float.NaN).Get(DisplaySetting.Speed), Is.EqualTo(84));
        }

        [Test]
        public void DisplayAppliesNowWhileGameDraftCanBeCancelledOrExplicitlyApplied()
        {
            var store = new MemoryStore();
            var service = new SettingsService(store, MissionTestContent.Roles(), 20);
            service.BeginGameEdit(); service.Edit(GameSetting.Rounds, 7);
            service.Display(DisplaySetting.Speed, 120);
            Assert.That(service.Current.Game.Get(GameSetting.Rounds), Is.EqualTo(5));
            Assert.That(service.Current.Display.Get(DisplaySetting.Speed), Is.EqualTo(120));
            Assert.That(service.Flush(), Is.Null);
            var reloaded = new SettingsService(store, MissionTestContent.Roles(), 20);
            Assert.That(reloaded.Current.Game.Get(GameSetting.Rounds), Is.EqualTo(5));
            service.CancelGame(); Assert.That(service.Draft.Get(GameSetting.Rounds), Is.EqualTo(5));
            service.Edit(GameSetting.Rounds, 6);
            Assert.That(service.ApplyGame(out _), Is.True);
            Assert.That(service.Current.Game.Get(GameSetting.Rounds), Is.EqualTo(6));
        }

        [Test]
        public void InvalidRoleSetupCannotApplyAndResetIsExplicit()
        {
            var service = new SettingsService(new MemoryStore(), MissionTestContent.Roles(), 20);
            service.Edit(GameSetting.EvilCount, 7);
            Assert.That(service.ApplyGame(out string error), Is.False);
            Assert.That(error, Does.Contain("Evil count"));
            Assert.That(service.Current.Game.Get(GameSetting.EvilCount), Is.EqualTo(-1));
            service.Display(DisplaySetting.FontSize, 32); service.ResetDisplay(DisplaySetting.FontSize);
            Assert.That(service.Current.Display.Get(DisplaySetting.FontSize), Is.EqualTo(25));
            service.ResetAll();
            Assert.That(service.Draft.Get(GameSetting.PlayerCount), Is.EqualTo(9));
        }

        [Test]
        public void SettingsFileRoundTripPreservesCustomLimitsSlotsAndSeedButContainsNoRun()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SocialStimSettings-" + Guid.NewGuid().ToString("N"));
            try
            {
                var game = new GamePreferences(20).With(GameSetting.PlayerCount, 5).With(GameSetting.Preset, (int)SetupPreset.Custom)
                    .WithLimit(new RoleId("archon"), 1, 1).WithSlot(0, "archon").WithSeed("147");
                var preferences = new AppPreferences(new DisplayPreferences(), game);
                var store = new SettingsFileStore(Path.Combine(directory, "settings.json"));
                store.Write(PreferencesCodec.Write(preferences));
                store.Write(PreferencesCodec.Write(preferences.WithDisplay(preferences.Display.With(DisplaySetting.FontSize, 28))));
                var loaded = PreferencesCodec.Read(store.Read(), 20, out _);
                Assert.That(loaded.Game.Slots[0], Is.EqualTo("archon"));
                Assert.That(loaded.Game.Limits[0].Minimum, Is.EqualTo(1));
                Assert.That(loaded.Game.Seed, Is.EqualTo("147"));
                Assert.That(loaded.Display.Get(DisplaySetting.FontSize), Is.EqualTo(28));
                Assert.That(store.Read().Contains("transcript"), Is.False);
                Assert.That(store.Read().Contains("beliefs"), Is.False);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private sealed class MemoryStore : ISettingsStore
        {
            private string _text;
            public string Read() => _text;
            public void Write(string json) { _text = json; }
        }
    }
}
