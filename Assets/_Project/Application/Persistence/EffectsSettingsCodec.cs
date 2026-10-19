using System.IO;
using SocialStim.Application.PresentationCues;

namespace SocialStim.Application.Persistence
{
    /// <summary>Small independently versioned accessibility/settings payload.</summary>
    public static class EffectsSettingsCodec
    {
        private const int Magic = 0x53535345;
        public static byte[] Save(EffectsSettings settings)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Magic); w.Write(1); w.Write(settings.Enabled); w.Write(settings.ReducedMotion); w.Write(settings.Flashes);
                w.Write(settings.Intensity); w.Write(settings.InstantText); w.Write(settings.ElementsPerSecond);
                return stream.ToArray();
            }
        }
        public static EffectsSettings Load(byte[] data)
        {
            if (data == null || data.Length != 20) throw new InvalidDataException("Invalid settings size.");
            using (var stream = new MemoryStream(data, false))
            using (var r = new BinaryReader(stream))
            {
                if (r.ReadInt32() != Magic || r.ReadInt32() != 1) throw new InvalidDataException("Unsupported settings version.");
                bool enabled = ReadBool(r), reduced = ReadBool(r), flashes = ReadBool(r);
                float intensity = r.ReadSingle(); bool instant = ReadBool(r); float speed = r.ReadSingle();
                return new EffectsSettings(enabled, reduced, flashes, intensity, instant, speed);
            }
        }
        private static bool ReadBool(BinaryReader r)
        {
            byte value = r.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid settings flag.");
            return value != 0;
        }
    }
}
