using System;
using System.Collections.Generic;
using System.Globalization;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Factual, editable report formatting; personality cannot replace its action, target or results.</summary>
    public static class NightReportFormatter
    {
        public static string Format(NightActionClaim claim, string roleName, Func<CharacterId, string> name, GameText text)
        {
            var parts = new List<string> { text.Format("report.heading", ("night", claim.Night.ToString(CultureInfo.InvariantCulture)), ("role", roleName)) };
            string ability = claim.Ability.ToString();
            string key = char.ToLowerInvariant(ability[0]) + ability.Substring(1);
            parts.Add(text.Format(claim.Ability == Roles.AbilityKind.None ? "report.none" : !claim.Target.HasValue ? "report.pass" : "report." + key,
                ("target", claim.Target.HasValue ? name(claim.Target.Value) : "")));
            foreach (ReportedNightResult result in claim.Results)
            {
                string target = result.Target.HasValue ? name(result.Target.Value) : claim.Target.HasValue ? name(claim.Target.Value) : "";
                switch (result.Kind)
                {
                    case ReportKind.Blocked: parts.Add(text.Format(claim.Ability == Roles.AbilityKind.Block && claim.Target.HasValue ? "report.blockedBlock" : "report.blocked")); break;
                    case ReportKind.Investigation: parts.Add(text.Format("report.investigation", ("target", target), ("result", result.Alignment.ToString()))); break;
                    case ReportKind.VisitorSeen:
                        parts.Add(text.Format("report.visitor", ("visitor", target), ("subject", name(result.Subject.Value)))); break;
                    case ReportKind.NoVisitors: parts.Add(text.Format("report.noVisitors", ("target", target))); break;
                    case ReportKind.ProtectionApplied: parts.Add(text.Format("report.protection", ("target", target))); break;
                    case ReportKind.AttackFailed: parts.Add(text.Format("report.attackFailed", ("target", target))); break;
                    case ReportKind.Targeted: parts.Add(text.Format("report.targeted")); break;
                    case ReportKind.ForgeryApplied: parts.Add(text.Format("report.forgery", ("target", target))); break;
                }
            }
            if (claim.Target.HasValue && claim.Results.Count == 0) parts.Add(text.Format("report.noResult"));
            return string.Join(" ", parts);
        }
    }
}
