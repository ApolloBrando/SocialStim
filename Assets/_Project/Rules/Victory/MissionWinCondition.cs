using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Night;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Victory
{
    /// <summary>Stable-boundary mission outcomes with explicit neutral precedence.</summary>
    public sealed class MissionWinCondition
    {
        /// <summary>Evaluate only after the whole vote or night batch and succession are complete.</summary>
        public OutcomeKind Evaluate(IReadOnlyList<NightActor> living, int day = 1, int maximumDays = 30,
            NeutralParityMode parityMode = NeutralParityMode.Neither)
        {
            if (living == null) throw new ArgumentNullException(nameof(living));
            if (!Enum.IsDefined(typeof(NeutralParityMode), parityMode)) throw new ArgumentException("Unknown parity mode.");
            if (living.Count == 0) return OutcomeKind.Draw;
            int town = living.Count(a => a.Role.Faction == Faction.Town);
            int evil = living.Count(a => a.Role.Faction == Faction.Antagonist);
            int killers = living.Count(a => a.Role.Objective == RoleObjective.LastSurvivor);
            int stealers = living.Count(a => a.Role.Objective == RoleObjective.StealParity);
            if (living.Count == 1 && killers == 1) return OutcomeKind.LoneKillerVictory;
            if (living.Count == 2 && killers == 1 && stealers == 1) return OutcomeKind.Draw;
            if (evil == 0 && killers == 0) return town > 0 ? OutcomeKind.TownVictory : OutcomeKind.Draw;
            int parityTown = town + (parityMode == NeutralParityMode.CountAsTown ? stealers : 0);
            int parityEvil = evil + (parityMode == NeutralParityMode.CountAsEvil ? stealers : 0);
            if (evil > 0 && killers == 0 && parityEvil >= parityTown)
                return stealers > 0 ? OutcomeKind.InfinitumVictory : OutcomeKind.AntagonistVictory;
            return day >= maximumDays ? OutcomeKind.Draw : OutcomeKind.Ongoing;
        }

        /// <summary>Faction wins include dead allies; neutral objectives use actual survival or elimination provenance.</summary>
        public bool Won(RoleSpec role, bool alive, bool eliminatedByVote, OutcomeKind outcome)
        {
            if (outcome == OutcomeKind.Ongoing) return false;
            switch (role.Objective)
            {
                case RoleObjective.LastSurvivor: return alive && outcome == OutcomeKind.LoneKillerVictory;
                case RoleObjective.StealParity: return alive && outcome == OutcomeKind.InfinitumVictory;
                case RoleObjective.EliminatedByVote: return eliminatedByVote;
                case RoleObjective.Survive: return alive;
                default: return role.Faction == Faction.Town ? outcome == OutcomeKind.TownVictory
                    : role.Faction == Faction.Antagonist && outcome == OutcomeKind.AntagonistVictory;
            }
        }
    }
}
