using System.Runtime.Serialization;

namespace SocialStim.Data.Dialogue
{
    /// <summary>One authored string; keys stay stable when wording changes.</summary>
    [DataContract]
    public sealed class GameTextEntry
    {
        [DataMember(Name = "key", IsRequired = true)] public string Key { get; private set; }
        [DataMember(Name = "text", IsRequired = true)] public string Text { get; private set; }
    }
}
