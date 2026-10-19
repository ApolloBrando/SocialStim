using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Council
{
    /// <summary>Player-only view. True roles and foreign private events exist here only after the run ends.</summary>
    public sealed class CouncilSnapshot
    {
        internal CouncilSnapshot(int revision, uint seed, int day, int round, CouncilPhase phase, CouncilKnowledge knowledge,
            int rounds, int windowsLeft, bool respond, CharacterId? respondTo, IReadOnlyList<CouncilEvent> events,
            IReadOnlyList<CharacterId> nightTargets, IReadOnlyList<CharacterId> voteTargets,
            CharacterId? eliminated, OutcomeKind outcome, bool won, IReadOnlyDictionary<CharacterId, RoleId> finalRoles,
            IReadOnlyList<CouncilEvent> afterAction)
        { Revision = revision; Seed = seed; Day = day; Round = round; Phase = phase; Knowledge = knowledge;
            Rounds = rounds; WindowsLeft = windowsLeft; Respond = respond; RespondTo = respondTo; Events = events;
            NightTargets = nightTargets; VoteTargets = voteTargets; Eliminated = eliminated; Outcome = outcome;
            Won = won; FinalRoles = finalRoles; AfterAction = afterAction; }
        public int Revision { get; }
        public uint Seed { get; }
        public int Day { get; }
        public int Round { get; }
        public CouncilPhase Phase { get; }
        public CouncilKnowledge Knowledge { get; }
        public int Rounds { get; }
        public int WindowsLeft { get; }
        public bool Respond { get; }
        public CharacterId? RespondTo { get; }
        public IReadOnlyList<CouncilEvent> Events { get; }
        public IReadOnlyList<CharacterId> NightTargets { get; }
        public IReadOnlyList<CharacterId> VoteTargets { get; }
        public CharacterId? Eliminated { get; }
        public OutcomeKind Outcome { get; }
        public bool Won { get; }
        public IReadOnlyDictionary<CharacterId, RoleId> FinalRoles { get; }
        public IReadOnlyList<CouncilEvent> AfterAction { get; }
    }
}
