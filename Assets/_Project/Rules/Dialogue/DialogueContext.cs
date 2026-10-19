using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Authorized substitution values; target hidden roles never enter this record.</summary>
    public sealed class DialogueContext
    {
        /// <summary>Creates a context for one assertion.</summary>
        public DialogueContext(CharacterId speakerId, string speaker, string target, string assertedRole,
            string assertedResult, int day, RoleId ownRole, Personality traits, GamePhase phase = GamePhase.Discussion,
            CharacterId? targetId = null, IEnumerable<EvidenceRecord> evidence = null, int selfExposure = 0)
        { SpeakerId = speakerId; Speaker = speaker; Target = target; AssertedRole = assertedRole;
            AssertedResult = assertedResult; Day = day; OwnRole = ownRole; Traits = traits;
            Phase = phase; TargetId = targetId; SelfExposure = selfExposure;
            Evidence = (evidence ?? Enumerable.Empty<EvidenceRecord>()).Where(e => e.Kind != EvidenceKind.PrivateInvestigation || e.Actor == speakerId).ToList().AsReadOnly(); }
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
        /// <summary>Current public phase.</summary>
        public GamePhase Phase { get; }
        /// <summary>Explicitly selected public target.</summary>
        public CharacterId? TargetId { get; }
        /// <summary>Whether a target is available for this utterance.</summary>
        public bool HasTarget => TargetId.HasValue || !string.IsNullOrEmpty(Target);
        /// <summary>Whether this utterance asserts a role or result.</summary>
        public bool HasAssertion => !string.IsNullOrEmpty(AssertedRole) || !string.IsNullOrEmpty(AssertedResult);
        /// <summary>Speaker-authorized evidence, excluding other observers' private records.</summary>
        public IReadOnlyList<EvidenceRecord> Evidence { get; }
        /// <summary>Speaker's estimate from visible pressure.</summary>
        public int SelfExposure { get; }
    }
}
