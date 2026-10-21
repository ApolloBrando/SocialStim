using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Night
{
    /// <summary>Immutable authoritative batch result; the session filters recipient knowledge.</summary>
    public sealed class NightResolution
    {
        internal NightResolution(NightContext context)
        {
            TownKillers = context.TownKillers.OrderBy(id => id.Value).ToList().AsReadOnly();
            Deaths = context.Deaths.OrderBy(id => id.Value).ToList().AsReadOnly();
            SpentCharges = context.SpentCharges.OrderBy(id => id.Value).ToList().AsReadOnly();
            Visits = context.Visits.AsReadOnly(); Reports = context.Reports.AsReadOnly();
        }
        /// <summary>Actors to schedule for guilt next night, never the shooting night.</summary>
        public IReadOnlyList<CharacterId> TownKillers { get; }
        /// <summary>Simultaneous casualties.</summary>
        public IReadOnlyList<CharacterId> Deaths { get; }
        /// <summary>Actors whose limited resource was consumed.</summary>
        public IReadOnlyList<CharacterId> SpentCharges { get; }
        /// <summary>Private authoritative visits, not a public log.</summary>
        public IReadOnlyList<Visit> Visits { get; }
        /// <summary>Reports to route only to living recipients.</summary>
        public IReadOnlyList<NightReport> Reports { get; }
    }

    /// <summary>Actual visit record, retained by authoritative resolution.</summary>
    public sealed class Visit
    {
        /// <summary>Records a completed visit.</summary>
        public Visit(CharacterId visitor, CharacterId target) { Visitor = visitor; Target = target; }
        /// <summary>Visitor identity.</summary>
        public CharacterId Visitor { get; }
        /// <summary>Visited identity.</summary>
        public CharacterId Target { get; }
    }
}
