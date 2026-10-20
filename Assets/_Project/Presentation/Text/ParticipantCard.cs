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
        private SocialStim.Rules.Dialogue.TextTemplates _text;
        private Image _surface;
        private bool _self, _speaking, _selectable;
        private Button _button;
        private ChoiceFocusIndicator _focus;
        /// <summary>Roster selection by public identity only.</summary>
        public event Action<CharacterId> Selected;
        public CharacterId Id { get; private set; }

        public void Initialize(CharacterId id, TMP_FontAsset font, SocialStim.Rules.Dialogue.TextTemplates text = null)
        {
            Id = id; _text = text ?? SocialStim.Data.Definitions.GameText.Load();
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
            string text = _text.Render("roster.card", ("name", participant.Name), ("self", self ? _text.Render("roster.self") : ""), ("state", _text.Render(!participant.Alive ? "roster.dead" : participant.IsTeammate ? "roster.teammate" : "roster.alive")), ("activity", speaking ? _text.Render("roster.speaking") : selectable ? _text.Render("roster.select") : ""), ("mark", string.IsNullOrEmpty(mark) ? "" : _text.Render("roster.mark", ("mark", _text.Render("mark." + mark)))), ("claim", string.IsNullOrEmpty(claim) ? "" : _text.Render("roster.claim", ("role", claim))));
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
