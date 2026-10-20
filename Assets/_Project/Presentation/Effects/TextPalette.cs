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
        public Color Background => Night ? new Color(0.075f, 0.09f, 0.105f)
            : Color.Lerp(new Color(0.975f, 0.955f, 0.87f), new Color(0.955f, 0.84f, 0.80f), DayProgress);
        public Color Foreground => Night ? new Color(0.94f, 0.92f, 0.86f) : new Color(0.16f, 0.14f, 0.13f);
        public Color Panel => Night ? new Color(0.105f, 0.125f, 0.14f) : Color.Lerp(Background, Color.white, 0.38f);
        public Color ButtonBackground => Night ? new Color(0.145f, 0.165f, 0.18f) : Color.Lerp(Background, Color.white, 0.68f);
        public Color FocusBackground => Night ? new Color(0.22f, 0.275f, 0.26f) : new Color(0.91f, 0.92f, 0.81f);
        public Color Muted => Night ? new Color(0.77f, 0.79f, 0.75f) : new Color(0.34f, 0.31f, 0.28f);
        public Color Frame => Night ? new Color(0.62f, 0.65f, 0.59f, 0.35f) : new Color(0.48f, 0.39f, 0.30f, 0.25f);
        /// <summary>Situation colours encode public actions, not secret factions or verified truth.</summary>
        public Color Situation(EffectTone tone)
        {
            switch (tone)
            {
                case EffectTone.Pressure: return Night ? new Color(1f, 0.53f, 0.27f) : new Color(0.76f, 0.23f, 0.06f);
                case EffectTone.Reassurance: return Night ? new Color(0.32f, 0.88f, 0.72f) : new Color(0.02f, 0.43f, 0.36f);
                case EffectTone.Evidence: return Night ? new Color(0.34f, 0.70f, 1f) : new Color(0.08f, 0.36f, 0.75f);
                case EffectTone.Challenge: return Night ? new Color(0.83f, 0.52f, 1f) : new Color(0.51f, 0.22f, 0.72f);
                case EffectTone.Judgement: return Night ? new Color(1f, 0.77f, 0.30f) : new Color(0.66f, 0.40f, 0.03f);
                case EffectTone.Loss: return Night ? new Color(1f, 0.39f, 0.47f) : new Color(0.65f, 0.09f, 0.20f);
                case EffectTone.Triumph: return Night ? new Color(0.61f, 0.93f, 0.40f) : new Color(0.29f, 0.50f, 0.08f);
                case EffectTone.Night: return new Color(0.38f, 0.44f, 0.87f);
                case EffectTone.Morning: return new Color(0.88f, 0.55f, 0.18f);
                case EffectTone.Uncertainty: return Night ? new Color(0.78f, 0.65f, 1f) : new Color(0.47f, 0.32f, 0.64f);
                default: return Foreground;
            }
        }
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
