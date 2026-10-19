using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Roles;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Minimal scoped text view; display timing never changes session state.</summary>
    public sealed class TextGameView : MonoBehaviour, IGameView, IPlayerInputProvider
    {
        private TMP_FontAsset _font;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _transcript;
        private RectTransform _choices;
        private ScrollRect _transcriptScroll;
        private ScrollRect _choiceScroll;
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<Button> _controls = new List<Button>();
        private readonly List<Image> _surfaces = new List<Image>();
        private readonly List<TextMeshProUGUI> _labels = new List<TextMeshProUGUI>();
        private readonly StringBuilder _shown = new StringBuilder();
        private readonly List<SemanticSpan> _spans = new List<SemanticSpan>();
        private readonly DisplayPlayback _playback = new DisplayPlayback();
        private VisibleCueResolver _cues = new VisibleCueResolver();
        private IReadOnlyDictionary<PresentationCue, EffectProfile> _profiles;
        private EffectsSettings _settings = new EffectsSettings();
        private PlayerInputRequest _request;
        private int _next;
        private int _currentStart;
        private TextPalette _palette;
        private TextEffectRenderer _textEffects;
        private ScreenEffectOverlay _screenEffects;
        private Action<DisplayAcknowledgement> _onAcknowledged;
        private Button _skip;
        private bool _wasDisplaying;
        private TextMeshProUGUI _claimSummary;
        private readonly Dictionary<CharacterId, string> _visibleClaims = new Dictionary<CharacterId, string>();
        private string _menuCategory;
        private string _menuFamily;
        private int _menuPage;
        private int _displayDay = 1;
        private Button _saveButton;
        private Button _loadButton;
        private bool _hasSave;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action<EffectsSettings> SettingsChanged;
        public EffectsSettings Settings => _settings;
        public event Action<string> OptionChosen;
        public event Action RestartRequested;
        public bool IsDisplaying => _playback.Busy || _request != null && _next < _request.View.Transcript.Count;

        public void Initialize(TMP_FontAsset font, IReadOnlyDictionary<PresentationCue, EffectProfile> profiles = null, EffectsSettings initialSettings = null)
        {
            _settings = initialSettings ?? new EffectsSettings();
            _font = font; _profiles = profiles ?? DefaultCueProfiles.Create();
            _onAcknowledged = acknowledgement =>
            {
                if (_request != null && _next < _request.View.Transcript.Count)
                {
                    GameObservation observation = _request.View.Transcript[_next];
                    if (observation.Intent == SpeechIntent.ClaimRole && observation.Speaker.HasValue && observation.ClaimedRole.HasValue)
                    {
                        string name = _request.View.Characters.First(c => c.Id == observation.Speaker.Value).Name;
                        _visibleClaims[observation.Speaker.Value] = name + ": " + observation.ClaimedRole.Value.Value;
                        _claimSummary.text = "ROLE CLAIMS (unverified)  |  " + string.Join("  |  ", _visibleClaims.OrderBy(c => c.Key.Value).Select(c => c.Value));
                    }
                }
                _next++;
            };
            var canvas = new GameObject("Text Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 0.5f;
            _surfaces.Add(canvas.AddComponent<Image>());
            _status = TextUiFactory.Text("Status", canvas.transform, font, 17); _labels.Add(_status);
            _claimSummary = TextUiFactory.Text("Role Claims Board", canvas.transform, font, 16); _labels.Add(_claimSummary);
            TextUiFactory.Fill(_claimSummary.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -166), new Vector2(-20, -112));
            TextUiFactory.Fill(_status.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -128), new Vector2(-380, -12));
            Button restart = TextUiFactory.Button(canvas.transform, font, "New run", 42);
            _controls.Add(restart);
            TextUiFactory.Fill((RectTransform)restart.transform, Vector2.one, Vector2.one, new Vector2(-140, -55), new Vector2(-20, -12));
            restart.onClick.AddListener(() => RestartRequested?.Invoke());
            _transcriptScroll = TextUiFactory.Scroll("Transcript", canvas.transform, out RectTransform transcriptContent);
            _surfaces.Add(_transcriptScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)_transcriptScroll.transform, Vector2.zero, new Vector2(0.64f, 1), new Vector2(20, 68), new Vector2(-8, -178));
            _transcript = TextUiFactory.Text("Transcript Text", transcriptContent, font, 19);
            var transcriptLayout = transcriptContent.gameObject.AddComponent<VerticalLayoutGroup>();
            transcriptLayout.childControlHeight = true; transcriptLayout.childControlWidth = true; transcriptLayout.childForceExpandHeight = false;
            transcriptContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _textEffects = gameObject.AddComponent<TextEffectRenderer>(); _textEffects.Initialize(_transcript);
            _textEffects.SetSemanticColors(_settings.Enabled);
            _choiceScroll = TextUiFactory.Scroll("Choices", canvas.transform, out _choices);
            _surfaces.Add(_choiceScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)_choiceScroll.transform, new Vector2(0.64f, 0), Vector2.one, new Vector2(8, 68), new Vector2(-20, -178));
            var choicesLayout = _choices.gameObject.AddComponent<VerticalLayoutGroup>();
            choicesLayout.spacing = 8; choicesLayout.childControlWidth = true; choicesLayout.childControlHeight = true; choicesLayout.childForceExpandHeight = false;
            _choices.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform footer = TextUiFactory.Rect("Accessibility", canvas.transform);
            TextUiFactory.Fill(footer, Vector2.zero, new Vector2(1, 0), new Vector2(20, 14), new Vector2(-20, 52));
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 6; footerLayout.childControlWidth = true; footerLayout.childControlHeight = true; footerLayout.childForceExpandWidth = true;
            _skip = Control(footer, "Skip [Space]", SkipAll);
            Control(footer, "Effects: on", () => ChangeSettings(enabled: !_settings.Enabled));
            Control(footer, "Motion: reduced", () => ChangeSettings(reduced: !_settings.ReducedMotion));
            Control(footer, "Flash: off", () => ChangeSettings(flashes: !_settings.Flashes));
            Control(footer, "Text: animated", () => ChangeSettings(instant: !_settings.InstantText));
            Control(footer, "Intensity: 50%", () => ChangeSettings(intensity: _settings.Intensity >= 1 ? 0 : _settings.Intensity + 0.5f));
            Control(footer, "Speed: " + _settings.ElementsPerSecond.ToString("0"), () =>
                ChangeSettings(speed: _settings.ElementsPerSecond < 25 ? 28 : _settings.ElementsPerSecond < 35 ? 40 : 18));
            _saveButton = TextUiFactory.Button(canvas.transform, font, "Save", 42);
            _controls.Add(_saveButton);
            TextUiFactory.Fill((RectTransform)_saveButton.transform, Vector2.one, Vector2.one, new Vector2(-250, -55), new Vector2(-150, -12));
            _saveButton.onClick.AddListener(() => { if (!IsDisplaying && _request != null) SaveRequested?.Invoke(); });
            _loadButton = TextUiFactory.Button(canvas.transform, font, "Load", 42);
            _controls.Add(_loadButton);
            TextUiFactory.Fill((RectTransform)_loadButton.transform, Vector2.one, Vector2.one, new Vector2(-360, -55), new Vector2(-260, -12));
            _loadButton.onClick.AddListener(() => { if (!IsDisplaying) LoadRequested?.Invoke(); });
            _saveButton.interactable = false; _loadButton.interactable = false;
            RefreshSettingLabels();
            RectTransform overlay = TextUiFactory.Rect("Effects Overlay", canvas.transform);
            TextUiFactory.Fill(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _screenEffects = overlay.gameObject.AddComponent<ScreenEffectOverlay>(); _screenEffects.raycastTarget = false;
            ApplyPalette(false);
        }

        public void ShowSetup(IReadOnlyList<RoleSpec> roles, string seedDescription, Action<RoleId?> start)
        {
            ResetDisplay(); ClearChoices(); ApplyPalette(false);
            _status.text = "SocialStim | New run | " + seedDescription;
            _transcript.text = "You belong to the cast. Speak, claim, lie, nominate, judge, and choose a night action.\n\n"
                + "Dialogue is marked PLACEHOLDER. Choose a role or random assignment.\n\n"
                + "Day: warm light background. Night: dark background.\n\n"
                + "Scroll either panel. Arrow keys and Enter select choices; > marks focus. "
                + "Space or Skip reveals pending text. Accessibility controls apply immediately. A dead player may spectate.";
            AddButton("Random role", "Generate a fresh cast and role assignment.", () => start(null));
            foreach (RoleSpec role in roles)
            {
                RoleId id = role.Id;
                AddButton("Play " + role.Name, role.Faction + " | Night: " + role.Ability, () => start(id));
            }
            _textEffects.SetDocument(SemanticText.Literal(_transcript.text), 0, _palette);
            _textEffects.Apply(int.MaxValue, default, 0);
            _transcriptScroll.verticalNormalizedPosition = 1;
            _choiceScroll.verticalNormalizedPosition = 1;
            FocusFirstChoice();
        }

        public void Render(PlayerInputRequest request)
        {
            _request = request;
            var view = request.View;
            string cast = string.Join(" | ", view.Characters.Select(c => c.Name + (c.Id == view.PlayerId ? " (you)" : "")
                + (!c.Alive ? " [dead]" : "") + (c.IsTeammate ? " [teammate]" : "")
                + (c.RevealedRole.HasValue ? " [" + c.RevealedRole.Value + "]" : "")));
            _status.text = $"Day {view.Day} | {view.Phase} | {view.OwnRole.Name} ({view.OwnRole.Faction}) | {(view.Alive ? "Alive" : "Spectator")}"
                + $" | Uses: {(view.Charges < 0 ? "unlimited" : view.Charges.ToString())}\nSeed {view.Seed} | Trials {view.TrialsUsed}/{view.TrialsAllowed}\n{cast}";
            ClearChoices();
            _menuCategory = null; _menuFamily = null; _menuPage = 0;
            _choiceScroll.gameObject.SetActive(false);
            if (_saveButton != null) _saveButton.interactable = false;
            if (_loadButton != null) _loadButton.interactable = false;
            _wasDisplaying = true;
            Pump(); UpdateChoiceState();
            _choiceScroll.verticalNormalizedPosition = 1;
            if (IsDisplaying && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_skip.gameObject);
        }

        public void ShowInputError(string message) => _status.text = message + "\n" + _status.text;

        private void Update()
        {
            if (IsDisplaying && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) SkipAll();
            _playback.Tick(Time.unscaledDeltaTime);
            Pump();
            if (_wasDisplaying && !IsDisplaying) UpdateChoiceState();
        }
        private void LateUpdate()
        {
            if (_request == null) return;
            _textEffects.Apply(_currentStart + _playback.VisibleUtf16, _playback.Frame, _playback.Elapsed);
            _screenEffects.Apply(_playback.Frame, _palette);
        }
        private void Pump()
        {
            while (_request != null && !_playback.Busy && _next < _request.View.Transcript.Count)
            {
                var observation = _request.View.Transcript[_next];
                if (observation.Day != _displayDay) { _displayDay = observation.Day; ApplyPalette(false); }
                if (observation.Cue == GameCue.NightFalls) ApplyPalette(true);
                if (observation.Cue == GameCue.DawnBreaks) ApplyPalette(false);
                if (!_palette.Night && observation.Cue == GameCue.TrialStarted)
                    ApplyPalette(false, Mathf.Max(_palette.DayProgress, 0.65f));
                if (!_palette.Night && (observation.Cue == GameCue.VerdictGuilty || observation.Cue == GameCue.VerdictInnocent))
                    ApplyPalette(false, 0.85f);
                if (observation.Cue == GameCue.ExecutionCarriedOut) ApplyPalette(false, 1f);
                SemanticText line;
                try { line = SemanticMarkup.Parse(observation.Text); }
                catch (ArgumentException) { line = SemanticText.Literal(observation.Text); }
                bool roleClaim = observation.Intent == SpeechIntent.ClaimRole;
                if (roleClaim) line = new SemanticText("[ROLE CLAIM] " + line.Text,
                    new[] { new SemanticSpan(0, 13 + line.Text.Length, SemanticStyle.Evidence) });
                if (_shown.Length > 0) _shown.Append("\n\n");
                _currentStart = _shown.Length;
                _shown.Append(line.Text);
                foreach (SemanticSpan span in line.Spans) _spans.Add(new SemanticSpan(span.Start + _currentStart, span.Length, span.Style));
                _textEffects.SetDocument(new SemanticText(_shown.ToString(), _spans), _currentStart, _palette);
                EffectProfile profile = _profiles[_cues.Resolve(observation, _request.View)];
                var pacing = new SpeechPacing(observation);
                _playback.Begin(observation.Sequence, line, profile, _settings, _onAcknowledged, pacing.Pause, pacing.Speed);
                Canvas.ForceUpdateCanvases();
                _transcriptScroll.verticalNormalizedPosition = 0;
            }
        }
        private void SkipAll()
        {
            while (IsDisplaying) { Pump(); _playback.Skip(); }
            _screenEffects.Apply(default, _palette);
            if (_request != null) _textEffects.Apply(_shown.Length, default, 0);
            UpdateChoiceState();
        }
        private void ResetDisplay()
        {
            _playback.Cancel(); _request = null; _next = 0; _currentStart = 0;
            _shown.Clear(); _spans.Clear(); _cues = new VisibleCueResolver(); _wasDisplaying = false;
            _visibleClaims.Clear(); _displayDay = 1;
            if (_claimSummary != null) _claimSummary.text = "ROLE CLAIMS (unverified)  |  Nobody has claimed yet.";
            if (_choiceScroll != null) _choiceScroll.gameObject.SetActive(true);
            if (_saveButton != null) _saveButton.interactable = false;
            if (_loadButton != null) _loadButton.interactable = _hasSave;
            if (_screenEffects != null) _screenEffects.Apply(default, _palette);
        }
        private void ChangeSettings(bool? enabled = null, bool? reduced = null, bool? flashes = null, bool? instant = null, float? intensity = null, float? speed = null)
        {
            _settings = new EffectsSettings(enabled ?? _settings.Enabled, reduced ?? _settings.ReducedMotion,
                flashes ?? _settings.Flashes, intensity ?? _settings.Intensity, instant ?? _settings.InstantText, speed ?? _settings.ElementsPerSecond);
            _textEffects.SetSemanticColors(_settings.Enabled);
            SkipAll();
            SettingsChanged?.Invoke(_settings);
            RefreshSettingLabels();
        }
        private void RefreshSettingLabels()
        {
            SetControl(2, "Effects: " + (_settings.Enabled ? "on" : "off"));
            SetControl(3, "Motion: " + (_settings.ReducedMotion ? "reduced" : "full"));
            SetControl(4, "Flash: " + (_settings.Flashes ? "on" : "off"));
            SetControl(5, "Text: " + (_settings.InstantText ? "instant" : "animated"));
            SetControl(6, "Intensity: " + (_settings.Intensity * 100).ToString("0") + "%");
            SetControl(7, "Speed: " + _settings.ElementsPerSecond.ToString("0"));
        }
        private void SetControl(int index, string text) => _controls[index].GetComponent<ChoiceFocusIndicator>().SetText(text);
        private Button Control(Transform parent, string label, Action selected)
        {
            Button button = TextUiFactory.Button(parent, _font, label, 38);
            button.onClick.AddListener(() => selected()); _controls.Add(button); return button;
        }
        private void ApplyPalette(bool night, float progress = 0)
        {
            _palette = new TextPalette(night, progress);
            foreach (Image surface in _surfaces) surface.color = _palette.Background;
            foreach (TextMeshProUGUI label in _labels) label.color = _palette.Foreground;
            foreach (Button button in _controls) ThemeButton(button);
            foreach (Button button in _buttons) ThemeButton(button);
            if (_transcript != null) _transcript.color = _palette.Foreground;
            if (_textEffects != null) _textEffects.SetPalette(_palette);
        }
        private void ThemeButton(Button button)
        { button.GetComponent<Image>().color = _palette.Background; button.GetComponentInChildren<TextMeshProUGUI>().color = _palette.Foreground; }
        private void AddButton(string label, string detail, Action selected)
        {
            Button button = TextUiFactory.Button(_choices, _font, label + "\n" + detail, 88);
            button.onClick.AddListener(() => selected()); _buttons.Add(button); ThemeButton(button);
        }
        private void ClearChoices()
        {
            foreach (Button button in _buttons) { button.interactable = false; button.gameObject.SetActive(false); Destroy(button.gameObject); }
            _buttons.Clear();
        }
        private void UpdateChoiceState()
        {
            bool ready = !IsDisplaying;
            if (_saveButton != null) _saveButton.interactable = ready && _request != null;
            if (_loadButton != null) _loadButton.interactable = ready && _hasSave;
            _choiceScroll.gameObject.SetActive(ready);
            _wasDisplaying = !ready;
            if (ready && _request != null)
            {
                if (_request.View.Phase != GamePhase.Ended)
                    ApplyPalette(Daylight.IsNight(_request.View.Phase), Daylight.Progress(_request.View.Phase, _request.View.TrialsUsed, _request.View.TrialsAllowed));
                BuildMenu();
            }
        }
        private void BuildMenu()
        {
            ClearChoices();
            var options = _request.Options.AsEnumerable();
            if (_menuCategory != null) options = options.Where(o => o.Category == _menuCategory);
            if (_menuFamily != null) options = options.Where(o => o.Family == _menuFamily);
            var entries = new List<Tuple<string, string, Action>>();
            if (_menuCategory == null && options.Count() > 8)
            {
                foreach (var group in options.GroupBy(o => o.Category))
                {
                    string category = group.Key;
                    entries.Add(Tuple.Create(category, group.Count() + " available choices", (Action)(() => {
                        _menuCategory = category; _menuPage = 0; BuildMenu();
                    })));
                }
            }
            else if (_menuCategory != null && _menuFamily == null && options.Select(o => o.Family).Distinct().Count() > 1 && options.Count() > 8)
            {
                foreach (var group in options.GroupBy(o => o.Family))
                {
                    string family = group.Key;
                    entries.Add(Tuple.Create(family, "Choose a target or statement", (Action)(() => {
                        _menuFamily = family; _menuPage = 0; BuildMenu();
                    })));
                }
            }
            else
                foreach (PlayerOption option in options)
                {
                    string id = option.Id;
                    entries.Add(Tuple.Create(option.Label, option.Interpretation, (Action)(() => { if (!IsDisplaying) OptionChosen?.Invoke(id); })));
                }
            if (_menuCategory != null)
                AddButton("Back", "Return without spending your turn.", () => {
                    if (_menuFamily != null) _menuFamily = null; else _menuCategory = null;
                    _menuPage = 0; BuildMenu();
                });
            foreach (var entry in entries.Skip(_menuPage * 6).Take(6)) AddButton(entry.Item1, entry.Item2, entry.Item3);
            if (_menuPage > 0) AddButton("Previous page", "", () => { _menuPage--; BuildMenu(); });
            if ((_menuPage + 1) * 6 < entries.Count) AddButton("Next page", "", () => { _menuPage++; BuildMenu(); });
            _choiceScroll.verticalNormalizedPosition = 1;
            FocusFirstChoice();
        }

        public void SetSaveAvailable(bool available)
        {
            _hasSave = available;
            if (_loadButton != null) _loadButton.interactable = available && !IsDisplaying;
            if (_saveButton != null) _saveButton.interactable = _request != null && !IsDisplaying;
        }
        public void PrepareLoadedRun() { ResetDisplay(); ClearChoices(); }
        public void RevealLoadedHistory() => SkipAll();

        private void FocusFirstChoice()
        {
            Canvas.ForceUpdateCanvases();
            if (_buttons.Count > 0 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
        }
        private void OnDisable() { ResetDisplay(); }
    }
}
