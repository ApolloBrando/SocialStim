using SocialStim.Application.Input;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Single entry point for all run text and semantic cues.</summary>
    public interface IGameView
    {
        /// <summary>Displays scoped state and its current input request.</summary>
        void Render(PlayerInputRequest request);
        /// <summary>Reports a rejected input without changing game rules.</summary>
        void ShowInputError(string message);
    }
}
