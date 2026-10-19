using SocialStim.Rules.Commands;

namespace SocialStim.Application.Input
{
    /// <summary>One portable action option; IDs are scoped to a particular input revision.</summary>
    public sealed class PlayerOption
    {
        internal PlayerOption(string id, string label, string interpretation, GameCommand command)
        { Id = id; Label = label; Interpretation = interpretation; Command = command; }
        /// <summary>Opaque revision-scoped selection key.</summary>
        public string Id { get; }
        /// <summary>Plain-text choice label.</summary>
        public string Label { get; }
        /// <summary>Qualitative social meaning, not a promise of exact NPC scores.</summary>
        public string Interpretation { get; }
        internal GameCommand Command { get; }
    }
}
