using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Reasoning;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Stable public participant UI object; no secret roles or belief scores are rendered.</summary>
    public sealed class ParticipantCard : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private Image _surface;
        private bool _self, _speaking, _selectable;
        private Button _button;
        private ChoiceFocusIndicator _focus;
        /// <summary>Roster selection by public identity only.</summary>
        public event Action<CharacterId> Selected;
        public CharacterId Id { get; private set; }

        public void Initialize(CharacterId id, TMP_FontAsset font)
        {
            Id = id;
            _surface = gameObject.AddComponent<Image>(); _surface.raycastTarget = true;
            _button = gameObject.AddComponent<Button>(); _button.targetGraphic = _surface; _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(() => Selected?.Invoke(Id));
            TextUiFactory.Border((RectTransform)transform);
            _label = TextUiFactory.Text("Identity", transform, font, 14);
            _label.alignment = TextAlignmentOptions.Center;
            _label.overflowMode = TextOverflowModes.Ellipsis;
            TextUiFactory.Fill(_label.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            var layout = gameObject.AddComponent<LayoutElement>(); layout.minWidth = 144; layout.preferredWidth = 144; layout.preferredHeight = 76;
            _focus = gameObject.AddComponent<ChoiceFocusIndicator>(); _focus.Initialize(_label, "");
        }

        public void Render(KnownCharacter participant, bool self, TextPalette palette, bool speaking = false, bool selectable = false, string mark = "", string claim = "", bool targetMode = false)
        {
            _self = self; _speaking = speaking; _selectable = selectable; _button.interactable = !targetMode || selectable;
            string text = participant.Name + (self ? " (you)" : "")
                + "\n" + (!participant.Alive ? "DEAD" : participant.IsTeammate ? "TEAMMATE" : "ALIVE");
            text += (speaking ? " · SPEAKING" : selectable ? " · SELECT" : "")
                + (string.IsNullOrEmpty(mark) ? "" : "\nMark: " + mark) + (string.IsNullOrEmpty(claim) ? "" : "\nClaims " + claim);
            _focus.SetText(text); Theme(palette);
        }

        public void Theme(TextPalette palette)
        {
            _surface.color = _self || _speaking || _selectable ? palette.FocusBackground : palette.Panel;
            GetComponent<Outline>().effectColor = palette.Frame;
            _label.color = palette.Foreground;
            _focus.SetSurfaceColors(_surface.color, palette.FocusBackground);
        }
    }
}
