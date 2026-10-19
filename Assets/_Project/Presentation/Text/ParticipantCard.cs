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
        public CharacterId Id { get; private set; }

        public void Initialize(CharacterId id, TMP_FontAsset font)
        {
            Id = id;
            _surface = gameObject.AddComponent<Image>(); _surface.raycastTarget = false;
            TextUiFactory.Border((RectTransform)transform);
            _label = TextUiFactory.Text("Identity", transform, font, 14);
            _label.alignment = TextAlignmentOptions.Center;
            TextUiFactory.Fill(_label.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            var layout = gameObject.AddComponent<LayoutElement>(); layout.minWidth = 72; layout.preferredHeight = 40;
        }

        public void Render(KnownCharacter participant, bool self, TextPalette palette)
        {
            _label.text = participant.Name + (self ? " (you)" : "")
                + "\n" + (!participant.Alive ? "DEAD" : participant.IsTeammate ? "TEAMMATE" : "ALIVE");
            Theme(palette);
        }

        public void Theme(TextPalette palette)
        {
            _surface.color = palette.Background;
            _label.color = palette.Foreground;
        }
    }
}
