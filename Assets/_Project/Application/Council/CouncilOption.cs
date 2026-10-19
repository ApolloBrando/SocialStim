using SocialStim.Rules.Council;

namespace SocialStim.Application.Council
{
    /// <summary>Portable option data; navigation is distinct from a gameplay action.</summary>
    public sealed class CouncilOption
    {
        internal CouncilOption(string id, string title, string detail, CouncilAction action = null,
            CouncilActionKind? navigate = null, bool back = false, bool claims = false)
        { Id = id; Title = title; Detail = detail; Action = action; Navigate = navigate; Back = back; Claims = claims; }
        public string Id { get; }
        public string Title { get; }
        public string Detail { get; }
        public CouncilAction Action { get; }
        internal CouncilActionKind? Navigate { get; }
        internal bool Back { get; }
        internal bool Claims { get; }
    }
}
