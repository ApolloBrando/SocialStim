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
            GuiltTriggered = context.GuiltTriggered.OrderBy(id => id.Value).ToList().AsReadOnly();
            Deaths = context.Deaths.OrderBy(id => id.Value).ToList().AsReadOnly();
            SpentCharges = context.SpentCharges.OrderBy(id => id.Value).ToList().AsReadOnly();
            Visits = context.Visits.AsReadOnly(); Reports = context.Reports.AsReadOnly();
        }
        /// <summary>Simultaneous casualties.</summary>
        public IReadOnlyList<CharacterId> GuiltTriggered { get; }
        public IReadOnlyList<CharacterId> Deaths { get; }
        /// <summary>Actors whose limited resource was consumed.</summary>
        public IReadOnlyList<CharacterId> SpentCharges { get; }
        /// <summary>Private authoritative visits, not a public log.</summary>
        public IReadOnlyList<Visit> Visits { get; }
        /// <summary>Reports to route only to living recipients.</summary>
        public IReadOnlyList<NightReport> Reports { get; }
    }
}
