using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Selects authored first-person ability accounts; claims never certify success.</summary>
    public static class NightActionText
    {
        /// <summary>Describes an accepted selection from the injected editable templates.</summary>
        public static string Selection(TextTemplates text, AbilityKind ability, string target)
            => text.Render(ability == AbilityKind.None ? "night.selection.None" : target == null
                ? "night.selection.Pass" : "night.selection." + ability, ("target", target));
        /// <summary>False claimants describe their publicly claimed ability, without another actor's results.</summary>
        public static string Claimed(TextTemplates text, AbilityKind ability) => text.Render("night.claimed." + ability);
    }
}
