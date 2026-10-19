using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Night;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>The complete authorized NPC/player decision context; never holds the world or foreign role assignments.</summary>
    public sealed class CouncilKnowledge
    {
        internal CouncilKnowledge(CharacterId self, RoleSpec ownRole, Personality traits, int day, int round,
            int charges, bool selfHealUsed, IReadOnlyList<KnownCharacter> characters, IReadOnlyList<CouncilEvent> events,
            IReadOnlyList<NightReport> results, IReadOnlyList<RoleSpec> catalog, SetupKnowledge counts,
            IReadOnlyDictionary<RoleId, int> roleCounts, IReadOnlyList<CharacterId> voteTargets = null, IReadOnlyDictionary<CharacterId, CharacterStats> stats = null,
            AiPolicy policy = null, uint seed = 0, CouncilInferenceResult inference = null, bool selfHealAllowed = true, CharacterId? teamTarget = null)
        { Self = self; OwnRole = ownRole; Traits = traits; Day = day; Round = round; Charges = charges;
            SelfHealUsed = selfHealUsed; Characters = characters; Events = events; Results = results;
            Catalog = catalog; Counts = counts; RoleCounts = roleCounts; VoteTargets = voteTargets; Stats = stats; Policy = policy ?? new AiPolicy(); Seed = seed; Inference = inference; SelfHealAllowed = selfHealAllowed; TeamTarget = teamTarget; }
        public IReadOnlyDictionary<CharacterId, CharacterStats> Stats { get; }
        public AiPolicy Policy { get; }
        public uint Seed { get; }
        public CouncilInferenceResult Inference { get; }
        public bool SelfHealAllowed { get; }
        public CharacterId? TeamTarget { get; }
        internal CouncilKnowledge WithTeamTarget(CharacterId? target) => new CouncilKnowledge(Self, OwnRole, Traits, Day, Round,
            Charges, SelfHealUsed, Characters, Events, Results, Catalog, Counts, RoleCounts, VoteTargets, Stats, Policy, Seed, Inference, SelfHealAllowed, target);
        public CharacterStats Skill(CharacterId actor) => Stats != null && Stats.TryGetValue(actor, out CharacterStats value) ? value : new CharacterStats();
        public CharacterId Self { get; }
        public RoleSpec OwnRole { get; }
        public Personality Traits { get; }
        public int Day { get; }
        public int Round { get; }
        public int Charges { get; }
        public bool SelfHealUsed { get; }
        public IReadOnlyList<KnownCharacter> Characters { get; }
        public IReadOnlyList<CouncilEvent> Events { get; }
        public IReadOnlyList<NightReport> Results { get; }
        public IReadOnlyList<RoleSpec> Catalog { get; }
        public SetupKnowledge Counts { get; }
        public IReadOnlyList<CharacterId> VoteTargets { get; }
        public IReadOnlyDictionary<RoleId, int> RoleCounts { get; }
    }
}
