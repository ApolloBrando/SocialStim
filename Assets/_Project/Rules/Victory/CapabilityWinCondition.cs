using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Victory
{
    /// <summary>Conservative control policy: independent killers and usable Town combat postpone parity.</summary>
    public sealed class CapabilityWinCondition : IWinCondition
    {
        public OutcomeKind Evaluate(IReadOnlyList<Faction> livingFactions, int day, int maximumDays)
            => throw new InvalidOperationException("Capability outcomes require living role/resource snapshots.");

        public OutcomeKind Evaluate(IReadOnlyList<NightActor> living, int day, int maximumDays)
        {
            if (living.Count == 0) return OutcomeKind.Draw;
            var town = living.Where(a => a.Role.Faction == Faction.Town).ToList();
            var antagonists = living.Where(a => a.Role.Faction == Faction.Antagonist).ToList();
            var independent = living.Where(a => a.Role.Category == RoleCategory.NeutralKilling).ToList();
            bool infinitum = living.Any(a => a.Role.StealsParityWin);
            if (antagonists.Count == 0 && independent.Count == 0)
                return town.Count > 0 ? OutcomeKind.TownVictory : OutcomeKind.Draw;
            if (town.Count == 0 && independent.Count == 0)
                return infinitum ? OutcomeKind.InfinitumVictory : OutcomeKind.AntagonistVictory;
            if (town.Count == 0 && antagonists.Count == 0 && independent.Count == 1)
                return infinitum ? OutcomeKind.Draw : OutcomeKind.LoneKillerVictory;
            bool townCanResist = town.Any(a => a.Charges != 0 &&
                (a.Role.Ability == AbilityKind.Block || a.Role.Ability == AbilityKind.Protect || a.Role.Ability == AbilityKind.SelfProtect
                    || a.Role.Ability == AbilityKind.Attack && a.Role.Attack > 0));
            if (independent.Count == 0 && !townCanResist && antagonists.Any(a => a.Role.Ability == AbilityKind.Attack && a.Charges != 0 && a.Role.Attack > 0)
                && antagonists.Count >= town.Count) return infinitum ? OutcomeKind.InfinitumVictory : OutcomeKind.AntagonistVictory;
            return day >= maximumDays ? OutcomeKind.Draw : OutcomeKind.Ongoing;
        }
    }
}
