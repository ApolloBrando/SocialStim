using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Complete seeded runs through the same application input port used by Unity.</summary>
    public sealed class DeterministicRunTests
    {
        /// <summary>Town and antagonist runs terminate with reproducible observations.</summary>
        [TestCase("inspector")]
        [TestCase("assassin")]
        [TestCase("survivor")]
        public void SeededRunIsRepeatableAndTerminates(string role)
        {
            string[] first = Play(147u, role);
            Assert.That(Play(147u, role), Is.EqualTo(first));
            Assert.That(first.Last(), Does.Contain("Run ended:"));
        }

        /// <summary>A reproducible batch exercises both player factions without stuck input requests.</summary>
        [Test]
        public void FiftySeedsPerSideReachAnOutcome()
        {
            foreach (string role in new[] { "inspector", "assassin" })
                for (uint seed = 1; seed <= 50; seed++)
                    Assert.That(Play(seed, role).Last(), Does.Contain("Run ended:"), $"Seed {seed}, {role}");
        }

        /// <summary>Every initial role can be selected without violating the seven-character list.</summary>
        [Test]
        public void ChosenRolesAndNamesAreDeterministic()
        {
            foreach (RoleSpec role in TestContent.Roles())
            {
                var run = RunFactory.Create(new RunOptions(100, role.Id, votingMode: VotingMode.Trial), TestContent.Roles(), TestContent.Names(), TestContent.Lines());
                Assert.That(run.GetPlayerView().OwnRole.Id, Is.EqualTo(role.Id));
                Assert.That(run.GetPlayerView().Characters.Count, Is.EqualTo(7));
                Assert.That(run.GetPlayerView().Characters.Select(c => c.Name).Distinct().Count(), Is.EqualTo(7));
            }
            Assert.Throws<ArgumentException>(() => RunFactory.Create(new RunOptions(100, new RoleId("unknown"), votingMode: VotingMode.Trial), TestContent.Roles(), TestContent.Names(), TestContent.Lines()));
        }

        private static string[] Play(uint seed, string role)
        {
            var run = RunFactory.Create(new RunOptions(seed, new RoleId(role), votingMode: VotingMode.Trial), TestContent.Roles(), TestContent.Names(), TestContent.Lines());
            var controller = new GameController(run, TestContent.Roles());
            for (int step = 0; step < 500; step++)
            {
                var request = controller.Request();
                if (request.View.Phase == GamePhase.Ended) return request.View.Transcript.Select(o => o.Text).ToArray();
                Assert.That(request.Options.Count, Is.GreaterThan(0), $"No input at {request.View.Phase}");
                // A stable policy, not an oracle: first available choice except choosing an actual night target.
                var option = request.View.Phase == GamePhase.NightSelection && request.Options.Count > 1
                    ? request.Options[1] : request.Options[0];
                Assert.That(controller.TryChoose(option.Id, out string error), Is.True, error);
            }
            Assert.Fail($"Run did not terminate: {seed}, {role}.");
            return Array.Empty<string>();
        }
    }
}
