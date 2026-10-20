using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Requested claim, content, seven-person setup and discussion regressions against actual role assets.</summary>
    public sealed class DiscussionRevisionTests
    {
        private sealed class Speaker : INpcStrategy
        {
            internal Func<KnowledgeView, GameCommand> Speech = _ => GameCommand.Speak(SpeechIntent.StaySilent);
            public GameCommand Speak(KnowledgeView k, IRandomSource r) => Speech(k);
            public CharacterId? Nominate(KnowledgeView k, IRandomSource r) => null;
            public Verdict Judge(KnowledgeView k, CharacterId d, IRandomSource r) => Verdict.Innocent;
            public CharacterId? NightTarget(KnowledgeView k, IRandomSource r) => null;
        }

        internal static IReadOnlyList<RoleSpec> AuthoredRoles()
        {
#if UNITY_EDITOR
            string directory = Path.Combine(UnityEngine.Application.dataPath, "_Project/ScriptableObjects/Roles");
#else
            string directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Roles");
#endif
            return Directory.GetFiles(directory, "*.asset").OrderBy(p => p, StringComparer.Ordinal).Select(path => {
                string source = File.ReadAllText(path);
                Func<string, string> text = key => Regex.Match(source, @"^  _" + key + @": (.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
                Func<string, int> number = key => int.TryParse(text(key), out int value) ? value : 0;
                return new RoleSpec(new RoleId(text("id")), text("displayName"), (Faction)number("faction"),
                    (RoleCategory)number("category"), (AbilityKind)number("ability"), number("attack"), number("defense"),
                    number("protection"), number("charges"), (Faction)number("appearance"), number("unique") == 1,
                    number("canClaimTruthfully") == 1, number("canBluff") == 1, number("nightImmune") == 1, number("stealsParityWin") == 1);
            }).ToList().AsReadOnly();
        }

        private static RunSession Run(Speaker strategy, params string[] ids) => RunFactory.CreateScenario(new RunOptions(42),
            ids.Select((id, index) => new CastMemberSpec("Person" + index, new RoleId(id))), AuthoredRoles(), TestContent.AuthoredLines(), strategy);
        private static void Submit(RunSession run, GameCommand command)
        { Assert.That(run.TrySubmit(command, out string error), Is.True, error); }

        [Test]
        public void EveryAuthoredRoleCanBeChosenAtSevenWithoutDuplicateSpecials()
        {
            var roles = AuthoredRoles();
            Assert.That(roles.Where(r => r.Ability != AbilityKind.None).All(r => r.Unique), Is.True);
            foreach (RoleSpec chosen in roles)
                for (uint seed = 1; seed <= 20; seed++)
                {
                    var setup = RoleList.ForPopulation(7).Resolve(roles, chosen.Id, new SeededRandom(seed));
                    Assert.That(setup.Count, Is.EqualTo(7));
                    Assert.That(setup.Any(r => r.Id == chosen.Id), Is.True, chosen.Name);
                    Assert.That(setup.GroupBy(r => r.Id).All(g => !g.First().Unique || g.Count() == 1), Is.True);
                }
        }

        [Test]
        public void DuplicateSpecialsAreRejectedInExplicitScenarios()
        {
            Assert.Throws<ArgumentException>(() => Run(new Speaker(), "physician", "physician", "assassin"));
            Assert.DoesNotThrow(() => Run(new Speaker(), "citizen", "citizen", "assassin"));
        }

        [Test]
        public void ClaimPermissionsMatchForEveryAuthoredRolePair()
        {
            var roles = AuthoredRoles();
            foreach (RoleSpec actor in roles)
                foreach (RoleSpec asserted in roles)
                {
                    bool expected = asserted.Faction == Faction.Town && asserted.Ability != AbilityKind.None
                        && (actor.Faction != Faction.Town || actor.Id == asserted.Id);
                    Assert.That(actor.CanClaim(asserted), Is.EqualTo(expected), actor.Name + " / " + asserted.Name);
                }
        }

        [Test]
        public void CivilianMenuHasOnlyWaitAccuseAndDefendAndAllLivingTargets()
        {
            var run = Run(new Speaker(), "citizen", "physician", "assassin", "inspector");
            var options = new GameController(run, AuthoredRoles()).Request().Options;
            Assert.That(options.Where(o => !o.HasSubmenu).Select(o => o.Label), Is.EqualTo(new[] { "Wait" }));
            Assert.That(options.Where(o => o.HasSubmenu).Select(o => o.Category).Distinct(), Is.EquivalentTo(new[] { "Accuse", "Defend" }));
            Assert.That(options.Count(o => o.Category == "Accuse"), Is.EqualTo(3));
            Assert.That(options.Count(o => o.Category == "Defend"), Is.EqualTo(3));
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Assert.That(run.GetPlayerView().Transcript.Any(e => e.Intent == SpeechIntent.StaySilent), Is.False);
        }

        [Test]
        public void TrueRoleOwnerCounterclaimsImmediatelyWithSeparateTraitDialogue()
        {
            var run = Run(new Speaker(), "assassin", "physician", "citizen", "inspector");
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")));
            var lines = run.GetPlayerView().Transcript.Where(e => e.Cue == GameCue.SpeechDelivered).ToList();
            Assert.That(lines[0].Intent, Is.EqualTo(SpeechIntent.ClaimRole));
            Assert.That(lines[1].Intent, Is.EqualTo(SpeechIntent.CounterClaim));
            Assert.That(lines[1].Speaker, Is.EqualTo(new CharacterId(1)));
            Assert.That(lines[1].ClaimedRole.Value.Value, Is.EqualTo("physician"));
        }

        [Test]
        public void PlayerCounterclaimWindowPrecedesUnrelatedNpcSpeechAndCannotSpendANormalTurn()
        {
            var strategy = new Speaker { Speech = k => GameCommand.Speak(SpeechIntent.ClaimRole, role: k.OwnRole.Id) };
            var run = Run(strategy, "assassin", "physician", "inspector");
            Submit(run, GameCommand.Continue());
            var before = run.GetPlayerView();
            Assert.That(before.CounterClaimRole.HasValue, Is.True);
            Assert.That(before.Transcript.Count(e => e.Intent == SpeechIntent.ClaimRole), Is.EqualTo(1));
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(1)), out _), Is.False);
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(before.Revision));
            var request = new GameController(run, AuthoredRoles()).Request();
            Assert.That(request.Options.Count, Is.EqualTo(2));
            Submit(run, GameCommand.Speak(SpeechIntent.CounterClaim, role: before.CounterClaimRole));
            var after = run.GetPlayerView();
            Assert.That(after.DiscussionRound, Is.EqualTo(before.DiscussionRound));
            Assert.That(after.Transcript.Last(e => e.Cue == GameCue.SpeechDelivered).Intent, Is.EqualTo(SpeechIntent.CounterClaim));
        }

        [Test]
        public void AntagonistTeammatesDoNotPileOntoTheSameCounterclaim()
        {
            var run = Run(new Speaker(), "physician", "assassin", "forger", "interrupter", "citizen");
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")));
            var counters = run.GetPlayerView().Transcript.Where(e => e.Intent == SpeechIntent.CounterClaim).ToList();
            Assert.That(counters.Count, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void CounterclaimDuringTheLastVictimAnswerMustFinishBeforeVoting()
        {
            var schedule = new DiscussionSchedule(3, 1);
            var exchange = new DiscussionExchange(schedule, new CharacterId(0));
            for (int i = 0; i < 2; i++)
            {
                exchange.Begin(Array.Empty<CharacterId>(), GameCommand.Speak(SpeechIntent.StaySilent), _ => { });
                exchange.Resume(_ => GameCommand.Speak(SpeechIntent.StaySilent), _ => { }, true);
            }
            bool counterclaim = false;
            exchange.Begin(new[] { new CharacterId(1) }, GameCommand.Speak(SpeechIntent.StaySilent), _ => { });
            exchange.Resume(_ => GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(2)), _ => counterclaim = true,
                true, () => counterclaim);
            Assert.That(schedule.Complete, Is.False);
            counterclaim = false;
            exchange.Resume(_ => GameCommand.Speak(SpeechIntent.StaySilent), _ => { }, true, () => counterclaim);
            Assert.That(schedule.Complete, Is.True);
        }

        [Test]
        public void BothStrategiesKeepTownClaimsHonestRegardlessOfTrait()
        {
            var roles = AuthoredRoles();
            var town = roles.First(r => r.Id.Value == "physician");
            var characters = new[] { new KnownCharacter(new CharacterId(0), "Self", true, 80, false),
                new KnownCharacter(new CharacterId(1), "Other", true, 70, false) };
            foreach (Personality trait in Enum.GetValues(typeof(Personality)))
                foreach (INpcStrategy strategy in new INpcStrategy[] { new BasicNpcStrategy(), new SocialNpcStrategy() })
                    for (uint seed = 1; seed <= 30; seed++)
                    {
                        var knowledge = new KnowledgeView(new CharacterId(0), town, trait, -1, 1, characters,
                            Array.Empty<NightReport>(), selfExposure: 90, roleCatalog: roles);
                        GameCommand speech = strategy.Speak(knowledge, new SeededRandom(seed));
                        if (speech.ClaimedRole.HasValue) Assert.That(speech.ClaimedRole.Value, Is.EqualTo(town.Id));
                    }
        }

        [Test]
        public void EveryTraitHasEightDistinctCounterclaimsAndUsesTheWholeEligiblePoolBeforeRepeating()
        {
            var lines = TestContent.AuthoredLines();
            var selector = new DialogueSelector(lines, new SeededRandom(48));
            foreach (Personality trait in Enum.GetValues(typeof(Personality)))
            {
                var pool = lines.Where(l => l.RequiredTrait == trait && l.Intent == SpeechIntent.CounterClaim).ToList();
                Assert.That(pool.Count, Is.GreaterThanOrEqualTo(trait == Personality.None ? 4 : 8));
                var selected = new HashSet<string>();
                for (int i = 0; i < pool.Count; i++)
                {
                    var context = new DialogueContext(new CharacterId((int)trait + 1), "Self", "Other", "Physician", "", 1,
                        new RoleId("assassin"), trait);
                    Assert.That(selected.Add(selector.Select(SpeechIntent.CounterClaim, context).Id), Is.True);
                }
            }
        }

        [Test]
        public void InfinitumIsPassiveNightImmuneAndStealsOnlyAntagonistParity()
        {
            var roles = AuthoredRoles();
            Func<int, string, NightActor> actor = (id, role) => new NightActor(new CharacterId(id), roles.First(r => r.Id.Value == role), -1);
            var cast = new[] { actor(0, "survivor"), actor(1, "assassin"), actor(2, "lone-killer"), actor(3, "citizen") };
            Assert.That(cast[0].Role.Name, Is.EqualTo("Infinitum"));
            Assert.That(cast[0].Role.Ability, Is.EqualTo(AbilityKind.None));
            var resolution = new NightResolver().Resolve(cast, new[] {
                new NightOrder(new CharacterId(1), new CharacterId(0)), new NightOrder(new CharacterId(2), new CharacterId(0)) });
            Assert.That(resolution.Deaths, Has.No.Member(new CharacterId(0)));
            var win = new CapabilityWinCondition();
            Assert.That(win.Evaluate(new[] { cast[0], cast[1], cast[3] }, 1, 30), Is.EqualTo(OutcomeKind.InfinitumVictory));
            Assert.That(win.Evaluate(new[] { cast[0], cast[3] }, 1, 30), Is.EqualTo(OutcomeKind.TownVictory));
            Assert.That(win.Evaluate(new[] { cast[0], cast[1], actor(4, "physician") }, 1, 30), Is.EqualTo(OutcomeKind.Ongoing),
                "Infinitum must wait for the configured normal antagonist win condition.");
            Assert.That(win.Evaluate(new[] { cast[0], cast[2] }, 1, 30), Is.EqualTo(OutcomeKind.Draw));
        }

        [Test]
        public void SevenPersonAuthoredRunsTerminateForEveryPlayerRoleWithoutIllegalOptions()
        {
            var roles = AuthoredRoles();
            foreach (RoleSpec role in roles)
                for (uint seed = 1; seed <= 6; seed++)
                {
                    var run = RunFactory.Create(new RunOptions(seed, role.Id, maximumDays: 8), roles, TestContent.Names(),
                        TestContent.AuthoredLines(), setup: RoleList.ForPopulation(7));
                    var controller = new GameController(run, roles);
                    var random = new SeededRandom(seed ^ 5891u);
                    for (int turn = 0; turn < 1500 && run.GetPlayerView().Phase != GamePhase.Ended; turn++)
                    {
                        var request = controller.Request();
                        Assert.That(request.Options.Count, Is.GreaterThan(0), role.Name);
                        Assert.That(controller.TryChoose(request.Options[random.Next(request.Options.Count)].Id, out string error), Is.True,
                            role.Name + " / " + request.View.Phase + " / " + error);
                    }
                    Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Ended), role.Name);
                }
        }
    }
}
