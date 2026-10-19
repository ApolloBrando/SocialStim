using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>A private result, deliverable only to its recipient.</summary>
    public sealed class NightReport
    {
        /// <summary>Creates a scoped report; target/alignment are only legitimate result data.</summary>
        public NightReport(CharacterId recipient, ReportKind kind, CharacterId? target = null, Faction? alignment = null, int night = 0, CharacterId? subject = null, Readout? readout = null)
        { Recipient = recipient; Kind = kind; Target = target; Alignment = alignment; Night = night; Subject = subject; Readout = readout; }
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
        /// <summary>Apparent three-way mission result; never a target role reference.</summary>
        public Readout? Readout { get; }
    }
}
