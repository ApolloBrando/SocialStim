using System;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;

namespace SocialStim.Tests.EditMode
{
    internal sealed class ScenarioStrategy : INpcStrategy
    {
        internal CharacterId? Nominee { get; set; }
        internal Verdict Verdict { get; set; } = Verdict.Innocent;
        internal Func<KnowledgeView, CharacterId?> Target { get; set; } = _ => null;
        public GameCommand Speak(KnowledgeView knowledge, IRandomSource random) => GameCommand.Speak(SpeechIntent.StaySilent);
        public CharacterId? Nominate(KnowledgeView knowledge, IRandomSource random) => Nominee;
        public Verdict Judge(KnowledgeView knowledge, CharacterId defendant, IRandomSource random) => Verdict;
        public CharacterId? NightTarget(KnowledgeView knowledge, IRandomSource random) => Target(knowledge);
    }
}
