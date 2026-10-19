using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SocialStim.Application.Settings
{
    /// <summary>Versioned file shape with legacy root fields for migration.</summary>
    [DataContract]
    internal sealed class PreferencesRecord
    {
        [DataMember(Name = "version")] internal int Version = default;
        [DataMember(Name = "display")] internal Dictionary<string, float> Display = default;
        [DataMember(Name = "game")] internal Dictionary<string, int> Game = default;
        [DataMember(Name = "playerRole")] internal string PlayerRole = default;
        [DataMember(Name = "playerFaction")] internal int? PlayerFaction = default;
        [DataMember(Name = "seed")] internal string Seed = default;
        [DataMember(Name = "roles")] internal RoleLimitRecord[] Roles = default;
        [DataMember(Name = "slots")] internal string[] Slots = default;
        [DataMember(Name = "textSpeed", EmitDefaultValue = false)] internal float? TextSpeed = default;
        [DataMember(Name = "intensity", EmitDefaultValue = false)] internal float? Intensity = default;
        [DataMember(Name = "reducedMotion", EmitDefaultValue = false)] internal bool? ReducedMotion = default;
        [DataMember(Name = "flashes", EmitDefaultValue = false)] internal bool? Flashes = default;
        [DataMember(Name = "instant", EmitDefaultValue = false)] internal bool? Instant = default;
    }
}
