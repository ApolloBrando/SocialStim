using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Metamorphic tests: changing secrets cannot alter an uninformed observer's initial estimates.</summary>
    public sealed class InformationBoundaryTests
    {
        /// <summary>Swapping hidden assignments while preserving the public role list leaves the Town player's knowledge unchanged.</summary>
        [Test]
        public void HiddenRolePermutationDoesNotChangeInitialKnowledge()
        {
            var roles = TestContent.Roles();
            Func<string[], PlayerView> build = ids => RunFactory.CreateScenario(TestContent.Text, new RunOptions(31, votingMode: VotingMode.Trial),
                ids.Select((id, index) => new CastMemberSpec("Person" + index, new RoleId(id))), roles, TestContent.Lines(), new ScenarioStrategy()).GetPlayerView();
            PlayerView first = build(new[] { "inspector", "physician", "interrupter", "assassin", "survivor" });
            PlayerView second = build(new[] { "inspector", "assassin", "survivor", "physician", "interrupter" });
            foreach (int slot in Enumerable.Range(0, 5))
            {
                Assert.That(first.Characters[slot].FactionEstimate.Antagonist, Is.EqualTo(second.Characters[slot].FactionEstimate.Antagonist));
                Assert.That(first.Characters[slot].RoleEstimates.Select(r => r.BasisPoints), Is.EqualTo(second.Characters[slot].RoleEstimates.Select(r => r.BasisPoints)));
                Assert.That(first.Characters[slot].RevealedRole, Is.Null);
            }
        }

        /// <summary>A second production strategy can replace the richer default without changing observation permissions.</summary>
        [Test]
        public void BasicStrategyRemainsSwappable()
        {
            var run = RunFactory.Create(TestContent.Text, new RunOptions(55, votingMode: VotingMode.Trial), TestContent.Roles(), TestContent.Names(), TestContent.Lines(), new BasicNpcStrategy());
            Assert.That(run.GetPlayerView().Characters.Count, Is.EqualTo(7));
            Assert.That(run.GetPlayerView().Characters.All(c => !c.RevealedRole.HasValue), Is.True);
        }
    }
}
