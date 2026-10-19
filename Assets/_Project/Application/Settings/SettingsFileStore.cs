using System;
using System.IO;
using System.Text;

namespace SocialStim.Application.Settings
{
    /// <summary>Path-injected temporary-file adapter, independent of Unity APIs.</summary>
    public sealed class SettingsFileStore : ISettingsStore
    {
        private readonly string _path;
        public SettingsFileStore(string path)
        { if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Settings path missing."); _path = path; }
        public string Read() => File.Exists(_path) ? File.ReadAllText(_path, Encoding.UTF8) : null;
        public void Write(string json)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            if (!File.Exists(_path)) File.Move(temporary, _path);
            else
            {
                try { File.Replace(temporary, _path, null); }
                catch (PlatformNotSupportedException) { File.Copy(temporary, _path, true); File.Delete(temporary); }
            }
        }
    }
}
