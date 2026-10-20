using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Night;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Attributed observer-local evidence. No hidden truth/lie flag is stored.</summary>
    public sealed class EvidenceRecord
    {
        internal EvidenceRecord(int sequence, int day, EvidenceKind kind, CharacterId actor,
            CharacterId? target = null, SpeechIntent? intent = null, RoleId? role = null,
            Faction? alignment = null, Verdict? verdict = null, int night = 0, NightActionClaim nightReport = null)
        { Sequence = sequence; Day = day; Kind = kind; Actor = actor; Target = target;
            Intent = intent; Role = role; Alignment = alignment; Verdict = verdict; Night = night; NightReport = nightReport; }
        /// <summary>Local chronological sequence.</summary>
        public int Sequence { get; }
        /// <summary>Public day when this evidence was received.</summary>
        public int Day { get; }
        /// <summary>Assertion, observation, contradiction, or legitimate private result.</summary>
        public EvidenceKind Kind { get; }
        /// <summary>Attributed speaker/voter, or the observer for a private result.</summary>
        public CharacterId Actor { get; }
        /// <summary>Subject of the statement/result, when applicable.</summary>
        public CharacterId? Target { get; }
        /// <summary>Original speech intent.</summary>
        public SpeechIntent? Intent { get; }
        /// <summary>Claimed or publicly revealed role, never an inferred true role.</summary>
        public RoleId? Role { get; }
        /// <summary>Asserted, observed, or apparently investigated alignment.</summary>
        public Faction? Alignment { get; }
        /// <summary>Public judgement stance.</summary>
        public Verdict? Verdict { get; }
        /// <summary>Night associated with an investigation assertion/result.</summary>
        public int Night { get; }
        public NightActionClaim NightReport { get; }
    }
}
