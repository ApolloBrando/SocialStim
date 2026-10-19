using System;
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
                    _catalog.LoadMissionRoles();
                    _council = gameObject.AddComponent<CouncilScreen>();
                    _council.NewRunRequested += ShowCouncilSetup;
                    _council.Initialize(font, UiPreferences.Load(), _effects != null ? _effects.Load() : null);
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
        {
            int count = Mathf.Max(5, _participantCount);
            _council.ShowSetup(_catalog.LoadMissionRoles(), count + " participants · " + _councilRounds + " rounds", BeginCouncil);
        }

        private void BeginCouncil(RoleId? chosenRole, Faction? chosenFaction)
        {
            try
            {
                uint seed = _randomSeed ? BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) : unchecked((uint)_fixedSeed);
                var options = new CouncilOptions(seed, _councilRounds, _playerWindows, revealRoles: _revealOnDeath);
                var setup = new SetupConfiguration(Mathf.Max(5, _participantCount), playerRole: chosenRole, playerFaction: chosenFaction);
                var session = CouncilFactory.Create(options, setup, _catalog.LoadMissionRoles(), _catalog.LoadNames(), _catalog.LoadDialogue());
                _council.Begin(session);
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
            if (_council != null) _council.NewRunRequested -= ShowCouncilSetup;
            if (_view != null)
            {
                _view.RestartRequested -= ShowSetup;
                _view.SettingsChanged -= SavePreferences;
            }
        }
    }
}
