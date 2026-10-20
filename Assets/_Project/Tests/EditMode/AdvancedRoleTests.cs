using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    /// <summary>Role interactions, resources, setup constraints, and public morning reporting.</summary>
    public sealed class AdvancedRoleTests
    {
        internal static IReadOnlyList<RoleSpec> Roles() => TestContent.Roles().Concat(new[] {
            TestContent.Role("citizen", Faction.Town, RoleCategory.TownBenign, AbilityKind.None, charges: 0),
            TestContent.Role("vigilante", Faction.Town, RoleCategory.TownKilling, AbilityKind.Attack, attack: 1, charges: 2),
            TestContent.Role("forger", Faction.Antagonist, RoleCategory.AntagonistDeception, AbilityKind.Forge),
            TestContent.Role("coordinator", Faction.Antagonist, RoleCategory.AntagonistSupport, AbilityKind.Watch),
            TestContent.Role("provocateur", Faction.Neutral, RoleCategory.NeutralEvil, AbilityKind.None, charges: 0),
            new RoleSpec(new RoleId("lone-killer"), "Lone Killer", Faction.Neutral, RoleCategory.NeutralKilling, AbilityKind.Attack,
                2, 1, 0, -1, Faction.Neutral, true)
        }).ToList().AsReadOnly();

        private static NightActor Actor(int id, string role) => new NightActor(new CharacterId(id), Roles().Single(r => r.Id.Value == role),
            Roles().Single(r => r.Id.Value == role).Charges);
        private static NightOrder Order(int actor, int? target) => new NightOrder(new CharacterId(actor), target.HasValue ? new CharacterId(target.Value) : (CharacterId?)null);
        private static void Submit(RunSession run, GameCommand command)
        { bool accepted = run.TrySubmit(command, out string error); Assert.That(accepted, Is.True, error); }
        private static RunSession Run(ScenarioStrategy strategy, params string[] roles) => RunFactory.CreateScenario(
            new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial), roles.Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
            Roles(), TestContent.Lines(), strategy);

        [Test]
        public void ActionReportAuthoringSupportsPassesAndTargetedAssertions()
        {
            Assert.DoesNotThrow(() => new DialogueLine("targeted", SpeechIntent.ReportAction, "PLACEHOLDER {target}: {result}",
                conditions: new[] { new DialogueCondition(DialogueConditionKind.HasTarget, flag: true), new DialogueCondition(DialogueConditionKind.HasAssertion, flag: true) }));
            Assert.DoesNotThrow(() => new DialogueLine("passed", SpeechIntent.ReportAction, "PLACEHOLDER {result}",
                conditions: new[] { new DialogueCondition(DialogueConditionKind.HasTarget, flag: false) }));
            Assert.Throws<ArgumentException>(() => new DialogueLine("too-early", SpeechIntent.ReportAction, "PLACEHOLDER",
                conditions: new[] { new DialogueCondition(DialogueConditionKind.MaximumDay, number: 1) }));
        }

        [Test]
        public void ForgeryAffectsOnlyThisNightsAppearanceAndBlockingPreventsIt()
        {
            var cast = new[] { Actor(0, "forger"), Actor(1, "inspector"), Actor(2, "citizen"), Actor(3, "interrupter") };
            var resolver = new NightResolver();
            var result = resolver.Resolve(cast, new[] { Order(0, 2), Order(1, 2) });
            Assert.That(result.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(Faction.Antagonist));
            result = resolver.Resolve(cast, new[] { Order(0, 2), Order(1, 2), Order(3, 0) });
            Assert.That(result.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(Faction.Town));
            result = resolver.Resolve(cast, new[] { Order(1, 2) });
            Assert.That(result.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(Faction.Town));
        }

        [Test]
        public void WatcherSeesActualVisitorsIncludingBlocksButNotBlockedVisits()
        {
            var cast = new[] { Actor(0, "coordinator"), Actor(1, "inspector"), Actor(2, "physician"), Actor(3, "interrupter"), Actor(4, "citizen") };
            var result = new NightResolver().Resolve(cast, new[] { Order(0, 4), Order(1, 4), Order(2, 4), Order(3, 1) });
            var reports = result.Reports.Where(r => r.Kind == ReportKind.VisitorSeen).ToList();
            Assert.That(reports.Count, Is.EqualTo(1));
            Assert.That(reports[0].Recipient, Is.EqualTo(new CharacterId(0)));
            Assert.That(reports[0].Target, Is.EqualTo(new CharacterId(2)));
            Assert.That(reports[0].Subject, Is.EqualTo(new CharacterId(4)));
            result = new NightResolver().Resolve(cast, new[] { Order(0, 1), Order(1, 4), Order(3, 1) });
            Assert.That(result.Reports.Any(r => r.Kind == ReportKind.VisitorSeen && r.Target == new CharacterId(3)), Is.True);
        }

        [Test]
        public void BlockedWatcherGetsNoVisitIntelligence()
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, "coordinator"), Actor(1, "interrupter"), Actor(2, "inspector"), Actor(3, "citizen") },
                new[] { Order(0, 3), Order(1, 0), Order(2, 3) });
            Assert.That(result.Reports.Any(r => r.Kind == ReportKind.VisitorSeen || r.Kind == ReportKind.NoVisitors), Is.False);
        }

        [Test]
        public void LimitedAttackSpendsOnExecutionEvenIfProtectedButNotWhenBlockedOrPassing()
        {
            var cast = new[] { Actor(0, "vigilante"), Actor(1, "physician"), Actor(2, "interrupter"), Actor(3, "citizen") };
            var resolver = new NightResolver();
            var result = resolver.Resolve(cast, new[] { Order(0, 3), Order(1, 3) });
            Assert.That(result.Deaths.Count, Is.Zero);
            Assert.That(result.SpentCharges.Contains(new CharacterId(0)), Is.True);
            result = resolver.Resolve(cast, new[] { Order(0, 3), Order(2, 0) });
            Assert.That(result.SpentCharges.Contains(new CharacterId(0)), Is.False);
            result = resolver.Resolve(cast, new[] { Order(0, null) });
            Assert.That(result.SpentCharges.Count, Is.Zero);
        }

        [Test]
        public void IndependentAttacksRemainSimultaneousAndDoNotConsumeTeamKill()
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, "vigilante"), Actor(1, "assassin"), Actor(2, "lone-killer"), Actor(3, "citizen") },
                new[] { Order(0, 1), Order(1, 0), Order(2, 3) });
            Assert.That(result.Deaths.Select(c => c.Value), Is.EquivalentTo(new[] { 0, 1, 3 }));
        }

        [Test]
        public void CitizenCannotVisitAndDuplicateCitizensFillOrdinaryTownSlots()
        {
            var citizen = Actor(0, "citizen"); var other = Actor(1, "assassin");
            Assert.That(NightLegality.IsLegal(citizen, Order(0, 1), new[] { citizen, other }), Is.False);
            Assert.That(NightLegality.IsLegal(citizen, Order(0, null), new[] { citizen, other }), Is.True);
            var list = new RoleList(new[] { new RoleSlot(new RoleId("citizen")), new RoleSlot(new RoleId("citizen")), new RoleSlot(new RoleId("assassin")) });
            var assigned = list.Resolve(Roles(), new RoleId("citizen"), new SeededRandom(1));
            Assert.That(assigned.Count(r => r.Id.Value == "citizen"), Is.EqualTo(2));
        }

        [Test]
        public void ChosenRoleFitsAnEligibleSlotAndImpossibleUniqueSetupFails()
        {
            var list = new RoleList(new[] { new RoleSlot(faction: Faction.Town), new RoleSlot(faction: Faction.Antagonist), new RoleSlot(faction: Faction.Neutral) });
            for (uint seed = 1; seed <= 20; seed++)
            {
                var a = list.Resolve(Roles(), new RoleId("vigilante"), new SeededRandom(seed));
                var b = list.Resolve(Roles(), new RoleId("vigilante"), new SeededRandom(seed));
                Assert.That(a.Select(r => r.Id), Is.EqualTo(b.Select(r => r.Id)));
                Assert.That(a[0].Id.Value, Is.EqualTo("vigilante"));
            }
            var impossible = new RoleList(new[] { new RoleSlot(new RoleId("lone-killer")), new RoleSlot(new RoleId("lone-killer")), new RoleSlot(faction: Faction.Town) });
            Assert.Throws<ArgumentException>(() => impossible.Resolve(Roles(), null, new SeededRandom(1)));
            Assert.Throws<ArgumentException>(() => list.Resolve(Roles(), new RoleId("missing"), new SeededRandom(1)));
        }

        [Test]
        public void ParityWaitsForTownCombatAndIndependentKillersAndProtectionLoopsReachCap()
        {
            var condition = new CapabilityWinCondition();
            Assert.That(condition.Evaluate(new[] { Actor(0, "assassin"), Actor(1, "vigilante") }, 1, 30), Is.EqualTo(OutcomeKind.Ongoing));
            Assert.That(condition.Evaluate(new[] { Actor(0, "assassin"), Actor(1, "citizen"), Actor(2, "lone-killer") }, 1, 30), Is.EqualTo(OutcomeKind.Ongoing));
            Assert.That(condition.Evaluate(new[] { Actor(0, "assassin"), Actor(1, "physician") }, 30, 30), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(condition.Evaluate(new[] { Actor(0, "assassin"), Actor(1, "citizen") }, 1, 30), Is.EqualTo(OutcomeKind.AntagonistVictory));
            Assert.That(condition.Evaluate(new[] { Actor(0, "lone-killer"), Actor(1, "survivor") }, 1, 30), Is.EqualTo(OutcomeKind.Ongoing));
            Assert.That(condition.Evaluate(new[] { Actor(0, "lone-killer") }, 1, 30), Is.EqualTo(OutcomeKind.LoneKillerVictory));
            Assert.That(condition.Evaluate(new NightActor[0], 1, 30), Is.EqualTo(OutcomeKind.Draw));
        }

        [Test]
        public void YieldingOpeningLetsOneNpcReactWithoutUsingADiscussionRound()
        {
            var strategy = new ScenarioStrategy();
            RunSession run = Run(strategy, "inspector", "physician", "assassin", "citizen", "survivor");
            Submit(run, GameCommand.Continue());
            var view = run.GetPlayerView();
            Assert.That(view.Phase, Is.EqualTo(GamePhase.Discussion));
            Assert.That(view.Opening, Is.False);
            Assert.That(view.Transcript.Count(e => e.Cue == GameCue.SpeechDelivered), Is.Zero, "A silent opener emits no speech.");
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Nomination));
        }

        [Test]
        public void LastNightReportIsUnavailableOnDayOneAndReportsRecordedActionOnDayTwo()
        {
            RunSession run = Run(new ScenarioStrategy(), "physician", "inspector", "assassin", "citizen", "survivor");
            Assert.That(run.TrySubmit(GameCommand.ReportLastAction(), out _), Is.False);
            Submit(run, GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician"))); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(new CharacterId(1))); Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.ReportLastAction());
            var line = run.GetPlayerView().Transcript.Last(e => e.Speaker == new CharacterId(0));
            Assert.That(line.Intent, Is.EqualTo(SpeechIntent.ReportAction));
            Assert.That(line.Target, Is.EqualTo(new CharacterId(1)));
            Assert.That(run.GetPlayerView().CanReportAction, Is.False, "A report is given once for the previous night.");
        }

        [Test]
        public void ClaimedInvestigatorReportsFindingAtDawnBeforePlayerOpening()
        {
            var strategy = new ScenarioStrategy { Target = k => k.OwnRole.Ability == AbilityKind.Investigate ? new CharacterId(3) : (CharacterId?)null };
            // Claim through a dedicated permitted strategy; all other decisions retain the deterministic scenario policy.
            RunSession run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "citizen", "inspector", "assassin", "physician", "survivor" }.Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
                Roles(), TestContent.Lines(), new ClaimingStrategy(strategy));
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null)); Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Opening, Is.True);
            Assert.That(run.GetPlayerView().Transcript.Any(e => e.Day == 2 && e.Speaker == new CharacterId(1)
                && e.NightReport != null && e.NightReport.Results.Any(r => r.Kind == ReportKind.Investigation)), Is.True);
        }

        [Test]
        public void ProvocateurExecutionRecordsPersonalWinWithoutEndingFactionGame()
        {
            var strategy = new ScenarioStrategy { Nominee = new CharacterId(0), Verdict = Verdict.Guilty };
            var members = new[] { "provocateur", "inspector", "physician", "interrupter", "assassin", "citizen" };
            RunSession run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, maximumDays: 2, votingMode: VotingMode.Trial),
                members.Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))), Roles(), TestContent.Lines(), strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Speak(SpeechIntent.Defend)); Submit(run, GameCommand.Continue());
            Submit(run, GameCommand.Speak(SpeechIntent.LastWords));
            Assert.That(run.GetPlayerView().Alive, Is.False);
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.NightSelection));
            Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Dawn));
            for (int step = 0; step < 10 && run.GetPlayerView().Phase != GamePhase.Ended; step++)
                Submit(run, GameCommand.Continue());
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Ended));
            Assert.That(run.GetPlayerView().Won, Is.True);
        }

        [Test]
        public void ProvocateurNightDeathDoesNotEarnExecutionObjective()
        {
            var strategy = new ScenarioStrategy { Target = k => k.OwnRole.Ability == AbilityKind.Attack ? new CharacterId(0) : (CharacterId?)null };
            var members = new[] { "provocateur", "inspector", "physician", "interrupter", "assassin", "citizen" };
            RunSession run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, maximumDays: 1, votingMode: VotingMode.Trial),
                members.Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))), Roles(), TestContent.Lines(), strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Ended));
            Assert.That(run.GetPlayerView().Won, Is.False);
        }

        [Test]
        public void FabricatedMorningReportsDoNotAccidentallyExposeKnownTeammates()
        {
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "citizen", "forger", "assassin", "physician", "inspector", "survivor" }
                    .Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
                Roles(), TestContent.Lines(), new FalseInvestigators());
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null));
            Submit(run, GameCommand.Night(null)); Submit(run, GameCommand.Continue());
            var reports = run.GetPlayerView().Transcript.Where(e => e.Day == 2 && e.NightReport != null
                && (e.Speaker == new CharacterId(1) || e.Speaker == new CharacterId(2))).ToList();
            Assert.That(reports.Count, Is.EqualTo(1), "Teammates must not all impersonate the same investigator.");
            Assert.That(reports.All(e => e.NightReport.Results.All(r => r.Kind != ReportKind.Investigation
                || r.Alignment != Faction.Antagonist || r.Target != new CharacterId(1) && r.Target != new CharacterId(2))), Is.True);
        }

        [Test]
        public void ExhaustedAntagonistCannotStealAnActiveTeammatesKill()
        {
            var roles = Roles().Concat(new[] {
                TestContent.Role("spent-assassin", Faction.Antagonist, RoleCategory.AntagonistKilling, AbilityKind.Attack, attack: 1, charges: 0)
            }).ToList();
            var strategy = new ScenarioStrategy { Target = k => k.OwnRole.Ability == AbilityKind.Attack && k.Charges != 0
                ? new CharacterId(4) : (CharacterId?)null };
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                new[] { "inspector", "spent-assassin", "assassin", "physician", "citizen", "interrupter", "survivor" }
                    .Select((id, i) => new CastMemberSpec("Person" + i, new RoleId(id))),
                roles, TestContent.Lines(), strategy);
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(null)); Submit(run, GameCommand.Night(null));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == new CharacterId(4)).Alive, Is.False);
            Assert.That(run.GetPlayerView().Characters.Count(c => !c.Alive), Is.EqualTo(1));
        }

        private sealed class FalseInvestigators : INpcStrategy
        {
            public GameCommand Speak(KnowledgeView k, IRandomSource r) => k.OwnRole.Faction == Faction.Antagonist
                ? GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("inspector")) : GameCommand.Speak(SpeechIntent.StaySilent);
            public CharacterId? Nominate(KnowledgeView k, IRandomSource r) => null;
            public Verdict Judge(KnowledgeView k, CharacterId d, IRandomSource r) => Verdict.Innocent;
            public CharacterId? NightTarget(KnowledgeView k, IRandomSource r) => null;
        }

        private sealed class ClaimingStrategy : INpcStrategy
        {
            private readonly ScenarioStrategy _inner;
            internal ClaimingStrategy(ScenarioStrategy inner) { _inner = inner; }
            public GameCommand Speak(KnowledgeView k, IRandomSource r) => k.OwnRole.Ability == AbilityKind.Investigate
                ? GameCommand.Speak(SpeechIntent.ClaimRole, role: k.OwnRole.Id) : _inner.Speak(k, r);
            public CharacterId? Nominate(KnowledgeView k, IRandomSource r) => _inner.Nominate(k, r);
            public Verdict Judge(KnowledgeView k, CharacterId d, IRandomSource r) => _inner.Judge(k, d, r);
            public CharacterId? NightTarget(KnowledgeView k, IRandomSource r) => _inner.NightTarget(k, r);
        }
    }
}
