using System;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Validated immutable authoring parameters, interpreted only by presentation.</summary>
    public readonly struct GlyphEffect
    {
        /// <summary>Creates an effect with bounded intensity, speed, amplitude and lifetime.</summary>
        public GlyphEffect(GlyphEffectKind kind, float intensity = 1, float speed = 1, float amplitude = 3,
            float duration = 1, EffectLifetime lifetime = EffectLifetime.Continuous)
        {
            if (!Enum.IsDefined(typeof(GlyphEffectKind), kind) || !Enum.IsDefined(typeof(EffectLifetime), lifetime)
                || !EffectStep.Finite(intensity) || intensity < 0 || intensity > 4 || !EffectStep.Finite(speed) || speed < 0.1f || speed > 20
                || !EffectStep.Finite(amplitude) || amplitude < 0 || amplitude > 20 || !EffectStep.Finite(duration) || duration < 0.05f || duration > 30)
                throw new ArgumentException("Invalid glyph effect parameters.");
            Kind = kind; Intensity = intensity; Speed = speed; Amplitude = amplitude; Duration = duration; Lifetime = lifetime;
        }
        /// <summary>Transform behavior.</summary>
        public GlyphEffectKind Kind { get; }
        /// <summary>Authored strength multiplier.</summary>
        public float Intensity { get; }
        /// <summary>Authored speed multiplier; flash rates are independently safety-capped.</summary>
        public float Speed { get; }
        /// <summary>Reference-pixel movement bound.</summary>
        public float Amplitude { get; }
        /// <summary>Lifetime or attack/fade duration in seconds.</summary>
        public float Duration { get; }
        /// <summary>When the effect stops or settles.</summary>
        public EffectLifetime Lifetime { get; }
        /// <summary>Gain at a glyph's time since reveal.</summary>
        public float Gain(float age)
        {
            if (age < 0) return 0;
            if (Lifetime == EffectLifetime.Continuous) return 1;
            if (age >= Duration) return 0;
            if (Lifetime == EffectLifetime.Burst) return (float)Math.Exp(-6 * age / Duration);
            if (Lifetime == EffectLifetime.Envelope) return Math.Max(0, Math.Min(1, Math.Min(age, Duration - age) / (Duration * 0.2f)));
            return 1;
        }
    }
}
