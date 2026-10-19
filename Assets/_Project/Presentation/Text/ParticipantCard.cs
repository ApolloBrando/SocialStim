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
        private bool _self;
        public CharacterId Id { get; private set; }

        public void Initialize(CharacterId id, TMP_FontAsset font)
        {
            Id = id;
            _surface = gameObject.AddComponent<Image>(); _surface.raycastTarget = false;
            TextUiFactory.Border((RectTransform)transform);
            _label = TextUiFactory.Text("Identity", transform, font, 14);
            _label.alignment = TextAlignmentOptions.Center;
            _label.overflowMode = TextOverflowModes.Ellipsis;
            TextUiFactory.Fill(_label.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            var layout = gameObject.AddComponent<LayoutElement>(); layout.minWidth = 104; layout.preferredWidth = 104; layout.preferredHeight = 40;
        }

        public void Render(KnownCharacter participant, bool self, TextPalette palette)
        {
            _self = self;
            _label.text = participant.Name + (self ? " (you)" : "")
                + "\n" + (!participant.Alive ? "DEAD" : participant.IsTeammate ? "TEAMMATE" : "ALIVE");
            Theme(palette);
        }

        public void Theme(TextPalette palette)
        {
            _surface.color = _self ? palette.FocusBackground : palette.Panel;
            GetComponent<Outline>().effectColor = palette.Frame;
            _label.color = palette.Foreground;
        }
    }
}
