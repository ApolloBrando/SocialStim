using System;

namespace SocialStim.Application.Input
{
    /// <summary>Device-independent option selection supplied by any front end.</summary>
    public interface IPlayerInputProvider
    {
        /// <summary>Emits the selected opaque application option ID.</summary>
        event Action<string> OptionChosen;
    }
}
