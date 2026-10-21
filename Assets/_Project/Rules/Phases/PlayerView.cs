using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;
using SocialStim.Rules.Night;

namespace SocialStim.Rules.Phases
{
    /// <summary>Immutable player-facing snapshot with no cast role assignments or NPC beliefs.</summary>
    public sealed class PlayerView
    {
        internal PlayerView(int revision, uint seed, int day, GamePhase phase, int trialsUsed, int trialsAllowed,
            bool alive, RoleSpec ownRole, int charges, CharacterId? defendant, OutcomeKind outcome, bool won,
            IReadOnlyList<KnownCharacter> characters, IReadOnlyList<CharacterId> legalNightTargets,
            IReadOnlyList<GameObservation> transcript, bool opening = false, bool canReportAction = false, int discussionRoundsAllowed = 3, VotingMode votingMode = VotingMode.Trial,
            int discussionRound = 1, int playerWindow = 1, int windowsPerRound = 1, CharacterId? respondTo = null,
            IReadOnlyList<CharacterId> voteTargets = null, bool allowAbstain = true, bool revote = false,
            RoleId? counterClaimRole = null, CharacterId? followUpTo = null, NightActionClaim lastNightAction = null, IReadOnlyList<CharacterId> revengeTargets = null, IDictionary<CharacterId, RoleId> spectatorRoles = null, IReadOnlyList<CharacterId> reportParticipants = null,
            IReadOnlyList<TeamNightRecord> teamReports = null)
        {
            SpectatorRoles = new System.Collections.ObjectModel.ReadOnlyDictionary<CharacterId, RoleId>(
                spectatorRoles == null ? new Dictionary<CharacterId, RoleId>() : new Dictionary<CharacterId, RoleId>(spectatorRoles));
            ReportParticipants = reportParticipants ?? new CharacterId[0];
            TeamReports = ownRole.Faction == Faction.Antagonist ? teamReports ?? new TeamNightRecord[0] : new TeamNightRecord[0];
            Revision = revision; Seed = seed; Day = day; Phase = phase; TrialsUsed = trialsUsed; TrialsAllowed = trialsAllowed;
            Alive = alive; OwnRole = ownRole; Charges = charges; Defendant = defendant; Outcome = outcome; Won = won;
            Characters = characters; LegalNightTargets = legalNightTargets; Transcript = transcript;
            VotingMode = votingMode; DiscussionRound = discussionRound; PlayerWindow = playerWindow;
            WindowsPerRound = windowsPerRound; RespondTo = respondTo; VoteTargets = voteTargets ?? new CharacterId[0];
            AllowAbstain = allowAbstain; Revote = revote;
            CounterClaimRole = counterClaimRole; FollowUpTo = followUpTo;
            LastNightAction = lastNightAction; RevengeTargets = revengeTargets ?? new CharacterId[0];
            Opening = opening; CanReportAction = canReportAction; DiscussionRoundsAllowed = discussionRoundsAllowed;
        }
        /// <summary>True roles available only after player death for the spectator roster; never fed into NPC knowledge.</summary>
        public IReadOnlyDictionary<CharacterId, RoleId> SpectatorRoles { get; }
        /// <summary>Public identities alive at the start of the most recent night, including its casualties.</summary>
        public IReadOnlyList<CharacterId> ReportParticipants { get; }
        /// <summary>Private faction history; empty for Town and Neutral players.</summary>
        public IReadOnlyList<TeamNightRecord> TeamReports { get; }
        /// <summary>Granted only to an executed player Jester: living Town participants eligible for revenge.</summary>
        public IReadOnlyList<CharacterId> RevengeTargets { get; }
        /// <summary>Configured daily elimination policy.</summary>
        public VotingMode VotingMode { get; }
        /// <summary>One-based public discussion round.</summary>
        public int DiscussionRound { get; }
        /// <summary>One-based ordinary player window in this round.</summary>
        public int PlayerWindow { get; }
        /// <summary>Configured ordinary player windows per round.</summary>
        public int WindowsPerRound { get; }
        /// <summary>Public speaker requiring a priority response, never a private hostile selection.</summary>
        public CharacterId? RespondTo { get; }
        /// <summary>Legal public candidates for this ballot, excluding the player.</summary>
        public IReadOnlyList<CharacterId> VoteTargets { get; }
        /// <summary>Whether a deliberate abstention is allowed.</summary>
        public bool AllowAbstain { get; }
        /// <summary>Whether the current ballot is the one permitted tie revote.</summary>
        public bool Revote { get; }
        /// <summary>Role just claimed in the current immediate counterclaim window.</summary>
        public RoleId? CounterClaimRole { get; }
        /// <summary>Public speaker whose accusation or defense may be agreed with or disputed.</summary>
        public CharacterId? FollowUpTo { get; }
        /// <summary>The player may take the opening or yield it to one random NPC.</summary>
        public bool Opening { get; }
        public bool CanReportAction { get; }
        /// <summary>Only the player's own recorded action and legitimately delivered results.</summary>
        public NightActionClaim LastNightAction { get; }
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
