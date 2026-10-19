using NUnit.Framework;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Live settings changes preserve the current display transaction and reveal position.</summary>
    public sealed class DisplaySettingsRegressionTests
    {
        [Test]
        public void ChangingSpeedDoesNotSkipOrAcknowledgeCurrentText()
        {
            var playback = new DisplayPlayback(); int acknowledgements = 0;
            playback.Begin(1, SemanticText.Literal("abcdefghij"), EffectProfile.None,
                new EffectsSettings(elementsPerSecond: 2), _ => acknowledgements++);
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(2));
            playback.Reconfigure(new EffectsSettings(elementsPerSecond: 4));
            Assert.That(playback.VisibleUtf16, Is.EqualTo(2));
            Assert.That(playback.Busy, Is.True);
            Assert.That(acknowledgements, Is.Zero);
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(6));
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(10));
            Assert.That(acknowledgements, Is.EqualTo(1));
        }

        [Test]
        public void EnablingReducedMotionImmediatelySuppressesMotionAndFlash()
        {
            var playback = new DisplayPlayback();
            var profile = new EffectProfile(new[] {
                new EffectStep(EffectChannel.TextShake, 2, 1),
                new EffectStep(EffectChannel.Flash, 2, 1)
            });
            playback.Begin(1, SemanticText.Literal("abcdefghij"), profile,
                new EffectsSettings(reducedMotion: false, flashes: true, elementsPerSecond: 1), null);
            playback.Tick(0.5f);
            Assert.That(playback.Frame.Shake, Is.GreaterThan(0));
            playback.Reconfigure(new EffectsSettings());
            Assert.That(playback.Frame.Shake, Is.Zero);
            Assert.That(playback.Frame.Flash, Is.Zero);
            Assert.That(playback.Busy, Is.True);
        }
    }
}
