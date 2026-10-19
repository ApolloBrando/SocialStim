using System;
using System.IO;
using System.Text;

namespace SocialStim.Rules.Persistence
{
    /// <summary>Bounded, explicit binary primitives; no object or type-name deserialization.</summary>
    internal static class SnapshotIO
    {
        internal const int MaximumBytes = 8 * 1024 * 1024;
        internal static void Text(BinaryWriter writer, string value)
        {
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(value ?? "");
            if (bytes.Length > 262144) throw new InvalidDataException("Snapshot text is too long.");
            writer.Write(bytes.Length); writer.Write(bytes);
        }
        internal static int Int(BinaryReader reader, int minimum, int maximum)
        {
            int value = reader.ReadInt32();
            if (value < minimum || value > maximum) throw new InvalidDataException("Snapshot integer is outside its valid range.");
            return value;
        }
        internal static long Weight(BinaryReader reader)
        {
            long value = reader.ReadInt64();
            if (value < 1 || value > 1000000) throw new InvalidDataException("Invalid belief weight.");
            return value;
        }
        internal static bool Bool(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid snapshot boolean.");
            return value != 0;
        }
        internal static string Text(BinaryReader reader, int maximumBytes = 262144)
        {
            int count = Int(reader, 0, maximumBytes);
            if (count > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Truncated snapshot string.");
            return new UTF8Encoding(false, true).GetString(reader.ReadBytes(count));
        }
        internal static T EnumValue<T>(BinaryReader reader) where T : struct
        {
            int value = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(T), value)) throw new InvalidDataException("Unknown snapshot enum value.");
            return (T)Enum.ToObject(typeof(T), value);
        }
        internal static T? OptionalEnum<T>(BinaryReader reader) where T : struct
            => Bool(reader) ? EnumValue<T>(reader) : (T?)null;
        internal static void OptionalEnum<T>(BinaryWriter writer, T? value) where T : struct
        { writer.Write(value.HasValue); if (value.HasValue) writer.Write(Convert.ToInt32(value.Value)); }
    }
}
