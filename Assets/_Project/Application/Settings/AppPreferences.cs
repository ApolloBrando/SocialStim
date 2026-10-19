namespace SocialStim.Application.Settings
{
    /// <summary>Preferences only: no resumable gameplay state.</summary>
    public sealed class AppPreferences
    {
        public AppPreferences(DisplayPreferences display, GamePreferences game) { Display = display; Game = game; }
        public DisplayPreferences Display { get; }
        public GamePreferences Game { get; }
        public static AppPreferences Defaults(int capacity) => new AppPreferences(new DisplayPreferences(), new GamePreferences(capacity));
        public AppPreferences WithDisplay(DisplayPreferences display) => new AppPreferences(display, Game);
        public AppPreferences WithGame(GamePreferences game) => new AppPreferences(Display, game);
    }
}
