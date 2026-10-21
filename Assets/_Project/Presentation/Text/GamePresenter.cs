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
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _input.OptionChosen += Choose;
            try { _view.Render(_controller.Request()); }
            catch { _input.OptionChosen -= Choose; throw; }
        }

        /// <summary>Removes the session's presentation subscription.</summary>
        public void Dispose() => _input.OptionChosen -= Choose;

        private void Choose(string id)
        {
            if (_view.IsDisplaying) return;
            if (!_controller.TryChoose(id, out string error)) { _view.ShowInputError(error); return; }
            _view.Render(_controller.Request());
        }
    }
}
