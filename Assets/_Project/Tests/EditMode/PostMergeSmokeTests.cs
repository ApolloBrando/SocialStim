using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Sessions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Exercises the full surviving catalogue and authored dialogue through real input requests.</summary>
    public sealed class PostMergeSmokeTests
    {
        [TestCase(VotingMode.Gnosia)]
        [TestCase(VotingMode.Trial)]
        public void EveryPlayerRoleHasLegalTerminatingSeededRuns(VotingMode mode)
        {
            var dialogue = TestContent.AuthoredLines();
            var roles = AdvancedRoleTests.Roles();
            int completed = 0;
            foreach (RoleSpec role in roles)
                for (uint seed = 1; seed <= 16; seed++)
                {
                    string[] play = Play(mode, role.Id, seed, roles, dialogue);
                    if (seed == 1) Assert.That(Play(mode, role.Id, seed, roles, dialogue), Is.EqualTo(play));
                    completed++;
                }
            TestContext.WriteLine("Completed " + completed + " seeded full-catalogue games in " + mode + ".");
        }

        private static string[] Play(VotingMode mode, RoleId role, uint seed,
            System.Collections.Generic.IReadOnlyList<RoleSpec> roles,
            System.Collections.Generic.IReadOnlyList<SocialStim.Rules.Dialogue.DialogueLine> dialogue)
        {
            var run = RunFactory.Create(new RunOptions(seed, role, discussionRounds: 3,
                    maximumDays: 6, votingMode: mode), roles, TestContent.Names(), dialogue,
                setup: RoleList.ForPopulation(9));
            var controller = new GameController(run, roles);
            // This input policy sees only the same public choices as the front end.
            // Its separate stream cannot consume authoritative game randomness.
            var choices = new SeededRandom(seed ^ 0x92482u);
            for (int turn = 0; turn < 1500; turn++)
            {
                var request = controller.Request();
                if (request.View.Phase == GamePhase.Ended)
                {
                    Assert.That(request.Options, Is.Empty);
                    Assert.That(request.View.Transcript.All(line => !line.Text.Contains("{") && !line.Text.Contains("}")), Is.True);
                    return request.View.Transcript.Select(line => line.Text).ToArray();
                }
                Assert.That(request.Options.Count, Is.GreaterThan(0), role + "/" + seed + "/" + request.View.Phase);
                int revision = request.View.Revision;
                string id = request.Options[choices.Next(request.Options.Count)].Id;
                Assert.That(controller.TryChoose(id, out string error), Is.True, error);
                Assert.That(run.GetPlayerView().Revision, Is.EqualTo(revision + 1));
                Assert.That(controller.TryChoose(id, out _), Is.False, "Completed-turn choices must expire.");
            }
            Assert.Fail("Stalled game: " + mode + "/" + role + "/" + seed);
            return Array.Empty<string>();
        }
    }
}
