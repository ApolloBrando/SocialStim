using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Rules.Phases
{
    /// <summary>Already scoped player observation; safe for any presentation subscriber.</summary>
    public sealed class GameObservation
    {
        internal GameObservation(int sequence, int day, GameCue cue, string text, CharacterId? speaker = null, DialogueMetadata metadata = null)
        { Sequence = sequence; Day = day; Cue = cue; Text = text; Speaker = speaker; Metadata = metadata; }
        /// <summary>Monotonic transcript identity.</summary>
        public int Sequence { get; }
        /// <summary>Public day associated with the observation.</summary>
        public int Day { get; }
        /// <summary>Meaning, independent of effects.</summary>
        public GameCue Cue { get; }
        /// <summary>Text or authored dialogue ready for plain display.</summary>
        public string Text { get; }
        /// <summary>Public speaker if this is speech.</summary>
        public CharacterId? Speaker { get; }
        /// <summary>Optional presentation metadata.</summary>
        public DialogueMetadata Metadata { get; }
    }
}
