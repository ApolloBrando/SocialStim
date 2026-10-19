using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Phases;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Display holds input through the full reading pause, independently of game timing.</summary>
    public sealed class PlaytestPresentationTests
    {
        [Test]
        public void FullLineRemainsVisibleDuringPauseAndAcknowledgesOnlyAtItsEnd()
        {
            var playback = new DisplayPlayback(); int acknowledgements = 0;
            playback.Begin(1, SemanticText.Literal("Hi"), EffectProfile.None, new EffectsSettings(elementsPerSecond: 2),
                _ => acknowledgements++, pauseAfter: 1.5f);
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(2));
            Assert.That(playback.Busy, Is.True);
            Assert.That(acknowledgements, Is.Zero);
            playback.Tick(1.49f);
            Assert.That(playback.Busy, Is.True);
            playback.Tick(0.02f);
            Assert.That(playback.Busy, Is.False);
            Assert.That(acknowledgements, Is.EqualTo(1));
        }

        [Test]
        public void SkipBypassesReadingPauseExactlyOnceAndInstantTextHasNoPause()
        {
            var playback = new DisplayPlayback(); int count = 0;
            playback.Begin(1, SemanticText.Literal("Hi"), EffectProfile.None, new EffectsSettings(), _ => count++, pauseAfter: 1.5f);
            playback.Skip(); playback.Skip(); playback.Tick(10);
            Assert.That(count, Is.EqualTo(1));
            playback.Begin(2, SemanticText.Literal("Hi"), EffectProfile.None, new EffectsSettings(instantText: true), _ => count++, pauseAfter: 1.5f);
            Assert.That(playback.Busy, Is.False);
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void DaylightTracksPublicTrialProgressAndOnlyNightPhasesAreDark()
        {
            Assert.That(Daylight.IsNight(GamePhase.Discussion), Is.False);
            Assert.That(Daylight.IsNight(GamePhase.Dawn), Is.False);
            Assert.That(Daylight.IsNight(GamePhase.NightSelection), Is.True);
            Assert.That(Daylight.IsNight(GamePhase.NightResolution), Is.True);
            Assert.That(Daylight.Progress(GamePhase.Nomination, 0, 2), Is.LessThan(Daylight.Progress(GamePhase.Judgement, 1, 2)));
            Assert.That(Daylight.Progress(GamePhase.LastWords, 1, 2), Is.EqualTo(1));
        }
    }
}
