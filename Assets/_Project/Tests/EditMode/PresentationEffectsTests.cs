using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Portable presentation contracts tested independently of Unity frame timing.</summary>
    public sealed class PresentationEffectsTests
    {
        private static readonly EffectsSettings Full = new EffectsSettings(reducedMotion: false, flashes: true, intensity: 1);
        [Test]
        public void NestedMarkupPreservesTextAndInnerStyle()
        {
            SemanticText text = SemanticMarkup.Parse("A <fear>cold <emphasis>voice</emphasis></fear>. <<literal>");
            Assert.That(text.Text, Is.EqualTo("A cold voice. <literal>"));
            Assert.That(text.StyleAt(2), Is.EqualTo(SemanticStyle.Fear));
            Assert.That(text.StyleAt(7), Is.EqualTo(SemanticStyle.Emphasis));
            Assert.That(text.StyleAt(12), Is.EqualTo(SemanticStyle.Default));
        }
        [TestCase("<fear>unclosed")]
        [TestCase("</fear>")]
        [TestCase("<fear><calm>x</fear></calm>")]
        [TestCase("<color=red>x</color>")]
        [TestCase("<sprite=0>")]
        [TestCase("<fear")]
        public void InvalidMarkupIsRejected(string text) => Assert.Throws<ArgumentException>(() => SemanticMarkup.Parse(text));

        [Test]
        public void TypewriterKeepsSurrogatesAndCombiningMarksTogether()
        {
            var playback = new DisplayPlayback();
            playback.Begin(1, SemanticMarkup.Parse("😀e\u0301X"), EffectProfile.None, new EffectsSettings(elementsPerSecond: 1), null);
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(2));
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(4));
            playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(5));
            Assert.That(playback.Busy, Is.False);
        }
        [Test]
        public void ParallelTracksFinishBeforeTheNextSequenceStep()
        {
            var profile = new EffectSequence().Parallel(new EffectStep(EffectChannel.Border, 2, 1),
                new EffectStep(EffectChannel.TextShake, 1, 1))
                .Then(new EffectStep(EffectChannel.Vignette, 2, 1)).Build();
            Assert.That(profile.Duration, Is.EqualTo(4));
            Assert.That(profile.Sample(1, Full).Border, Is.EqualTo(1).Within(0.0001));
            Assert.That(profile.Sample(1, Full).Vignette, Is.Zero);
            Assert.That(profile.Sample(3, Full).Border, Is.Zero);
            Assert.That(profile.Sample(3, Full).Vignette, Is.EqualTo(1).Within(0.0001));
            Assert.That(profile.Sample(4, Full).Vignette, Is.Zero);
        }
        [Test]
        public void ReplacementUsesPriorityThenAuthoredOrderAndAdditionClamps()
        {
            var profile = new EffectProfile(new[] {
                new EffectStep(EffectChannel.Border, 2, 0.9f, 1),
                new EffectStep(EffectChannel.Border, 2, 0.4f, 2),
                new EffectStep(EffectChannel.Border, 2, 0.2f, 2),
                new EffectStep(EffectChannel.Border, 2, 0.3f, blend: EffectBlend.Add),
            });
            Assert.That(profile.Sample(1, Full).Border, Is.EqualTo(0.5f).Within(0.0001));
            var clamped = new EffectProfile(new[] { new EffectStep(EffectChannel.Border, 2, 0.8f), new EffectStep(EffectChannel.Border, 2, 0.8f, blend: EffectBlend.Add) });
            Assert.That(clamped.Sample(1, Full).Border, Is.EqualTo(1));
        }
        [TestCase(EffectEnvelope.Sine)]
        [TestCase(EffectEnvelope.Triangle)]
        [TestCase(EffectEnvelope.Smooth)]
        public void EnvelopesRestoreBothEndpointsAndPeakAtTheMidpoint(EffectEnvelope envelope)
        {
            var profile = new EffectSequence().Then(new EffectStep(EffectChannel.Border, 2, 1, envelope: envelope)).Build();
            Assert.That(profile.Sample(0, Full).Border, Is.Zero);
            Assert.That(profile.Sample(1, Full).Border, Is.EqualTo(1).Within(0.0001));
            Assert.That(profile.Sample(2, Full).Border, Is.Zero);
        }
        [Test]
        public void ReducedMotionAndDisabledEffectsSuppressOwnedChannels()
        {
            var profile = new EffectSequence().Parallel(new EffectStep(EffectChannel.TextShake, 2, 1),
                new EffectStep(EffectChannel.Flash, 2, 1), new EffectStep(EffectChannel.Border, 2, 1)).Build();
            EffectFrame reduced = profile.Sample(1, new EffectsSettings());
            Assert.That(reduced.Shake, Is.Zero); Assert.That(reduced.Flash, Is.Zero);
            Assert.That(reduced.Border, Is.EqualTo(0.5f).Within(0.0001));
            Assert.That(profile.Sample(1, new EffectsSettings(enabled: false)).Border, Is.Zero);
            Assert.That(profile.Sample(1, Full).Flash, Is.LessThanOrEqualTo(0.08f));
        }
        [Test]
        public void FlashCannotBeAuthoredAsFastOrRepeatedPulse()
        {
            Assert.Throws<ArgumentException>(() => new EffectProfile(new[] { new EffectStep(EffectChannel.Flash, 0.1f, 1) }));
            Assert.Throws<ArgumentException>(() => new EffectProfile(new[] { new EffectStep(EffectChannel.Flash, 1, 1), new EffectStep(EffectChannel.Flash, 1, 1, delay: 2) }));
        }
        [Test]
        public void FlashCooldownSurvivesSkippingAndStartingAnotherLine()
        {
            var playback = new DisplayPlayback();
            var profile = new EffectProfile(new[] { new EffectStep(EffectChannel.Flash, 1, 1) });
            var settings = new EffectsSettings(reducedMotion: false, flashes: true, intensity: 1, instantText: true);
            playback.Begin(1, SemanticText.Literal("A"), profile, settings, null);
            playback.Tick(0.1f); Assert.That(playback.Frame.Flash, Is.GreaterThan(0)); playback.Skip();
            playback.Begin(2, SemanticText.Literal("B"), profile, settings, null);
            playback.Tick(0.1f); Assert.That(playback.Frame.Flash, Is.Zero); playback.Skip();
            playback.Tick(2);
            playback.Begin(3, SemanticText.Literal("C"), profile, settings, null);
            playback.Tick(0.1f); Assert.That(playback.Frame.Flash, Is.GreaterThan(0));
        }
        [TestCase(DisplayCompletion.Completed)]
        [TestCase(DisplayCompletion.Skipped)]
        [TestCase(DisplayCompletion.Cancelled)]
        public void DisplayAcknowledgesExactlyOnceAndRestoresOutputs(DisplayCompletion expected)
        {
            var playback = new DisplayPlayback(); int calls = 0;
            var profile = new EffectProfile(new[] { new EffectStep(EffectChannel.Border, 1, 1) });
            playback.Begin(14, SemanticText.Literal("hello"), profile, Full, acknowledgement => {
                calls++; Assert.That(acknowledgement.Sequence, Is.EqualTo(14)); Assert.That(acknowledgement.Completion, Is.EqualTo(expected));
                Assert.That(playback.Busy, Is.False); playback.Skip(); playback.Cancel();
            });
            playback.Tick(0.2f); Assert.That(playback.Frame.Border, Is.GreaterThan(0));
            if (expected == DisplayCompletion.Completed) playback.Tick(10);
            else if (expected == DisplayCompletion.Skipped) playback.Skip(); else playback.Cancel();
            playback.Tick(10); playback.Skip(); playback.Cancel();
            Assert.That(calls, Is.EqualTo(1)); Assert.That(playback.Frame.Border, Is.Zero);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(5));
        }
        [Test]
        public void DisabledEffectsAreImmediateAndInstantTextStillAllowsCueTiming()
        {
            var playback = new DisplayPlayback(); int calls = 0;
            var profile = new EffectProfile(new[] { new EffectStep(EffectChannel.Border, 1, 1) });
            playback.Begin(1, SemanticText.Literal("hello"), profile, new EffectsSettings(enabled: false), _ => calls++);
            Assert.That(playback.Busy, Is.False); Assert.That(calls, Is.EqualTo(1));
            playback.Begin(2, SemanticText.Literal("hello"), profile, new EffectsSettings(instantText: true), _ => calls++);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(5)); Assert.That(playback.Busy, Is.True);
            playback.Tick(1); Assert.That(calls, Is.EqualTo(2));
        }
        [Test]
        public void InvalidClockOrOverlappingDisplayIsRejected()
        {
            var playback = new DisplayPlayback();
            playback.Begin(1, SemanticText.Literal("A"), EffectProfile.None, Full, null);
            Assert.Throws<InvalidOperationException>(() => playback.Begin(2, SemanticText.Literal("B"), EffectProfile.None, Full, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => playback.Tick(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => playback.Tick(-1));
        }
        [Test]
        public void PublicSpeechCuesUseTheAssertedRoleAndIntent()
        {
            var run = RunFactory.CreateScenario(new RunOptions(123, discussionRounds: 3), new[] {
                new CastMemberSpec("Player", new SocialStim.Rules.Roles.RoleId("assassin")),
                new CastMemberSpec("Town1", new SocialStim.Rules.Roles.RoleId("inspector")),
                new CastMemberSpec("Town2", new SocialStim.Rules.Roles.RoleId("physician")),
                new CastMemberSpec("Neutral", new SocialStim.Rules.Roles.RoleId("survivor")),
            }, TestContent.Roles(), TestContent.Lines(), new ScenarioStrategy());
            var resolver = new VisibleCueResolver();
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ClaimRole, role: new SocialStim.Rules.Roles.RoleId("inspector")), out _), Is.True);
            var view = run.GetPlayerView();
            var first = view.Transcript.First(o => o.Speaker == view.PlayerId);
            Assert.That(first.ClaimedRole.Value.Value, Is.EqualTo("inspector"));
            Assert.That(resolver.Resolve(first, view), Is.EqualTo(PresentationCue.Speech));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.ClaimRole, role: new SocialStim.Rules.Roles.RoleId("survivor")), out _), Is.True);
            view = run.GetPlayerView();
            Assert.That(resolver.Resolve(view.Transcript.Last(o => o.Speaker == view.PlayerId), view), Is.EqualTo(PresentationCue.ContradictedClaim));
        }
        [Test]
        public void ActualAuthoredLinesHaveValidPresentationMarkup()
        {
            string path = System.IO.File.Exists("Assets/_Project/Data/Dialogue/StarterDialogue.json")
                ? "Assets/_Project/Data/Dialogue/StarterDialogue.json" : System.IO.Path.Combine(AppContext.BaseDirectory, "StarterDialogue.json");
            foreach (var line in SocialStim.Data.Dialogue.DialogueDocument.Parse(System.IO.File.ReadAllText(path)))
                Assert.That(SemanticMarkup.Parse(line.Text).Text, Is.Not.Empty);
        }
#if NET8_0_OR_GREATER && !UNITY_5_3_OR_NEWER
        [Test]
        public void SteadyPortablePlaybackDoesNotAllocate()
        {
            var playback = new DisplayPlayback();
            playback.Begin(1, SemanticText.Literal(new string('a', 1000)), DefaultCueProfiles.Create()[PresentationCue.Accusation],
                new EffectsSettings(elementsPerSecond: 1), null);
            for (int i = 0; i < 100; i++) playback.Tick(0.0001f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) playback.Tick(0.0001f);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }
#endif
    }
}
