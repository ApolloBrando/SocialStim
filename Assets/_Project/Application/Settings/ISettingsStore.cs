namespace SocialStim.Application.Settings
{
    /// <summary>Persistence boundary for preferences only.</summary>
    public interface ISettingsStore
    {
        string Read();
        void Write(string json);
    }
}
