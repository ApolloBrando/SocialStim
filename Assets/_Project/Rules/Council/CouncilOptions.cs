using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Council
{
    /// <summary>Run-local mission policy, copied before any random stream is consumed.</summary>
    public sealed class CouncilOptions
    {
        public CouncilOptions(uint seed, int rounds = 5, int playerWindows = 1, bool revote = true, bool abstain = true,
            bool revealRoles = true, bool lastWords = true, bool freeTime = false, int freeTimeActions = 1,
            bool promoteEvil = true, bool backlash = true, bool selfHeal = true, bool notifyLull = true,
            int nullifierCharges = 2, bool forgerReadsClear = false, int maximumDays = 30,
            SetupVisibility visibility = SetupVisibility.FactionCounts, NeutralParityMode parity = NeutralParityMode.Neither)
        {
            if (rounds < 3 || rounds > 7 || playerWindows < 1 || playerWindows > 2 || freeTimeActions < 1 || freeTimeActions > 2
                || nullifierCharges < 0 || nullifierCharges > 10 || maximumDays < 1) throw new ArgumentException("Run setting out of range.");
            Seed = seed; Rounds = rounds; PlayerWindows = playerWindows; Revote = revote; Abstain = abstain;
            RevealRoles = revealRoles; LastWords = lastWords; FreeTime = freeTime; FreeTimeActions = freeTimeActions;
            PromoteEvil = promoteEvil; Backlash = backlash; SelfHeal = selfHeal; NotifyLull = notifyLull;
            NullifierCharges = nullifierCharges; ForgerReadsClear = forgerReadsClear; MaximumDays = maximumDays;
            Visibility = visibility; Parity = parity;
        }
        public uint Seed { get; }
        public int Rounds { get; }
        public int PlayerWindows { get; }
        public bool Revote { get; }
        public bool Abstain { get; }
        public bool RevealRoles { get; }
        public bool LastWords { get; }
        public bool FreeTime { get; }
        public int FreeTimeActions { get; }
        public bool PromoteEvil { get; }
        public bool Backlash { get; }
        public bool SelfHeal { get; }
        public bool NotifyLull { get; }
        public int NullifierCharges { get; }
        public bool ForgerReadsClear { get; }
        public int MaximumDays { get; }
        public SetupVisibility Visibility { get; }
        public NeutralParityMode Parity { get; }
    }
}
