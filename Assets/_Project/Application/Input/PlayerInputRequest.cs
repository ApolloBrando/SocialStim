using System.Collections.Generic;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.Input
{
    /// <summary>Current player view and valid semantic choices, independent of device/UI.</summary>
    public sealed class PlayerInputRequest
    {
        internal PlayerInputRequest(PlayerView view, IReadOnlyList<PlayerOption> options) { View = view; Options = options; }
        /// <summary>Already scoped state for display.</summary>
        public PlayerView View { get; }
        /// <summary>Legal choices for this request.</summary>
        public IReadOnlyList<PlayerOption> Options { get; }
    }
}
