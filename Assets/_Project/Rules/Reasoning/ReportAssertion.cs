using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    internal sealed class ReportAssertion
    {
        internal ReportAssertion(CharacterId speaker, CharacterId target, int night, Faction alignment)
        { Speaker = speaker; Target = target; Night = night; Alignment = alignment; }
        internal CharacterId Speaker { get; }
        internal CharacterId Target { get; }
        internal int Night { get; }
        internal Faction Alignment { get; }
    }
}
