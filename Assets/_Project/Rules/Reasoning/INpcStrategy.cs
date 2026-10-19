using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Randomness;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Swappable decisions receiving restricted observations only.</summary>
    public interface INpcStrategy
    {
        /// <summary>Chooses an attributed statement.</summary>
        GameCommand Speak(KnowledgeView knowledge, IRandomSource random);
        /// <summary>Nominates one observed living target, or abstains.</summary>
        CharacterId? Nominate(KnowledgeView knowledge, IRandomSource random);
        /// <summary>Judges a public defendant.</summary>
        Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random);
        /// <summary>Selects an ability target, or passes.</summary>
        CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random);
    }
}
