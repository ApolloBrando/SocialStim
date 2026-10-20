using System;
using System.IO;
using System.Text;

namespace SocialStim.Application.Settings
{
    /// <summary>Atomic preference-file persistence using an injected path, independent of Unity and run saves.</summary>
    public sealed class SettingsStore
    {
        private readonly string _path;
        /// <summary>Creates a file store; the Unity adapter supplies persistentDataPath.</summary>
        public SettingsStore(string path) { _path = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path))); }
        /// <summary>Whether a file already exists; used for one-time legacy migration.</summary>
        public bool Exists => File.Exists(_path);
        /// <summary>Loads preferences or returns defaults and an actionable warning without rewriting a bad file.</summary>
        public GamePreferences Load(out string warning)
        {
            warning = null;
            if (!Exists) return new GamePreferences();
            try
            {
                if (new FileInfo(_path).Length > 65536) throw new ArgumentException("Settings file exceeds 64 KB.");
                return SettingsCodec.Decode(File.ReadAllText(_path, Encoding.UTF8));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { warning = "Settings could not be loaded: " + e.Message; return new GamePreferences(); }
        }
        /// <summary>Writes and flushes a sibling temporary file before atomically replacing the existing file.</summary>
        public void Save(GamePreferences preferences)
        {
            string json = SettingsCodec.Encode(preferences);
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            string temporary = _path + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(json); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                if (Exists) File.Replace(temporary, _path, null); else File.Move(temporary, _path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
