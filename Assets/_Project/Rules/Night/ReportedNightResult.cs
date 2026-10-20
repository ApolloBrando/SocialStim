using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
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
}
