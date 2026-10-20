using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.Input
{
    /// <summary>Looks up authored phase and ability labels without hardcoded display names.</summary>
    public static class ActionLabels
    {
        /// <summary>Readable current phase from editable copy.</summary>
        public static string Phase(TextTemplates text, GamePhase phase) => text.Render("label.phase." + phase);
        /// <summary>Readable night action from editable copy.</summary>
        public static string Ability(TextTemplates text, AbilityKind ability) => text.Render("label.ability." + ability);
    }
}
