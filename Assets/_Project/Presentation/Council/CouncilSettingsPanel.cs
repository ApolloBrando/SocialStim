using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.Settings;
using SocialStim.Presentation.Text;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Council
{
    /// <summary>Grouped native controls; all validation, persistence and draft semantics live in SettingsService.</summary>
    internal sealed class CouncilSettingsPanel
    {
        private readonly CouncilLayout _layout;
        private readonly SettingsService _service;
        private readonly IReadOnlyList<RoleSpec> _roles;
        private readonly Action<string, string> _open;
        private readonly Action<string, Action> _button;
        private readonly Action _back;
        private readonly RectTransform _rows;
        private readonly TMP_FontAsset _font;
        private bool _setup;
        private Action _start;
        private TextMeshProUGUI _error;
        private Action _rebuild;
        private Button _apply;
        internal CouncilSettingsPanel(CouncilLayout layout, SettingsService service, IReadOnlyList<RoleSpec> roles,
            Action<string, string> open, Action<string, Action> button, Action back)
        {
            _layout = layout; _service = service; _roles = roles; _open = open; _button = button; _back = back;
            _font = layout.DrawerText.font;
            _rows = TextUiFactory.Rect("Settings rows", layout.DrawerText.transform.parent);
            var group = _rows.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 14; group.childControlWidth = true; group.childControlHeight = true; group.childForceExpandHeight = false;
            _rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _rows.gameObject.SetActive(false);
        }
        internal void Hide() { _rows.gameObject.SetActive(false); }
        internal void Show()
        {
            Page("Settings", _service.Warning ?? "");
            _button("Game", () => BeginGame(false, null));
            _button("Text & Display", Text);
            _button("Accessibility", Accessibility);
            _button("Controls", Controls);
            _button("Data", Data);
            _button("Back", () => { string error = _service.Flush(); if (error != null) { _error.text = error; return; } Hide(); _back(); });
        }
        internal void BeginGame(bool setup, Action start)
        { _setup = setup; _start = start; _service.BeginGameEdit(); Basic(); }

        private void Page(string title, string description)
        {
            _open(title, description); _layout.Clear(_rows); _rows.gameObject.SetActive(true);
            _error = Label("", _rows, 16, 38); _apply = null;
        }
        private void Finish(bool game = false)
        {
            if (game)
            {
                Validate();
                _button("Basic", Basic); _button("Roles", Roles); _button("Rules", Rules); _button("Stats", Stats); _button("Seed", Seed);
                _apply = _layout.Button(_layout.DrawerButtons, _setup ? "Start Run" : "Apply", "", 48);
                _apply.onClick.AddListener(() => {
                        if (!_service.ApplyGame(out string error)) { _error.text = error; return; }
                        string save = _service.Flush(); if (save != null) { _error.text = save; return; }
                        Hide(); if (_setup) _start?.Invoke(); else Show();
                    });
                Validate();
                _button("Cancel", () => { _service.CancelGame(); Hide(); if (_setup) _back(); else Show(); });
            }
            else _button("Back", Show);
            Canvas.ForceUpdateCanvases(); _layout.Resize();
        }
        private void Validate()
        {
            string error = _service.Validation();
            if (_error != null) _error.text = error ?? "";
            if (_apply != null) _apply.gameObject.SetActive(error == null);
        }
        private void Basic()
        {
            _rebuild = Basic;
            Page(_setup ? "New Run" : "Game", "Game changes apply to new runs after Apply.");
            GameSlider("Participants", GameSetting.PlayerCount, 5, _service.Draft.Capacity);
            Cycle("Preset", GameSetting.Preset, new[] { "Classic", "Chaos", "Minimal", "Custom" }, Basic);
            GameSlider("Evil", GameSetting.EvilCount, -1, _service.Draft.Get(GameSetting.PlayerCount) - 1, true);
            GameSlider("Neutral", GameSetting.NeutralCount, -1, _service.Draft.Get(GameSetting.PlayerCount) - 1, true);
            Cycle("Role-list visibility", GameSetting.Visibility, new[] { "Full list", "Faction counts", "Hidden" }, Basic);
            string[] choices = new[] { "Random", "Town", "Evil", "Neutral" }.Concat(_roles.Select(r => r.Id.Value)).ToArray();
            string selected = _service.Draft.PlayerRole ?? (_service.Draft.PlayerFaction.HasValue
                ? _service.Draft.PlayerFaction == Faction.Antagonist ? "Evil" : _service.Draft.PlayerFaction.Value.ToString() : "Random");
            Choice("Player role: " + selected, () => {
                int next = (Array.IndexOf(choices, selected) + 1) % choices.Length; string value = choices[next];
                _service.SelectPlayer(next >= 4 ? value : null, next == 1 ? Faction.Town : next == 2 ? Faction.Antagonist : next == 3 ? Faction.Neutral : (Faction?)null);
                Basic();
            }, () => { _service.SelectPlayer(null, null); Basic(); });
            Finish(true);
        }
        private void Rules()
        {
            _rebuild = Rules;
            Page("Rules", "Apply affects new runs.");
            GameSlider("Discussion rounds", GameSetting.Rounds, 3, 7);
            GameSlider("Player windows per round", GameSetting.Windows, 1, 2);
            GameToggle("One revote on a tie", GameSetting.Revote);
            GameToggle("Allow abstention", GameSetting.Abstain);
            GameToggle("Reveal roles on death", GameSetting.RevealRoles);
            GameToggle("Last words", GameSetting.LastWords);
            GameToggle("Promote Evil attack carrier", GameSetting.PromoteEvil);
            GameSlider("Nullifier charges", GameSetting.NullifierCharges, 0, 10);
            GameToggle("Nullifier backlash", GameSetting.Backlash);
            GameToggle("One Physician self-heal", GameSetting.SelfHeal);
            GameToggle("Notify lulled targets", GameSetting.NotifyLull);
            GameToggle("Aperture reads CLEAR", GameSetting.ForgerReadsClear);
            Cycle("Infinitum parity count", GameSetting.InfinitumParity, new[] { "Neither", "Town", "Evil" }, Rules);
            GameSlider("Day limit", GameSetting.MaximumDays, 1, 100);
            Finish(true);
        }
        private void Roles()
        {
            _rebuild = Roles;
            Page("Role limits", "Maximum 0 disables a role. Unique roles allow one copy.");
            foreach (var role in _roles)
            {
                var id = role.Id; var rule = _service.Draft.Limits.FirstOrDefault(r => r.Role == id);
                int maximum = rule?.Maximum ?? (role.Unique ? 1 : _service.Draft.Get(GameSetting.PlayerCount));
                int minimum = rule?.Minimum ?? 0;
                int cap = role.Unique ? 1 : _service.Draft.Get(GameSetting.PlayerCount);
                Choice(role.Name + " · min: " + minimum, () => {
                    int next = (minimum + 1) % (cap + 1);
                    _service.Limit(id, next, Math.Max(next, maximum)); Roles();
                }, () => { _service.Limit(id, 0, maximum); Roles(); });
                Choice(role.Name + " · max: " + Math.Min(maximum, cap), () => {
                    int next = (Math.Min(maximum, cap) + 1) % (cap + 1);
                    _service.Limit(id, Math.Min(minimum, next), next); Roles();
                }, () => { _service.Limit(id, minimum, cap); Roles(); });
            }
            _button("Edit slots", Slots);
            Finish(true);
        }
        private void Slots()
        {
            _rebuild = Slots;
            Page("Custom slots", "Specific role, Random Town/Evil/Neutral, or Any.");
            string[] choices = new[] { "Any", "Town", "Evil", "Neutral" }.Concat(_roles.Select(r => r.Id.Value)).ToArray();
            for (int i = 0; i < _service.Draft.Get(GameSetting.PlayerCount); i++)
            {
                int index = i; string selected = i < _service.Draft.Slots.Count ? _service.Draft.Slots[i] : "Any";
                Choice((i + 1) + ": " + selected, () => {
                    _service.Edit(GameSetting.Preset, (int)SetupPreset.Custom);
                    _service.Slot(index, choices[(Array.IndexOf(choices, selected) + 1) % choices.Length]); Slots();
                }, () => { _service.Slot(index, "Any"); Slots(); });
            }
            Finish(true);
        }
        private void Stats()
        {
            _rebuild = Stats;
            Page("Difficulty & stats", "Stats reset every run. Point-buy uses a 30-point budget.");
            Cycle("Difficulty", GameSetting.Difficulty, new[] { "Easy", "Normal", "Hard", "Brutal" }, Stats);
            Cycle("Stats", GameSetting.StatsMode, new[] { "Random", "Balanced", "Point-buy" }, Stats);
            if (_service.Draft.Get(GameSetting.StatsMode) == 2)
                foreach (var key in new[] { GameSetting.Charisma, GameSetting.Intuition, GameSetting.Logic, GameSetting.Charm, GameSetting.Performance, GameSetting.Stealth })
                    GameSlider(key.ToString(), key, 1, 10);
            Finish(true);
        }
        private void Seed()
        {
            _rebuild = Seed;
            Page("Seed", "Blank creates a random seed.");
            var row = Row(76); Label("Seed", row, 16, 26);
            var inputRoot = TextUiFactory.Rect("Seed input", row); inputRoot.gameObject.AddComponent<Image>(); TextUiFactory.Border(inputRoot);
            TextUiFactory.Fill(inputRoot, Vector2.zero, Vector2.one, new Vector2(0, 4), new Vector2(-76, -30));
            var input = inputRoot.gameObject.AddComponent<TMP_InputField>();
            var label = TextUiFactory.Text("Value", inputRoot, _font, 18);
            TextUiFactory.Fill(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-10, -4));
            input.textViewport = inputRoot; input.textComponent = label; input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.text = _service.Draft.Seed; input.onValueChanged.AddListener(value => { _service.Seed(value); Validate(); });
            Finish(true);
        }
        private void Text()
        {
            Page("Text & Display", "Changes apply immediately.");
            DisplaySlider("Text speed", DisplaySetting.Speed, 1, 240, true);
            DisplayToggle("Instant text", DisplaySetting.Instant);
            DisplayToggle("Auto-advance", DisplaySetting.AutoAdvance);
            DisplaySlider("Auto-advance delay", DisplaySetting.AutoDelay, 0.1f, 3);
            DisplaySlider("Text size", DisplaySetting.FontSize, 16, 32, true);
            DisplaySlider("UI scale", DisplaySetting.UiScale, 0.75f, 1.25f);
            DisplayToggle("Quick phrasing", DisplaySetting.QuickMode);
            Finish();
        }
        private void Accessibility()
        {
            Page("Accessibility", "Reduced motion suppresses movement and flashes.");
            DisplayToggle("Effects", DisplaySetting.Effects);
            DisplaySlider("Effects intensity", DisplaySetting.Intensity, 0, 1);
            DisplayToggle("Reduced motion", DisplaySetting.ReducedMotion);
            DisplayToggle("Allow safety-limited flashes", DisplaySetting.Flashes);
            DisplayToggle("Colorblind palette", DisplaySetting.Colorblind);
            Finish();
        }
        private void Controls()
        {
            Page("Controls", "Mouse/touch: choose a button or roster target.\nArrows / Enter: navigate / choose.\nSpace: reveal current line, then advance.\nEscape: menu.");
            Finish();
        }
        private void Data()
        {
            Page("Data", "Only preferences persist. Runs are never saved.");
            _button("Reset all settings", () => { _service.ResetAll(); _service.Flush(); Data(); });
            Finish();
        }
        private void GameSlider(string title, GameSetting key, float min, float max, bool auto = false)
            => SliderRow(title, _service.Draft.Get(key), min, max, true, value => { _service.Edit(key, (int)value); Validate(); },
                () => { _service.ResetGame(key); _rebuild(); }, auto);
        private void DisplaySlider(string title, DisplaySetting key, float min, float max, bool whole = false)
            => SliderRow(title, _service.Current.Display.Get(key), min, max, whole, value => _service.Display(key, value),
                () => { _service.ResetDisplay(key); if (key == DisplaySetting.Intensity) Accessibility(); else Text(); });
        private void GameToggle(string title, GameSetting key) => ToggleRow(title, _service.Draft.On(key),
            value => { _service.Edit(key, value ? 1 : 0); Validate(); }, () => { _service.ResetGame(key); Rules(); });
        private void DisplayToggle(string title, DisplaySetting key) => ToggleRow(title, _service.Current.Display.On(key),
            value => _service.Display(key, value ? 1 : 0), () => { _service.ResetDisplay(key); if (key == DisplaySetting.Effects || key == DisplaySetting.ReducedMotion || key == DisplaySetting.Flashes || key == DisplaySetting.Colorblind) Accessibility(); else Text(); });
        private void Cycle(string title, GameSetting key, string[] choices, Action rebuild)
            => Choice(title + ": " + choices[_service.Draft.Get(key)], () => {
                _service.Edit(key, (_service.Draft.Get(key) + 1) % choices.Length); rebuild();
            }, () => { _service.ResetGame(key); rebuild(); });
        private RectTransform Row(float height)
        {
            var row = TextUiFactory.Rect("Setting", _rows); var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height; layout.minHeight = height; return row;
        }
        private TextMeshProUGUI Label(string text, Transform parent, int size, float height)
        {
            var label = TextUiFactory.Text("Setting label", parent, _font, size); label.text = text; label.color = _layout.DrawerText.color;
            var layout = label.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = height; layout.minHeight = height;
            return label;
        }
        private void Choice(string title, Action next, Action reset)
        {
            var row = Row(52); var button = _layout.Button(row, title, "", 48);
            TextUiFactory.Fill((RectTransform)button.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-66, 0));
            var clear = _layout.Button(row, "↺", "", 48);
            TextUiFactory.Fill((RectTransform)clear.transform, new Vector2(1, 0), Vector2.one, new Vector2(-60, 0), Vector2.zero);
            button.onClick.AddListener(() => next()); clear.onClick.AddListener(() => reset());
        }
        private void SliderRow(string title, float current, float min, float max, bool whole, Action<float> changed, Action reset, bool auto = false)
        {
            var row = Row(80); var label = Label(title + ": " + (auto && current < 0 ? "Auto" : current.ToString("0.##")), row, 16, 28);
            TextUiFactory.Fill(label.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -28), Vector2.zero);
            var root = TextUiFactory.Rect(title, row);
            TextUiFactory.Fill(root, Vector2.zero, Vector2.one, new Vector2(0, 6), new Vector2(-76, -34));
            var track = TextUiFactory.Rect("Track", root); TextUiFactory.Fill(track, Vector2.zero, Vector2.one, new Vector2(0, 14), new Vector2(0, -14));
            var image = track.gameObject.AddComponent<Image>(); image.color = new Color(0.4f, 0.45f, 0.4f, 0.35f);
            var fill = TextUiFactory.Rect("Fill", track); TextUiFactory.Fill(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); fill.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.5f, 0.4f);
            var handle = TextUiFactory.Rect("Handle", root); handle.sizeDelta = new Vector2(22, 30);
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = _layout.DrawerText.color;
            var slider = root.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = whole; slider.value = current;
            slider.onValueChanged.AddListener(value => {
                changed(value); label.text = title + ": " + (auto && value < 0 ? "Auto" : value.ToString("0.##"));
            });
            var clear = _layout.Button(row, "↺", "", 40);
            TextUiFactory.Fill((RectTransform)clear.transform, new Vector2(1, 0), Vector2.one, new Vector2(-60, 0), new Vector2(0, -34));
            clear.onClick.AddListener(() => reset());
        }
        private void ToggleRow(string title, bool current, Action<bool> changed, Action reset)
        {
            var row = Row(50); var label = Label(title, row, 16, 40);
            TextUiFactory.Fill(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 8), new Vector2(-140, -4));
            var root = TextUiFactory.Rect(title, row);
            TextUiFactory.Fill(root, new Vector2(1, 0), Vector2.one, new Vector2(-124, 6), new Vector2(-76, -6));
            var image = root.gameObject.AddComponent<Image>(); image.color = new Color(0.5f, 0.5f, 0.45f, 0.3f); TextUiFactory.Border(root);
            var check = TextUiFactory.Rect("Check", root); TextUiFactory.Fill(check, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            var checkImage = check.gameObject.AddComponent<Image>(); checkImage.color = _layout.DrawerText.color;
            var toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = image; toggle.graphic = checkImage; toggle.isOn = current;
            toggle.onValueChanged.AddListener(value => changed(value));
            var clear = _layout.Button(row, "↺", "", 40);
            TextUiFactory.Fill((RectTransform)clear.transform, new Vector2(1, 0), Vector2.one, new Vector2(-60, 4), new Vector2(0, -4));
            clear.onClick.AddListener(() => reset());
        }
    }
}
