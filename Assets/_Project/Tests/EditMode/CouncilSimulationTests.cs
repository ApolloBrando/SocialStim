using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Victory;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Two thousand seeded AI-only games with failure, diversity and broad vote-quality bounds.</summary>
    public sealed class CouncilSimulationTests
    {
        [TestCase(AiDifficulty.Easy, 500)]
        [TestCase(AiDifficulty.Normal, 500)]
        [TestCase(AiDifficulty.Hard, 500)]
        [TestCase(AiDifficulty.Brutal, 500)]
        [Category("Simulation")]
        public void SeededBatchFinishesAndProducesABalanceReceipt(AiDifficulty difficulty, int games)
        {
            var options = new CouncilOptions(1, rounds: 3, maximumDays: 15, difficulty: difficulty, sampleBudget: 16);
            var report = CouncilSimulation.Run(options, new SetupConfiguration(9), MissionTestContent.Roles(),
                TestContent.Names(), TestContent.Lines(), games, 1000u + (uint)difficulty * 10000u);
            Console.WriteLine(report.Summary(difficulty, 16));
            foreach (string failure in report.Failures.Take(10)) Console.WriteLine("SIM_FAILURE " + failure);
            Assert.That(report.Failures, Is.Empty);
            Assert.That(report.Outcomes.Values.Sum(), Is.EqualTo(games));
            Assert.That(report.Outcomes.Where(p => p.Key != OutcomeKind.Ongoing && p.Value > 0).Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(report.VoteAccuracy, Is.InRange(0.05, 0.99));
            Assert.That(report.AverageDays, Is.InRange(1, 15));
        }
    }
}
