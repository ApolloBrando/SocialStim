using SocialStim.Application.PresentationCues;
using UnityEngine;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Minimal inverse day/night palettes, with readable semantic accents.</summary>
    public readonly struct TextPalette
    {
        public TextPalette(bool night) { Night = night; }
        public bool Night { get; }
        public Color Background => Night ? new Color(0.96f, 0.95f, 0.93f) : new Color(0.035f, 0.035f, 0.04f);
        public Color Foreground => Night ? new Color(0.07f, 0.07f, 0.08f) : new Color(0.92f, 0.92f, 0.9f);
        public Color Accent(SemanticStyle style)
        {
            switch (style)
            {
                case SemanticStyle.Emphasis: return Night ? new Color(0.01f, 0.01f, 0.02f) : Color.white;
                case SemanticStyle.Fear: return Night ? new Color(0.38f, 0.18f, 0.48f) : new Color(0.83f, 0.7f, 0.95f);
                case SemanticStyle.Calm: return Night ? new Color(0.07f, 0.3f, 0.26f) : new Color(0.6f, 0.86f, 0.77f);
                case SemanticStyle.Evidence: return Night ? new Color(0.1f, 0.27f, 0.45f) : new Color(0.63f, 0.8f, 0.98f);
                case SemanticStyle.Danger: return Night ? new Color(0.55f, 0.1f, 0.1f) : new Color(0.98f, 0.68f, 0.64f);
                default: return Foreground;
            }
        }
    }
}
