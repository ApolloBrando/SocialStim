using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Night
{
    /// <summary>Mutable pipeline context, created only after command validation.</summary>
    public sealed class NightContext
    {
        internal NightContext(IReadOnlyList<NightActor> actors, IReadOnlyList<NightOrder> orders)
        {
            Actors = actors; Orders = orders;
            foreach (NightActor actor in actors) Defense.Add(actor.Id, actor.Role.Defense);
        }
        /// <summary>Living actors at selection time, in stable order.</summary>
        public IReadOnlyList<NightActor> Actors { get; }
        /// <summary>Validated accepted selections in stable actor order.</summary>
        public IReadOnlyList<NightOrder> Orders { get; }
        /// <summary>Actors whose non-block abilities do not execute.</summary>
        public HashSet<CharacterId> Blocked { get; } = new HashSet<CharacterId>();
        /// <summary>Temporary effective defense per participant.</summary>
        public Dictionary<CharacterId, int> Defense { get; } = new Dictionary<CharacterId, int>();
        /// <summary>Deaths are accumulated before simultaneous cleanup.</summary>
        public HashSet<CharacterId> Deaths { get; } = new HashSet<CharacterId>();
        /// <summary>Actors spending one limited charge.</summary>
        public HashSet<CharacterId> SpentCharges { get; } = new HashSet<CharacterId>();
        /// <summary>Actual visitors; these are not public observations.</summary>
        public List<Visit> Visits { get; } = new List<Visit>();
        /// <summary>Scoped ability reports, filtered before delivery.</summary>
        public List<NightReport> Reports { get; } = new List<NightReport>();
        /// <summary>Looks up a validated actor.</summary>
        public NightActor Actor(CharacterId id) => Actors.First(a => a.Id == id);
        /// <summary>Whether a non-block order executes after role-block.</summary>
        public bool Executes(NightOrder order) => order.Target.HasValue && !Blocked.Contains(order.Actor);
    }
}
