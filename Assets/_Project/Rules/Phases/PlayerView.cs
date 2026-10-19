using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Phases
{
    /// <summary>Immutable player-facing snapshot with no cast role assignments or NPC beliefs.</summary>
    public sealed class PlayerView
    {
        internal PlayerView(int revision, uint seed, int day, GamePhase phase, int trialsUsed, int trialsAllowed,
            bool alive, RoleSpec ownRole, int charges, CharacterId? defendant, OutcomeKind outcome, bool won,
            IReadOnlyList<KnownCharacter> characters, IReadOnlyList<CharacterId> legalNightTargets,
            IReadOnlyList<GameObservation> transcript, bool opening = false, bool canReportAction = false, int discussionRoundsAllowed = 3)
        {
            Revision = revision; Seed = seed; Day = day; Phase = phase; TrialsUsed = trialsUsed; TrialsAllowed = trialsAllowed;
            Alive = alive; OwnRole = ownRole; Charges = charges; Defendant = defendant; Outcome = outcome; Won = won;
            Characters = characters; LegalNightTargets = legalNightTargets; Transcript = transcript;
            Opening = opening; CanReportAction = canReportAction; DiscussionRoundsAllowed = discussionRoundsAllowed;
        }
        /// <summary>The player may take the opening or yield it to one random NPC.</summary>
        public bool Opening { get; }
        public bool CanReportAction { get; }
        /// <summary>Configured discussion length for presentation progress.</summary>
        public int DiscussionRoundsAllowed { get; }
        /// <summary>Request generation, used to reject stale input.</summary>
        public int Revision { get; }
        /// <summary>Reproducibility seed, never a cast reveal.</summary>
        public uint Seed { get; }
        /// <summary>Player is always the participant at slot zero.</summary>
        public CharacterId PlayerId => new CharacterId(0);
        /// <summary>Current public day.</summary>
        public int Day { get; }
        /// <summary>Current input phase.</summary>
        public GamePhase Phase { get; }
        /// <summary>Trials already used today.</summary>
        public int TrialsUsed { get; }
        /// <summary>Trials configured per day.</summary>
        public int TrialsAllowed { get; }
        /// <summary>Whether the player can still participate.</summary>
        public bool Alive { get; }
        /// <summary>Only the player's known true role.</summary>
        public RoleSpec OwnRole { get; }
        /// <summary>Only the player's remaining ability charges.</summary>
        public int Charges { get; }
        /// <summary>Current publicly nominated participant.</summary>
        public CharacterId? Defendant { get; }
        /// <summary>Run result, or ongoing.</summary>
        public OutcomeKind Outcome { get; }
        /// <summary>Player's faction/personal objective result once terminal.</summary>
        public bool Won { get; }
        /// <summary>Public identities and permitted teammate/reveal knowledge.</summary>
        public IReadOnlyList<KnownCharacter> Characters { get; }
        /// <summary>Targets legal for the player's actual ability.</summary>
        public IReadOnlyList<CharacterId> LegalNightTargets { get; }
        /// <summary>Public transcript plus the player's legitimate private observations.</summary>
        public IReadOnlyList<GameObservation> Transcript { get; }
    }
}
