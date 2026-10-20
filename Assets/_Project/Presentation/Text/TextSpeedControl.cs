using System;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Dialogue;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SocialStim.Presentation.Text
{
    /// <summary>Native percentage slider; settings do not submit gameplay commands.</summary>
    public sealed class TextSpeedControl : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Slider _slider;
        private TextMeshProUGUI _label, _hint;
        private Image _track, _fill, _handle;
        private Outline _outline;
        private GameText _words;
        private bool _focused;
        private TextPalette _palette;
        public void Initialize(TMP_FontAsset font, GameText words, Action<float> changed)
        {
            _words = words;
            gameObject.AddComponent<LayoutElement>().preferredHeight = 82;
            _label = TextUiFactory.Text("Speed label", transform, font, 14);
            TextUiFactory.Fill(_label.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(4, -23), new Vector2(-4, 0));
            _hint = TextUiFactory.Text("Speed help", transform, font, 11); _hint.text = words.Format("ui.speedHelp");
            TextUiFactory.Fill(_hint.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(4, 0), new Vector2(-4, 22));
            RectTransform rail = TextUiFactory.Rect("Speed slider", transform);
            TextUiFactory.Fill(rail, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(14, -16), new Vector2(-14, 16));
            _track = rail.gameObject.AddComponent<Image>(); TextUiFactory.Border(rail); _outline = rail.GetComponent<Outline>();
            _slider = rail.gameObject.AddComponent<Slider>(); _slider.minValue = 0; _slider.maxValue = 200; _slider.wholeNumbers = true;
            var fill = TextUiFactory.Rect("Fill", rail); TextUiFactory.Fill(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _fill = fill.gameObject.AddComponent<Image>(); _fill.raycastTarget = false;
            var area = TextUiFactory.Rect("Handle area", rail); TextUiFactory.Fill(area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handle = TextUiFactory.Rect("Handle", area); handle.sizeDelta = new Vector2(18, 30);
            _handle = handle.gameObject.AddComponent<Image>(); TextUiFactory.Border(handle);
            _slider.fillRect = fill; _slider.handleRect = handle; _slider.targetGraphic = _handle;
            _slider.direction = Slider.Direction.LeftToRight; _slider.transition = Selectable.Transition.None;
            _slider.onValueChanged.AddListener(value => changed(value));
        }
        public void Refresh(EffectsSettings settings)
        {
            _slider.SetValueWithoutNotify(Mathf.Clamp(settings.TextSpeedPercent, 0, 200));
            _label.text = (_focused ? "> " : "") + _words.Format("ui.textSpeed", ("percent", _slider.value.ToString("0")));
        }
        public void Theme(TextPalette palette)
        {
            _palette = palette; _label.color = palette.Foreground; _hint.color = palette.Muted;
            _track.color = palette.ButtonBackground; _fill.color = palette.Muted; _handle.color = palette.Foreground;
            _outline.effectColor = _focused ? palette.Foreground : palette.Frame;
        }
        public void OnSelect(BaseEventData data) { _focused = true; Theme(_palette); }
        public void OnDeselect(BaseEventData data) { _focused = false; Theme(_palette); }
        private void Update()
        {
            bool focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _slider.gameObject;
            if (_focused != focused) { _focused = focused; Theme(_palette); _label.text = (_focused ? "> " : "") + _words.Format("ui.textSpeed", ("percent", _slider.value.ToString("0"))); }
            if (!focused || Keyboard.current == null) return;
            if (Keyboard.current.homeKey.wasPressedThisFrame) _slider.value = 0;
            if (Keyboard.current.endKey.wasPressedThisFrame) _slider.value = 200;
        }
    }
}
