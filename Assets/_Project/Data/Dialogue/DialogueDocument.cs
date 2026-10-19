using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;

namespace SocialStim.Data.Dialogue
{
    /// <summary>Versioned, hand-authored JSON document; no Unity runtime dependency.</summary>
    [DataContract]
    public sealed class DialogueDocument
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)] private int _schemaVersion = default;
        [DataMember(Name = "lines", IsRequired = true)] private DialogueRecord[] _lines = default;

        /// <summary>Parses definitions and validates eligibility fallbacks without consuming a run's stream.</summary>
        public static IReadOnlyList<DialogueLine> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Dialogue JSON is empty.");
            DialogueDocument document;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    document = (DialogueDocument)new DataContractJsonSerializer(typeof(DialogueDocument)).ReadObject(stream);
            }
            catch (SerializationException exception)
            { throw new ArgumentException("Malformed dialogue JSON or missing required fields.", exception); }
            if (document == null || document._schemaVersion != 1 || document._lines == null)
                throw new ArgumentException("Dialogue schemaVersion must be 1 and lines must be an array.");
            var lines = document._lines.Select(row => row?.ToLine() ?? throw new ArgumentException("Null dialogue row.")).ToList().AsReadOnly();
            _ = new DialogueSelector(lines, new SeededRandom(1));
            return lines;
        }
    }
}
