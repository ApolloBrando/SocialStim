using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Data.Dialogue
{
    /// <summary>Loads editable UI, role-reference and factual report templates.</summary>
    [DataContract]
    public sealed class GameTextDocument
    {
        [DataMember(Name = "entries", IsRequired = true)] private GameTextEntry[] _entries = default;
        /// <summary>Validates a detached string catalog for rules/application/presentation consumers.</summary>
        public static GameText Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Empty game text catalog.");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var document = (GameTextDocument)new DataContractJsonSerializer(typeof(GameTextDocument)).ReadObject(stream);
                if (document?._entries == null || document._entries.Any(e => e == null)) throw new ArgumentException("Missing game text entries.");
                return new GameText(document._entries.Select(e => new System.Collections.Generic.KeyValuePair<string, string>(e.Key, e.Text)));
            }
        }
    }

    /// <summary>One authored string; keys stay stable when wording changes.</summary>
    [DataContract]
    public sealed class GameTextEntry
    {
        [DataMember(Name = "key", IsRequired = true)] public string Key { get; private set; }
        [DataMember(Name = "text", IsRequired = true)] public string Text { get; private set; }
    }
}
