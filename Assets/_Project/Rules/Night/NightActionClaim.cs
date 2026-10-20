using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Structured public account. Honest and deceptive accounts have identical shapes.</summary>
    public sealed class NightActionClaim
    {
        public NightActionClaim(int night, RoleId role, AbilityKind ability, CharacterId? target,
            IEnumerable<ReportedNightResult> results = null)
        {
            if (night < 1 || !Enum.IsDefined(typeof(AbilityKind), ability)) throw new ArgumentException("Invalid night account.");
            Night = night; Role = role; Ability = ability; Target = target;
            var copy = (results ?? Enumerable.Empty<ReportedNightResult>()).ToList();
            if (copy.Any(r => r == null || !Enum.IsDefined(typeof(ReportKind), r.Kind)
                || r.Kind == ReportKind.Investigation && (!r.Target.HasValue || !r.Alignment.HasValue || !Enum.IsDefined(typeof(Faction), r.Alignment.Value))
                || r.Kind == ReportKind.VisitorSeen && (!r.Target.HasValue || !r.Subject.HasValue)
                || r.Kind == ReportKind.NoVisitors && !r.Target.HasValue)) throw new ArgumentException("Invalid reported result.");
            Results = copy.AsReadOnly();
        }
        public int Night { get; }
        public RoleId Role { get; }
        public AbilityKind Ability { get; }
        public CharacterId? Target { get; }
        public IReadOnlyList<ReportedNightResult> Results { get; }
        public bool Blocked => Results.Any(r => r.Kind == ReportKind.Blocked);
        /// <summary>Repetition cannot manufacture extra corroboration.</summary>
        internal string Signature => Night + ":" + Role.Value + ":" + Ability + ":" + Target + ":"
            + string.Join(";", Results.Select(r => r.Kind + ":" + r.Target + ":" + r.Alignment + ":" + r.Subject));
    }
}
