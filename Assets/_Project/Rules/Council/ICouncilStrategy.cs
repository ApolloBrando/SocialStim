using SocialStim.Rules.Cast;
using SocialStim.Rules.Randomness;

namespace SocialStim.Rules.Council
{
    /// <summary>Replaceable strategy with only observer-authorized inputs.</summary>
    public interface ICouncilStrategy
    {
        CouncilAction Speak(CouncilKnowledge knowledge, IRandomSource random);
        CharacterId? Vote(CouncilKnowledge knowledge, IRandomSource random);
        CharacterId? NightTarget(CouncilKnowledge knowledge, IRandomSource random);
    }
}
