using System;

namespace SocialStim.Rules.Cast
{
    /// <summary>Stable, run-local participant identity.</summary>
    public readonly struct CharacterId : IEquatable<CharacterId>, IComparable<CharacterId>
    {
        /// <summary>Creates an identity from a nonnegative slot.</summary>
        public CharacterId(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        /// <summary>Serialized identity value, unrelated to a Unity instance ID.</summary>
        public int Value { get; }
        /// <inheritdoc />
        public bool Equals(CharacterId other) => Value == other.Value;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is CharacterId other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => Value;
        /// <inheritdoc />
        public int CompareTo(CharacterId other) => Value.CompareTo(other.Value);
        /// <inheritdoc />
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <summary>Compares identities by slot.</summary>
        public static bool operator ==(CharacterId left, CharacterId right) => left.Equals(right);
        /// <summary>Compares identities by slot.</summary>
        public static bool operator !=(CharacterId left, CharacterId right) => !left.Equals(right);
    }
}
