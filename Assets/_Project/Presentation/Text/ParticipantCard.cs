using SocialStim.Rules.Cast;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
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
        private TextMeshProUGUI _claim;
        private Image _surface;
        private bool _self;
        private bool _alive;
        private RoleSpec _claimedRole;
        public CharacterId Id { get; private set; }

        public void Initialize(CharacterId id, TMP_FontAsset font)
        {
            Id = id;
            _surface = gameObject.AddComponent<Image>(); _surface.raycastTarget = false;
            TextUiFactory.Border((RectTransform)transform);
            _label = TextUiFactory.Text("Identity", transform, font, 14);
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.overflowMode = TextOverflowModes.Ellipsis;
            TextUiFactory.Fill(_label.rectTransform, Vector2.zero, new Vector2(0.72f, 1), new Vector2(8, 2), new Vector2(-4, -2));
            _claim = TextUiFactory.Text("Claimed role", transform, font, 14);
            _claim.alignment = TextAlignmentOptions.MidlineRight;
            TextUiFactory.Fill(_claim.rectTransform, new Vector2(0.72f, 0), Vector2.one, Vector2.zero, new Vector2(-8, 0));
            var layout = gameObject.AddComponent<LayoutElement>(); layout.minWidth = 180; layout.preferredWidth = 210; layout.preferredHeight = 44;
        }

        public void Render(KnownCharacter participant, bool self, TextPalette palette, RoleSpec claimedRole = null)
        {
            _self = self;
            _alive = participant.Alive; _claimedRole = claimedRole;
            _label.text = participant.Name + (self ? " (you)" : "")
                + "\n" + (!participant.Alive ? "DEAD" : participant.IsTeammate ? "TEAMMATE" : "ALIVE");
            _claim.text = claimedRole == null ? "" : claimedRole.Name.Substring(0, System.Math.Min(3, claimedRole.Name.Length)).ToUpperInvariant();
            Theme(palette);
        }

        public void Theme(TextPalette palette)
        {
            _surface.color = _self ? palette.FocusBackground : palette.Panel;
            GetComponent<Outline>().effectColor = palette.Frame;
            Color dead = palette.Night ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.38f, 0.38f, 0.38f);
            _label.color = _alive ? palette.Foreground : dead;
            _claim.color = !_alive ? dead : _claimedRole == null ? palette.Muted
                : palette.Accent(_claimedRole.Faction == Faction.Town ? SocialStim.Application.PresentationCues.SemanticStyle.Evidence
                    : _claimedRole.Faction == Faction.Antagonist ? SocialStim.Application.PresentationCues.SemanticStyle.Danger
                    : SocialStim.Application.PresentationCues.SemanticStyle.Fear);
        }
    }
}
