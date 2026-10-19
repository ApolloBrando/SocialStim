using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Persistence;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Complete snapshots preserve continuation, not only a replay seed or visible transcript.</summary>
    public sealed class SaveResumeTests
    {
        private static RunSession Create(uint seed = 123, string player = "vigilante") => RunFactory.CreateScenario(
            new RunOptions(seed, discussionRounds: 3, maximumDays: 5),
            new[] { player, "assassin", "physician", "inspector", "interrupter", "citizen", "forger", "coordinator", "lone-killer" }
                .Select((role, i) => new CastMemberSpec("Person" + i, new RoleId(role))),
            AdvancedRoleTests.Roles(), TestContent.Lines());

        private static GameCommand Next(RunSession run)
        {
            var v = run.GetPlayerView();
            if (!v.Alive) return GameCommand.Continue();
            CharacterId? target = v.Characters.Where(c => c.Alive && c.Id != v.PlayerId).Select(c => (CharacterId?)c.Id).FirstOrDefault();
            switch (v.Phase)
            {
                case GamePhase.Discussion: return v.Opening ? GameCommand.Continue() : v.CanReportAction && v.Revision % 3 == 0
                    ? GameCommand.ReportLastAction() : target.HasValue ? GameCommand.Speak(SpeechIntent.Accuse, target) : GameCommand.Speak(SpeechIntent.StaySilent);
                case GamePhase.Nomination: return GameCommand.Nominate(target);
                case GamePhase.Defense: return v.Defendant == v.PlayerId ? GameCommand.Speak(SpeechIntent.Defend) : GameCommand.Continue();
                case GamePhase.Judgement: return v.Defendant == v.PlayerId ? GameCommand.Continue() : GameCommand.Judge(Verdict.Guilty);
                case GamePhase.LastWords: return v.Defendant == v.PlayerId ? GameCommand.Speak(SpeechIntent.LastWords) : GameCommand.Continue();
                case GamePhase.NightSelection: return GameCommand.Night(v.LegalNightTargets.Select(id => (CharacterId?)id).FirstOrDefault());
                default: return GameCommand.Continue();
            }
        }

        [TestCase("vigilante")]
        [TestCase("survivor")]
        [TestCase("inspector")]
        [TestCase("assassin")]
        public void SnapshotRoundTripAndContinuationRemainByteIdenticalAtEveryInputBoundary(string player)
        {
            for (uint seed = 1; seed <= 3; seed++)
            {
                RunSession original = Create(seed, player);
                for (int step = 0; step < 120; step++)
                {
                    byte[] saved = original.Save();
                    RunSession restored = RunSession.Load(saved, AdvancedRoleTests.Roles(), TestContent.Lines());
                    Assert.That(restored.Save(), Is.EqualTo(saved), "Complete state differs after load.");
                    var before = new GameController(original, AdvancedRoleTests.Roles()).Request();
                    var after = new GameController(restored, AdvancedRoleTests.Roles()).Request();
                    Assert.That(after.Options.Select(o => o.Id + "|" + o.Label), Is.EqualTo(before.Options.Select(o => o.Id + "|" + o.Label)));
                    if (original.GetPlayerView().Phase == GamePhase.Ended) break;
                    GameCommand command = Next(original);
                    bool accepted = original.TrySubmit(command, out string error);
                    Assert.That(accepted, Is.True, error);
                    accepted = restored.TrySubmit(command, out error);
                    Assert.That(accepted, Is.True, error);
                    Assert.That(restored.Save(), Is.EqualTo(original.Save()), "Random streams, beliefs, resources, or dialogue diverged.");
                    if (step == 119) Assert.Fail("Seeded continuation did not reach a bounded outcome.");
                }
            }
        }

        private static void RoundTrip(RunSession run)
        {
            byte[] saved = run.Save();
            RunSession loaded = RunSession.Load(saved, AdvancedRoleTests.Roles(), TestContent.Lines());
            Assert.That(loaded.Save(), Is.EqualTo(saved));
        }

        [Test]
        public void TrialDefenseJudgementLastWordsAndExecutionSnapshotsPreservePendingInput()
        {
            RunSession run = RunFactory.CreateScenario(new RunOptions(123, discussionRounds: 10, maximumDays: 5),
                new[] { "vigilante", "assassin", "citizen", "citizen" }.Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
                AdvancedRoleTests.Roles(), TestContent.Lines(), new BasicNpcStrategy());
            for (int i = 0; i < 10; i++) Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(1)), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Nominate(new CharacterId(1)), out _), Is.True);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Defense)); RoundTrip(run);
            bool playerDefendant = run.GetPlayerView().Defendant == run.GetPlayerView().PlayerId;
            Assert.That(run.TrySubmit(playerDefendant ? GameCommand.Speak(SpeechIntent.Defend) : GameCommand.Continue(), out _), Is.True);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Judgement)); RoundTrip(run);
            Assert.That(run.TrySubmit(playerDefendant ? GameCommand.Continue() : GameCommand.Judge(Verdict.Guilty), out _), Is.True);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.LastWords)); RoundTrip(run);
            Assert.That(run.TrySubmit(playerDefendant ? GameCommand.Speak(SpeechIntent.LastWords) : GameCommand.Continue(), out _), Is.True);
            Assert.That(run.GetPlayerView().Phase == GamePhase.Ended || run.GetPlayerView().Phase == GamePhase.NightSelection, Is.True);
            RoundTrip(run);
        }

        [Test]
        public void CorruptionTruncationFutureSchemaAndContentChangesFailWithoutMutatingLiveRun()
        {
            RunSession run = Create(); byte[] original = run.Save();
            byte[] damaged = (byte[])original.Clone(); damaged[damaged.Length - 1] ^= 1;
            Assert.Throws<InvalidDataException>(() => RunSession.Load(damaged, AdvancedRoleTests.Roles(), TestContent.Lines()));
            damaged = (byte[])original.Clone(); damaged[4] = 99;
            Assert.Throws<InvalidDataException>(() => RunSession.Load(damaged, AdvancedRoleTests.Roles(), TestContent.Lines()));
            Assert.Throws<InvalidDataException>(() => RunSession.Load(original.Take(original.Length - 4).ToArray(), AdvancedRoleTests.Roles(), TestContent.Lines()));
            var changedLines = TestContent.Lines().Select(l => new DialogueLine(l.Id, l.Intent, l.Text + " changed")).ToList();
            Assert.Throws<InvalidDataException>(() => RunSession.Load(original, AdvancedRoleTests.Roles(), changedLines));
            Assert.Throws<InvalidDataException>(() => RunSession.Load(original, AdvancedRoleTests.Roles(), TestContent.Lines(), new ReasoningSettings(assertionWeight: 13)));
            Assert.Throws<InvalidDataException>(() => RunSession.Load(original, AdvancedRoleTests.Roles().Where(r => r.Id.Value != "forger"), TestContent.Lines()));
            Assert.That(run.Save(), Is.EqualTo(original));
        }

        [Test]
        public void SavingOrSubmittingInsideAnObservationCallbackCannotCaptureHalfACommand()
        {
            RunSession run = Create(); int callbacks = 0;
            run.Observation += observation =>
            {
                callbacks++;
                Assert.Throws<InvalidOperationException>(() => run.Save());
                Assert.That(run.TrySubmit(GameCommand.Continue(), out _), Is.False);
            };
            Assert.That(run.TrySubmit(GameCommand.Continue(), out _), Is.True);
            Assert.That(callbacks, Is.GreaterThan(0));
            RoundTrip(run);
        }

        [Test]
        public void UnsupportedCustomStrategiesAreRejectedExplicitly()
        {
            var run = RunFactory.CreateScenario(new RunOptions(1),
                new[] { "inspector", "assassin", "survivor" }.Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
                TestContent.Roles(), TestContent.Lines(), new ScenarioStrategy());
            Assert.Throws<NotSupportedException>(() => run.Save());
        }

        [Test]
        public void AtomicSlotsRecoverValidBackupAndPreserveItAfterCorruptPrimary()
        {
            string root = Path.Combine(Path.GetTempPath(), "SocialStim-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(root); RunSession run = Create();
                byte[] first = run.Save();
                RunSaveService.Save(store, run, AdvancedRoleTests.Roles(), TestContent.Lines(), null);
                Assert.That(run.TrySubmit(Next(run), out _), Is.True);
                RunSaveService.Save(store, run, AdvancedRoleTests.Roles(), TestContent.Lines(), null);
                Assert.That(store.Read(SaveSlot.Run, backup: true), Is.EqualTo(first));
                File.WriteAllBytes(Path.Combine(root, "run.sstim"), new byte[] { 1, 2, 3 });
                RunSession recovered = RunSaveService.Load(store, AdvancedRoleTests.Roles(), TestContent.Lines(), null, out bool usedBackup);
                Assert.That(usedBackup, Is.True);
                Assert.That(recovered.Save(), Is.EqualTo(first));
                RunSaveService.Save(store, run, AdvancedRoleTests.Roles(), TestContent.Lines(), null);
                Assert.That(store.Read(SaveSlot.Run, backup: true), Is.EqualTo(first));
                byte[] current = store.Read(SaveSlot.Run);
                Assert.Throws<InvalidDataException>(() => store.Write(SaveSlot.Run, null));
                Assert.That(store.Read(SaveSlot.Run), Is.EqualTo(current));
                Assert.That(Directory.GetFiles(root, "*.tmp").Length, Is.Zero);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void AccessibilitySettingsPersistIndependentlyAndValidateVersionAndFlags()
        {
            RunSession run = Create(); byte[] before = run.Save();
            var settings = new EffectsSettings(enabled: false, reducedMotion: false, flashes: true, intensity: 0.75f, instantText: true, elementsPerSecond: 18);
            byte[] encoded = EffectsSettingsCodec.Save(settings);
            var restored = EffectsSettingsCodec.Load(encoded);
            Assert.That(EffectsSettingsCodec.Save(restored), Is.EqualTo(encoded));
            Assert.That(run.Save(), Is.EqualTo(before));
            encoded[4] = 99;
            Assert.Throws<InvalidDataException>(() => EffectsSettingsCodec.Load(encoded));
            encoded = EffectsSettingsCodec.Save(settings); encoded[8] = 2;
            Assert.Throws<InvalidDataException>(() => EffectsSettingsCodec.Load(encoded));
        }
    }
}
