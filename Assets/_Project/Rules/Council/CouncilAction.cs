using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>One immutable player/NPC action with explicit assertion provenance.</summary>
    public sealed class CouncilAction
    {
        public CouncilAction(CouncilActionKind kind, CharacterId? target = null, ReasonType reason = ReasonType.None,
            RoleId? claim = null, Readout? readout = null, string lineId = null, bool declaredLie = false,
            SpeechIntent? intent = null, bool? patientAttacked = null)
        { Kind = kind; Target = target; Reason = reason; Claim = claim; Readout = readout; LineId = lineId;
            DeclaredLie = declaredLie; Intent = intent; PatientAttacked = patientAttacked; }
        public CouncilActionKind Kind { get; }
        public CharacterId? Target { get; }
        public ReasonType Reason { get; }
        public RoleId? Claim { get; }
        public Readout? Readout { get; }
        public string LineId { get; }
        public bool DeclaredLie { get; }
        public SpeechIntent? Intent { get; }
        public bool? PatientAttacked { get; }
        public CouncilAction WithLine(string id) => new CouncilAction(Kind, Target, Reason, Claim, Readout, id, DeclaredLie, Intent, PatientAttacked);
    }
}
