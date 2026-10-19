using System.Runtime.Serialization;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Data.Dialogue
{
    [DataContract]
    internal sealed class DialogueMetadataRecord
    {
        [DataMember(Name = "emotionKey")] private string _emotion = default;
        [DataMember(Name = "portraitKey")] private string _portrait = default;
        [DataMember(Name = "audioKey")] private string _audio = default;

        internal DialogueMetadata ToMetadata() => new DialogueMetadata(_emotion, _portrait, _audio);
    }
}
