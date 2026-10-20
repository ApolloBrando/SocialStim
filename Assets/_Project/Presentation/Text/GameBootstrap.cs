using System;
using SocialStim.Application.Sessions;
using SocialStim.Application.Settings;
using SocialStim.Presentation.Effects;
using SocialStim.Data.Definitions;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SocialStim.Presentation.Text
{
    /// <summary>Single scene composition root; only wires content, rules, input and views.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameCatalog _catalog;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private CueProfileDefinition _effects;
        [SerializeField] private TextStyleDefinition _textStyles;
        [SerializeField, Tooltip("Optional Input System action; cloned for the text view. Defaults to Space.")]
        private InputActionReference _advanceAction;
        [SerializeField, Tooltip("Optional close-modal action. Defaults to Escape / gamepad B.")]
        private InputActionReference _cancelAction;
        private SocialStim.Rules.Dialogue.TextTemplates _text;
        private bool _preferencesDirty;
        private float _saveAt;
        private GamePreferences _preferences;
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
                UiEventSystem.Ensure(transform);
                _view = gameObject.AddComponent<TextGameView>();
                _preferences = UiPreferences.Load(); _text = GameText.Load();
                _view.Initialize(font, _effects != null ? _effects.Load() : null, _preferences.Effects, _textStyles != null ? _textStyles.Load() : null, _text, _advanceAction, _cancelAction);
                _view.ApplyPreferences(_preferences);
                _view.RestartRequested += ShowSetup;
                _view.PreferencesChanged += SavePreferences;
                ShowSetup();
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
#endif
        }

        private void ShowSetup()
        {
            _presenter?.Dispose(); _presenter = null;
            _view.ShowSetup(_catalog.LoadRoles(), _text.Render("setup.summary", ("count", _preferences.Game.PlayerCount), ("seed", _preferences.Game.Seed.HasValue ? _text.Render("setup.seed", ("seed", _preferences.Game.Seed.Value)) : _text.Render("setup.randomSeed"))), Begin);
        }

        private void Begin(RoleId? chosenRole)
        {
            try
            {
                NewRunSettings setup = _preferences.Game;
                uint seed = setup.Seed ?? BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0);
                var roles = _catalog.LoadRoles();
                var options = new RunOptions(seed, chosenRole, setup.Trials, setup.Rounds, setup.Reveal,
                    votingMode: setup.Mode, playerWindowsPerRound: setup.Windows,
                    allowAbstain: setup.Abstain, revoteTies: setup.Revote, lastWords: setup.LastWords);
                var session = RunFactory.Create(_text, options, roles, _catalog.LoadNames(), _catalog.LoadDialogue(), reasoning: _catalog.LoadReasoning(), setup: _catalog.LoadSetup(setup.PlayerCount));
                _presenter = new GamePresenter(new GameController(session, roles), _view, _view);
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException)
            { _view.ShowInputError(_text.Render("setup.error", ("reason", e.Message))); }
        }

        private void SavePreferences(GamePreferences settings)
        {
            _preferences = settings; _preferencesDirty = true; _saveAt = Time.unscaledTime + 0.35f;
        }
        private void Update() { if (_preferencesDirty && Time.unscaledTime >= _saveAt) FlushPreferences(); }
        private void OnApplicationPause(bool paused) { if (paused) FlushPreferences(false); }
        private void OnApplicationQuit() => FlushPreferences(false);
        private void FlushPreferences(bool notify = true)
        {
            if (!_preferencesDirty) return; _preferencesDirty = false;
            try { UiPreferences.Store(_preferences); }
            catch (Exception exception) when (exception is System.IO.IOException || exception is UnauthorizedAccessException)
            { if (notify && _view != null) _view.ShowInputError(_text.Render("settings.saveError", ("reason", exception.Message))); else Debug.LogWarning(exception.Message); }
        }

        private void OnDestroy()
        {
            FlushPreferences(false);
            _presenter?.Dispose();
            if (_view != null)
            {
                _view.RestartRequested -= ShowSetup;
                _view.PreferencesChanged -= SavePreferences;
            }
        }
    }
}
