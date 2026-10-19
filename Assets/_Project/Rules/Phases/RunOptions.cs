using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Phases
{
    /// <summary>Run-local configuration; a seed is chosen before constructing this record.</summary>
    public sealed class RunOptions
    {
        /// <summary>Creates the initial seven-participant setup options.</summary>
        public RunOptions(uint seed, RoleId? playerRole = null, int trialsPerDay = 2,
            int discussionRounds = 3, bool revealOnDeath = true, int maximumDays = 30)
        {
            if (trialsPerDay < 1 || trialsPerDay > 10 || discussionRounds < 1 || discussionRounds > 10 || maximumDays < 1)
                throw new ArgumentOutOfRangeException(nameof(trialsPerDay));
            Seed = seed; PlayerRole = playerRole; TrialsPerDay = trialsPerDay;
            DiscussionRounds = discussionRounds; RevealOnDeath = revealOnDeath; MaximumDays = maximumDays;
        }
        /// <summary>Recorded reproducibility seed.</summary>
        public uint Seed { get; }
        /// <summary>Optional chosen role, required to occur in the setup.</summary>
        public RoleId? PlayerRole { get; }
        /// <summary>Maximum trials before night.</summary>
        public int TrialsPerDay { get; }
        /// <summary>Player/NPC speech rounds before nomination.</summary>
        public int DiscussionRounds { get; }
        /// <summary>Whether deaths disclose roles publicly.</summary>
        public bool RevealOnDeath { get; }
        /// <summary>Safety cap ending unresolved runs as a draw.</summary>
        public int MaximumDays { get; }
    }
}
