using System;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Deterministic effect math. No Unity calls, RNG consumption or managed allocation during sampling.</summary>
    public static class GlyphEffectSampler
    {
        /// <summary>Samples span age plus a shared flash clock; glyph phases stagger movement, never flash onsets.</summary>
        public static GlyphTransform Sample(GlyphEffect effect, float age, int glyph, EffectsSettings settings, float clock)
        {
            if (!EffectStep.Finite(age) || !EffectStep.Finite(clock)) throw new ArgumentOutOfRangeException(nameof(age));
            float gain = effect.Gain(age) * effect.Intensity * settings.Intensity;
            if (!settings.Enabled || gain == 0) return GlyphTransform.Identity;
            double t = age * effect.Speed, phase = glyph * 0.73;
            float wave = (float)Math.Sin(t * 5 + phase), slow = (float)Math.Sin(t * 2 + phase);
            float a = effect.Amplitude * gain, x = 0, y = 0, sx = 1, sy = 1, angle = 0, alpha = 1, color = 0;
            switch (effect.Kind)
            {
                case GlyphEffectKind.Shake: x = a * (float)Math.Sin(t * 27 + phase); y = a * (float)Math.Cos(t * 31 + phase); break;
                case GlyphEffectKind.Tremble: x = a * 0.3f * wave; y = a * 0.25f * (float)Math.Cos(t * 19 + phase); break;
                case GlyphEffectKind.Quake: y = a * (float)Math.Sin(t * 24) * (float)Math.Sin(t * 3 + phase); break;
                case GlyphEffectKind.Jitter: x = a * Noise(t * 12, glyph); y = a * Noise(t * 13, glyph + 5); break;
                case GlyphEffectKind.Wave: y = a * wave; break;
                case GlyphEffectKind.Wobble: angle = 0.12f * gain * wave; x = a * 0.3f * slow; break;
                case GlyphEffectKind.Bounce: y = a * Math.Abs(wave); break;
                case GlyphEffectKind.Float: y = a * slow * 0.5f; break;
                case GlyphEffectKind.Sway: x = a * slow; angle = slow * gain * 0.04f; break;
                case GlyphEffectKind.Pulse: sx = sy = 1 + 0.1f * gain * wave; break;
                case GlyphEffectKind.Heartbeat: sx = sy = 1 + 0.15f * gain * (float)Math.Pow(Math.Max(0, Math.Sin(t * 7)), 8); break;
                case GlyphEffectKind.Breathe: sx = 1 + 0.03f * gain * slow; sy = 1 + 0.08f * gain * slow; break;
                case GlyphEffectKind.Flicker: alpha = settings.Flashes ? 0.85f + 0.15f * (float)Math.Sin(clock * 6) : 1; break;
                case GlyphEffectKind.Blink: alpha = settings.Flashes ? 0.6f + 0.4f * (float)(0.5 + 0.5 * Math.Sin(clock * Math.PI * 2)) : 1; break;
                case GlyphEffectKind.FadeIn: alpha = Math.Min(1, age / effect.Duration); break;
                case GlyphEffectKind.FadeOut: alpha = Math.Max(0, 1 - age / effect.Duration); break;
                case GlyphEffectKind.Glitch: x = a * (Noise(t * 4, glyph) > 0.6f ? Noise(t * 8, glyph + 9) : 0); color = Math.Abs(slow); break;
                case GlyphEffectKind.Noise: x = a * 0.4f * Noise(t * 17, glyph); y = a * 0.4f * Noise(t * 18, glyph + 3); break;
                case GlyphEffectKind.Stutter: x = a * ((int)(t * 4) % 3 == 0 ? -0.5f : 0); break;
                case GlyphEffectKind.Whisper: sx = sy = 1 - Math.Min(0.3f, gain * 0.15f); alpha = 1 - Math.Min(0.3f, gain * 0.2f); break;
                case GlyphEffectKind.Shout: sx = sy = 1 + gain * 0.3f * (float)Math.Exp(-age * 6); break;
                case GlyphEffectKind.Drip: y = -a * (float)((t + glyph * 0.13) % 1); sy = 1 + Math.Abs(wave) * gain * 0.15f; break;
                case GlyphEffectKind.Melt: y = -a * (float)Math.Min(1, t); sy = 1 - Math.Min(0.5f, gain * (float)Math.Min(1, t) * 0.3f); break;
                case GlyphEffectKind.Crumble: y = -a * (float)Math.Min(5, t * t); angle = gain * (float)Math.Min(1, t) * (glyph % 2 == 0 ? 0.4f : -0.4f); break;
                case GlyphEffectKind.Rise: y = -a * (float)Math.Exp(-age * 5); break;
                case GlyphEffectKind.Ripple: y = a * wave * (float)Math.Exp(-age * 0.4); sx = 1 + gain * wave * 0.05f; break;
                case GlyphEffectKind.Zoom: sx = sy = 1 + gain * 0.3f * (float)Math.Exp(-age * 3); break;
                case GlyphEffectKind.Squash: sx = 1 + gain * wave * 0.15f; sy = 1 - gain * wave * 0.15f; break;
                case GlyphEffectKind.Gradient: color = (float)(0.5 + 0.5 * Math.Sin(t * 2 + phase)); break;
                case GlyphEffectKind.ColorPulse: color = (float)(0.5 + 0.5 * Math.Sin(t * 2)); break;
                case GlyphEffectKind.Spotlight: alpha = 0.7f + 0.3f * Math.Max(0, wave); break;
                case GlyphEffectKind.Strobe: alpha = settings.Flashes ? 0.75f + 0.25f * (float)Math.Sin(clock * Math.PI * 3) : 1; break;
            }
            if (settings.ReducedMotion)
            { if (x != 0 || y != 0 || sx != 1 || sy != 1 || angle != 0) color = Math.Max(color, 0.15f); x = y = angle = 0; sx = sy = 1; if (IsFlash(effect.Kind)) alpha = 1; }
            return new GlyphTransform(x, y, Math.Max(0.2f, sx), Math.Max(0.2f, sy), angle,
                Math.Max(0, Math.Min(1, 1 + (alpha - 1) * Math.Min(1, gain))), color * Math.Min(1, gain));
        }
        private static bool IsFlash(GlyphEffectKind kind) => kind == GlyphEffectKind.Strobe || kind == GlyphEffectKind.Flicker || kind == GlyphEffectKind.Blink;
        private static float Noise(double tick, int glyph)
        { unchecked { uint x = (uint)(long)Math.Floor(tick) * 747796405u + (uint)glyph * 2891336453u; x = (x ^ (x >> 16)) * 2246822519u; return (x & 65535) / 32767.5f - 1; } }
    }
}
