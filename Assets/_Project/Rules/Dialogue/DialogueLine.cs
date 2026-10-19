using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>One authored opaque text record and its selection conditions.</summary>
    public sealed class DialogueLine
    {
        /// <summary>Creates a validated line; total pool weights must fit an integer.</summary>
        public DialogueLine(string id, SpeechIntent intent, string text, int weight = 1,
            Personality requiredTrait = Personality.None, RoleId? speakerRole = null,
            DialogueMetadata metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(text) || weight < 1 || weight > 1000)
                throw new ArgumentException("Dialogue requires an ID, text, and weight in [1, 1000].");
            Id = id; Intent = intent; Text = text; Weight = weight;
            RequiredTrait = requiredTrait; SpeakerRole = speakerRole; Metadata = metadata ?? new DialogueMetadata();
        }
        /// <summary>Stable authored line identity.</summary>
        public string Id { get; }
        /// <summary>Semantic speech intent.</summary>
        public SpeechIntent Intent { get; }
        /// <summary>Text remains opaque except for explicit variable substitution.</summary>
        public string Text { get; }
        /// <summary>Positive integer selection weight.</summary>
        public int Weight { get; }
        /// <summary>All required speaker traits.</summary>
        public Personality RequiredTrait { get; }
        /// <summary>Optional condition on the speaker's own known role.</summary>
        public RoleId? SpeakerRole { get; }
        /// <summary>Future presentation hints.</summary>
        public DialogueMetadata Metadata { get; }
    }
}
