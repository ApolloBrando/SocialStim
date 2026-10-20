using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Application.Input
{
    /// <summary>Legal action/target navigation over the same application options used by command validation.</summary>
    public static class InteractionMenu
    {
        /// <summary>Returns legal options for an action and public roster target; null is Everyone/self as appropriate.</summary>
        public static IReadOnlyList<PlayerOption> Options(PlayerInputRequest request, InteractionAction action, CharacterId? target)
            => request.Options.Where(option => Matches(option, action) && (action == InteractionAction.Claim || option.Target == target)).ToList().AsReadOnly();
        /// <summary>Valid roster targets; no hidden character role or belief score is consulted.</summary>
        public static IReadOnlyList<CharacterId> Targets(PlayerInputRequest request, InteractionAction action)
            => request.Options.Where(option => Matches(option, action) && option.Target.HasValue).Select(option => option.Target.Value)
                .Distinct().OrderBy(id => id.Value).ToList().AsReadOnly();
        /// <summary>Whether a contextual action has any valid command at this moment.</summary>
        public static bool Available(PlayerInputRequest request, InteractionAction action) => request.Options.Any(option => Matches(option, action));
        private static bool Matches(PlayerOption option, InteractionAction action)
        {
            switch (action)
            {
                case InteractionAction.Claim: return option.Intent == SpeechIntent.ClaimRole;
                case InteractionAction.Accuse: return option.Intent == SpeechIntent.Accuse || option.Intent == SpeechIntent.Deflect || option.Intent == SpeechIntent.CallForTrial;
                case InteractionAction.Defend: return option.Intent == SpeechIntent.Defend || option.Intent == SpeechIntent.Vouch;
                default: return option.Intent == SpeechIntent.ExpressFear || option.Intent == SpeechIntent.Agree || option.Intent == SpeechIntent.Disagree
                    || option.Intent == SpeechIntent.ReportInvestigation || option.Command.Kind == SocialStim.Rules.Commands.CommandKind.ReportLastAction;
            }
        }
    }
}
