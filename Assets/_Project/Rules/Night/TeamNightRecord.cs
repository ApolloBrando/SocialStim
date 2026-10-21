using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Night
{
    /// <summary>Recorded Mafia action, shared privately with that faction after resolution.</summary>
    public sealed class TeamNightRecord
    {
        internal TeamNightRecord(CharacterId actor, string name, NightActionClaim action, CharacterId? visitTarget)
        { Actor = actor; Name = name; Action = action; VisitTarget = visitTarget; }
        public CharacterId Actor { get; }
        public string Name { get; }
        public NightActionClaim Action { get; }
        public CharacterId? VisitTarget { get; }
    }
}
