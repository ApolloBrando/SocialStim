using System;
using System.IO;
using SocialStim.Application.Persistence;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Sessions;
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
        private TextGameView _view;
        private GamePresenter _presenter;
        private RunSession _session;
        private FileSaveStore _saves;

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
                _saves = new FileSaveStore(Path.Combine(UnityEngine.Application.persistentDataPath, "SocialStim"));
                var settings = new EffectsSettings();
                string settingsError = null;
                try { if (_saves.Exists(SaveSlot.Settings)) settings = EffectsSettingsCodec.Load(_saves.Read(SaveSlot.Settings)); }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException || e is NotSupportedException)
                { settingsError = "Saved settings could not be loaded; defaults are in use. " + e.Message; }
                _view = gameObject.AddComponent<TextGameView>();
                _view.Initialize(font, _effects != null ? _effects.Load() : null, settings);
                _view.SaveRequested += SaveRun;
                _view.LoadRequested += LoadRun;
                _view.SettingsChanged += SaveSettings;
                _view.RestartRequested += ShowSetup;
                ShowSetup();
                if (settingsError != null) _view.ShowInputError(settingsError);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
#endif
        }

        private void ShowSetup()
        {
            _presenter?.Dispose(); _presenter = null; _session = null;
            _view.ShowSetup(_catalog.LoadRoles(), _randomSeed ? "Random seed per run" : "Fixed seed " + unchecked((uint)_fixedSeed), Begin);
            _view.SetSaveAvailable(_saves != null && (_saves.Exists(SaveSlot.Run) || _saves.HasBackup(SaveSlot.Run)));
        }

        private void Begin(RoleId? chosenRole)
        {
            uint seed = _randomSeed ? BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) : unchecked((uint)_fixedSeed);
            var roles = _catalog.LoadRoles();
            var options = new RunOptions(seed, chosenRole, _trialsPerDay, _discussionRounds, _revealOnDeath);
            var session = RunFactory.Create(options, roles, _catalog.LoadNames(), _catalog.LoadDialogue(), reasoning: _catalog.LoadReasoning(), setup: _catalog.LoadSetup());
            _session = session;
            _presenter = new GamePresenter(new GameController(session, roles), _view, _view);
        }

        private void SaveRun()
        {
            if (_session == null || _view.IsDisplaying) return;
            try
            {
                RunSaveService.Save(_saves, _session, _catalog.LoadRoles(), _catalog.LoadDialogue(), _catalog.LoadReasoning());
                _view.SetSaveAvailable(true);
                _view.ShowInputError("Run saved.");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException)
            { _view.ShowInputError("Save failed; the previous save was preserved. " + e.Message); }
        }

        private void LoadRun()
        {
            if (_view.IsDisplaying) return;
            try
            {
                var roles = _catalog.LoadRoles();
                var lines = _catalog.LoadDialogue();
                var reasoning = _catalog.LoadReasoning();
                RunSession loaded = RunSaveService.Load(_saves, roles, lines, reasoning, out bool recovered);
                var controller = new GameController(loaded, roles);
                _ = controller.Request();
                _presenter?.Dispose();
                _view.PrepareLoadedRun();
                _session = loaded;
                _presenter = new GamePresenter(controller, _view, _view);
                _view.RevealLoadedHistory();
                _view.SetSaveAvailable(true);
                _view.ShowInputError(recovered ? "Recovered the previous valid save from backup." : "Run loaded.");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException)
            { _view.ShowInputError("Load failed; the current run is unchanged. " + e.Message); }
        }

        private void SaveSettings(EffectsSettings settings)
        {
            try { _saves.Write(SaveSlot.Settings, EffectsSettingsCodec.Save(settings)); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            { _view.ShowInputError("Settings could not be saved. " + e.Message); }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            if (_view != null)
            {
                _view.RestartRequested -= ShowSetup;
                _view.SaveRequested -= SaveRun;
                _view.LoadRequested -= LoadRun;
                _view.SettingsChanged -= SaveSettings;
            }
        }
    }
}
