using System;
using System.Collections.Generic;
using SocialStim.Application.Settings;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Modal, grouped preference controls with immediate display edits and explicit next-run configuration.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private RectTransform _root, _rows;
        private TMP_FontAsset _font;
        private GamePreferences _preferences = new GamePreferences();
        private Action<GamePreferences> _changed;
        private TextPalette _palette;
        private GameObject _previousFocus;
        private readonly List<Action> _refresh = new List<Action>();
        private Button _close;
        /// <summary>Builds the modal once; rows are rebuilt only when changing groups.</summary>
        public void Initialize(Transform parent, TMP_FontAsset font, Action<GamePreferences> changed)
        {
            _font = font; _changed = changed;
            _root = TextUiFactory.Rect("Settings Modal", parent);
            TextUiFactory.Fill(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            var panel = TextUiFactory.Rect("Settings", _root);
            TextUiFactory.Fill(panel, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>(); TextUiFactory.Border(panel);
            var tabs = TextUiFactory.Rect("Groups", panel);
            TextUiFactory.Fill(tabs, new Vector2(0, 1), Vector2.one, new Vector2(20, -66), new Vector2(-20, -16));
            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 8;
            foreach (string group in new[] { "Game", "Text & Display", "Accessibility", "Data" })
            { string selected = group; Button button = TextUiFactory.Button(tabs, font, group, 42); button.onClick.AddListener(() => Group(selected)); }
            var scroll = TextUiFactory.Scroll("Preferences", panel, out _rows);
            TextUiFactory.Fill((RectTransform)scroll.transform, Vector2.zero, Vector2.one, new Vector2(16, 76), new Vector2(-16, -76));
            var rows = _rows.gameObject.AddComponent<VerticalLayoutGroup>(); rows.spacing = 8;
            rows.childControlHeight = true; rows.childControlWidth = true; rows.childForceExpandHeight = false;
            _rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _close = TextUiFactory.Button(panel, font, "Close settings", 42);
            TextUiFactory.Fill((RectTransform)_close.transform, Vector2.zero, Vector2.zero, new Vector2(20, 18), new Vector2(200, 62));
            _close.onClick.AddListener(Close);
            var hint = TextUiFactory.Text("Apply policy", panel, font, 15);
            hint.text = "Display changes apply now. Game changes apply to the next run.";
            TextUiFactory.Fill(hint.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(224, 20), new Vector2(-20, 60));
            _root.gameObject.SetActive(false); Group("Text & Display");
        }
        /// <summary>Synchronizes controls without invoking their edit callbacks.</summary>
        public void SetPreferences(GamePreferences preferences)
        { _preferences = preferences; foreach (Action refresh in _refresh) refresh(); }
        /// <summary>Opens/closes the modal and restores keyboard focus on close.</summary>
        public void Toggle(GamePreferences preferences)
        {
            if (_root.gameObject.activeSelf) { Close(); return; }
            SetPreferences(preferences); _previousFocus = EventSystem.current?.currentSelectedGameObject;
            _root.SetAsLastSibling(); _root.gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_close.gameObject);
        }
        /// <summary>Returns focus to the previous usable control.</summary>
        public void Close()
        {
            _root.gameObject.SetActive(false);
            if (_previousFocus != null && _previousFocus.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(_previousFocus);
        }
        /// <summary>Updates every settings surface to match the phase palette.</summary>
        public void SetPalette(TextPalette palette)
        {
            _palette = palette;
            foreach (Image image in _root.GetComponentsInChildren<Image>(true)) if (image.transform != _root) image.color = image.name == "Handle" ? palette.Foreground : palette.Panel;
            foreach (TextMeshProUGUI text in _root.GetComponentsInChildren<TextMeshProUGUI>(true)) text.color = palette.Foreground;
            foreach (Button button in _root.GetComponentsInChildren<Button>(true))
                button.GetComponent<ChoiceFocusIndicator>()?.SetSurfaceColors(palette.ButtonBackground, palette.FocusBackground);
        }
        private void Update()
        {
            if (!_root.gameObject.activeSelf) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            // Keep automatic navigation inside the modal even if a control behind it was selected externally.
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(_root)) EventSystem.current?.SetSelectedGameObject(_close.gameObject);
        }
        private void Commit(GamePreferences preferences) { SetPreferences(preferences); _changed(preferences); }
        private void Group(string group)
        {
            _refresh.Clear();
            foreach (Transform child in _rows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (group == "Game")
            {
                Number("Participants", PreferenceKey.PlayerCount, 5, 20, true); Number("Discussion rounds", PreferenceKey.Rounds, 3, 7, true);
                Number("Player windows", PreferenceKey.Windows, 1, 2, true); Toggle("Trial Mode", PreferenceKey.Mode);
                Toggle("Allow abstentions", PreferenceKey.Abstain); Toggle("One tie revote", PreferenceKey.Revote);
                Toggle("Reveal roles on death", PreferenceKey.Reveal); Toggle("Last words", PreferenceKey.LastWords);
                Number("Trials per day (Trial Mode)", PreferenceKey.Trials, 1, 10, true); Seed();
            }
            else if (group == "Text & Display")
            {
                Number("Text speed", PreferenceKey.Speed, 1, 240); Toggle("Instant text", PreferenceKey.InstantText);
                Number("Text size", PreferenceKey.TextSize, 16, 36); Number("UI scale", PreferenceKey.UiScale, 0.75f, 1.5f);
                Toggle("Auto advance", PreferenceKey.AutoAdvance); Number("Auto advance hold", PreferenceKey.AdvanceDelay, 0.25f, 5);
                Toggle("Quick phrasing", PreferenceKey.QuickMode);
            }
            else if (group == "Accessibility")
            {
                Toggle("Effects", PreferenceKey.Effects); Toggle("Reduced motion", PreferenceKey.ReducedMotion);
                Toggle("Allow safe flashes", PreferenceKey.Flashes); Number("Effects intensity", PreferenceKey.Intensity, 0, 1);
                Toggle("Neutral accent palette", PreferenceKey.Colorblind);
            }
            else
            {
                Button reset = TextUiFactory.Button(_rows, _font, "Reset all defaults", 52);
                reset.onClick.AddListener(() => Commit(new GamePreferences()));
            }
            SetPalette(_palette);
        }
        private RectTransform Row(int height)
        {
            var row = TextUiFactory.Rect("Preference", _rows);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height; return row;
        }
        private void ResetButton(RectTransform row, PreferenceKey key)
        {
            Button reset = TextUiFactory.Button(row, _font, "Reset", 38);
            TextUiFactory.Fill((RectTransform)reset.transform, new Vector2(0.82f, 0), Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            reset.onClick.AddListener(() => Commit(PreferencesEditor.Reset(_preferences, key)));
        }
        private void Toggle(string title, PreferenceKey key)
        {
            RectTransform row = Row(54); Button toggle = TextUiFactory.Button(row, _font, title, 42);
            TextUiFactory.Fill((RectTransform)toggle.transform, Vector2.zero, new Vector2(0.82f, 1), Vector2.zero, Vector2.zero);
            Action refresh = () => toggle.GetComponent<ChoiceFocusIndicator>().SetText(title + ": "
                + (PreferencesEditor.Value(_preferences, key) >= 0.5f ? "On" : "Off"));
            _refresh.Add(refresh); refresh();
            toggle.onClick.AddListener(() => Commit(PreferencesEditor.Apply(_preferences, key, PreferencesEditor.Value(_preferences, key) >= 0.5f ? 0 : 1)));
            ResetButton(row, key);
        }
        private void Number(string title, PreferenceKey key, float low, float high, bool whole = false)
        {
            RectTransform row = Row(80); var label = TextUiFactory.Text(title, row, _font, 18);
            TextUiFactory.Fill(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0.8f, 1), Vector2.zero, Vector2.zero);
            RectTransform area = TextUiFactory.Rect("Slider", row);
            TextUiFactory.Fill(area, new Vector2(0.02f, 0.08f), new Vector2(0.78f, 0.44f), Vector2.zero, Vector2.zero);
            area.gameObject.AddComponent<Image>(); var slider = area.gameObject.AddComponent<Slider>();
            var handle = TextUiFactory.Rect("Handle", area); var image = handle.gameObject.AddComponent<Image>();
            handle.sizeDelta = new Vector2(20, 28); slider.handleRect = handle; slider.targetGraphic = image;
            slider.minValue = low; slider.maxValue = high; slider.wholeNumbers = whole;
            Action refresh = () => { float value = PreferencesEditor.Value(_preferences, key); slider.SetValueWithoutNotify(value); label.text = title + ": " + value.ToString("0.##"); };
            _refresh.Add(refresh); refresh();
            slider.onValueChanged.AddListener(value => Commit(PreferencesEditor.Apply(_preferences, key, value))); ResetButton(row, key);
        }
        private void Seed()
        {
            RectTransform row = Row(70); var field = row.gameObject.AddComponent<TMP_InputField>(); row.gameObject.AddComponent<Image>();
            var text = TextUiFactory.Text("Seed", row, _font, 18); field.textComponent = text; field.textViewport = row;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.SetTextWithoutNotify(_preferences.Game.Seed?.ToString() ?? "");
            var hint = TextUiFactory.Text("Seed hint", row, _font, 14); hint.text = "Seed (blank = random)";
            TextUiFactory.Fill(hint.rectTransform, new Vector2(0, 0.65f), Vector2.one, Vector2.zero, Vector2.zero);
            TextUiFactory.Fill(text.rectTransform, Vector2.zero, new Vector2(1, 0.6f), new Vector2(8, 0), new Vector2(-8, 0));
            field.onEndEdit.AddListener(value => {
                if (string.IsNullOrWhiteSpace(value)) Commit(PreferencesEditor.Seed(_preferences, null));
                else if (uint.TryParse(value, out uint seed)) Commit(PreferencesEditor.Seed(_preferences, seed));
                else { hint.text = "Seed must be a number from 0 to 4294967295."; field.SetTextWithoutNotify(_preferences.Game.Seed?.ToString() ?? ""); }
            });
        }
    }
}
