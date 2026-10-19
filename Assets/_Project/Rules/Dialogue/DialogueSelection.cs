namespace SocialStim.Rules.Dialogue
{
    /// <summary>Selected authored line with safe contextual substitution.</summary>
    public sealed class DialogueSelection
    {
        /// <summary>Creates a presentation-ready text record.</summary>
        public DialogueSelection(string id, string text, DialogueMetadata metadata) { Id = id; Text = text; Metadata = metadata; }
        /// <summary>Selected line ID.</summary>
        public string Id { get; }
        /// <summary>Substituted text; no visual markup interpretation has occurred.</summary>
        public string Text { get; }
        /// <summary>Optional presentation metadata.</summary>
        public DialogueMetadata Metadata { get; }
    }
}
