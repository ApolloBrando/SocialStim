using SocialStim.Application.PresentationCues;
using UnityEngine;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Warm light daytime surfaces and dark nighttime surfaces with contrasting text.</summary>
    public readonly struct TextPalette
    {
        public TextPalette(bool night, float dayProgress = 0) { Night = night; DayProgress = Mathf.Clamp01(dayProgress); }
        public bool Night { get; }
        public float DayProgress { get; }
        public Color Background => Night ? new Color(0.035f, 0.035f, 0.045f)
            : Color.Lerp(new Color(0.98f, 0.96f, 0.84f), new Color(0.96f, 0.80f, 0.78f), DayProgress);
        public Color Foreground => Night ? new Color(0.93f, 0.93f, 0.91f) : new Color(0.10f, 0.08f, 0.09f);
        public Color Accent(SemanticStyle style)
        {
            switch (style)
            {
                case SemanticStyle.Emphasis: return Foreground;
                case SemanticStyle.Fear: return Night ? new Color(0.84f, 0.72f, 0.96f) : new Color(0.36f, 0.12f, 0.42f);
                case SemanticStyle.Calm: return Night ? new Color(0.6f, 0.86f, 0.77f) : new Color(0.04f, 0.28f, 0.22f);
                case SemanticStyle.Evidence: return Night ? new Color(0.65f, 0.82f, 1f) : new Color(0.04f, 0.23f, 0.43f);
                case SemanticStyle.Danger: return Night ? new Color(1f, 0.7f, 0.66f) : new Color(0.48f, 0.06f, 0.07f);
                default: return Foreground;
            }
        }
    }
}
