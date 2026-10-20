using System;
using System.Linq;
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

namespace SocialStim.Tests.EditMode
{
    /// <summary>Player-slot neutrality, bounded discussion and evidence-based social decisions.</summary>
    public sealed class AiRefinementTests
    {
        [Test]
        public void FirstDayPressureSample()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var dialogue = TestContent.AuthoredLines();
            int actions = 0, pressure = 0, playerPressure = 0, maximum = 0;
            for (uint seed = 1; seed <= 200; seed++)
            {
                var run = RunFactory.Create(new RunOptions(seed, new RoleId("citizen")), roles, TestContent.Names(), dialogue,
                    setup: RoleList.ForPopulation(7));
                var controller = new GameController(run, roles);
                int turns = 0;
                while (run.GetPlayerView().Phase == GamePhase.Discussion && turns < 100)
                {
                    var request = controller.Request();
                    var wait = request.Options.First(o => o.Label == "Wait");
                    Assert.That(controller.TryChoose(wait.Id, out string error), Is.True, error);
                    turns++;
                }
                actions += turns; maximum = Math.Max(maximum, turns);
                Assert.That(turns, Is.EqualTo(5), "Every accepted action, including a counterclaim pass, consumes a round.");
                foreach (var speech in run.GetPlayerView().Transcript.Where(e => e.Speaker.HasValue && e.Speaker.Value.Value != 0
                    && (e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Deflect || e.Intent == SpeechIntent.CallForTrial)))
                { pressure++; if (speech.Target == new CharacterId(0)) playerPressure++; }
            }
            TestContext.WriteLine("First-day sample: 200 seeds; actions=" + actions + "; max actions=" + maximum
                + "; NPC accusations=" + pressure + "; player accusations=" + playerPressure);
        }

        private static CharacterId Id(int value) => new CharacterId(value);
        private static BeliefState Model(uint seed = 42, int observer = 0) => new BeliefState(
            Enumerable.Range(0, 5).Select(Id), Personality.None, new SeededRandom(seed), Id(observer),
            TestContent.Roles()[0], new SetupKnowledge(3, 1, 1), TestContent.Roles(),
            Array.Empty<CharacterId>(), new ReasoningSettings());

        [Test]
        public void InitialFeelingsAreIndependentSeededFractionalValuesAcrossTheWholeRange()
        {
            float minimum = 10, maximum = 0; bool fractional = false, independent = false;
            for (uint seed = 1; seed <= 100; seed++)
            {
                var first = Model(seed); var replay = Model(seed);
                Assert.That(first.SuspicionValue(Id(0)), Is.Zero);
                Assert.That(first.TrustValue(Id(0)), Is.EqualTo(10));
                for (int target = 1; target < 5; target++)
                {
                    float suspicion = first.SuspicionValue(Id(target)), trust = first.TrustValue(Id(target));
                    Assert.That(suspicion, Is.InRange(0f, 10f)); Assert.That(trust, Is.InRange(0f, 10f));
                    Assert.That(replay.SuspicionValue(Id(target)), Is.EqualTo(suspicion));
                    Assert.That(replay.TrustValue(Id(target)), Is.EqualTo(trust));
                    minimum = Math.Min(minimum, Math.Min(suspicion, trust)); maximum = Math.Max(maximum, Math.Max(suspicion, trust));
                    fractional |= Math.Abs(suspicion - (float)Math.Round(suspicion)) > 0.01f;
                    independent |= Math.Abs(suspicion + trust - 10) > 1;
                }
            }
            Assert.That(minimum, Is.LessThan(0.1f)); Assert.That(maximum, Is.GreaterThan(9.9f));
            Assert.That(fractional && independent, Is.True);
        }

        [Test]
        public void ObserverMindsAreSeparateAndReportsDoNotSelfPersuadeTheirAuthor()
        {
            var author = Model(); var listener = Model(43, 1);
            float before = author.SuspicionValue(Id(3)), other = listener.SuspicionValue(Id(3));
            var lie = GameCommand.Speak(SpeechIntent.ReportInvestigation, Id(3), faction: Faction.Antagonist);
            author.Observe(Id(0), lie, 2);
            Assert.That(author.SuspicionValue(Id(3)), Is.EqualTo(before));
            Assert.That(listener.SuspicionValue(Id(3)), Is.EqualTo(other));
            listener.Observe(Id(0), lie, 2);
            Assert.That(listener.SuspicionValue(Id(3)), Is.GreaterThan(other));
        }

