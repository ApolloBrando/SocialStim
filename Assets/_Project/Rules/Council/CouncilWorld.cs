using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SocialStim.Rules.Reasoning;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Council
{
    /// <summary>Owns authoritative state and scoped record publication; never crosses a view/strategy port.</summary>
    internal sealed class CouncilWorld
    {
        internal CouncilWorld(CouncilOptions options, IEnumerable<CastMemberSpec> cast, IEnumerable<RoleSpec> catalog)
        {
            Options = options; Catalog = catalog.Select(r => options.ForgerReadsClear && r.Ability == AbilityKind.Forge
                ? CopyRole(r, r.Ability, r.Attack, r.Flags | RoleFlags.ReadsClear) : r).OrderBy(r => r.Id.Value, StringComparer.Ordinal).ToList().AsReadOnly();
            Actors = cast.Select((c, i) => new CouncilActor(new CharacterId(i), c, Catalog.Single(r => r.Id == c.Role))).ToList();
            Decisions = new SeededRandom(options.Seed ^ 0xA341316Cu);
            var statRandom = new SeededRandom(options.Seed ^ 0xB5297A4Du);
            foreach (var actor in Actors)
            {
                actor.Stats = actor.Id == Player.Id && options.StatsMode != StatsMode.Random
                    ? options.StatsMode == StatsMode.PointBuy ? options.PlayerStats : new CharacterStats() : CharacterStats.Generate(statRandom);
                if (actor.Role.Flags.HasFlag(RoleFlags.Backlash)) actor.Charges = options.NullifierCharges;
                if (actor.Role.Ability == AbilityKind.Forge && options.ForgerReadsClear)
                    actor.Role = CopyRole(actor.Role, actor.Role.Ability, actor.Role.Attack, actor.Role.Flags | RoleFlags.ReadsClear);
            }
            InitialCounts = new[] { Actors.Count(c => c.Role.Faction == Faction.Town), Actors.Count(c => c.Role.Faction == Faction.Antagonist), Actors.Count(c => c.Role.Faction == Faction.Neutral) };
        }
        private readonly Dictionary<CharacterId, Tuple<string, CouncilInferenceResult>> _beliefCache = new Dictionary<CharacterId, Tuple<string, CouncilInferenceResult>>();
        internal CouncilOptions Options { get; }
        internal IReadOnlyList<RoleSpec> Catalog { get; }
        internal List<CouncilActor> Actors { get; }
        internal List<CouncilEvent> Events { get; } = new List<CouncilEvent>();
        internal IRandomSource Decisions { get; }
        internal int[] InitialCounts { get; }
        internal int Day { get; set; } = 1;
        internal int Round { get; set; } = 1;
        internal int Revision { get; set; }
        internal CouncilPhase Phase { get; set; } = CouncilPhase.Discussion;
        internal OutcomeKind Outcome { get; set; }
        internal CharacterId? Eliminated { get; set; }
        internal CouncilActor Player => Actors[0];
        internal IEnumerable<CouncilActor> Living => Actors.Where(a => a.Alive);
        internal CouncilActor Actor(CharacterId id) => Actors.Single(a => a.Id == id);
        internal IReadOnlyList<CouncilEvent> PublicEvents => Events.Where(e => e.Public).Select((e, i) => e.Scoped(i)).ToList().AsReadOnly();
        internal void Emit(CouncilEventKind kind, string text, CharacterId? actor = null, CharacterId? target = null,
            SpeechIntent? intent = null, ReasonType reason = ReasonType.None, RoleId? claim = null,
            Readout? readout = null, CharacterId? recipient = null, string lineId = null, bool? patientAttacked = null)
            => Events.Add(new CouncilEvent(Events.Count, Day, Round, kind, text, actor, target, intent, reason, claim, readout, recipient, lineId, patientAttacked));
        internal void Trace(CouncilActor actor, string kind, CharacterId? target, ReasonType reason = ReasonType.None)
        {
            var k = Knowledge(actor); CouncilBelief belief = null;
            if (target.HasValue) k.Inference.Beliefs.TryGetValue(target.Value, out belief);
            string why = actor.Name + " · " + kind + (target.HasValue ? " → " + Actor(target.Value).Name : " → Pass")
                + " · " + reason + (belief == null ? "" : " · estimated hostile " + Math.Round(belief.Hostile * 100) + "%")
                + " · pressure " + Math.Round(k.Inference.Exposure * 100) + "%";
            Emit(CouncilEventKind.Decision, why, actor.Id, target, reason: reason, recipient: actor.Id);
        }
        internal RoleId? Claim(CharacterId actor) => Events.LastOrDefault(e => e.Public && e.Actor == actor && e.Kind == CouncilEventKind.Claim)?.Claim;
        internal RoleId? Revealed(CharacterId actor) => Events.LastOrDefault(e => e.Kind == CouncilEventKind.RoleReveal && e.Target == actor)?.Claim;
        internal CouncilKnowledge Knowledge(CouncilActor observer, IReadOnlyList<CharacterId> voteTargets = null)
        {
            var characters = Actors.Select(actor => {
                RoleId? revealed = Revealed(actor.Id);
                RoleSpec known = revealed.HasValue ? Catalog.FirstOrDefault(r => r.Id == revealed.Value) : null;
                return new KnownCharacter(actor.Id, actor.Name, actor.Alive, actor.Id == observer.Id ? 0 : 35,
                    observer.Role.Faction == Faction.Antagonist && actor.Role.Faction == Faction.Antagonist && actor.Id != observer.Id,
                    revealed, known?.Faction, Claim(actor.Id));
            }).ToList().AsReadOnly();
            SetupKnowledge counts = Options.Visibility == SetupVisibility.Hidden ? null
                : new SetupKnowledge(InitialCounts[0], InitialCounts[1], InitialCounts[2]);
            IReadOnlyDictionary<RoleId, int> roles = Options.Visibility != SetupVisibility.Full ? null
                : new ReadOnlyDictionary<RoleId, int>(Actors.GroupBy(a => a.StartingRole.Id).ToDictionary(g => g.Key, g => g.Count()));
            var stats = new ReadOnlyDictionary<CharacterId, CharacterStats>(Actors.ToDictionary(a => a.Id, a => a.Stats));
            var events = Events.Where(e => e.Public || e.Kind == CouncilEventKind.FreeTime && e.Recipient == observer.Id)
                .Select((e, i) => e.Scoped(i)).ToList().AsReadOnly();
            var raw = new CouncilKnowledge(observer.Id, observer.Role, observer.Traits, Day, Round, observer.Charges,
                observer.SelfHealUsed, characters, events, observer.Results.ToList().AsReadOnly(), Catalog, counts, roles,
                voteTargets, stats, Options.Ai, Options.Seed, selfHealAllowed: Options.SelfHeal);
            string stamp = Day + ":" + Round + ":" + events.Count + ":" + observer.Results.Count;
            if (!_beliefCache.TryGetValue(observer.Id, out var cached) || cached.Item1 != stamp)
            {
                cached = Tuple.Create(stamp, new CouncilInference().Evaluate(raw));
                _beliefCache[observer.Id] = cached;
            }
            return new CouncilKnowledge(observer.Id, observer.Role, observer.Traits, Day, Round, observer.Charges,
                observer.SelfHealUsed, characters, events, raw.Results, Catalog, counts, roles,
                voteTargets, stats, Options.Ai, Options.Seed, cached.Item2, Options.SelfHeal);
        }

        internal static RoleSpec CopyRole(RoleSpec role, AbilityKind ability, int attack, RoleFlags flags) =>
            new RoleSpec(role.Id, role.Name, role.Faction, role.Category, ability, attack, role.Defense, role.Protection,
                role.Charges, role.Appearance, role.Unique, role.CanClaimTruthfully, role.CanBluff, flags, role.Objective);
    }
}
