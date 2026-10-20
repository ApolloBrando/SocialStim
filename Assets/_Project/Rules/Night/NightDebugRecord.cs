using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Night
{
    /// <summary>Privileged testing data. Never place this in PlayerView, transcripts, evidence or strategy inputs.</summary>
    public sealed class NightDebugRecord
    {
        internal NightDebugRecord(CharacterId actor, string name, NightActionClaim action, CharacterId? visitTarget, bool spentCharge)
        { Actor = actor; Name = name; Action = action; VisitTarget = visitTarget; SpentCharge = spentCharge; }
        public CharacterId Actor { get; }
        public string Name { get; }
        public NightActionClaim Action { get; }
        public CharacterId? VisitTarget { get; }
        public bool SpentCharge { get; }
    }
}
