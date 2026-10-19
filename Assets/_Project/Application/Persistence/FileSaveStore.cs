using System;
using System.IO;

namespace SocialStim.Application.Persistence
{
    /// <summary>Atomic local slots with one previous-version backup. Failed writes preserve the old primary.</summary>
    public sealed class FileSaveStore
    {
        private readonly string _root;
        private const int MaximumBytes = 8 * 1024 * 1024 + 1024;
        public FileSaveStore(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Save root is required.");
            _root = Path.GetFullPath(root);
        }
        private string Location(SaveSlot slot)
        {
            if (!Enum.IsDefined(typeof(SaveSlot), slot)) throw new ArgumentException("Unknown save slot.");
            return Path.Combine(_root, slot == SaveSlot.Run ? "run.sstim" : "settings.sstim");
        }
        public bool Exists(SaveSlot slot) => File.Exists(Location(slot));
        public bool HasBackup(SaveSlot slot) => File.Exists(Location(slot) + ".bak");
        public byte[] Read(SaveSlot slot, bool backup = false)
        {
            string path = Location(slot) + (backup ? ".bak" : "");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 1 || stream.Length > MaximumBytes) throw new InvalidDataException("Invalid save file size.");
                var bytes = new byte[(int)stream.Length]; int read = 0;
                while (read < bytes.Length)
                {
                    int amount = stream.Read(bytes, read, bytes.Length - read);
                    if (amount == 0) throw new EndOfStreamException("Truncated save file.");
                    read += amount;
                }
                return bytes;
            }
        }
        public void Write(SaveSlot slot, byte[] bytes, bool preserveBackup = false)
        {
            if (bytes == null || bytes.Length < 1 || bytes.Length > MaximumBytes) throw new InvalidDataException("Invalid save payload size.");
            string destination = Location(slot);
            Directory.CreateDirectory(_root);
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(destination)) File.Replace(temporary, destination, preserveBackup ? null : destination + ".bak");
                else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
