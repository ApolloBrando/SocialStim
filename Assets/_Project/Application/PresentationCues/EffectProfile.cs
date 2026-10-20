using System;
using System.Collections.Generic;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Immutable timeline. Equal-priority replacement uses later authored track order.</summary>
    public sealed class EffectProfile
    {
        private readonly EffectStep[] _steps;
        public static readonly EffectProfile None = new EffectProfile(Array.Empty<EffectStep>());
        public EffectProfile(IEnumerable<EffectStep> steps)
        {
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
        public EffectFrame Sample(float time, EffectsSettings settings)
        {
            if (!EffectStep.Finite(time) || time < 0) throw new ArgumentOutOfRangeException(nameof(time));
            if (!settings.Enabled) return default;
            return new EffectFrame(settings.ReducedMotion ? 0 : Channel(EffectChannel.TextShake, time) * settings.Intensity,
                Channel(EffectChannel.Border, time) * settings.Intensity,
                Channel(EffectChannel.Vignette, time) * settings.Intensity,
                !settings.Flashes || settings.ReducedMotion ? 0 : Channel(EffectChannel.Flash, time) * settings.Intensity * 0.08f);
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
}
