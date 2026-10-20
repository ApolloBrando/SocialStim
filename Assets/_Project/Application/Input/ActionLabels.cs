using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.Input
{
    /// <summary>Readable action-family labels shared by presentation clients.</summary>
    public static class ActionLabels
    {
        public static string Speech(SpeechIntent intent)
        {
            switch (intent)
            {
                case SpeechIntent.ClaimRole: return "Claim a role";
                case SpeechIntent.ReportInvestigation: return "Report an alignment";
                case SpeechIntent.ReportAction: return "Report a night action";
                case SpeechIntent.CallForTrial: return "Call for a trial";
                case SpeechIntent.StaySilent: return "Stay silent";
                case SpeechIntent.ExpressFear: return "Express fear";
                case SpeechIntent.ReactToDeath: return "React to a death";
                case SpeechIntent.ReactToRoleReveal: return "React to a reveal";
                case SpeechIntent.LastWords: return "Last words";
                case SpeechIntent.VoteJustification: return "Explain a vote";
                default: return intent.ToString();
            }
        }
        public static string Phase(GamePhase phase, GameText text = null)
        {
            switch (phase)
            {
                case GamePhase.Discussion: return "Discussion";
                case GamePhase.Nomination: return "Nominations";
                case GamePhase.Voting: return "Vote";
                case GamePhase.VoteReveal: return "Vote reveal";
                case GamePhase.Defense: return "Defense";
                case GamePhase.Judgement: return "Judgement";
                case GamePhase.LastWords: return "Last words";
                case GamePhase.JesterRevenge: return (text ?? GameText.Default).Format("ui.jesterRevenge");
                case GamePhase.Execution: return "Execution";
                case GamePhase.NightSelection:
                case GamePhase.NightResolution: return "Night";
                case GamePhase.Dawn: return "Morning";
                default: return "Run complete";
            }
        }
        public static string Ability(AbilityKind ability)
        {
            switch (ability)
            {
                case AbilityKind.SelfProtect: return "Self-protect";
                case AbilityKind.None: return "No night ability";
                case AbilityKind.Forge: return "Forge evidence";
                case AbilityKind.Watch: return "Watch visits";
                default: return ability.ToString();
            }
        }
    }
}
