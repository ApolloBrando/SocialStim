using System.Linq;
using NUnit.Framework;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Team planner integration and audience routing through the real session.</summary>
    public sealed class TeamSessionTests
    {
        private static RunSession Create(params string[] ids) => RunFactory.CreateScenario(TestContent.Text, new RunOptions(19, discussionRounds: 1, votingMode: VotingMode.Trial),
            ids.Select((id, index) => new CastMemberSpec("Person" + index, new RoleId(id))), TestContent.Roles(), TestContent.Lines(),
            new ScenarioStrategy { Target = knowledge => knowledge.OwnRole.Ability == AbilityKind.Attack ? new CharacterId(2) : (CharacterId?)null });

        private static void ReachAndResolveNight(RunSession run)
        {
            Assert.That(run.TrySubmit(GameCommand.Speak(SpeechIntent.StaySilent), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Nominate(null), out _), Is.True);
            Assert.That(run.TrySubmit(GameCommand.Night(null), out _), Is.True);
        }

        /// <summary>Two NPC killers produce one death and cannot expose their plan to a Town player.</summary>
        [Test]
        public void CoordinatedNpcTeamKillsOnceWithoutLeakingPlan()
        {
            RunSession run = Create("inspector", "assassin", "physician", "assassin", "interrupter", "survivor");
            ReachAndResolveNight(run);
            var view = run.GetPlayerView();
            Assert.That(view.Characters.Count(c => !c.Alive), Is.EqualTo(1));
            Assert.That(view.Characters.Single(c => !c.Alive).Id, Is.EqualTo(new CharacterId(2)));
            Assert.That(view.Transcript.Any(e => e.Text.StartsWith("Team:", System.StringComparison.Ordinal)), Is.False);
        }

        /// <summary>A player killer's pass cancels the team's kill rather than allowing an NPC to override it.</summary>
        [Test]
        public void PlayerPassControlsTeamAndPlanIsVisibleToThem()
        {
            RunSession run = Create("assassin", "assassin", "physician", "inspector", "interrupter", "survivor");
            ReachAndResolveNight(run);
            var view = run.GetPlayerView();
            Assert.That(view.Characters.All(c => c.Alive), Is.True);
            Assert.That(view.Transcript.Any(e => e.Text.StartsWith("Team:", System.StringComparison.Ordinal) && e.Text.Contains("pass tonight")), Is.True);
        }
    }
}
