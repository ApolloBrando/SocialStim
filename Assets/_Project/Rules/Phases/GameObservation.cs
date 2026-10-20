using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Phases
{
    /// <summary>Already scoped player observation; safe for any presentation subscriber.</summary>
    public sealed class GameObservation
    {
        internal GameObservation(int sequence, int day, GameCue cue, string text, CharacterId? speaker = null, DialogueMetadata metadata = null,
            CharacterId? target = null, SpeechIntent? intent = null, RoleId? claimedRole = null, int round = 1)
        { Round = round; Sequence = sequence; Day = day; Cue = cue; Text = text; Speaker = speaker; Metadata = metadata; Target = target; Intent = intent; ClaimedRole = claimedRole; }
        /// <summary>Public discussion round at event delivery, before the next window advances.</summary>
        public int Round { get; }
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
        /// <summary>Public/scoped subject, never a pending hostile selection.</summary>
        public CharacterId? Target { get; }
        /// <summary>Intent of an already delivered public statement.</summary>
        public SpeechIntent? Intent { get; }
        /// <summary>A speaker's assertion, not their true role.</summary>
        public RoleId? ClaimedRole { get; }
    }
}
