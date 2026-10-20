using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Evidence provenance, credibility, constraints and public self-exposure.</summary>
    public sealed class SocialEvidenceTests
    {
        private static BeliefState Model(Personality personality = Personality.None) => new BeliefState(
            Enumerable.Range(0, 5).Select(i => new CharacterId(i)), personality, new SeededRandom(42),
            new CharacterId(0), TestContent.Roles()[0], new SetupKnowledge(3, 1, 1), TestContent.Roles(),
            Array.Empty<CharacterId>(), new ReasoningSettings());

        /// <summary>Disagreeing reports remain competing assertions; neither becomes a hidden truth flag.</summary>
        [Test]
        public void ConflictingWitnessesLoseCredibilityWithoutRevealingWhoLied()
        {
            BeliefState model = Model();
            var first = new CharacterId(1); var second = new CharacterId(2); var target = new CharacterId(3);
            int firstTrust = model.Trust(first), secondTrust = model.Trust(second);
            model.Observe(first, GameCommand.Speak(SpeechIntent.ReportInvestigation, target, faction: Faction.Town), 2);
            model.Observe(second, GameCommand.Speak(SpeechIntent.ReportInvestigation, target, faction: Faction.Antagonist), 2);
            Assert.That(model.Trust(first), Is.LessThan(firstTrust));
            Assert.That(model.Trust(second), Is.LessThan(secondTrust));
            Assert.That(model.Evidence.Count(e => e.Kind == EvidenceKind.InvestigationClaim), Is.EqualTo(2));
            Assert.That(model.Factions(target).Town, Is.GreaterThan(0));
            Assert.That(model.Factions(target).Antagonist, Is.GreaterThan(0));
        }

        /// <summary>Repeating a claim is recorded but cannot repeatedly manufacture confidence.</summary>
        [Test]
        public void DuplicateReportDoesNotStackItsInfluence()
        {
            BeliefState model = Model();
            var speaker = new CharacterId(1); var target = new CharacterId(3);
            var report = GameCommand.Speak(SpeechIntent.ReportInvestigation, target, faction: Faction.Antagonist);
            model.Observe(speaker, report, 2);
            int suspicion = model.Suspicion(target), estimate = model.Factions(target).Antagonist;
            model.Observe(speaker, report, 2);
            Assert.That(model.Suspicion(target), Is.EqualTo(suspicion));
            Assert.That(model.Factions(target).Antagonist, Is.EqualTo(estimate));
        }

        /// <summary>A private result may discredit a conflicting report without leaking into public exposure.</summary>
        [Test]
        public void PrivateKnowledgeDoesNotBecomeVisibleSelfExposure()
        {
            BeliefState model = Model();
            var self = new CharacterId(0); var target = new CharacterId(3);
            model.Receive(new NightReport(self, ReportKind.Investigation, target, Faction.Antagonist, 1));
            model.Observe(self, GameCommand.Speak(SpeechIntent.ReportInvestigation, target, faction: Faction.Town), 2);
            Assert.That(model.Exposure, Is.Zero);
            model.Observe(new CharacterId(1), GameCommand.Speak(SpeechIntent.Accuse, self), 2);
            Assert.That(model.Exposure, Is.GreaterThan(0));
            Assert.Throws<ArgumentException>(() => model.Receive(new NightReport(new CharacterId(1), ReportKind.Investigation, target, Faction.Town, 1)));
        }

        /// <summary>A reveal discredits an incorrect report based on appearance, not a forbidden true-role lookup.</summary>
        [Test]
        public void RevealReevaluatesEarlierReporters()
        {
            BeliefState model = Model(); var speaker = new CharacterId(1); var target = new CharacterId(3);
            model.Observe(speaker, GameCommand.Speak(SpeechIntent.ReportInvestigation, target, faction: Faction.Antagonist), 2);
            int before = model.Trust(speaker);
            model.Reveal(target, TestContent.Roles()[1], 3);
            Assert.That(model.Trust(speaker), Is.LessThan(before));
            Assert.That(model.Factions(target).Town, Is.EqualTo(10000));
            Assert.That(model.Roles(target).Single(r => r.Role.Value == "physician").BasisPoints, Is.EqualTo(10000));
        }

        /// <summary>Publicly revealed alignment changes the credibility of earlier judgement stances.</summary>
        [Test]
        public void RevealReevaluatesEarlierVoters()
        {
            BeliefState model = Model(); var voter = new CharacterId(1); var target = new CharacterId(3);
            model.ObserveVote(voter, target, Verdict.Guilty, 2);
            int before = model.Trust(voter);
            model.Reveal(target, TestContent.Roles()[1], 2);
            Assert.That(model.Trust(voter), Is.LessThan(before));
            Assert.That(model.Evidence.Any(e => e.Kind == EvidenceKind.Judgement && e.Actor == voter), Is.True);
        }

        /// <summary>Secret death alignment cannot change the public initial-count model.</summary>
        [Test]
        public void UnrevealedDeathDoesNotRecomputeSecretFactionCounts()
        {
            BeliefState model = Model(); var target = new CharacterId(3);
            int before = model.Factions(new CharacterId(4)).Antagonist;
            model.ObserveDeath(target, 2);
            Assert.That(model.Factions(new CharacterId(4)).Antagonist, Is.EqualTo(before));
        }

        /// <summary>Distributions normalize exactly and preserve impossible-faction constraints.</summary>
        [Test]
        public void EstimatesNormalizeAndRespectKnownSlots()
        {
            BeliefState model = Model();
            model.Reveal(new CharacterId(3), TestContent.Roles()[3], 2);
            foreach (int slot in Enumerable.Range(0, 5))
            {
                var id = new CharacterId(slot); FactionEstimate estimate = model.Factions(id);
                Assert.That(estimate.Town + estimate.Antagonist + estimate.Neutral, Is.EqualTo(10000));
                Assert.That(model.Roles(id).Sum(r => r.BasisPoints), Is.EqualTo(10000));
            }
            Assert.That(model.Factions(new CharacterId(1)).Antagonist, Is.Zero);
            Assert.That(model.KnownFaction(new CharacterId(1)), Is.Null);
            model.Reveal(new CharacterId(4), TestContent.Roles()[4], 2);
            Assert.That(model.KnownFaction(new CharacterId(1)), Is.EqualTo(Faction.Town));
        }

        /// <summary>Gullibility increases uncertain-assertion influence compared with caution.</summary>
        [Test]
        public void TraitsChangeEvidenceResponse()
        {
            BeliefState gullible = Model(Personality.Gullible), cautious = Model(Personality.Cautious);
            var accusation = GameCommand.Speak(SpeechIntent.Accuse, new CharacterId(3));
            gullible.Observe(new CharacterId(1), accusation, 1); cautious.Observe(new CharacterId(1), accusation, 1);
            Assert.That(gullible.Suspicion(new CharacterId(3)), Is.GreaterThan(cautious.Suspicion(new CharacterId(3))));
        }

        /// <summary>Knowledge snapshot history is detached from later observer updates.</summary>
        [Test]
        public void EvidenceSnapshotsDoNotMutateAfterDelivery()
        {
            BeliefState model = Model(); var history = model.Evidence;
            model.Observe(new CharacterId(1), GameCommand.Speak(SpeechIntent.ClaimRole, role: new RoleId("physician")), 1);
            Assert.That(history, Is.Empty);
            Assert.That(model.Evidence.Single().Role, Is.EqualTo(new RoleId("physician")));
        }

        /// <summary>A known unique role cannot remain a candidate for a different participant.</summary>
        [Test]
        public void KnownUniqueRoleIsExcludedFromOtherCandidates()
        {
            RoleSpec inspector = TestContent.Roles()[0];
            var unique = new RoleSpec(inspector.Id, inspector.Name, inspector.Faction, inspector.Category, inspector.Ability,
                inspector.Attack, inspector.Defense, inspector.Protection, inspector.Charges, inspector.Appearance, unique: true);
            var catalog = TestContent.Roles().Select(r => r.Id == unique.Id ? unique : r).ToList().AsReadOnly();
            var model = new BeliefState(Enumerable.Range(0, 5).Select(i => new CharacterId(i)), Personality.None, new SeededRandom(10),
                new CharacterId(0), unique, new SetupKnowledge(3, 1, 1), catalog, Array.Empty<CharacterId>(), new ReasoningSettings());
            Assert.That(model.Roles(new CharacterId(1)).Single(r => r.Role == unique.Id).BasisPoints, Is.Zero);
            Assert.That(model.Roles(new CharacterId(1)).Sum(r => r.BasisPoints), Is.EqualTo(10000));
        }
    }
}
