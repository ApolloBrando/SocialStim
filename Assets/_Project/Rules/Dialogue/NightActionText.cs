using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Editable first-person night-action wording. Claims do not certify success.</summary>
    public static class NightActionText
    {
        /// <summary>Describes an accepted selection without claiming the ability succeeded.</summary>
        public static string Selection(AbilityKind ability, string target)
        {
            if (ability == AbilityKind.None) return "I had no night action.";
            if (target == null) return "I stayed in last night.";
            switch (ability)
            {
                case AbilityKind.Investigate: return "I tried to check on " + target + " last night.";
                case AbilityKind.Protect: return "I tried to protect " + target + " last night.";
                case AbilityKind.Block: return "I tried to block " + target + " last night.";
                case AbilityKind.Attack: return "I tried to attack " + target + " last night.";
                case AbilityKind.SelfProtect: return "I tried to keep myself safe last night.";
                case AbilityKind.Forge: return "I tried to plant evidence against " + target + " last night.";
                case AbilityKind.Watch: return "I tried to watch " + target + " last night.";
                default: return "I tried to use my ability on " + target + " last night.";
            }
        }

        /// <summary>A false claimant's account uses their public claimed ability, never private results.</summary>
        public static string Claimed(AbilityKind ability)
        {
            switch (ability)
            {
                case AbilityKind.None: return "I had no night action.";
                case AbilityKind.Investigate: return "I checked someone last night.";
                case AbilityKind.Protect: return "I tried to keep someone safe last night.";
                case AbilityKind.Block: return "I tried to stop someone last night.";
                case AbilityKind.Attack: return "I went after someone last night.";
                case AbilityKind.SelfProtect: return "I tried to keep myself safe last night.";
                case AbilityKind.Forge: return "I planted evidence last night.";
                case AbilityKind.Watch: return "I kept watch last night.";
                default: return "I used my ability last night.";
            }
        }
    }
}
