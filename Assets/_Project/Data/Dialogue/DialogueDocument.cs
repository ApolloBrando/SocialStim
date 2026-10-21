using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;

namespace SocialStim.Data.Dialogue
{
    /// <summary>Versioned, hand-authored JSON document; no Unity runtime dependency.</summary>
    [DataContract]
    public sealed class DialogueDocument
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)] private int _schemaVersion = default;
        [DataMember(Name = "lines", IsRequired = true)] private DialogueRecord[] _lines = default;

        /// <summary>Combines existing-schema files and validates IDs/fallbacks across the entire catalogue.</summary>
        public static IReadOnlyList<DialogueLine> ParseMany(IEnumerable<string> documents)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            var lines = documents.SelectMany(Parse).ToList().AsReadOnly();
            _ = new DialogueSelector(lines, new SeededRandom(1));
            return lines;
        }

        /// <summary>Parses definitions and validates eligibility fallbacks without consuming a run's stream.</summary>
        public static IReadOnlyList<DialogueLine> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Dialogue JSON is empty.");
            DialogueSchema.Validate(json);
            DialogueDocument document;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    document = (DialogueDocument)new DataContractJsonSerializer(typeof(DialogueDocument)).ReadObject(stream);
            }
            catch (SerializationException exception)
            { throw new ArgumentException("Malformed dialogue JSON or missing required fields.", exception); }
            if (document == null || (document._schemaVersion != 1 && document._schemaVersion != 2) || document._lines == null)
                throw new ArgumentException("Dialogue schemaVersion must be 1 or 2 and lines must be an array.");
            var lines = document._lines.Select(row => row?.ToLine() ?? throw new ArgumentException("Null dialogue row.")).ToList().AsReadOnly();
            _ = new DialogueSelector(lines, new SeededRandom(1));
            return lines;
        }
    }

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
        [DataMember(Name = "conditions")] private DialogueConditionRecord[] _conditions = default;

        internal DialogueLine ToLine()
        {
            if (!Enum.TryParse(_intent, true, out SpeechIntent intent) || !Enum.IsDefined(typeof(SpeechIntent), intent))
                throw new ArgumentException($"{_id}: unknown intent {_intent}.");
            Personality trait = Personality.None;
            if (!string.IsNullOrEmpty(_requiredTrait) && (!Enum.TryParse(_requiredTrait, true, out trait)
                || !Enum.IsDefined(typeof(Personality), trait)))
                throw new ArgumentException($"{_id}: unknown speaker trait.");
            try
            {
                return new DialogueLine(_id, intent, _text, _weight, trait,
                    string.IsNullOrWhiteSpace(_speakerRole) ? (RoleId?)null : new RoleId(_speakerRole), _metadata?.ToMetadata(),
                    _conditions?.Select(c => c?.ToCondition() ?? throw new ArgumentException("Null condition.")));
            }
            catch (ArgumentException exception) { throw new ArgumentException($"{_id}: {exception.Message}", exception); }
        }
    }

    [DataContract]
    internal sealed class DialogueConditionRecord
    {
        [DataMember(Name = "kind", IsRequired = true)] private string _kind = default;
        [DataMember(Name = "value")] private string _value = default;
        [DataMember(Name = "number")] private int? _number = default;
        [DataMember(Name = "flag")] private bool? _flag = default;

        internal DialogueCondition ToCondition()
        {
            if (!Enum.TryParse(_kind, true, out DialogueConditionKind kind) || !Enum.IsDefined(typeof(DialogueConditionKind), kind))
                throw new ArgumentException($"Unknown dialogue condition: {_kind}.");
            return new DialogueCondition(kind, _value, _number, _flag);
        }
    }

    [DataContract]
    internal sealed class DialogueMetadataRecord
    {
        [DataMember(Name = "emotionKey")] private string _emotion = default;
        [DataMember(Name = "portraitKey")] private string _portrait = default;
        [DataMember(Name = "audioKey")] private string _audio = default;

        internal DialogueMetadata ToMetadata() => new DialogueMetadata(_emotion, _portrait, _audio);
    }
}
