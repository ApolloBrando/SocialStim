using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;

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
        /// <summary>Presentation navigation only; choosing a category never submits a command.</summary>
        public string Category => Command.Kind == CommandKind.ReportLastAction ? "Night reports" : Command.Kind != CommandKind.Speak ? "Actions"
            : Command.Intent == SpeechIntent.ClaimRole ? "Role claims"
            : Command.Intent == SpeechIntent.ReportInvestigation || Command.Intent == SpeechIntent.ReportAction ? "Night reports"
            : Command.Intent == SpeechIntent.Accuse || Command.Intent == SpeechIntent.CallForTrial || Command.Intent == SpeechIntent.Deflect ? "Pressure"
            : Command.Intent == SpeechIntent.Vouch || Command.Intent == SpeechIntent.Agree || Command.Intent == SpeechIntent.Disagree ? "Support and challenge"
            : "Personal";
        public string Family => Command.Kind == CommandKind.Speak ? Command.Intent.ToString() : Command.Kind.ToString();
        internal GameCommand Command { get; }
    }
}
