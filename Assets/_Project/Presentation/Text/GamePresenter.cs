using System;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;

namespace SocialStim.Presentation.Text
{
    /// <summary>Routes every run display/input through the same front-end ports.</summary>
    public sealed class GamePresenter : IDisposable
    {
        private readonly GameController _controller;
        private readonly IGameView _view;
        private readonly IPlayerInputProvider _input;

        /// <summary>Subscribes input and displays the initial scoped state.</summary>
        public GamePresenter(GameController controller, IGameView view, IPlayerInputProvider input)
        {
            _controller = controller; _view = view; _input = input;
            _input.OptionChosen += Choose;
            _view.Render(_controller.Request());
        }

        /// <summary>Removes the session's presentation subscription.</summary>
        public void Dispose() => _input.OptionChosen -= Choose;

        private void Choose(string id)
        {
            if (!_controller.TryChoose(id, out string error)) { _view.ShowInputError(error); return; }
            _view.Render(_controller.Request());
        }
    }
}
