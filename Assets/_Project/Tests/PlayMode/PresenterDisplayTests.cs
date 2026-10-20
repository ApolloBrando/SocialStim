using System;
using System.Linq;
using NUnit.Framework;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;
using SocialStim.Presentation.Text;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Presenter input/acknowledgement integration; also compiled by the headless suite.</summary>
    public sealed class PresenterDisplayTests
    {
        [Test]
        public void InputWaitsForDisplayWithoutAdvancingRules()
        {
            var town = new RoleSpec(new RoleId("inspector"), "Inspector", Faction.Town, RoleCategory.TownInvestigative, AbilityKind.Investigate, 0, 0, 0, -1, Faction.Town);
            var killer = new RoleSpec(new RoleId("assassin"), "Assassin", Faction.Antagonist, RoleCategory.AntagonistKilling, AbilityKind.Attack, 1, 0, 0, -1, Faction.Antagonist);
            var roles = new[] { town, killer };
            var lines = Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>().Select(intent => new DialogueLine(intent.ToString(), intent, "PLACEHOLDER: line")).ToList();
            var run = RunFactory.CreateScenario(new RunOptions(123, votingMode: VotingMode.Trial), new[] {
                new CastMemberSpec("Player", town.Id), new CastMemberSpec("Town", town.Id), new CastMemberSpec("Killer", killer.Id),
            }, roles, lines);
            var adapter = new FakeDisplay();
            using (var presenter = new GamePresenter(new GameController(run, roles), adapter, adapter))
            {
                string option = adapter.Request.Options[0].Id;
                adapter.Choose(option); Assert.That(run.GetPlayerView().Revision, Is.Zero);
                adapter.IsDisplaying = false;
                adapter.Choose(option); Assert.That(run.GetPlayerView().Revision, Is.EqualTo(1));
            }
            adapter.IsDisplaying = false;
            adapter.Choose(adapter.Request.Options[0].Id);
            Assert.That(run.GetPlayerView().Revision, Is.EqualTo(1), "Disposed presenter must unsubscribe input.");
        }
        private sealed class FakeDisplay : IGameView, IPlayerInputProvider
        {
            public bool IsDisplaying { get; set; } = true;
            public PlayerInputRequest Request { get; private set; }
            public event Action<string> OptionChosen;
            public void Render(PlayerInputRequest request) { Request = request; }
            public void ShowInputError(string message) { Assert.Fail(message); }
            public void Choose(string option) => OptionChosen?.Invoke(option);
        }
    }
}
