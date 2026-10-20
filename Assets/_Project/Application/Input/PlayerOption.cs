using SocialStim.Rules.Commands;
using System.Collections.Generic;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Application.Input
{
    /// <summary>One portable action option; IDs are scoped to a particular input revision.</summary>
    public sealed class PlayerOption
    {
        internal PlayerOption(string id, string label, string interpretation, GameCommand command, IReadOnlyList<DialogueSelection> phrasings = null)
        { Phrasings = phrasings ?? new DialogueSelection[0]; Id = id; Label = label; Interpretation = interpretation; Command = command; }
        /// <summary>Actual eligible authored words; navigation never consumes dialogue RNG.</summary>
        public IReadOnlyList<DialogueSelection> Phrasings { get; }
        /// <summary>Underlying public speech intent, when this is a speech action.</summary>
        public SpeechIntent? Intent => Command.Kind == CommandKind.Speak ? Command.Intent : (SpeechIntent?)null;
        /// <summary>Public target, if any.</summary>
        public SocialStim.Rules.Cast.CharacterId? Target => Command.Target;
        /// <summary>Opaque revision-scoped selection key.</summary>
        public string Id { get; }
        /// <summary>Plain-text choice label.</summary>
        public string Label { get; }
        /// <summary>Qualitative social meaning, not a promise of exact NPC scores.</summary>
        public string Interpretation { get; }
        internal GameCommand Command { get; }
    }
}