        [Test]
        public void RumorAndEchoSpamCannotManufactureUnlimitedSuspicionOrTrust()
        {
            var model = Model(); float initial = model.SuspicionValue(Id(3));
            model.Observe(Id(1), GameCommand.Speak(SpeechIntent.Accuse, Id(3)), 1);
            model.Observe(Id(2), GameCommand.Speak(SpeechIntent.Agree, Id(1)), 1);
            float suspicion = model.SuspicionValue(Id(3)), trust = model.TrustValue(Id(1));
            for (int repeat = 0; repeat < 100; repeat++)
            {
                model.Observe(Id(1), GameCommand.Speak(SpeechIntent.Accuse, Id(3)), 1);
                model.Observe(Id(2), GameCommand.Speak(SpeechIntent.Agree, Id(1)), 1);
            }
            Assert.That(model.SuspicionValue(Id(3)), Is.EqualTo(suspicion));
            Assert.That(model.TrustValue(Id(1)), Is.EqualTo(trust));
            Assert.That(suspicion - initial, Is.LessThanOrEqualTo(1.2f));
        }

        [Test]
        public void DuplicatePrivateDeliveryAndRevealAreIdempotent()
        {
            var model = Model(); var report = new NightReport(Id(0), ReportKind.Investigation, Id(3), Faction.Antagonist, 1);
            model.Receive(report); int count = model.Evidence.Count; float suspicion = model.SuspicionValue(Id(3));
            model.Receive(report);
            Assert.That(model.Evidence.Count, Is.EqualTo(count)); Assert.That(model.SuspicionValue(Id(3)), Is.EqualTo(suspicion));
            model.Observe(Id(1), GameCommand.Speak(SpeechIntent.Accuse, Id(3)), 2);
            model.Reveal(Id(3), TestContent.Roles().First(r => r.Faction == Faction.Antagonist), 2);
            float trust = model.TrustValue(Id(1)); count = model.Evidence.Count;
            model.Reveal(Id(3), TestContent.Roles().First(r => r.Faction == Faction.Antagonist), 2);
            Assert.That(model.TrustValue(Id(1)), Is.EqualTo(trust)); Assert.That(model.Evidence.Count, Is.EqualTo(count));
        }

        [Test]
        public void JointBeliefsConserveDisclosedFactionCountsAfterUnevenEvidenceAndReveals()
        {
            var model = Model();
            model.Observe(Id(1), GameCommand.Speak(SpeechIntent.ReportInvestigation, Id(3), faction: Faction.Antagonist), 2);
            model.Observe(Id(2), GameCommand.Speak(SpeechIntent.Accuse, Id(4)), 2);
            for (int stage = 0; stage < 2; stage++)
            {
                var values = Enumerable.Range(0, 5).Select(i => model.Factions(Id(i))).ToArray();
                Assert.That(values.Sum(v => v.Town), Is.InRange(29995, 30005));
                Assert.That(values.Sum(v => v.Antagonist), Is.InRange(9995, 10005));
                Assert.That(values.Sum(v => v.Neutral), Is.InRange(9995, 10005));
                Assert.That(values.All(v => v.Town + v.Antagonist + v.Neutral == 10000), Is.True);
                if (stage == 0) model.Reveal(Id(3), TestContent.Roles().First(r => r.Faction == Faction.Antagonist), 2);
            }
        }

