using System;

namespace SocialStim.Rules.Roles
{
    /// <summary>Content-defined role identity; new definitions do not need an enum edit.</summary>
    public readonly struct RoleId : IEquatable<RoleId>
    {
        /// <summary>Creates a case-sensitive, nonempty role ID.</summary>
        public RoleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Role ID is required.", nameof(value));
            Value = value;
        }

        /// <summary>Stable content key.</summary>
        public string Value { get; }
        /// <inheritdoc />
        public bool Equals(RoleId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is RoleId other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        /// <inheritdoc />
        public override string ToString() => Value ?? string.Empty;
        /// <summary>Compares role IDs.</summary>
        public static bool operator ==(RoleId left, RoleId right) => left.Equals(right);
        /// <summary>Compares role IDs.</summary>
        public static bool operator !=(RoleId left, RoleId right) => !left.Equals(right);
    }
}
