using System;
using System.Runtime.Serialization;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Data.Dialogue
{
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
}
