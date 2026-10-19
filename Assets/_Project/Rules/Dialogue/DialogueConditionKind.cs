namespace SocialStim.Rules.Dialogue
{
    /// <summary>Registered selection predicates operating only on the speaker's permitted context.</summary>
    public enum DialogueConditionKind { Phase, MinimumDay, MaximumDay, HasTarget, HasAssertion, SpeakerTrait, SpeakerRole, ObservedDeath, ObservedRoleReveal, TargetAccused, TargetHasClaim, SpeakerUnderPressure }
}
