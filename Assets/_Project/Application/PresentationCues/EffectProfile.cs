using System;
using System.Collections.Generic;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Immutable timeline. Equal-priority replacement uses later authored track order.</summary>
    public sealed class EffectProfile
    {
        private readonly EffectStep[] _steps;
        public static readonly EffectProfile None = new EffectProfile(Array.Empty<EffectStep>());
        public EffectProfile(IEnumerable<EffectStep> steps, EffectTone tone = EffectTone.Neutral)
        {
            if (!Enum.IsDefined(typeof(EffectTone), tone)) throw new ArgumentOutOfRangeException(nameof(tone));
            Tone = tone;
            _steps = new List<EffectStep>(steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (_steps.Length > 64) throw new ArgumentException("An effect profile supports at most 64 tracks.");
            int flashes = 0;
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i].Duration <= 0) throw new ArgumentException("Default/uninitialized effect step.");
                if (_steps[i].Channel == EffectChannel.Flash && (++flashes > 1 || _steps[i].Duration < 1))
                    throw new ArgumentException("A profile allows one flash pulse lasting at least one second.");
                Duration = Math.Max(Duration, _steps[i].Delay + _steps[i].Duration);
            }
        }
        public float Duration { get; }
        /// <summary>Semantic colour family, never an assertion that a speaker is truthful.</summary>
        public EffectTone Tone { get; }
        public EffectFrame Sample(float time, EffectsSettings settings)
        {
            if (!EffectStep.Finite(time) || time < 0) throw new ArgumentOutOfRangeException(nameof(time));
            if (!settings.Enabled) return default;
            return new EffectFrame(settings.ReducedMotion ? 0 : Channel(EffectChannel.TextShake, time) * settings.Intensity,
                Channel(EffectChannel.Border, time) * settings.Intensity,
                Channel(EffectChannel.Vignette, time) * settings.Intensity,
                !settings.Flashes || settings.ReducedMotion ? 0 : Channel(EffectChannel.Flash, time) * settings.Intensity * 0.08f,
                Channel(EffectChannel.ColorWash, time) * settings.Intensity * (settings.ReducedMotion ? 0.035f : 0.07f),
                settings.ReducedMotion ? 0 : Channel(EffectChannel.EdgeGlow, time) * settings.Intensity * 0.3f);
        }
        private float Channel(EffectChannel channel, float time)
        {
            float replacement = 0, addition = 0;
            int priority = int.MinValue;
            for (int i = 0; i < _steps.Length; i++)
            {
                EffectStep step = _steps[i];
                float local = time - step.Delay;
                if (step.Channel != channel || local < 0 || !step.Continuous && local >= step.Duration) continue;
                float progress = local / step.Duration;
                float pulse = 1 - Math.Abs(progress * 2 - 1);
                float envelope = step.Envelope == EffectEnvelope.Sine ? (float)Math.Sin(progress * Math.PI)
                    : step.Envelope == EffectEnvelope.Smooth ? pulse * pulse * (3 - 2 * pulse) : pulse;
                float value = step.Amount * (step.Continuous ? 1 : envelope);
                if (step.Blend == EffectBlend.Add) addition += value;
                else if (step.Priority >= priority) { replacement = value; priority = step.Priority; }
            }
            return Math.Min(1, Math.Max(0, replacement + addition));
        }
    }

    public enum EffectBlend { Replace, Add }

    public enum EffectChannel { TextShake, Border, Vignette, Flash, ColorWash, EdgeGlow }

    public enum EffectEnvelope { Sine, Triangle, Smooth }

    /// <summary>Public situation mood; Unity palettes supply the actual day/night colours.</summary>
    public enum EffectTone { Neutral, Pressure, Reassurance, Evidence, Challenge, Judgement, Loss, Triumph, Night, Morning, Uncertainty }

    /// <summary>Bounded scalar outputs; the Unity adapter owns colours and geometry.</summary>
    public readonly struct EffectFrame
    {
        public EffectFrame(float shake, float border, float vignette, float flash, float colorWash = 0, float edgeGlow = 0)
        { Shake = shake; Border = border; Vignette = vignette; Flash = flash; ColorWash = colorWash; EdgeGlow = edgeGlow; }
        public float Shake { get; }
        public float Border { get; }
        public float Vignette { get; }
        public float Flash { get; }
        /// <summary>Low-opacity situation tint, independent of optional flashes.</summary>
        public float ColorWash { get; }
        /// <summary>Soft peripheral accent, suppressed by reduced motion.</summary>
        public float EdgeGlow { get; }
    }

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
