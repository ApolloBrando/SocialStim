using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Immutable event; private records are filtered before view or strategy delivery.</summary>
    public sealed class CouncilEvent
    {
        internal CouncilEvent(int sequence, int day, int round, CouncilEventKind kind, string text,
            CharacterId? actor = null, CharacterId? target = null, SpeechIntent? intent = null,
            ReasonType reason = ReasonType.None, RoleId? claim = null, Readout? readout = null,
            CharacterId? recipient = null, string lineId = null, bool? patientAttacked = null)
        { Sequence = sequence; Day = day; Round = round; Kind = kind; Text = text; Actor = actor;
            Target = target; Intent = intent; Reason = reason; Claim = claim; Readout = readout;
            Recipient = recipient; LineId = lineId; PatientAttacked = patientAttacked; }
        public int Sequence { get; }
        public int Day { get; }
        public int Round { get; }
        public CouncilEventKind Kind { get; }
        public string Text { get; }
        public CharacterId? Actor { get; }
        public CharacterId? Target { get; }
        public SpeechIntent? Intent { get; }
        public ReasonType Reason { get; }
        public RoleId? Claim { get; }
        public Readout? Readout { get; }
        public CharacterId? Recipient { get; }
        public string LineId { get; }
        public bool? PatientAttacked { get; }
        public bool Public => !Recipient.HasValue;
        internal CouncilEvent Scoped(int sequence) => new CouncilEvent(sequence, Day, Round, Kind, Text, Actor, Target, Intent, Reason, Claim, Readout, Recipient, LineId, PatientAttacked);
    }
}
