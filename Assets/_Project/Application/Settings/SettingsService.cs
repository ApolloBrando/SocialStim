using System;
using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Settings
{
    /// <summary>Display changes apply immediately; game edits use an explicit validated draft.</summary>
    public sealed class SettingsService
    {
        private readonly ISettingsStore _store;
        private readonly IReadOnlyList<RoleSpec> _roles;
        private readonly int _capacity;
        public SettingsService(ISettingsStore store, IReadOnlyList<RoleSpec> roles, int capacity)
        {
            _store = store; _roles = roles; _capacity = capacity;
            try { Current = PreferencesCodec.Read(store.Read(), capacity, out string warning); Warning = warning; }
            catch (Exception exception) when (exception is System.IO.IOException || exception is UnauthorizedAccessException)
            { Current = AppPreferences.Defaults(capacity); Warning = "Settings could not be read; defaults loaded."; }
            Draft = Current.Game;
        }
        public AppPreferences Current { get; private set; }
        public GamePreferences Draft { get; private set; }
        public string Warning { get; private set; }
        public bool Dirty { get; private set; }
        public event Action<DisplayPreferences> DisplayChanged;
        public void Display(DisplaySetting key, float value)
        { Current = Current.WithDisplay(Current.Display.With(key, value)); Dirty = true; DisplayChanged?.Invoke(Current.Display); }
        public void ResetDisplay(DisplaySetting key) => Display(key, DisplayPreferences.Default(key));
        public void BeginGameEdit() { Draft = Current.Game; }
        public void Edit(GameSetting key, int value) { Draft = Draft.With(key, value); }
        public void ResetGame(GameSetting key) { Draft = Draft.Reset(key); }
        public void SelectPlayer(string role, Faction? faction) { Draft = Draft.WithPlayer(role, faction); }
        public void Seed(string seed) { Draft = Draft.WithSeed(seed); }
        public void Limit(RoleId role, int min, int max) { Draft = Draft.WithLimit(role, min, max); }
        public void Slot(int index, string code) { Draft = Draft.WithSlot(index, code); }
        public string Validation()
        {
            if (!string.IsNullOrWhiteSpace(Draft.Seed) && !uint.TryParse(Draft.Seed, out _)) return "Seed must be a whole number.";
            return SetupBuilder.Error(Draft.Setup(), _roles, _capacity);
        }
        public bool ApplyGame(out string error)
        {
            error = Validation(); if (error != null) return false;
            Current = Current.WithGame(Draft); Dirty = true; return true;
        }
        public void CancelGame() { Draft = Current.Game; }
        public void ResetAll()
        { Current = AppPreferences.Defaults(_capacity); Draft = Current.Game; Dirty = true; DisplayChanged?.Invoke(Current.Display); }
        public string Flush()
        {
            if (!Dirty) return null;
            try { _store.Write(PreferencesCodec.Write(Current)); Dirty = false; Warning = null; return null; }
            catch (Exception exception) when (exception is System.IO.IOException || exception is UnauthorizedAccessException)
            { Warning = "Settings could not be saved."; return Warning; }
        }
    }
}
