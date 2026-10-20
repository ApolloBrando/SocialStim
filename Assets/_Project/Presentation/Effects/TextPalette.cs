using SocialStim.Application.PresentationCues;
using UnityEngine;
using SocialStim.Rules.Roles;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Warm light daytime surfaces and dark nighttime surfaces with contrasting text.</summary>
    public readonly struct TextPalette
    {
        public TextPalette(bool night, float dayProgress = 0) { Night = night; DayProgress = Mathf.Clamp01(dayProgress); }
        public bool Night { get; }
        public float DayProgress { get; }
        public Color Background => Night ? new Color(0.035f, 0.055f, 0.12f) : SkyBottom;
        public Color SkyTop => Night ? new Color(0.025f, 0.035f, 0.09f)
            : DayProgress < 0.5f ? Color.Lerp(new Color(0.46f, 0.71f, 0.92f), new Color(0.73f, 0.83f, 0.96f), DayProgress * 2)
                : Color.Lerp(new Color(0.73f, 0.83f, 0.96f), new Color(0.78f, 0.62f, 0.78f), (DayProgress - 0.5f) * 2);
        public Color SkyBottom => Night ? new Color(0.10f, 0.13f, 0.26f)
            : DayProgress < 0.5f ? Color.Lerp(new Color(0.96f, 0.94f, 0.82f), new Color(1f, 0.86f, 0.57f), DayProgress * 2)
                : Color.Lerp(new Color(1f, 0.86f, 0.57f), new Color(0.93f, 0.51f, 0.32f), (DayProgress - 0.5f) * 2);
        public Color Foreground => Night ? new Color(0.94f, 0.92f, 0.86f) : new Color(0.16f, 0.14f, 0.13f);
        public Color Panel => Night ? new Color(0.075f, 0.095f, 0.16f) : Color.Lerp(new Color(0.96f, 0.98f, 0.99f), new Color(1f, 0.94f, 0.87f), DayProgress);
        public Color ButtonBackground => Night ? new Color(0.12f, 0.15f, 0.22f) : Color.Lerp(Panel, Color.white, 0.65f);
        public Color FocusBackground => Night ? new Color(0.22f, 0.275f, 0.26f) : new Color(0.91f, 0.92f, 0.81f);
        public Color Muted => Night ? new Color(0.77f, 0.79f, 0.75f) : new Color(0.34f, 0.31f, 0.28f);
        public Color Frame => Night ? new Color(0.62f, 0.65f, 0.59f, 0.35f) : new Color(0.48f, 0.39f, 0.30f, 0.25f);
        /// <summary>Use only for the player's known role, public claims/reveals and the public role reference.</summary>
        public Color RoleColor(Faction faction) => faction == Faction.Town
            ? Night ? new Color(0.46f, 0.91f, 0.56f) : new Color(0.08f, 0.40f, 0.18f)
            : faction == Faction.Antagonist ? Night ? new Color(1f, 0.47f, 0.47f) : new Color(0.70f, 0.10f, 0.13f)
            : Night ? new Color(1f, 0.69f, 0.31f) : new Color(0.67f, 0.29f, 0.02f);
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
        /// <summary>Stable seat-based identity palette, independent of assigned or claimed roles.</summary>
        public Color Character(int index)
        {
            switch (index % 12)
            {
                case 0: return Night ? new Color(0.91f, 0.72f, 1f) : new Color(0.40f, 0.17f, 0.55f);
                case 1: return Night ? new Color(1f, 0.69f, 0.79f) : new Color(0.58f, 0.12f, 0.29f);
                case 2: return Night ? new Color(0.50f, 0.89f, 0.73f) : new Color(0.04f, 0.36f, 0.24f);
                case 3: return Night ? new Color(1f, 0.79f, 0.54f) : new Color(0.49f, 0.27f, 0.05f);
                case 4: return Night ? new Color(0.82f, 0.88f, 0.49f) : new Color(0.34f, 0.37f, 0.06f);
                case 5: return Night ? new Color(0.73f, 0.75f, 1f) : new Color(0.28f, 0.26f, 0.58f);
                case 6: return Night ? new Color(1f, 0.70f, 0.62f) : new Color(0.57f, 0.23f, 0.15f);
                case 7: return Night ? new Color(0.73f, 0.85f, 0.83f) : new Color(0.23f, 0.35f, 0.33f);
                case 8: return Night ? new Color(0.96f, 0.72f, 0.94f) : new Color(0.49f, 0.20f, 0.46f);
                case 9: return Night ? new Color(0.80f, 0.81f, 0.70f) : new Color(0.33f, 0.33f, 0.24f);
                case 10: return Night ? new Color(1f, 0.84f, 0.69f) : new Color(0.46f, 0.29f, 0.20f);
                default: return Night ? new Color(0.76f, 0.81f, 0.94f) : new Color(0.29f, 0.33f, 0.44f);
            }
        }
        public Color Accent(SemanticStyle style)
        {
            if ((int)style >= (int)SemanticStyle.Speaker0 && (int)style <= (int)SemanticStyle.Speaker11) return Character((int)style - (int)SemanticStyle.Speaker0);
            switch (style)
            {
                case SemanticStyle.ActionPressure: return Muted;
                case SemanticStyle.ActionSupport: return Muted;
                case SemanticStyle.ActionChallenge: return Muted;
                case SemanticStyle.ActionEvidence: return Muted;
                case SemanticStyle.Emphasis: return Foreground;
                case SemanticStyle.Fear: return Night ? new Color(0.84f, 0.72f, 0.96f) : new Color(0.36f, 0.12f, 0.42f);
                case SemanticStyle.Calm: return Night ? new Color(0.6f, 0.86f, 0.77f) : new Color(0.04f, 0.28f, 0.22f);
                case SemanticStyle.Evidence: return Night ? new Color(0.65f, 0.82f, 1f) : new Color(0.04f, 0.23f, 0.43f);
                case SemanticStyle.Danger: return Night ? new Color(1f, 0.7f, 0.66f) : new Color(0.48f, 0.06f, 0.07f);
                case SemanticStyle.Participant: return Night ? new Color(0.58f, 0.87f, 1f) : new Color(0.02f, 0.28f, 0.52f);
                default: return Foreground;
            }
        }
    }
}
