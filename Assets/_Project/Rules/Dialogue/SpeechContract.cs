using SocialStim.Rules.Phases;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Shared semantic requirements for command validation and authored conditions.</summary>
    public static class SpeechContract
    {
        /// <summary>Whether the intent requires another living participant as its subject.</summary>
        public static bool RequiresTarget(SpeechIntent intent) => intent == SpeechIntent.Accuse || intent == SpeechIntent.Vouch
            || intent == SpeechIntent.CallForTrial || intent == SpeechIntent.Deflect || intent == SpeechIntent.ReportInvestigation
            || intent == SpeechIntent.Agree || intent == SpeechIntent.Disagree;

        /// <summary>Recorded action reports may describe passing or a self/dead/ordinary target.</summary>
        public static bool AllowsOptionalTarget(SpeechIntent intent) => intent == SpeechIntent.ReportAction;

        /// <summary>Whether the intent necessarily asserts a role or investigation result.</summary>
        public static bool HasAssertion(SpeechIntent intent) => intent == SpeechIntent.ClaimRole || intent == SpeechIntent.CounterClaim || intent == SpeechIntent.ReportInvestigation || intent == SpeechIntent.ReportAction;

        /// <summary>Actual authored-speech entry phases supported by the current participant loop.</summary>
        public static bool AllowsPhase(SpeechIntent intent, GamePhase phase) => phase == GamePhase.Discussion
            ? intent != SpeechIntent.LastWords : phase == GamePhase.Defense
            ? intent == SpeechIntent.Defend || intent == SpeechIntent.ClaimRole
            : phase == GamePhase.Execution ? intent == SpeechIntent.ReactToDeath || intent == SpeechIntent.ReactToRoleReveal
            : phase == GamePhase.LastWords && intent == SpeechIntent.LastWords;
    }
}
