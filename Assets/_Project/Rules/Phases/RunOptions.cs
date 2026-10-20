using System;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Phases
{
    /// <summary>Run-local configuration; a seed is chosen before constructing this record.</summary>
    public sealed class RunOptions
    {
        /// <summary>Creates run options independently of the authored participant count.</summary>
        public RunOptions(uint seed, RoleId? playerRole = null, int trialsPerDay = 2,
            int discussionRounds = 5, bool revealOnDeath = true, int maximumDays = 30,
            VotingMode votingMode = VotingMode.Gnosia, int playerWindowsPerRound = 1, bool allowAbstain = true,
            bool revoteTies = true, bool lastWords = true)
        {
            if (trialsPerDay < 1 || trialsPerDay > 10 || discussionRounds < 1 || discussionRounds > 10 || maximumDays < 1)
                throw new ArgumentOutOfRangeException(nameof(trialsPerDay));
            if (!Enum.IsDefined(typeof(VotingMode), votingMode) || playerWindowsPerRound < 1 || playerWindowsPerRound > 2
                || votingMode == VotingMode.Gnosia && (discussionRounds < 3 || discussionRounds > 7))
                throw new ArgumentOutOfRangeException(nameof(discussionRounds));
            VotingMode = votingMode; PlayerWindowsPerRound = playerWindowsPerRound;
            AllowAbstain = allowAbstain; RevoteTies = revoteTies; LastWords = lastWords;
            Seed = seed; PlayerRole = playerRole; TrialsPerDay = trialsPerDay;
            DiscussionRounds = discussionRounds; RevealOnDeath = revealOnDeath; MaximumDays = maximumDays;
        }
        /// <summary>Daily elimination policy.</summary>
        public VotingMode VotingMode { get; }
        /// <summary>Ordinary player actions per discussion round, excluding responses.</summary>
        public int PlayerWindowsPerRound { get; }
        /// <summary>Whether a living voter may deliberately abstain.</summary>
        public bool AllowAbstain { get; }
        /// <summary>Whether the first tied plurality receives one restricted revote.</summary>
        public bool RevoteTies { get; }
        /// <summary>Whether a plurality elimination allows last words before death.</summary>
        public bool LastWords { get; }
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
