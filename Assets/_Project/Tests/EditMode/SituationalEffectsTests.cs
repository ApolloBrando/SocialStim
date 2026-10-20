using System;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Scoped public cue routing, bounded new channels and live accessibility behavior.</summary>
    public sealed class SituationalEffectsTests
    {
        private static readonly EffectsSettings Full = new EffectsSettings(reducedMotion: false, flashes: true, intensity: 1);

        [TestCase(SpeechIntent.ClaimRole, PresentationCue.Claim)]
        [TestCase(SpeechIntent.CounterClaim, PresentationCue.CounterClaim)]
        [TestCase(SpeechIntent.Accuse, PresentationCue.Accusation)]
        [TestCase(SpeechIntent.Deflect, PresentationCue.Refutation)]
        [TestCase(SpeechIntent.Defend, PresentationCue.Defense)]
        [TestCase(SpeechIntent.Vouch, PresentationCue.Defense)]
        [TestCase(SpeechIntent.Agree, PresentationCue.Agreement)]
        [TestCase(SpeechIntent.Disagree, PresentationCue.Disagreement)]
        [TestCase(SpeechIntent.ReportInvestigation, PresentationCue.Investigation)]
        [TestCase(SpeechIntent.ReportAction, PresentationCue.ActionReport)]
        [TestCase(SpeechIntent.ExpressFear, PresentationCue.Fear)]
        [TestCase(SpeechIntent.LastWords, PresentationCue.LastWords)]
        public void SpeechEffectsFollowPublicIntentRatherThanGuessedTruth(SpeechIntent intent, PresentationCue expected)
        {
            var view = TestContent.Scenario("inspector", "physician", "assassin", "survivor").GetPlayerView();
            var observation = new GameObservation(1, 1, GameCue.SpeechDelivered, "Public statement", new CharacterId(1),
                intent: intent, claimedRole: intent == SpeechIntent.ClaimRole || intent == SpeechIntent.CounterClaim ? new RoleId("inspector") : (RoleId?)null);
            Assert.That(new VisibleCueResolver().Resolve(observation, view), Is.EqualTo(expected));
        }

        [TestCase(GameCue.BallotRevealed, PresentationCue.Ballot)]
        [TestCase(GameCue.VoteTallied, PresentationCue.Tally)]
        [TestCase(GameCue.RevoteStarted, PresentationCue.Tie)]
        [TestCase(GameCue.ProtectionApplied, PresentationCue.Protection)]
        public void VotingAndProtectionHaveExplicitPublicCues(GameCue cue, PresentationCue expected)
        {
            var view = TestContent.Scenario("inspector", "physician", "assassin", "survivor").GetPlayerView();
            Assert.That(new VisibleCueResolver().Resolve(new GameObservation(1, 1, cue, "Public event"), view), Is.EqualTo(expected));
        }

        [Test]
        public void EveryProfileSettlesAndNeverExceedsTheNewOpacityCaps()
        {
            foreach (var pair in DefaultCueProfiles.Create())
                for (float time = 0; time <= pair.Value.Duration + 0.2f; time += 0.05f)
                {
                    EffectFrame frame = pair.Value.Sample(time, Full);
                    Assert.That(frame.ColorWash, Is.InRange(0f, 0.07f), pair.Key.ToString());
                    Assert.That(frame.EdgeGlow, Is.InRange(0f, 0.3f));
                    Assert.That(frame.Flash, Is.InRange(0f, 0.08f));
                    if (time >= 1.5f) Assert.That(frame.Shake, Is.Zero);
                    EffectFrame disabled = pair.Value.Sample(time, new EffectsSettings(enabled: false));
                    Assert.That(disabled.ColorWash + disabled.EdgeGlow + disabled.Border + disabled.Vignette + disabled.Flash + disabled.Shake, Is.Zero);
                    EffectFrame reduced = pair.Value.Sample(time, new EffectsSettings(intensity: 1));
                    Assert.That(reduced.EdgeGlow + reduced.Shake + reduced.Flash, Is.Zero);
                    Assert.That(reduced.ColorWash, Is.InRange(0f, 0.035f));
                }
            foreach (var profile in DefaultCueProfiles.Create().Values)
            {
                EffectFrame end = profile.Sample(profile.Duration + 1, Full);
                Assert.That(end.ColorWash + end.EdgeGlow + end.Border + end.Vignette + end.Flash + end.Shake, Is.Zero);
            }
        }

        [Test]
        public void SituationTonesDistinguishClaimsPressureDefenseAndOutcomes()
        {
            var profiles = DefaultCueProfiles.Create();
            Assert.That(profiles[PresentationCue.Claim].Tone, Is.EqualTo(EffectTone.Evidence));
            Assert.That(profiles[PresentationCue.CounterClaim].Tone, Is.EqualTo(EffectTone.Challenge));
            Assert.That(profiles[PresentationCue.Accusation].Tone, Is.EqualTo(EffectTone.Pressure));
            Assert.That(profiles[PresentationCue.Defense].Tone, Is.EqualTo(EffectTone.Reassurance));
            Assert.That(profiles[PresentationCue.Victory].Tone, Is.EqualTo(EffectTone.Triumph));
            Assert.That(profiles[PresentationCue.Death].Tone, Is.EqualTo(EffectTone.Loss));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EffectProfile(Array.Empty<EffectStep>(), (EffectTone)999));
        }

        [Test]
        public void FlashCooldownDoesNotAccidentallySuppressTheNewNonFlashChannels()
        {
            var playback = new DisplayPlayback();
            var profile = DefaultCueProfiles.Create()[PresentationCue.Death];
            playback.Begin(1, SemanticText.Literal("First"), profile, Full, null);
            playback.Tick(0.2f); Assert.That(playback.Frame.Flash, Is.GreaterThan(0)); playback.Skip();
            playback.Begin(2, SemanticText.Literal("Second"), profile, Full, null);
            playback.Tick(0.2f);
            Assert.That(playback.Frame.Flash, Is.Zero);
            Assert.That(playback.Frame.ColorWash, Is.GreaterThan(0));
            Assert.That(playback.Frame.EdgeGlow, Is.GreaterThan(0));
            playback.Reconfigure(new EffectsSettings(intensity: 1));
            Assert.That(playback.Frame.EdgeGlow + playback.Frame.Flash + playback.Frame.Shake, Is.Zero);
            Assert.That(playback.Frame.ColorWash, Is.InRange(0f, 0.035f));
            playback.Reconfigure(new EffectsSettings(enabled: false));
            Assert.That(playback.Frame.ColorWash + playback.Frame.EdgeGlow + playback.Frame.Border, Is.Zero);
        }

        [Test]
        public void SkipAndCancelClearSituationEffectsWithoutDoubleAcknowledging()
        {
            var playback = new DisplayPlayback(); int count = 0;
            foreach (bool cancel in new[] { false, true })
            {
                playback.Begin(count + 1, SemanticText.Literal("Long visible sentence"), DefaultCueProfiles.Create()[PresentationCue.CounterClaim], Full, _ => count++);
                playback.Tick(0.3f); Assert.That(playback.Frame.ColorWash, Is.GreaterThan(0));
                if (cancel) playback.Cancel(); else playback.Skip();
                playback.Tick(0.1f);
                Assert.That(playback.Frame.ColorWash + playback.Frame.EdgeGlow + playback.Frame.Border + playback.Frame.Vignette, Is.Zero);
                Assert.That(playback.Busy, Is.False);
            }
            Assert.That(count, Is.EqualTo(2));
        }
    }
}
