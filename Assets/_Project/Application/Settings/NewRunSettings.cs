using System;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.Settings
{
    /// <summary>Implemented game configuration persisted independently of an ongoing run.</summary>
    public sealed class NewRunSettings
    {
        /// <summary>Creates validated defaults or a detached setup snapshot.</summary>
        public NewRunSettings(int playerCount = 9, int rounds = 5, int windows = 1, VotingMode mode = VotingMode.Gnosia,
            bool abstain = true, bool revote = true, bool reveal = true, bool lastWords = true, int trials = 2, uint? seed = null)
        {
            if (playerCount < 5 || playerCount > 20 || rounds < 3 || rounds > 7 || windows < 1 || windows > 2
                || trials < 1 || trials > 10 || !Enum.IsDefined(typeof(VotingMode), mode)) throw new ArgumentOutOfRangeException(nameof(playerCount));
            PlayerCount = playerCount; Rounds = rounds; Windows = windows; Mode = mode; Abstain = abstain;
            Revote = revote; Reveal = reveal; LastWords = lastWords; Trials = trials; Seed = seed;
        }
        /// <summary>Total participants, including the player.</summary>
        public int PlayerCount { get; }
        /// <summary>Discussion rounds per day.</summary>
        public int Rounds { get; }
        /// <summary>Ordinary player windows per round.</summary>
        public int Windows { get; }
        /// <summary>Daily vote policy.</summary>
        public VotingMode Mode { get; }
        /// <summary>Permits deliberate abstention.</summary>
        public bool Abstain { get; }
        /// <summary>Permits one restricted tie revote.</summary>
        public bool Revote { get; }
        /// <summary>Publicly discloses roles on death.</summary>
        public bool Reveal { get; }
        /// <summary>Allows last words before elimination.</summary>
        public bool LastWords { get; }
        /// <summary>Trial Mode's daily trial budget.</summary>
        public int Trials { get; }
        /// <summary>Reproducibility seed; null requests a fresh seed.</summary>
        public uint? Seed { get; }
    }
}
