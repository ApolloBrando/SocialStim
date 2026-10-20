using System;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Session-local accessibility policy; reduced motion and no flashes by default.</summary>
    public sealed class EffectsSettings
    {
        public const float NormalTextSpeed = 84;
        public EffectsSettings(bool enabled = true, bool reducedMotion = true, bool flashes = false, float intensity = 0.5f,
            bool instantText = false, float elementsPerSecond = NormalTextSpeed)
        {
            if (!EffectStep.Finite(intensity) || intensity < 0 || intensity > 1 || !EffectStep.Finite(elementsPerSecond)
                || elementsPerSecond < 0 || elementsPerSecond > 240) throw new ArgumentOutOfRangeException(nameof(intensity));
            Enabled = enabled; ReducedMotion = reducedMotion; Flashes = flashes; Intensity = intensity;
            InstantText = instantText; ElementsPerSecond = elementsPerSecond;
        }
        public float TextSpeedPercent => ElementsPerSecond / NormalTextSpeed * 100;
        public static float SpeedForPercent(float percent)
        {
            if (!EffectStep.Finite(percent) || percent < 0 || percent > 200) throw new ArgumentOutOfRangeException(nameof(percent));
            return NormalTextSpeed * percent / 100;
        }
        public bool Enabled { get; }
        public bool ReducedMotion { get; }
        public bool Flashes { get; }
        public float Intensity { get; }
        public bool InstantText { get; }
        public float ElementsPerSecond { get; }
    }
}
