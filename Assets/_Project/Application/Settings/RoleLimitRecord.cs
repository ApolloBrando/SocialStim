using System.Runtime.Serialization;

namespace SocialStim.Application.Settings
{
    /// <summary>Persistence DTO for role multiplicities, not assigned roles.</summary>
    [DataContract]
    internal sealed class RoleLimitRecord
    {
        [DataMember(Name = "role")] internal string Role = default;
        [DataMember(Name = "min")] internal int Minimum = default;
        [DataMember(Name = "max")] internal int Maximum = default;
    }
}
