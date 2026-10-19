using System;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>A bounded pulse, or continuous text motion retained until its display is dismissed.</summary>
    public readonly struct EffectStep
    {
        public EffectStep(EffectChannel channel, float duration, float amount, int priority = 0, EffectBlend blend = EffectBlend.Replace, float delay = 0, EffectEnvelope envelope = EffectEnvelope.Sine, bool continuous = false)
        {
            if (!Enum.IsDefined(typeof(EffectChannel), channel) || !Enum.IsDefined(typeof(EffectBlend), blend) || !Enum.IsDefined(typeof(EffectEnvelope), envelope)
                || !Finite(duration) || duration <= 0 || duration > 10 || !Finite(delay) || delay < 0 || delay > 60
                || !Finite(amount) || amount < 0 || amount > 1) throw new ArgumentException("Invalid effect step.");
            if (continuous && channel != EffectChannel.TextShake) throw new ArgumentException("Only text motion can be continuous; screen pulses must remain bounded.");
            Continuous = continuous;
            Channel = channel; Duration = duration; Amount = amount; Priority = priority; Blend = blend; Delay = delay;
            Envelope = envelope;
        }
        /// <summary>Retains text motion after reveal completion until replacement or cancellation.</summary>
        public bool Continuous { get; }
        public EffectChannel Channel { get; }
        public float Duration { get; }
        public float Amount { get; }
        public int Priority { get; }
        public EffectBlend Blend { get; }
        public float Delay { get; }
        public EffectEnvelope Envelope { get; }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
