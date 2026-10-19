using System;
using System.Runtime.Serialization;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;

namespace SocialStim.Data.Dialogue
{
    [DataContract]
    internal sealed class DialogueRecord
    {
        [DataMember(Name = "id", IsRequired = true)] private string _id = default;
        [DataMember(Name = "intent", IsRequired = true)] private string _intent = default;
        [DataMember(Name = "text", IsRequired = true)] private string _text = default;
        [DataMember(Name = "weight", IsRequired = true)] private int _weight = default;
        [DataMember(Name = "requiredTrait")] private string _requiredTrait = default;
        [DataMember(Name = "speakerRole")] private string _speakerRole = default;
        [DataMember(Name = "metadata")] private DialogueMetadataRecord _metadata = default;

        internal DialogueLine ToLine()
        {
            if (!Enum.TryParse(_intent, true, out SpeechIntent intent) || !Enum.IsDefined(typeof(SpeechIntent), intent))
                throw new ArgumentException($"{_id}: unknown intent {_intent}.");
            Personality trait = Personality.None;
            if (!string.IsNullOrEmpty(_requiredTrait) && (!Enum.TryParse(_requiredTrait, true, out trait)
                || (trait & ~(Personality.Aggressive | Personality.Cautious | Personality.Gullible | Personality.Loyal | Personality.Deceptive)) != 0))
                throw new ArgumentException($"{_id}: unknown speaker trait.");
            return new DialogueLine(_id, intent, _text, _weight, trait,
                string.IsNullOrWhiteSpace(_speakerRole) ? (RoleId?)null : new RoleId(_speakerRole), _metadata?.ToMetadata());
        }
    }
}
