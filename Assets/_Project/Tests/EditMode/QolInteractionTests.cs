using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;

namespace SocialStim.Tests.EditMode
{
    public sealed class QolInteractionTests
    {
        [TestCase(0, 0)] [TestCase(50, 42)] [TestCase(100, 84)] [TestCase(200, 168)]
        public void PercentageSpeedUsesNormalReadingRate(float percent, float expected)
        {
            Assert.That(EffectsSettings.SpeedForPercent(percent), Is.EqualTo(expected));
            Assert.That(new EffectsSettings(elementsPerSecond: expected).TextSpeedPercent, Is.EqualTo(percent).Within(0.001));
        }
        [Test]
        public void InvalidPercentagesAreRejected()
        {
            foreach (float invalid in new[] { -1f, 201f, float.NaN, float.PositiveInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => EffectsSettings.SpeedForPercent(invalid));
        }
        [Test]
        public void ZeroSpeedPausesAndResumingPreservesRevealAndAcknowledgesExactlyOnce()
        {
            var playback = new DisplayPlayback(); int count = 0;
            playback.Begin(1, SemanticText.Literal("abcdefghij"), EffectProfile.None, new EffectsSettings(elementsPerSecond: 2), _ => count++);
            playback.Tick(1); Assert.That(playback.VisibleUtf16, Is.EqualTo(2));
            playback.Reconfigure(new EffectsSettings(elementsPerSecond: 0)); playback.Tick(100);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(2)); Assert.That(playback.Busy, Is.True); Assert.That(count, Is.Zero);
            playback.Reconfigure(new EffectsSettings(elementsPerSecond: 4)); playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(6)); playback.Tick(1); playback.Tick(1);
            Assert.That(playback.VisibleUtf16, Is.EqualTo(10)); Assert.That(count, Is.EqualTo(1));
        }
        [TestCase(false)] [TestCase(true)]
        public void InitiallyPausedRevealCanBeSkippedOrMadeInstant(bool instant)
        {
            var playback = new DisplayPlayback(); int count = 0;
            playback.Begin(1, SemanticText.Literal("Paused"), EffectProfile.None, new EffectsSettings(elementsPerSecond: 0), _ => count++);
            playback.Tick(100); Assert.That(playback.VisibleUtf16, Is.Zero);
            if (instant) { playback.Reconfigure(new EffectsSettings(instantText: true, elementsPerSecond: 0)); playback.Tick(0); }
            else playback.Skip();
            Assert.That(playback.Busy, Is.False); Assert.That(count, Is.EqualTo(1));
        }
        [Test]
        public void EmptyPausedTextDoesNotProduceAnInfiniteOrNaNDisplay()
        {
            var playback = new DisplayPlayback(); int count = 0;
            playback.Begin(1, SemanticText.Literal(""), EffectProfile.None, new EffectsSettings(elementsPerSecond: 0), _ => count++);
            Assert.That(playback.Busy, Is.False); Assert.That(count, Is.EqualTo(1));
        }
        [Test]
        public void SpeakerIdentityOnlyColoursLeadingAttributionAndMentionsRemainSeparate()
        {
            var text = SemanticMarkup.Parse("<fear>Echo: Echo thinks Nox should answer.</fear>");
            var marked = ParticipantNameStyler.Dialogue(text, new[] { "Echo", "Nox" }, "Echo", 3);
            Assert.That(marked.Text, Is.EqualTo(text.Text));
            Assert.That(marked.StyleAt(0), Is.EqualTo(ParticipantNameStyler.SpeakerStyle(3)));
            Assert.That(marked.StyleAt(6), Is.EqualTo(SemanticStyle.Participant));
            Assert.That(marked.StyleAt(text.Text.IndexOf("Nox", StringComparison.Ordinal)), Is.EqualTo(SemanticStyle.Participant));
            Assert.That(marked.StyleAt(text.Text.Length - 1), Is.EqualTo(SemanticStyle.Fear));
            var labelled = ActionAppearance.Label(marked, SpeechIntent.Accuse, GameText.Default);
            Assert.That(labelled.StyleAt("[ACCUSE] ".Length), Is.EqualTo(ParticipantNameStyler.SpeakerStyle(3)));
            Assert.That(Enumerable.Range(0, 7).Select(ParticipantNameStyler.SpeakerStyle).Distinct().Count(), Is.EqualTo(7));
            Assert.That(ParticipantNameStyler.SpeakerStyle(15), Is.EqualTo(ParticipantNameStyler.SpeakerStyle(3)));
            var unlabelled = ParticipantNameStyler.Dialogue(SemanticText.Literal("Echo was mentioned."), new[] { "Echo" }, "Echo", 3);
            Assert.That(unlabelled.StyleAt(0), Is.EqualTo(SemanticStyle.Participant));
        }
        [Test]
        public void DirectRosterClicksRequireASelectedDiscussionActionButSubmitNightAndVoteTargetsDirectly()
        {
            var target = new CharacterId(1);
            var options = new[] {
                new PlayerOption("accuse", "Accuse", "", GameCommand.Speak(SpeechIntent.Accuse, target)),
                new PlayerOption("defend", "Defend", "", GameCommand.Speak(SpeechIntent.Vouch, target)) };
            var request = new PlayerInputRequest(null, options);
            Assert.That(RosterInteractions.DirectChoice(request, target, null), Is.Null);
            Assert.That(RosterInteractions.DirectChoice(request, target, "Accuse").Id, Is.EqualTo("accuse"));
            Assert.That(RosterInteractions.DirectChoice(request, target, "Defend").Id, Is.EqualTo("defend"));
            Assert.That(RosterInteractions.DirectChoice(request, new CharacterId(99), "Accuse"), Is.Null);
            foreach (var command in new[] { GameCommand.Night(target), GameCommand.Vote(target), GameCommand.Nominate(target) })
            {
                request = new PlayerInputRequest(null, new[] { new PlayerOption("direct", "Action", "", command) });
                Assert.That(RosterInteractions.DirectChoice(request, target, null).Id, Is.EqualTo("direct"));
            }
        }
        [Test]
        public void ReplyContextResolvesLatestStatementByReferencedSpeakerAndDayNotAnInterveningAgreement()
        {
            var speaker = new CharacterId(1); var target = new CharacterId(2);
            var accusation = new GameObservation(0, 2, GameCue.SpeechDelivered, "Nox accuses Echo", speaker, target: target, intent: SpeechIntent.Accuse);
            var echo = new GameObservation(1, 2, GameCue.SpeechDelivered, "I agree", target, target: speaker, intent: SpeechIntent.Agree);
            Assert.That(RosterInteractions.ReplyStatement(new[] { accusation, echo }, speaker, 2), Is.SameAs(accusation));
            Assert.That(RosterInteractions.ReplyStatement(new[] { accusation, echo }, speaker, 3), Is.Null);
            var defense = new GameObservation(2, 2, GameCue.SpeechDelivered, "Nox defends Echo", speaker, target: target, intent: SpeechIntent.Vouch);
            Assert.That(RosterInteractions.ReplyStatement(new[] { accusation, echo, defense }, speaker, 2), Is.SameAs(defense));
        }
    }
}
