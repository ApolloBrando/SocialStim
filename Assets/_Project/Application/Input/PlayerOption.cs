using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Cast;

namespace SocialStim.Application.Input
{
    /// <summary>One portable action option; IDs are scoped to a particular input revision.</summary>
    public sealed class PlayerOption
    {
        internal PlayerOption(string id, string label, string interpretation, GameCommand command, System.Action navigate = null, CharacterId? navigationTarget = null)
        { Id = id; Label = label; Interpretation = interpretation; Command = command; Navigate = navigate; _navigationTarget = navigationTarget; }
        private readonly CharacterId? _navigationTarget;
        internal System.Action Navigate { get; }
        /// <summary>Reversible menu navigation; it does not consume a gameplay turn.</summary>
        public bool IsNavigation => Navigate != null;
        /// <summary>Opaque revision-scoped selection key.</summary>
        public string Id { get; }
        /// <summary>Plain-text choice label.</summary>
        public string Label { get; }
        /// <summary>Qualitative social meaning, not a promise of exact NPC scores.</summary>
        public string Interpretation { get; }
        /// <summary>Presentation navigation only; choosing a category never submits a command.</summary>
        public string Category => Command.Kind == CommandKind.ReportLastAction ? "Report" : Command.Kind != CommandKind.Speak ? "Actions"
            : Command.Intent == SpeechIntent.ClaimRole ? "Claim Role"
            : Command.Intent == SpeechIntent.Accuse ? "Accuse"
            : Command.Intent == SpeechIntent.Vouch ? "Defend" : "Actions";
        /// <summary>Whether presentation should open a target/role submenu before submitting.</summary>
        public bool HasSubmenu => Category != "Actions";
        public string Family => Command.Kind == CommandKind.Speak ? ActionLabels.Speech(Command.Intent) : Command.Kind == CommandKind.ReportLastAction ? "Report last night" : "Actions";
        internal GameCommand Command { get; }
        /// <summary>Public, already-validated UI target; never a hidden NPC selection.</summary>
        public CharacterId? Target => _navigationTarget ?? Command.Target;
        public CommandKind Kind => Command.Kind;
        public SpeechIntent Intent => Command.Intent;
    }
}
