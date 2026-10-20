namespace SocialStim.Rules.Night
{
    /// <summary>Semantic private night results; messages do not identify unknown visitors.</summary>
    public enum ReportKind { Blocked, Targeted, AttackFailed, ProtectionApplied, Investigation, VisitorSeen, NoVisitors, ForgeryApplied, Guilt }
}
