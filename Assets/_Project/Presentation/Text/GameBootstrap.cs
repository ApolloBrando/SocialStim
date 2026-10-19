using System;
using System.IO;
using SocialStim.Application.Settings;
using SocialStim.Application.Sessions;
using SocialStim.Presentation.Council;
using SocialStim.Rules.Council;
using SocialStim.Presentation.Effects;
using SocialStim.Data.Definitions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Single scene composition root; only wires content, rules, input and views.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameCatalog _catalog;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private CueProfileDefinition _effects;
        [SerializeField] private bool _randomSeed = true;
        [SerializeField] private int _fixedSeed = 147;
        [SerializeField, Range(1, 10)] private int _trialsPerDay = 2;
        [SerializeField, Range(1, 10)] private int _discussionRounds = 3;
        [SerializeField] private bool _revealOnDeath = true;
        [Tooltip("Total participants, including you. Zero uses the exact authored role slots.")]
        [SerializeField, Min(0)] private int _participantCount = 9;
        [SerializeField] private bool _trialMode;
        [SerializeField, Range(3, 7)] private int _councilRounds = 5;
        [SerializeField, Range(1, 2)] private int _playerWindows = 1;
        private CouncilScreen _council;
        private SettingsService _settings;
        private float _saveAfter;
        private TextGameView _view;
        private GamePresenter _presenter;

        private void Start()
        {
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("SocialStim requires Active Input Handling = Input System Package (New) or Both. Set it in Player Settings and restart the editor.", this);
            return;
#else
            try
            {
                if (_catalog == null) throw new InvalidOperationException("Assign the starter game catalog.");
                TMP_FontAsset font = _font != null ? _font : TMP_Settings.defaultFontAsset;
                if (font == null) throw new InvalidOperationException("Import TMP Essential Resources using Window > TextMeshPro, or assign a TMP font asset on GameBootstrap.");
                _catalog.LoadRoles(); _catalog.LoadDialogue(); _catalog.LoadNames(); _catalog.LoadReasoning();
                var events = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                if (!_trialMode)
                {
                    var roles = _catalog.LoadMissionRoles();
                    string path = Path.Combine(UnityEngine.Application.persistentDataPath, "settings-v2.json");
                    bool firstLoad = !File.Exists(path);
                    _settings = new SettingsService(new SettingsFileStore(path), roles, _catalog.LoadNames().Count);
                    if (firstLoad)
                    {
                        _settings.BeginGameEdit();
                        _settings.Edit(GameSetting.PlayerCount, Mathf.Max(5, _participantCount));
                        _settings.Edit(GameSetting.Rounds, _councilRounds);
                        _settings.Edit(GameSetting.Windows, _playerWindows);
                        _settings.Edit(GameSetting.RevealRoles, _revealOnDeath ? 1 : 0);
                        if (!_randomSeed) _settings.Seed(unchecked((uint)_fixedSeed).ToString());
                        _settings.ApplyGame(out _);
                        MigrateDisplayPreferences();
                    }
                    _council = gameObject.AddComponent<CouncilScreen>();
                    _council.NewRunRequested += ShowCouncilSetup;
                    _council.SettingsRequested += ShowCouncilSettings;
                    _council.Initialize(font, _settings.Current.Display.Effects(), _effects != null ? _effects.Load() : null);
                    _settings.DisplayChanged += ApplyCouncilPreferences;
                    _council.ApplyPreferences(_settings.Current.Display);
                    return;
                }
                _view = gameObject.AddComponent<TextGameView>();
                _view.Initialize(font, _effects != null ? _effects.Load() : null, UiPreferences.Load());
                _view.RestartRequested += ShowSetup;
                _view.SettingsChanged += SavePreferences;
                ShowSetup();
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
#endif
        }

        private void ShowCouncilSetup()
        { _council.ShowGameSetup(_settings, _catalog.LoadMissionRoles(), BeginCouncil); }
        private void ShowCouncilSettings()
        { _council.ShowPreferences(_settings, _catalog.LoadMissionRoles()); }
        private void ApplyCouncilPreferences(DisplayPreferences preferences)
        { _council.ApplyPreferences(preferences); _saveAfter = Time.unscaledTime + 0.35f; }
        private void Update()
        {
            if (_settings == null || !_settings.Dirty || Time.unscaledTime < _saveAfter) return;
            string error = _settings.Flush();
            if (error != null) { Debug.LogWarning(error, this); _saveAfter = Time.unscaledTime + 10; }
        }
        private void MigrateDisplayPreferences()
        {
            if (PlayerPrefs.GetInt("SocialStim.UI.version", 0) != 1) return;
            var old = UiPreferences.Load();
            _settings.Display(DisplaySetting.Speed, Math.Abs(old.ElementsPerSecond - 28) < 0.001f ? 84 : old.ElementsPerSecond);
            _settings.Display(DisplaySetting.Effects, old.Enabled ? 1 : 0);
            _settings.Display(DisplaySetting.Intensity, old.Intensity);
            _settings.Display(DisplaySetting.ReducedMotion, old.ReducedMotion ? 1 : 0);
            _settings.Display(DisplaySetting.Flashes, old.Flashes ? 1 : 0);
            _settings.Display(DisplaySetting.Instant, old.InstantText ? 1 : 0);
        }
        private void BeginCouncil()
        {
            try
            {
                var game = _settings.Current.Game;
                uint seed = string.IsNullOrWhiteSpace(game.Seed) ? BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) : uint.Parse(game.Seed);
                var session = CouncilFactory.Create(game.Options(seed), game.Setup(), _catalog.LoadMissionRoles(), _catalog.LoadNames(), _catalog.LoadDialogue());
                _council.Begin(session, _settings.Current.Display.On(DisplaySetting.QuickMode));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            { Debug.LogException(exception, this); }
        }

        private void ShowSetup()
        {
            _presenter?.Dispose(); _presenter = null;
            _view.ShowSetup(_catalog.LoadRoles(), (_participantCount > 0 ? _participantCount.ToString() : _catalog.LoadSetup()?.Slots.Count.ToString() ?? "7")
                + " participants · " + (_randomSeed ? "Random seed" : "Seed " + unchecked((uint)_fixedSeed)), Begin);
        }

        private void Begin(RoleId? chosenRole)
        {
            try
            {
                uint seed = _randomSeed ? BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) : unchecked((uint)_fixedSeed);
                var roles = _catalog.LoadRoles();
                var options = new RunOptions(seed, chosenRole, _trialsPerDay, _discussionRounds, _revealOnDeath);
                var session = RunFactory.Create(options, roles, _catalog.LoadNames(), _catalog.LoadDialogue(), reasoning: _catalog.LoadReasoning(), setup: _catalog.LoadSetup(_participantCount));
                _presenter = new GamePresenter(new GameController(session, roles), _view, _view);
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException)
            { _view.ShowInputError("Invalid setup: " + e.Message); }
        }

        private void SavePreferences(SocialStim.Application.PresentationCues.EffectsSettings settings)
        {
            try { UiPreferences.Store(settings); }
            catch (PlayerPrefsException exception) { _view.ShowInputError("Preferences could not be kept: " + exception.Message); }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            if (_settings != null) { _settings.Flush(); _settings.DisplayChanged -= ApplyCouncilPreferences; }
            if (_council != null)
            { _council.NewRunRequested -= ShowCouncilSetup; _council.SettingsRequested -= ShowCouncilSettings; }
            if (_view != null)
            {
                _view.RestartRequested -= ShowSetup;
                _view.SettingsChanged -= SavePreferences;
            }
        }
    }
}
