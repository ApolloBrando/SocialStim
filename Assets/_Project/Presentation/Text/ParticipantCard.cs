using SocialStim.Rules.Cast;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace SocialStim.Presentation.Text
{
    /// <summary>Stable public participant UI object; no secret roles or belief scores are rendered.</summary>
    public sealed class ParticipantCard : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private TextMeshProUGUI _claim;
        private TextMeshProUGUI _state;
        private Image _surface;
        private bool _self;
        private bool _alive;
        private RoleSpec _claimedRole;
        private Button _button;
        private bool _selected;
        private ChoiceFocusIndicator _focus;
        public event Action<CharacterId> Selected;
        public CharacterId Id { get; private set; }

        public void Initialize(CharacterId id, TMP_FontAsset font)
        {
            Id = id;
            _surface = gameObject.AddComponent<Image>(); _surface.raycastTarget = true;
            TextUiFactory.Border((RectTransform)transform);
            _button = gameObject.AddComponent<Button>(); _button.targetGraphic = _surface; _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(() => Selected?.Invoke(Id));
            _label = TextUiFactory.Text("Identity", transform, font, 14);
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.overflowMode = TextOverflowModes.Ellipsis;
            TextUiFactory.Fill(_label.rectTransform, Vector2.zero, new Vector2(0.72f, 1), new Vector2(8, 26), new Vector2(-4, -2));
            _state = TextUiFactory.Text("State", transform, font, 12);
            TextUiFactory.Fill(_state.rectTransform, Vector2.zero, new Vector2(0.72f, 0.5f), new Vector2(8, 2), new Vector2(-4, 0));
            _claim = TextUiFactory.Text("Claimed role", transform, font, 14);
            _claim.alignment = TextAlignmentOptions.MidlineRight;
            TextUiFactory.Fill(_claim.rectTransform, new Vector2(0.72f, 0), Vector2.one, Vector2.zero, new Vector2(-8, 0));
            _focus = gameObject.AddComponent<ChoiceFocusIndicator>(); _focus.Initialize(_label, "");
            var layout = gameObject.AddComponent<LayoutElement>(); layout.minWidth = 130; layout.preferredWidth = 160; layout.preferredHeight = 64;
        }

        public void Render(KnownCharacter participant, bool self, TextPalette palette, RoleSpec claimedRole = null,
            bool selectable = false, bool selected = false, string annotation = "", SocialStim.Rules.Dialogue.GameText words = null, RoleSpec spectatorRole = null)
        {
            words = words ?? SocialStim.Rules.Dialogue.GameText.Default;
            _self = self;
            _alive = participant.Alive; _claimedRole = claimedRole;
            _selected = selected; _button.interactable = selectable;
            _label.text = (participant.Id.Value + 1) + ". " + participant.Name + (self ? " " + words.Format("ui.you") : "")
                + (spectatorRole == null ? "" : " [" + (words.Entries.TryGetValue("role.short." + spectatorRole.Id.Value, out string shortName)
                    ? shortName : spectatorRole.Name) + "]");
            _focus.SetText(_label.text);
            _state.text = (words.Format(!participant.Alive ? "ui.dead" : participant.IsTeammate ? "ui.teammate" : "ui.alive"))
                + (string.IsNullOrEmpty(annotation) ? "" : "\n" + annotation);
            _claim.text = claimedRole == null ? "" : claimedRole.Name.Substring(0, System.Math.Min(3, claimedRole.Name.Length)).ToUpperInvariant();
            Theme(palette);
        }

        public void Theme(TextPalette palette)
        {
            _surface.color = _selected ? Color.Lerp(palette.Panel, palette.Accent(SocialStim.Application.PresentationCues.SemanticStyle.Participant), 0.18f) : _self ? palette.FocusBackground : palette.Panel;
            _focus.SetSurfaceColors(_surface.color, palette.FocusBackground);
            GetComponent<Outline>().effectColor = _selected ? palette.Accent(SocialStim.Application.PresentationCues.SemanticStyle.Evidence) : palette.Frame;
            Color dead = palette.Night ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.38f, 0.38f, 0.38f);
            _label.color = _alive ? palette.Character(Id.Value) : dead;
            _state.color = _alive ? palette.Muted : dead;
            _claim.color = !_alive ? dead : _claimedRole == null ? palette.Muted
                : palette.RoleColor(_claimedRole.Faction);
        }
        public void SetSelectable(bool selectable) { if (_button != null) _button.interactable = selectable; }
    }
}
