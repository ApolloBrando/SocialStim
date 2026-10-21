using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Checks the shape of an authored lie without consulting anyone else's hidden action.</summary>
    public static class FabricatedReportRules
    {
        public static bool IsLegal(NightActionClaim report, RoleSpec claimed, int night, CharacterId speaker,
            IReadOnlyCollection<CharacterId> participants)
        {
            if (report == null || claimed == null || claimed.Faction != Faction.Town || !claimed.CanClaimTruthfully
                || report.Role != claimed.Id || report.Ability != claimed.Ability || report.Night != night) return false;
            if (!report.Target.HasValue) return report.Results.Count == 0;
            if (report.Target == speaker || !participants.Contains(report.Target.Value)) return false;
            if (report.Results.Count == 1 && report.Blocked)
                return !report.Results[0].Target.HasValue && !report.Results[0].Alignment.HasValue && !report.Results[0].Subject.HasValue;
            if (report.Results.Any(r => r.Kind == ReportKind.Blocked)) return false;
            switch (claimed.Ability)
            {
                case AbilityKind.Investigate:
                    return report.Results.Count == 1 && report.Results[0].Kind == ReportKind.Investigation
                        && report.Results[0].Target == report.Target && !report.Results[0].Subject.HasValue;
                case AbilityKind.Protect:
                    return report.Results.Count == 1 && Simple(report.Results[0], ReportKind.ProtectionApplied, report.Target.Value);
                case AbilityKind.Attack:
                    return report.Results.Count == 0 || report.Results.Count == 1 && Simple(report.Results[0], ReportKind.AttackFailed, report.Target.Value);
                case AbilityKind.Watch:
                    if (report.Results.Count == 1 && report.Results[0].Kind == ReportKind.NoVisitors)
                        return report.Results[0].Target == report.Target && !report.Results[0].Alignment.HasValue
                            && (!report.Results[0].Subject.HasValue || report.Results[0].Subject == report.Target);
                    return report.Results.Count > 0 && report.Results.All(r => r.Kind == ReportKind.VisitorSeen
                        && r.Target.HasValue && r.Target != speaker && participants.Contains(r.Target.Value)
                        && r.Subject == report.Target && !r.Alignment.HasValue)
                        && report.Results.Select(r => r.Target).Distinct().Count() == report.Results.Count;
                default: return false;
            }
        }
        private static bool Simple(ReportedNightResult result, ReportKind kind, CharacterId target)
            => result.Kind == kind && result.Target == target && !result.Alignment.HasValue && !result.Subject.HasValue;
    }
}
