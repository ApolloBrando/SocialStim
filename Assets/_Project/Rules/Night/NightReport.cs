using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>A private result, deliverable only to its recipient.</summary>
    public sealed class NightReport
    {
        /// <summary>Creates a scoped report; target/alignment are only legitimate result data.</summary>
        public NightReport(CharacterId recipient, ReportKind kind, CharacterId? target = null, Faction? alignment = null, int night = 0, CharacterId? subject = null)
        { Recipient = recipient; Kind = kind; Target = target; Alignment = alignment; Night = night; Subject = subject; }
        /// <summary>Only authorized observer.</summary>
        public CharacterId Recipient { get; }
        /// <summary>Result category.</summary>
        public ReportKind Kind { get; }
        /// <summary>Known subject of the result, never an unknown attacker.</summary>
        public CharacterId? Target { get; }
        /// <summary>Apparent alignment for an investigation.</summary>
        public Faction? Alignment { get; }
        /// <summary>Watched location for visitor reports; never an undisclosed killer identity.</summary>
        public CharacterId? Subject { get; }
        /// <summary>Night number attached when the session delivers this result.</summary>
        public int Night { get; }
    }

    /// <summary>Semantic private night results; messages do not identify unknown visitors.</summary>
    public enum ReportKind { Blocked, Targeted, AttackFailed, ProtectionApplied, Investigation, VisitorSeen, NoVisitors, ForgeryApplied }

    /// <summary>A speaker's public assertion about a result, never a recipient's certified private report.</summary>
    public sealed class ReportedNightResult
    {
        /// <summary>Creates an attributed result payload without secret truth flags.</summary>
        public ReportedNightResult(ReportKind kind, CharacterId? target = null, Faction? alignment = null, CharacterId? subject = null)
        { Kind = kind; Target = target; Alignment = alignment; Subject = subject; }
        public ReportKind Kind { get; }
        public CharacterId? Target { get; }
        public Faction? Alignment { get; }
        public CharacterId? Subject { get; }
    }

    /// <summary>Privileged testing data. Never place this in PlayerView, transcripts, evidence or strategy inputs.</summary>
    public sealed class NightDebugRecord
    {
        internal NightDebugRecord(CharacterId actor, string name, NightActionClaim action, CharacterId? visitTarget, bool spentCharge)
        { Actor = actor; Name = name; Action = action; VisitTarget = visitTarget; SpentCharge = spentCharge; }
        public CharacterId Actor { get; }
        public string Name { get; }
        public NightActionClaim Action { get; }
        public CharacterId? VisitTarget { get; }
        public bool SpentCharge { get; }
    }

    /// <summary>Recorded Mafia action, shared privately with that faction after resolution.</summary>
    public sealed class TeamNightRecord
    {
        internal TeamNightRecord(CharacterId actor, string name, NightActionClaim action, CharacterId? visitTarget)
        { Actor = actor; Name = name; Action = action; VisitTarget = visitTarget; }
        public CharacterId Actor { get; }
        public string Name { get; }
        public NightActionClaim Action { get; }
        public CharacterId? VisitTarget { get; }
    }
}
