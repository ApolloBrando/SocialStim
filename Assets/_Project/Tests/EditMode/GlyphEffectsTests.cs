using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Span parsing, lifetime, safety, bounded math and steady-state allocation regressions.</summary>
    public sealed class GlyphEffectsTests
    {
        [Test]
        public void CatalogHasAtLeastTwentyFiveImplementedBehaviorsWithFiniteBoundedOutputs()
        {
            Assert.That(Enum.GetValues(typeof(GlyphEffectKind)).Length, Is.GreaterThanOrEqualTo(25));
            var policy = new EffectsSettings(reducedMotion: false, flashes: true, intensity: 1);
            foreach (GlyphEffectKind kind in Enum.GetValues(typeof(GlyphEffectKind)))
                for (int i = 0; i < 100; i++)
                {
                    var value = GlyphEffectSampler.Sample(new GlyphEffect(kind, intensity: 4, speed: 20, amplitude: 20), i * 0.07f, i, policy, i * 0.07f);
                    foreach (float number in new[] { value.X, value.Y, value.ScaleX, value.ScaleY, value.Angle, value.Alpha, value.Color })
                        Assert.That(float.IsNaN(number) || float.IsInfinity(number), Is.False, kind.ToString());
                    Assert.That(value.ScaleX, Is.GreaterThan(0)); Assert.That(value.ScaleY, Is.GreaterThan(0));
                    Assert.That(value.Alpha, Is.InRange(0, 1));
                }
        }
        [Test]
        public void ContinuousSpanMotionOutlivesRevealAndLifetimeModesExpirePrecisely()
        {
            var policy = new EffectsSettings(reducedMotion: false, intensity: 1);
            var continuous = new GlyphEffect(GlyphEffectKind.Wave);
            Assert.That(continuous.Gain(100), Is.EqualTo(1));
            Assert.That(GlyphEffectSampler.Sample(continuous, 10, 1, policy, 10).Y,
                Is.Not.EqualTo(GlyphEffectSampler.Sample(continuous, 11, 1, policy, 11).Y));
            foreach (EffectLifetime mode in new[] { EffectLifetime.Timed, EffectLifetime.Burst, EffectLifetime.Envelope })
                Assert.That(new GlyphEffect(GlyphEffectKind.Shake, duration: 2, lifetime: mode).Gain(2), Is.Zero);
            var envelope = new GlyphEffect(GlyphEffectKind.Wave, duration: 2, lifetime: EffectLifetime.Envelope);
            Assert.That(envelope.Gain(0), Is.Zero); Assert.That(envelope.Gain(1), Is.EqualTo(1));
        }
        [Test]
        public void MarkupPreservesNestedStylesAndIndependentEffectRanges()
        {
            var text = SemanticMarkup.Parse("<fear>A<shake i=2 d=3>B</shake>C</fear>");
            Assert.That(text.Text, Is.EqualTo("ABC")); Assert.That(text.Effects.Count, Is.EqualTo(2));
            Assert.That(text.Effects[0].Start, Is.Zero); Assert.That(text.Effects[0].Length, Is.EqualTo(3));
            Assert.That(text.Effects[1].Start, Is.EqualTo(1)); Assert.That(text.Effects[1].Length, Is.EqualTo(1));
            Assert.That(text.Effects[1].Effect.Lifetime, Is.EqualTo(EffectLifetime.Timed));
            Assert.That(text.StyleAt(1), Is.EqualTo(SemanticStyle.Fear));
            foreach (GlyphEffectKind kind in Enum.GetValues(typeof(GlyphEffectKind)))
                Assert.That(SemanticMarkup.Parse("<" + kind.ToString().ToLowerInvariant() + ">X</" + kind.ToString().ToLowerInvariant() + ">").Effects.Count, Is.GreaterThan(0));
        }
        [TestCase("<shake i=NaN>x</shake>")]
        [TestCase("<shake s=0>x</shake>")]
        [TestCase("<shake i=1 i=2>x</shake>")]
        [TestCase("<shake q=2>x</shake>")]
        [TestCase("<shake mode=unknown>x</shake>")]
        [TestCase("<shake mode=2>x</shake>")]
        [TestCase("<shake><wave>x</shake></wave>")]
        public void MalformedOrUnsafeParametersFailAtImport(string source) => Assert.Throws<ArgumentException>(() => SemanticMarkup.Parse(source));
        [Test]
        public void ReducedMotionRemovesEveryTransformAndFlashDisableRemovesFlashModulation()
        {
            foreach (GlyphEffectKind kind in Enum.GetValues(typeof(GlyphEffectKind)))
            {
                var sample = GlyphEffectSampler.Sample(new GlyphEffect(kind), 0.2f, 4, new EffectsSettings(), 0.2f);
                Assert.That(sample.X, Is.Zero); Assert.That(sample.Y, Is.Zero); Assert.That(sample.Angle, Is.Zero);
                Assert.That(sample.ScaleX, Is.EqualTo(1)); Assert.That(sample.ScaleY, Is.EqualTo(1));
            }
            foreach (var kind in new[] { GlyphEffectKind.Strobe, GlyphEffectKind.Blink, GlyphEffectKind.Flicker })
            {
                var policy = new EffectsSettings(reducedMotion: false, flashes: false);
                Assert.That(GlyphEffectSampler.Sample(new GlyphEffect(kind, speed: 20), 1, 5, policy, 1).Alpha, Is.EqualTo(1));
            }
        }
        [Test]
        public void FlashClockIsSharedAcrossGlyphsAndIgnoresAuthoredHighSpeed()
        {
            var policy = new EffectsSettings(reducedMotion: false, flashes: true, intensity: 1);
            var effect = new GlyphEffect(GlyphEffectKind.Strobe, speed: 20);
            int peaks = 0; float previous = 1, before = 1;
            for (int i = 0; i < 1000; i++)
            {
                float clock = i / 1000f;
                float alpha = GlyphEffectSampler.Sample(effect, 0.1f, 0, policy, clock).Alpha;
                Assert.That(GlyphEffectSampler.Sample(effect, 8, 99, policy, clock).Alpha, Is.EqualTo(alpha));
                if (previous > before && previous > alpha) peaks++;
                before = previous; previous = alpha;
            }
            Assert.That(peaks, Is.LessThanOrEqualTo(2));
        }
#if NET8_0_OR_GREATER && !UNITY_5_3_OR_NEWER
        [Test]
        public void WarmGlyphSamplingAllocatesNoManagedMemory()
        {
            var policy = new EffectsSettings(reducedMotion: false); var effect = new GlyphEffect(GlyphEffectKind.Wave);
            float sum = 0;
            for (int i = 0; i < 100; i++) sum += GlyphEffectSampler.Sample(effect, i * 0.01f, i, policy, 0).Y;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += GlyphEffectSampler.Sample(effect, i * 0.01f, i, policy, 0).Y;
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(float.IsNaN(sum), Is.False);
        }
#endif
    }
}
