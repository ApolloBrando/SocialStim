namespace SocialStim.Rules.Dialogue
{
    /// <summary>Shared semantic requirements for command validation and authored conditions.</summary>
    public static class SpeechContract
    {
        /// <summary>Whether the intent requires another living participant as its subject.</summary>
        public static bool RequiresTarget(SpeechIntent intent) => intent == SpeechIntent.Accuse || intent == SpeechIntent.Vouch
            || intent == SpeechIntent.CallForTrial || intent == SpeechIntent.Deflect || intent == SpeechIntent.ReportInvestigation
            || intent == SpeechIntent.Agree || intent == SpeechIntent.Disagree;

        /// <summary>Whether the intent necessarily asserts a role or investigation result.</summary>
        public static bool HasAssertion(SpeechIntent intent) => intent == SpeechIntent.ClaimRole || intent == SpeechIntent.ReportInvestigation;
    }
}
