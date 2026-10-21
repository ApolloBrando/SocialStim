using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Night
{
    /// <summary>Validates the locked night then runs an explicitly composed ordered pipeline.</summary>
    public sealed class NightResolver
    {
        private readonly IReadOnlyList<INightStage> _stages;

        /// <summary>Creates the initial pipeline or a custom set with unique priorities.</summary>
        public NightResolver(IEnumerable<INightStage> stages = null)
        {
            var ordered = (stages ?? new INightStage[] { new RoleBlockStage(), new ForgeryStage(), new VisitStage(),
                new ProtectionStage(), new AttackStage(), new WatchStage(), new InvestigationStage() }).OrderBy(s => s.Priority).ToList();
            if (ordered.Count == 0 || ordered.Select(s => s.Priority).Distinct().Count() != ordered.Count)
                throw new ArgumentException("Night stages require unique priorities.", nameof(stages));
            _stages = ordered.AsReadOnly();
        }

        /// <summary>Resolves one batch; rejects duplicates, missing actors, illegal targets and multiple team kills.</summary>
        public NightResolution Resolve(IEnumerable<NightActor> actors, IEnumerable<NightOrder> orders)
        {
            var cast = actors.OrderBy(a => a.Id.Value).ToList().AsReadOnly();
            var locked = orders.OrderBy(a => a.Actor.Value).ToList().AsReadOnly();
            if (cast.Select(a => a.Id).Distinct().Count() != cast.Count ||
                locked.Select(o => o.Actor).Distinct().Count() != locked.Count)
                throw new ArgumentException("Duplicate night actor or order.");
            foreach (NightOrder order in locked)
                if (!NightLegality.IsLegal(cast.FirstOrDefault(a => a.Id == order.Actor), order, cast))
                    throw new ArgumentException("Illegal night order.");
            if (locked.Count(o => o.Target.HasValue && cast.First(a => a.Id == o.Actor).Role.Faction == Faction.Antagonist
                && cast.First(a => a.Id == o.Actor).Role.Ability == AbilityKind.Attack) > 1)
                throw new ArgumentException("The antagonist team has only one kill per night.");
            var context = new NightContext(cast, locked);
            foreach (INightStage stage in _stages) stage.Resolve(context);
            foreach (NightActor actor in cast.Where(a => a.GuiltDue)) context.Deaths.Add(actor.Id);
            return new NightResolution(context);
        }
    }
}