        [Test]
        public void EquivalentTargetsReceiveSeededTieBreaksWithoutFavoringPlayerSlot()
        {
            var totals = new int[7];
            var view = new KnowledgeView(Id(6), TestContent.Roles()[0], Personality.None, -1, 1,
                Enumerable.Range(0, 7).Select(i => new KnownCharacter(Id(i), "P" + i, true, 50, trust: 50,
                    factionEstimate: new FactionEstimate(6000, 3000, 1000))), Array.Empty<NightReport>());
            for (uint seed = 1; seed <= 600; seed++) totals[SocialAssessment.Suspect(view, new SeededRandom(seed)).Id.Value]++;
            for (int i = 0; i < 6; i++) Assert.That(totals[i], Is.InRange(65, 135), "Equivalent seat " + i);
            Assert.That(totals[6], Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TownKillerCanActOnStrongPermittedEvidence(bool basic)
        {
            var role = TestContent.Role("guard", Faction.Town, RoleCategory.TownKilling, AbilityKind.Attack, attack: 1, charges: 2);
            var view = new KnowledgeView(Id(0), role, Personality.None, 2, 2,
                new[] { new KnownCharacter(Id(0), "Self", true, 0), new KnownCharacter(Id(1), "Suspect", true, 90) },
                new[] { new NightReport(Id(0), ReportKind.Investigation, Id(1), Faction.Antagonist, 1) });
            INpcStrategy strategy = basic ? (INpcStrategy)new BasicNpcStrategy() : new SocialNpcStrategy();
            Assert.That(strategy.NightTarget(view, new SeededRandom(4)), Is.EqualTo(Id(1)));
            Assert.That(strategy.Judge(view, Id(1), new SeededRandom(4)), Is.EqualTo(Verdict.Guilty));
        }

        [Test]
        public void ARandomGrudgeCannotBecomeATownGuiltyVerdict()
        {
            var view = new KnowledgeView(Id(0), TestContent.Roles()[0], Personality.Brave, -1, 1,
                new[] { new KnownCharacter(Id(0), "Self", true, 0), new KnownCharacter(Id(1), "Disliked", true, 100) }, Array.Empty<NightReport>());
            Assert.That(new SocialNpcStrategy().Judge(view, Id(1), new SeededRandom(1)), Is.EqualTo(Verdict.Abstain));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestrictedRevotesConsiderLegalCandidatesWithoutHidingOtherPublicPeople(bool basic)
        {
            var view = new KnowledgeView(Id(0), TestContent.Roles()[0], Personality.None, -1, 1,
                new[] { new KnownCharacter(Id(0), "Self", true, 0), new KnownCharacter(Id(1), "Excluded", true, 100),
                    new KnownCharacter(Id(2), "Tied", true, 90) }, Array.Empty<NightReport>(), nominationCandidates: new[] { Id(2) });
            INpcStrategy strategy = basic ? (INpcStrategy)new BasicNpcStrategy() : new SocialNpcStrategy();
            Assert.That(strategy.Nominate(view, new SeededRandom(1)), Is.EqualTo(Id(2)));
            Assert.That(view.Characters.Count, Is.EqualTo(3));
        }

        [Test]
        public void CustomFactionSharesAreRespectedAndIndependentKillerMustOutlastOtherVotableNeutrals()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles();
            var assigned = RoleList.ForPopulation(7, 100, 1, 0).Resolve(roles, null, new SeededRandom(4));
            Assert.That(assigned.Count(r => r.Faction == Faction.Antagonist), Is.EqualTo(1));
            var condition = new SocialStim.Rules.Victory.CapabilityWinCondition();
            var actors = new[] { "lone-killer", "survivor", "provocateur" }.Select((id, i) => new NightActor(Id(i), roles.First(r => r.Id.Value == id), -1)).ToArray();
            Assert.That(condition.Evaluate(actors, 1, 30), Is.EqualTo(SocialStim.Rules.Victory.OutcomeKind.Ongoing));
            Assert.That(condition.Evaluate(actors.Take(2).ToArray(), 1, 30), Is.EqualTo(SocialStim.Rules.Victory.OutcomeKind.Draw));
        }

        [Test]
        public void WatcherCanPrivatelyNoticeAnImpossibleClaimedInvestigationVisit()
        {
            var model = Model(); float trust = model.TrustValue(Id(1));
            model.Receive(new NightReport(Id(0), ReportKind.VisitorSeen, Id(1), night: 1, subject: Id(2)));
            model.Observe(Id(1), GameCommand.Speak(SpeechIntent.ReportInvestigation, Id(3), faction: Faction.Town), 2);
            Assert.That(model.TrustValue(Id(1)), Is.LessThan(trust));
            Assert.That(model.Evidence.Any(e => e.Kind == EvidenceKind.Contradiction && e.Actor == Id(1)), Is.True);
            Assert.That(model.Exposure, Is.Zero, "A private realization must not invent public attention.");
        }

        [Test]
        public void FabricationRequiresPublicInvestigativeCoverAndOnlyOneResultPerNight()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles(); var evil = roles.First(r => r.Id.Value == "assassin");
            Func<RoleId?, EvidenceRecord[], KnowledgeView> view = (claim, evidence) => new KnowledgeView(Id(0), evil, Personality.Smug, -1, 2,
                new[] { new KnownCharacter(Id(0), "Self", true, 0, claim: claim), new KnownCharacter(Id(1), "Target", true, 50),
                    new KnownCharacter(Id(2), "Ally", true, 50, teammate: true) }, Array.Empty<NightReport>(), evidence, roleCatalog: roles);
            Assert.That(ClaimPolicy.FabricatedInvestigation(view(null, null), new SeededRandom(1)), Is.Null);
            Assert.That(ClaimPolicy.FabricatedInvestigation(view(new RoleId("physician"), null), new SeededRandom(1)), Is.Null);
            for (uint seed = 1; seed <= 50; seed++)
            {
                var story = ClaimPolicy.FabricatedInvestigation(view(new RoleId("inspector"), null), new SeededRandom(seed));
                Assert.That(story, Is.Not.Null);
                if (story.Target == Id(2)) Assert.That(story.ReportedFaction, Is.EqualTo(Faction.Town));
                var prior = new EvidenceRecord(0, 2, EvidenceKind.InvestigationClaim, Id(0), story.Target,
                    SpeechIntent.ReportInvestigation, alignment: story.ReportedFaction, night: 1);
                Assert.That(ClaimPolicy.FabricatedInvestigation(view(new RoleId("inspector"), new[] { prior }), new SeededRandom(seed)), Is.Null);
            }
        }

        [Test]
        public void AntagonistCanPressureKnownTownWithoutBelievingTheirOwnDeception()
        {
            var role = TestContent.Roles().First(r => r.Faction == Faction.Antagonist);
            var view = new KnowledgeView(Id(0), role, Personality.None, -1, 2,
                new[] { new KnownCharacter(Id(0), "Self", true, 0),
                    new KnownCharacter(Id(1), "KnownTown", true, 0, knownFaction: Faction.Town),
                    new KnownCharacter(Id(2), "Ally", true, 100, teammate: true) }, Array.Empty<NightReport>());
            foreach (INpcStrategy strategy in new INpcStrategy[] { new SocialNpcStrategy(), new BasicNpcStrategy() })
            {
                Assert.That(strategy.Nominate(view, new SeededRandom(1)), Is.EqualTo(Id(1)));
                Assert.That(strategy.Judge(view, Id(1), new SeededRandom(1)), Is.EqualTo(Verdict.Guilty));
                Assert.That(strategy.Judge(view, Id(2), new SeededRandom(1)), Is.EqualTo(Verdict.Innocent));
            }
        }

        [Test]
        public void ElevenHundredScopedPolicyGamesTerminateWithoutRejectedCommands()
        {
            var roles = DiscussionRevisionTests.AuthoredRoles(); var dialogue = TestContent.AuthoredLines();
            var outcomes = new System.Collections.Generic.Dictionary<SocialStim.Rules.Victory.OutcomeKind, int>();
            int games = 0, days = 0, maximumInputs = 0;
            foreach (var role in roles)
                for (uint seed = 1; seed <= 100; seed++)
                {
                    var run = RunFactory.Create(new RunOptions(seed, role.Id, maximumDays: 20), roles, TestContent.Names(), dialogue, setup: RoleList.ForPopulation(7));
                    var strategy = new SocialNpcStrategy(); var random = new SeededRandom(seed ^ 0xA173u);
                    int inputs = 0;
                    while (run.GetPlayerView().Phase != GamePhase.Ended && inputs < 1200)
                    {
                        var view = run.GetPlayerView(); var knowledge = run.PlayerKnowledgeForDiagnostics(); GameCommand command;
                        if (view.Phase == GamePhase.JesterRevenge) command = GameCommand.Revenge(view.RevengeTargets.First());
                        else if (!view.Alive) command = GameCommand.Continue();
                        else if (view.CounterClaimRole.HasValue)
                            command = random.Next(100) < 40 ? GameCommand.Speak(SpeechIntent.CounterClaim, role: view.CounterClaimRole) : GameCommand.Speak(SpeechIntent.StaySilent);
                        else switch (view.Phase)
                        {
                            case GamePhase.Discussion:
                                command = view.RespondTo.HasValue ? GameCommand.Speak(SpeechIntent.Defend) : strategy.Speak(knowledge, random); break;
                            case GamePhase.Voting: command = GameCommand.Vote(strategy.Nominate(knowledge, random)); break;
                            case GamePhase.Nomination: command = GameCommand.Nominate(strategy.Nominate(knowledge, random)); break;
                            case GamePhase.Defense: command = view.Defendant == view.PlayerId ? GameCommand.Speak(SpeechIntent.Defend) : GameCommand.Continue(); break;
                            case GamePhase.Judgement: command = view.Defendant == view.PlayerId ? GameCommand.Continue() : GameCommand.Judge(strategy.Judge(knowledge, view.Defendant.Value, random)); break;
                            case GamePhase.NightSelection: command = GameCommand.Night(strategy.NightTarget(knowledge, random)); break;
                            case GamePhase.LastWords: command = view.Defendant == view.PlayerId ? GameCommand.Speak(SpeechIntent.LastWords) : GameCommand.Continue(); break;
                            default: command = GameCommand.Continue(); break;
                        }
                        Assert.That(run.TrySubmit(command, out string error), Is.True, role.Id + "/" + seed + "/" + view.Phase + ": " + error);
                        Assert.That(run.GetPlayerView().Revision, Is.EqualTo(view.Revision + 1)); inputs++;
                    }
                    var final = run.GetPlayerView(); Assert.That(final.Phase, Is.EqualTo(GamePhase.Ended), role.Id + "/" + seed);
                    if (!outcomes.ContainsKey(final.Outcome)) outcomes.Add(final.Outcome, 0);
                    outcomes[final.Outcome]++; games++; days += final.Day; maximumInputs = Math.Max(maximumInputs, inputs);
                }
            TestContext.WriteLine("Scoped policy benchmark: games=" + games + "; mean days=" + ((float)days / games).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                + "; max inputs=" + maximumInputs + "; " + string.Join(", ", outcomes.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value)));
        }
    }
}
