using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Authorized substitution values; target hidden roles never enter this record.</summary>
    public sealed class DialogueContext
    {
        /// <summary>Creates a context for one assertion.</summary>
        public DialogueContext(CharacterId speakerId, string speaker, string target, string assertedRole,
            string assertedResult, int day, RoleId ownRole, Personality traits)
        { SpeakerId = speakerId; Speaker = speaker; Target = target; AssertedRole = assertedRole;
            AssertedResult = assertedResult; Day = day; OwnRole = ownRole; Traits = traits; }
        /// <summary>Speaker identity used for repetition history.</summary>
        public CharacterId SpeakerId { get; }
        /// <summary>Public speaker name.</summary>
        public string Speaker { get; }
        /// <summary>Public target name.</summary>
        public string Target { get; }
        /// <summary>Role being claimed, not established truth.</summary>
        public string AssertedRole { get; }
        /// <summary>Result being asserted.</summary>
        public string AssertedResult { get; }
        /// <summary>Current public day.</summary>
        public int Day { get; }
        /// <summary>Speaker's own known role for eligibility.</summary>
        public RoleId OwnRole { get; }
        /// <summary>Speaker traits for eligibility.</summary>
        public Personality Traits { get; }
    }
}
