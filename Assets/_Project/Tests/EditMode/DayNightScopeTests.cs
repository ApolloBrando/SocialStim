using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Settings;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>The cancelled activity has no phase, commands or settings, including when older preferences are loaded.</summary>
    public sealed class DayNightScopeTests
    {
        private sealed class Quiet : ICouncilStrategy
        {
            public CouncilAction Speak(CouncilKnowledge k, IRandomSource r) => new CouncilAction(CouncilActionKind.Pass);
            public CharacterId? Vote(CouncilKnowledge k, IRandomSource r) => new CharacterId(1);
            public CharacterId? NightTarget(CouncilKnowledge k, IRandomSource r) => null;
        }
        private static CouncilSession Run() => CouncilFactory.CreateScenario(new CouncilOptions(147, rounds: 3),
            new[] { "solarian", "decryptor", "physician", "archon", "infinitum" }
                .Select((role, i) => new CastMemberSpec("Person" + i, new RoleId(role))),
            MissionTestContent.Roles(), TestContent.Lines(), new Quiet());
        private static void Submit(CouncilSession run, CouncilAction action)
            => Assert.That(run.TrySubmit(action, out string error), Is.True, error);
        [Test]
        public void DayEliminationProceedsDirectlyToNightThenMorningAndDiscussion()
        {
            var run = Run();
            for (int i = 0; i < 3; i++) Submit(run, new CouncilAction(CouncilActionKind.Pass));
            Assert.That(run.View().Phase, Is.EqualTo(CouncilPhase.Vote));
            Submit(run, new CouncilAction(CouncilActionKind.Vote, new CharacterId(1)));
            Assert.That(run.View().Phase, Is.EqualTo(CouncilPhase.Elimination));
            Submit(run, new CouncilAction(CouncilActionKind.Continue));
            Assert.That(run.View().Phase, Is.EqualTo(CouncilPhase.Night));
            Submit(run, new CouncilAction(CouncilActionKind.Night));
            Assert.That(run.View().Phase, Is.EqualTo(CouncilPhase.Morning));
            Submit(run, new CouncilAction(CouncilActionKind.Continue));
            Assert.That(run.View().Phase, Is.EqualTo(CouncilPhase.Discussion));
            Assert.That(run.View().Day, Is.EqualTo(2));
        }
        [Test]
        public void RetiredActionAndIntentCannotBeSubmittedOrConsumeATurn()
        {
            var run = Run(); int revision = run.View().Revision;
            Assert.That(run.TrySubmit(new CouncilAction((CouncilActionKind)9, new CharacterId(1)), out _), Is.False);
            Assert.That(run.TrySubmit(new CouncilAction(CouncilActionKind.Chat, intent: (SpeechIntent)26), out _), Is.False);
            Assert.That(run.View().Revision, Is.EqualTo(revision));
            Assert.That(Enum.IsDefined(typeof(CouncilPhase), 3), Is.False);
            Assert.That((int)CouncilPhase.Night, Is.EqualTo(4));
            Assert.That((int)CouncilEventKind.Decision, Is.EqualTo(16));
            Assert.That((int)SpeechIntent.AnnounceVote, Is.EqualTo(27));
            Assert.That((int)GameSetting.PromoteEvil, Is.EqualTo(11));
        }
        [Test]
        public void OldActivitySettingsAreIgnoredAndNotWrittenBack()
        {
            var loaded = PreferencesCodec.Read("{\"version\":2,\"game\":{\"FreeTime\":1,\"FreeTimeActions\":2,\"Rounds\":7}}", 20, out string warning);
            Assert.That(warning, Is.Null); Assert.That(loaded.Game.Get(GameSetting.Rounds), Is.EqualTo(7));
            Assert.That(PreferencesCodec.Write(loaded).Contains("FreeTime"), Is.False);
            Assert.That(Enum.IsDefined(typeof(GameSetting), 9), Is.False);
            Assert.That(Enum.IsDefined(typeof(GameSetting), 10), Is.False);
        }
    }
}
