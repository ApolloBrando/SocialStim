using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Input;
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
    /// <summary>Edge cases against the eleven shipped role assets, rather than legacy synthetic role definitions.</summary>
    public sealed class RoleEdgeCaseTests
    {
        private static readonly string[] RoleIds = { "inspector", "physician", "citizen", "vigilante", "coordinator", "assassin", "interrupter", "forger", "lone-killer", "survivor", "provocateur" };
        private static CharacterId Id(int value) => new CharacterId(value);
        private static RoleSpec Role(string id) => DiscussionRevisionTests.AuthoredRoles().Single(r => r.Id.Value == id);
        private static NightActor Actor(int id, string role) => new NightActor(Id(id), Role(role), Role(role).Charges);
        private static NightOrder Order(int actor, int? target) => new NightOrder(Id(actor), target.HasValue ? Id(target.Value) : (CharacterId?)null);
        private static void Submit(RunSession run, GameCommand command) => Assert.That(run.TrySubmit(command, out string error), Is.True, error);

        [TestCaseSource(nameof(RoleIds))]
        public void PassingNeverVisitsOrSpendsAChargeForEveryShippedRole(string role)
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, role), Actor(1, "citizen") }, new[] { Order(0, null) });
            Assert.That(result.Visits, Is.Empty); Assert.That(result.SpentCharges, Is.Empty);
            Assert.That(result.Deaths, Is.Empty); Assert.That(result.Reports, Is.Empty);
        }

        [TestCaseSource(nameof(RoleIds))]
        public void SelfUnknownExhaustedAndTeammateTargetRestrictionsMatchEveryRole(string role)
        {
            var actor = Actor(0, role); var actors = new[] { actor, Actor(1, "citizen"), Actor(2, "assassin") };
            bool active = actor.Role.Ability != AbilityKind.None && actor.Charges != 0;
            Assert.That(NightLegality.IsLegal(actor, Order(0, 0), actors), Is.False);
            Assert.That(NightLegality.IsLegal(actor, Order(0, 99), actors), Is.False);
            Assert.That(NightLegality.IsLegal(actor, Order(0, 1), actors), Is.EqualTo(active));
            Assert.That(NightLegality.IsLegal(actor, Order(0, 2), actors), Is.EqualTo(active && !(actor.Role.Faction == Faction.Antagonist && actor.Role.Ability == AbilityKind.Attack)));
            var exhausted = new NightActor(actor.Id, actor.Role, 0);
            Assert.That(NightLegality.IsLegal(exhausted, Order(0, 1), new[] { exhausted, actors[1] }), Is.False);
            Assert.That(NightLegality.IsLegal(exhausted, Order(0, null), new[] { exhausted, actors[1] }), Is.True);
        }

        [TestCaseSource(nameof(RoleIds))]
        public void SingerBlocksEachActiveRoleButAcceptedBlocksStillExecute(string role)
        {
            var actor = Actor(0, role); bool active = actor.Role.Ability != AbilityKind.None;
            var result = new NightResolver().Resolve(new[] { actor, Actor(1, "interrupter"), Actor(2, "citizen"), Actor(3, "coordinator") },
                new[] { Order(0, active ? 2 : (int?)null), Order(1, 0), Order(3, 2) });
            Assert.That(result.Reports.Count(r => r.Recipient == Id(0) && r.Kind == ReportKind.Blocked), Is.EqualTo(1));
            bool block = actor.Role.Ability == AbilityKind.Block;
            Assert.That(result.Visits.Any(v => v.Visitor == Id(0)), Is.EqualTo(block));
            Assert.That(result.SpentCharges.Contains(Id(0)), Is.False);
            Assert.That(result.Reports.Any(r => r.Recipient == Id(3) && r.Kind == ReportKind.VisitorSeen && r.Target == Id(0)), Is.EqualTo(block));
            Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.Reports.Any(r => r.Recipient == Id(0) && (r.Kind == ReportKind.Investigation || r.Kind == ReportKind.ProtectionApplied || r.Kind == ReportKind.ForgeryApplied || r.Kind == ReportKind.NoVisitors)), Is.False);
        }

        [TestCaseSource(nameof(RoleIds))]
        public void SimultaneousDeathDoesNotCancelALockedActionAndInfinitumRemainsImmune(string role)
        {
            var actor = Actor(0, role); bool active = actor.Role.Ability != AbilityKind.None;
            var result = new NightResolver().Resolve(new[] { actor, Actor(1, "lone-killer"), Actor(2, "citizen") },
                new[] { Order(0, active ? 2 : (int?)null), Order(1, 0) });
            Assert.That(result.Deaths.Contains(Id(0)), Is.EqualTo(!actor.Role.NightImmune));
            Assert.That(result.Visits.Any(v => v.Visitor == Id(0) && v.Target == Id(2)), Is.EqualTo(active));
            Assert.That(result.SpentCharges.Contains(Id(0)), Is.EqualTo(active && actor.Charges > 0));
            Assert.That(result.Deaths.Contains(Id(2)), Is.EqualTo(actor.Role.Ability == AbilityKind.Attack));
            if (actor.Role.Ability == AbilityKind.Investigate) Assert.That(result.Reports.Any(r => r.Recipient == Id(0) && r.Kind == ReportKind.Investigation), Is.True);
            if (actor.Role.Ability == AbilityKind.Protect) Assert.That(result.Reports.Any(r => r.Recipient == Id(0) && r.Kind == ReportKind.ProtectionApplied), Is.True);
            if (actor.Role.Ability == AbilityKind.Forge) Assert.That(result.Reports.Any(r => r.Recipient == Id(0) && r.Kind == ReportKind.ForgeryApplied), Is.True);
            if (actor.Role.Ability == AbilityKind.Watch) Assert.That(result.Reports.Any(r => r.Recipient == Id(0) && r.Kind == ReportKind.NoVisitors), Is.True);
        }

        [TestCaseSource(nameof(RoleIds))]
        public void DecryptorReadsEveryShippedRoleIncludingInfinitumAsTown(string role)
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, "inspector"), Actor(1, role) }, new[] { Order(0, 1) });
            Faction expected = role == "survivor" ? Faction.Town : Role(role).Faction;
            Assert.That(result.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(RoleIds))]
        public void ReportsUseLastNightOrderEvenForPassesSpentChargesAndBluffs(string role)
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var chosen = roles.Single(r => r.Id.Value == role);
            var cast = new[] { chosen }.Concat(roles.Where(r => r.Id != chosen.Id)).ToList();
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial),
                cast.Select((r, i) => new CastMemberSpec("N" + i, r.Id)), roles, TestContent.AuthoredLines(), new ScenarioStrategy());
            RoleId? claim = chosen.CanClaimTruthfully ? chosen.Id : chosen.CanBluff ? new RoleId("inspector") : (RoleId?)null;
            Submit(run, claim.HasValue ? GameCommand.Speak(SpeechIntent.ClaimRole, role: claim.Value) : GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, GameCommand.Nominate(null));
            CharacterId? target = chosen.Ability == AbilityKind.None ? (CharacterId?)null : Id(cast.FindIndex(r => r.Id.Value == "citizen"));
            Submit(run, GameCommand.Night(target)); Submit(run, GameCommand.Continue());
            var record = run.GetNightDiagnostics().Single(r => r.Actor == Id(0));
            Assert.That(record.Action.Role, Is.EqualTo(chosen.Id)); Assert.That(record.Action.Target, Is.EqualTo(target));
            Assert.That(run.GetPlayerView().Charges, Is.EqualTo(chosen.Charges > 0 && target.HasValue ? chosen.Charges - 1 : chosen.Charges));
            if (!claim.HasValue) { Assert.That(run.TrySubmit(GameCommand.ReportLastAction(), out _), Is.False); return; }
            Submit(run, GameCommand.ReportLastAction());
            var published = run.GetPlayerView().Transcript.Last(o => o.Speaker == Id(0) && o.NightReport != null);
            Assert.That(published.NightReport.Night, Is.EqualTo(1)); Assert.That(published.NightReport.Target, Is.EqualTo(target));
            Assert.That(published.NightReport.Role, Is.EqualTo(claim.Value));
            Assert.That(published.Text, Does.Contain("Night 1"));
            if (target.HasValue) Assert.That(published.Text, Does.Contain("N" + target.Value.Value));
            else Assert.That(published.Text, Does.Contain("chose not to use my ability"));
            Assert.That(run.TrySubmit(GameCommand.ReportLastAction(), out _), Is.False);
        }

        [TestCaseSource(nameof(RoleIds))]
        public void ExecutionRevealsAreAttributedAndPassiveNightImmunityDoesNotStopAVote(string role)
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var cast = new[] { Role("citizen"), Role(role) }.Concat(roles.Where(r => r.Id.Value != "citizen" && r.Id.Value != role));
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: 1, votingMode: VotingMode.Trial, lastWords: false),
                cast.Select((r, i) => new CastMemberSpec("N" + i, r.Id)), roles, TestContent.AuthoredLines(),
                new ScenarioStrategy { Nominee = Id(1), Verdict = Verdict.Guilty });
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent)); Submit(run, GameCommand.Nominate(Id(1)));
            Submit(run, GameCommand.Continue()); Submit(run, GameCommand.Judge(Verdict.Guilty));
            Assert.That(run.GetPlayerView().Characters.Single(c => c.Id == Id(1)).Alive, Is.False);
            var reveal = run.GetPlayerView().Transcript.Single(o => o.Cue == GameCue.RoleRevealed && o.Target == Id(1));
            Assert.That(reveal.Target, Is.EqualTo(Id(1)));
            Assert.That(RosterInteractions.PublicRecord(run.GetPlayerView(), Id(1)), Does.Contain(reveal));
        }

        [TestCaseSource(nameof(RoleIds))]
        public void PhysicianProtectionAndInnateDefenseInteractWithAllThreeAttackers(string role)
        {
            foreach (string attacker in new[] { "assassin", "vigilante", "lone-killer" })
            {
                var target = Actor(0, role); var killer = Actor(1, attacker);
                bool friendly = killer.Role.Faction == Faction.Antagonist && target.Role.Faction == Faction.Antagonist;
                if (friendly) continue;
                var result = new NightResolver().Resolve(new[] { target, killer, Actor(2, "physician") }, new[] { Order(1, 0), Order(2, 0) });
                bool killed = !target.Role.NightImmune && killer.Role.Attack > Math.Max(target.Role.Defense, Role("physician").Protection);
                Assert.That(result.Deaths.Contains(Id(0)), Is.EqualTo(killed), role + " attacked by " + attacker);
                Assert.That(result.Reports.Any(r => r.Recipient == Id(1) && r.Kind == ReportKind.AttackFailed), Is.EqualTo(!killed));
            }
        }

        [TestCase("survivor", Faction.Town)]
        [TestCase("citizen", Faction.Town)]
        [TestCase("lone-killer", Faction.Neutral)]
        public void ForgeryIsNightLocalAndDoesNotChangeActualAlignment(string role, Faction appearance)
        {
            var cast = new[] { Actor(0, role), Actor(1, "forger"), Actor(2, "inspector"), Actor(3, "interrupter") };
            var resolver = new NightResolver();
            var framed = resolver.Resolve(cast, new[] { Order(1, 0), Order(2, 0) });
            Assert.That(framed.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(Faction.Antagonist));
            var nextNight = resolver.Resolve(cast, new[] { Order(2, 0) });
            Assert.That(nextNight.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(appearance));
            var blocked = resolver.Resolve(cast, new[] { Order(1, 0), Order(2, 0), Order(3, 1) });
            Assert.That(blocked.Reports.Single(r => r.Kind == ReportKind.Investigation).Alignment, Is.EqualTo(appearance));
            Assert.That(cast[0].Role.Faction, Is.EqualTo(role == "citizen" ? Faction.Town : Faction.Neutral));
        }

        [TestCase(VotingMode.Trial, true)]
        [TestCase(VotingMode.Trial, false)]
        [TestCase(VotingMode.Gnosia, true)]
        [TestCase(VotingMode.Gnosia, false)]
        public void AuthoredFoolOnlyEarnsPersonalWinFromDayExecution(VotingMode mode, bool execute)
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var run = RunFactory.CreateScenario(new RunOptions(42, discussionRounds: mode == VotingMode.Trial ? 1 : 3, maximumDays: 1, votingMode: mode, lastWords: false),
                new[] { "provocateur", "inspector", "physician", "assassin", "citizen" }.Select((r, i) => new CastMemberSpec("N" + i, new RoleId(r))),
                roles, TestContent.AuthoredLines(), new ScenarioStrategy { Nominee = execute ? Id(0) : (CharacterId?)null, Verdict = Verdict.Guilty,
                    Target = k => !execute && k.OwnRole.Ability == AbilityKind.Attack ? Id(0) : (CharacterId?)null });
            Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            while (run.GetPlayerView().Phase == GamePhase.Discussion) Submit(run, GameCommand.Speak(SpeechIntent.StaySilent));
            Submit(run, mode == VotingMode.Trial ? GameCommand.Nominate(null) : GameCommand.Vote(null));
            if (mode == VotingMode.Gnosia) Submit(run, GameCommand.Continue());
            if (execute) { Submit(run, GameCommand.Speak(SpeechIntent.Defend)); Submit(run, GameCommand.Continue()); }
            else Submit(run, GameCommand.Night(null));
            if (run.GetPlayerView().Phase == GamePhase.JesterRevenge)
                Submit(run, GameCommand.Revenge(run.GetPlayerView().RevengeTargets[0]));
            Assert.That(run.GetPlayerView().Phase, Is.EqualTo(GamePhase.Ended));
            Assert.That(run.GetPlayerView().Won, Is.EqualTo(execute));
            Assert.That(run.GetPlayerView().Alive, Is.False);
        }

        [Test]
        public void NeutralEndgamesRespectChargesImmunityAndTerminalPriorityOverDayCap()
        {
            var win = new CapabilityWinCondition();
            var killer = Actor(0, "assassin"); var infinite = Actor(1, "survivor"); var nullifier = Actor(2, "vigilante");
            Assert.That(win.Evaluate(new[] { killer, infinite, nullifier }, 1, 30), Is.EqualTo(OutcomeKind.Ongoing));
            var spent = new NightActor(nullifier.Id, nullifier.Role, 0);
            Assert.That(win.Evaluate(new[] { killer, infinite, spent }, 1, 30), Is.EqualTo(OutcomeKind.InfinitumVictory));
            Assert.That(win.Evaluate(new[] { killer, spent }, 1, 30), Is.EqualTo(OutcomeKind.AntagonistVictory));
            Assert.That(win.Evaluate(new[] { infinite, Actor(3, "lone-killer") }, 30, 30), Is.EqualTo(OutcomeKind.Draw));
            Assert.That(win.Evaluate(new[] { Actor(3, "lone-killer") }, 30, 30), Is.EqualTo(OutcomeKind.LoneKillerVictory));
            Assert.That(win.Evaluate(new[] { infinite, spent }, 30, 30), Is.EqualTo(OutcomeKind.TownVictory));
            Assert.That(win.Evaluate(new[] { infinite, Actor(3, "provocateur") }, 30, 30), Is.EqualTo(OutcomeKind.Draw));
        }

        [Test]
        public void MultipleAttacksAgainstInfinitumProduceOneSurvivalNoticeAndNoCasualty()
        {
            var result = new NightResolver().Resolve(new[] { Actor(0, "survivor"), Actor(1, "assassin"), Actor(2, "lone-killer"), Actor(3, "vigilante") },
                new[] { Order(1, 0), Order(2, 0), Order(3, 0) });
            Assert.That(result.Deaths, Is.Empty);
            Assert.That(result.Reports.Count(r => r.Recipient == Id(0) && r.Kind == ReportKind.Targeted), Is.EqualTo(1));
            Assert.That(result.Reports.Count(r => r.Kind == ReportKind.AttackFailed), Is.EqualTo(3));
            Assert.That(result.SpentCharges, Does.Contain(Id(3)));
        }

        [Test]
        public void NeutralInvestigationIsNotMistakenForTownReassurance()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var model = new BeliefState(Enumerable.Range(0, 5).Select(Id), Personality.None, new SeededRandom(42), Id(0), Role("inspector"),
                new SetupKnowledge(3, 1, 1), roles, Array.Empty<CharacterId>(), new ReasoningSettings());
            float before = model.SuspicionValue(Id(3));
            model.Receive(new NightReport(Id(0), ReportKind.Investigation, Id(3), Faction.Neutral, 1));
            Assert.That(model.SuspicionValue(Id(3)), Is.EqualTo(before), "A Neutral result does not establish safety or hostility.");
            float publicBefore = model.SuspicionValue(Id(4));
            model.Observe(Id(1), GameCommand.PublishReport(new NightActionClaim(1, new RoleId("inspector"), AbilityKind.Investigate, Id(4),
                new[] { new ReportedNightResult(ReportKind.Investigation, Id(4), Faction.Neutral) })), 2);
            Assert.That(model.SuspicionValue(Id(4)), Is.EqualTo(publicBefore));
            Assert.That(model.KnownFaction(Id(3)), Is.Null);
        }
    }
}
