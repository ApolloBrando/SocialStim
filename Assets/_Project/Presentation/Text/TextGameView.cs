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
using SocialStim.Rules.Night;
using SocialStim.Rules.Reasoning;
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
        private InteractionLabels _copy = InteractionLabels.Default;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _choiceHeading;
        private TextMeshProUGUI _replyContext;
        private TextMeshProUGUI _conversationHeading;
        private RectTransform _displayOptionsPanel;
        private Image _canvasSurface;
        private TextMeshProUGUI _transcript;
        private RectTransform _choices;
        private ScrollRect _transcriptScroll;
        private ScrollRect _choiceScroll;
        private readonly List<Button> _buttons = new List<Button>();
        private readonly Dictionary<Button, Faction> _roleButtonFactions = new Dictionary<Button, Faction>();
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
        private GameText _words = GameText.Default;
        private InformationPanel _information;
        private Func<IReadOnlyList<NightDebugRecord>> _diagnostics;
        private DaylightBackdrop _backdrop;
        private TextMeshProUGUI _timeLabel;
        private Image _timeFill;
        private TextMeshProUGUI _roleBadge;
        private EffectTone _activeTone;
        private TextEffectRenderer _textEffects;
        private ScreenEffectOverlay _screenEffects;
        private Action<DisplayAcknowledgement> _onAcknowledged;
        private Button _skip;
        private Button _effectsControl, _motionControl, _flashControl, _instantControl;
        private Button _intensityControl, _optionsControl;
        private TextSpeedControl _speedControl;
        private ConversationScrollFollower _scrollFollower;
        private bool _wasDisplaying;
        private readonly Dictionary<RoleId, RoleSpec> _roles = new Dictionary<RoleId, RoleSpec>();
        private float _spectateAt;
        private bool _autoAdvance;
        private string _menuCategory;
        private int _menuPage;
        private int _choicesReadyFrame = -1;
        private int _displayDay = 1;
        private RectTransform _castPanel;
        private ScrollRect _castScroll;
        private string _pendingStatus;
        private readonly Dictionary<CharacterId, ParticipantCard> _cards = new Dictionary<CharacterId, ParticipantCard>();
        public event Action<string> OptionChosen;
        public event Action RestartRequested;
        public event Action<EffectsSettings> SettingsChanged;
        public bool IsDisplaying => _playback.Busy || _request != null && _next < _request.View.Transcript.Count;

        public void Initialize(TMP_FontAsset font, IReadOnlyDictionary<PresentationCue, EffectProfile> profiles = null, EffectsSettings initialSettings = null, InteractionLabels copy = null,
            GameText words = null, Func<IReadOnlyList<NightDebugRecord>> diagnostics = null)
        {
            _words = words ?? GameText.Default; _diagnostics = diagnostics;
            _copy = copy ?? InteractionLabels.Default;
            _settings = initialSettings ?? new EffectsSettings();
            _font = font; _profiles = profiles ?? DefaultCueProfiles.Create();
            _onAcknowledged = acknowledgement =>
            {
                if (_request != null && _next < _request.View.Transcript.Count)
                {
                    GameObservation observation = _request.View.Transcript[_next];
                    if (observation.Cue == GameCue.SpeechDelivered && !_palette.Night)
                    {
                        float progress = _request.View.Phase == GamePhase.Discussion
                            ? Daylight.DiscussionProgress(_request.View.DiscussionRound, _request.View.DiscussionRoundsAllowed)
                            : Daylight.Progress(_request.View.Phase, _request.View.TrialsUsed, _request.View.TrialsAllowed);
                        ApplyPalette(false, Mathf.Max(_palette.DayProgress, progress));
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
            _canvasSurface = canvas.AddComponent<Image>();
            RectTransform sky = TextUiFactory.Rect("Daylight sky", canvas.transform);
            TextUiFactory.Fill(sky, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _backdrop = sky.gameObject.AddComponent<DaylightBackdrop>(); _backdrop.raycastTarget = false;
            RectTransform daylight = TextUiFactory.Rect("Daylight progress", canvas.transform);
            TextUiFactory.Fill(daylight, new Vector2(0.42f, 1), new Vector2(0.70f, 1), new Vector2(0, -55), new Vector2(0, -25));
            _timeLabel = TextUiFactory.Text("Time of day", daylight, font, 14);
            var fill = TextUiFactory.Rect("Progress", daylight);
            TextUiFactory.Fill(fill, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 3));
            _timeFill = fill.gameObject.AddComponent<Image>();
            _roleBadge = TextUiFactory.Text("Your role", canvas.transform, font, 14);
            TextUiFactory.Fill(_roleBadge.rectTransform, new Vector2(0.71f, 1), new Vector2(0.85f, 1), new Vector2(0, -60), new Vector2(0, -20));
            var title = TextUiFactory.Text("Title", canvas.transform, font, 30); title.text = "SocialStim";
            title.fontStyle = FontStyles.Bold; title.characterSpacing = 1.2f; _labels.Add(title);
            TextUiFactory.Fill(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -61), new Vector2(-200, -20));
            _status = TextUiFactory.Text("Status", canvas.transform, font, 15); _labels.Add(_status);
            _castScroll = TextUiFactory.Scroll("Participants", canvas.transform, out _castPanel);
            _surfaces.Add(_castScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)_castScroll.transform, new Vector2(0.8f, 0), Vector2.one, new Vector2(0, 82), new Vector2(-32, -111));
            var castLayout = _castPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            castLayout.padding = new RectOffset(8, 8, 8, 8); castLayout.spacing = 5;
            castLayout.childControlWidth = true; castLayout.childControlHeight = true;
            castLayout.childForceExpandHeight = false;
            _castPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _castScroll.gameObject.SetActive(false);
            var rosterHeading = TextUiFactory.Text("Roster heading", _castScroll.transform, font, 13);
            rosterHeading.text = _copy.Players; rosterHeading.fontStyle = FontStyles.Bold; _labels.Add(rosterHeading);
            TextUiFactory.Fill(rosterHeading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(22, -36), new Vector2(-22, -16));
            TextUiFactory.Fill(_status.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(34, -104), new Vector2(-34, -66));
            Button restart = TextUiFactory.Button(canvas.transform, font, "New run", 42);
            _controls.Add(restart);
            TextUiFactory.Fill((RectTransform)restart.transform, Vector2.one, Vector2.one, new Vector2(-158, -62), new Vector2(-32, -22));
            restart.onClick.AddListener(() => RestartRequested?.Invoke());
            _transcriptScroll = TextUiFactory.Scroll("Transcript", canvas.transform, out RectTransform transcriptContent);
            _surfaces.Add(_transcriptScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)_transcriptScroll.transform, Vector2.zero, new Vector2(0.51f, 1), new Vector2(32, 82), new Vector2(-10, -130));
            var conversationHeading = TextUiFactory.Text("Conversation Heading", _transcriptScroll.transform, font, 13);
            _conversationHeading = conversationHeading;
            conversationHeading.text = "CONVERSATION"; conversationHeading.fontStyle = FontStyles.Bold; conversationHeading.characterSpacing = 1;
            _labels.Add(conversationHeading);
            TextUiFactory.Fill(conversationHeading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(22, -36), new Vector2(-22, -16));
            _transcript = TextUiFactory.Text("Transcript Text", transcriptContent, font, 19);
            _transcript.lineSpacing = 4;
            _scrollFollower = _transcriptScroll.gameObject.AddComponent<ConversationScrollFollower>();
            _scrollFollower.Initialize(_transcriptScroll);
            var transcriptLayout = transcriptContent.gameObject.AddComponent<VerticalLayoutGroup>();
            transcriptLayout.childControlHeight = true; transcriptLayout.childControlWidth = true; transcriptLayout.childForceExpandHeight = false;
            transcriptContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _textEffects = gameObject.AddComponent<TextEffectRenderer>(); _textEffects.Initialize(_transcript);
            _textEffects.SetSettings(_settings);
            _choiceScroll = TextUiFactory.Scroll("Choices", canvas.transform, out _choices);
            _surfaces.Add(_choiceScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)_choiceScroll.transform, new Vector2(0.51f, 0), new Vector2(0.79f, 1), new Vector2(10, 82), new Vector2(-32, -130));
            var choiceHeading = TextUiFactory.Text("Choice Heading", _choiceScroll.transform, font, 13);
            _choiceHeading = choiceHeading;
            choiceHeading.text = "YOUR TURN"; choiceHeading.fontStyle = FontStyles.Bold; choiceHeading.characterSpacing = 1;
            _labels.Add(choiceHeading);
            TextUiFactory.Fill(choiceHeading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(22, -36), new Vector2(-22, -16));
            _replyContext = TextUiFactory.Text("Reply context", _choiceScroll.transform, font, 13); _labels.Add(_replyContext);
            TextUiFactory.Fill(_replyContext.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(22, -85), new Vector2(-22, -38));
            _replyContext.gameObject.SetActive(false);
            var choicesLayout = _choices.gameObject.AddComponent<VerticalLayoutGroup>();
            choicesLayout.spacing = 10; choicesLayout.childControlWidth = true; choicesLayout.childControlHeight = true; choicesLayout.childForceExpandHeight = false;
            _choices.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform footer = TextUiFactory.Rect("Accessibility", canvas.transform);
            TextUiFactory.Fill(footer, Vector2.zero, new Vector2(1, 0), new Vector2(32, 20), new Vector2(-32, 60));
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 6; footerLayout.childControlWidth = true; footerLayout.childControlHeight = true; footerLayout.childForceExpandWidth = false;
            _skip = Control(footer, "Skip [Space]", SkipAll);
            _displayOptionsPanel = TextUiFactory.Rect("Display Options", canvas.transform);
            TextUiFactory.Fill(_displayOptionsPanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 70), new Vector2(-32, 520));
            _surfaces.Add(_displayOptionsPanel.gameObject.AddComponent<Image>());
            TextUiFactory.Border(_displayOptionsPanel);
            var displayLayout = _displayOptionsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            displayLayout.padding = new RectOffset(12, 12, 12, 12); displayLayout.spacing = 6;
            displayLayout.childControlWidth = true; displayLayout.childControlHeight = true;
            displayLayout.childForceExpandHeight = false;
            _effectsControl = Control(_displayOptionsPanel, "Effects: on", () => ChangeSettings(enabled: !_settings.Enabled));
            _motionControl = Control(_displayOptionsPanel, "Motion: reduced", () => ChangeSettings(reduced: !_settings.ReducedMotion));
            _flashControl = Control(_displayOptionsPanel, "Flash: off", () => ChangeSettings(flashes: !_settings.Flashes));
            _instantControl = Control(_displayOptionsPanel, "Text: animated", () => ChangeSettings(instant: !_settings.InstantText));
            _intensityControl = Control(_displayOptionsPanel, "Intensity: 50%", () => ChangeSettings(intensity: _settings.Intensity >= 1 ? 0 : Mathf.Min(1, _settings.Intensity + 0.5f)));
            RectTransform speedRoot = TextUiFactory.Rect("Text speed", _displayOptionsPanel);
            _speedControl = speedRoot.gameObject.AddComponent<TextSpeedControl>();
            _speedControl.Initialize(font, _words, percent => ChangeSettings(speed: EffectsSettings.SpeedForPercent(percent)));
            _optionsControl = Control(footer, "Display options", ToggleDisplayOptions);
            Control(footer, _words.Format("ui.rolesShortcut"), ShowRoleReference);
            Control(footer, _words.Format("ui.latest"), FollowLatest);
            if (_diagnostics != null) Control(footer, _words.Format("ui.debug"), ShowNightDebug);
            Control(_displayOptionsPanel, "Reset display defaults", ResetSettings);
            _displayOptionsPanel.gameObject.SetActive(false);
            RefreshSettingLabels();
            RectTransform overlay = TextUiFactory.Rect("Effects Overlay", canvas.transform);
            TextUiFactory.Fill(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _screenEffects = overlay.gameObject.AddComponent<ScreenEffectOverlay>(); _screenEffects.raycastTarget = false;
            RectTransform information = TextUiFactory.Rect("Reference panel", canvas.transform);
            TextUiFactory.Fill(information, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _information = information.gameObject.AddComponent<InformationPanel>(); _information.Initialize(font, _words.Format("ui.close"));
            ApplyPalette(false);
        }

        public void ShowSetup(IReadOnlyList<RoleSpec> roles, string seedDescription, Action<RoleId?> start)
        {
            ResetDisplay(); ClearChoices(); ApplyPalette(false);
            _roles.Clear();
            _roleBadge.gameObject.SetActive(false);
            foreach (RoleSpec role in roles) { _roles.Add(role.Id, role); }
            _status.text = "A new run · " + seedDescription;
            _conversationHeading.text = "SETTLE IN";
            TextUiFactory.Fill((RectTransform)_transcriptScroll.transform, Vector2.zero, new Vector2(0.51f, 1), new Vector2(32, 82), new Vector2(-10, -130));
            TextUiFactory.Fill((RectTransform)_choiceScroll.transform, new Vector2(0.51f, 0), new Vector2(0.79f, 1), new Vector2(10, 82), new Vector2(-32, -130));
            _transcript.text = "Listen closely. Choose who to trust.\n\n"
                + "Mouse or arrows + Enter to choose. Space to skip text.\n\n"
                + "Display options control text speed and effects.";
            _choiceHeading.text = "START A RUN";
            ShowRoleGroups(roles, start, null);
            _textEffects.SetDocument(SemanticText.Literal(_transcript.text), 0, _palette);
            _textEffects.Apply(int.MaxValue, default, 0);
            _transcriptScroll.verticalNormalizedPosition = 1;
            _choiceScroll.verticalNormalizedPosition = 1;
            FocusFirstChoice();
        }

        private void ShowRoleGroups(IReadOnlyList<RoleSpec> roles, Action<RoleId?> start, Faction? faction)
        {
            ClearChoices();
            if (!faction.HasValue)
            {
                AddButton("Random role", "Let chance decide.", () => start(null));
                foreach (var group in roles.GroupBy(role => role.Faction))
                {
                    Faction side = group.Key;
                    string description = side == Faction.Town ? "Find the threats."
                        : side == Faction.Antagonist ? "Protect your team. Mislead the town." : "Follow your own goal.";
                    AddButton(side + " roles", description, () => ShowRoleGroups(roles, start, side));
                    _roleButtonFactions[_buttons.Last()] = side; ThemeButton(_buttons.Last());
                }
            }
            else
            {
                AddButton("Back", "Choose a faction.", () => ShowRoleGroups(roles, start, null));
                foreach (RoleSpec role in roles.Where(role => role.Faction == faction.Value).OrderBy(role => role.Name, StringComparer.Ordinal))
                {
                    RoleId id = role.Id;
                    AddButton(role.Name, ActionLabels.Ability(role.Ability)
                        + (role.Charges > 0 ? " · " + role.Charges + " uses" : ""), () => start(id));
                    _roleButtonFactions[_buttons.Last()] = role.Faction; ThemeButton(_buttons.Last());
                }
            }
            _choiceScroll.verticalNormalizedPosition = 1;
            FocusFirstChoice();
        }

        public void Render(PlayerInputRequest request)
        {
            _request = request; _autoAdvance = false;
            _choiceHeading.text = "YOUR TURN";
            _conversationHeading.text = "CONVERSATION";
            TextUiFactory.Fill((RectTransform)_transcriptScroll.transform, Vector2.zero, new Vector2(0.51f, 1), new Vector2(32, 82), new Vector2(-10, -130));
            TextUiFactory.Fill((RectTransform)_choiceScroll.transform, new Vector2(0.51f, 0), new Vector2(0.79f, 1), new Vector2(10, 82), new Vector2(-32, -130));
            var view = request.View;
            _roleBadge.gameObject.SetActive(true);
            _roleBadge.text = view.OwnRole.Name + "\n" + view.OwnRole.Faction;
            _roleBadge.color = _palette.RoleColor(view.OwnRole.Faction);
            _choiceHeading.text = view.RespondTo.HasValue ? "RESPOND"
                : view.Phase == GamePhase.Voting ? (view.Revote ? "REVOTE" : "VOTE") : "YOUR TURN";
            bool night = Daylight.IsNight(view.Phase);
            if (view.Phase == GamePhase.Ended)
                night = view.Transcript.LastOrDefault(o => o.Cue == GameCue.NightFalls || o.Cue == GameCue.DawnBreaks)?.Cue == GameCue.NightFalls;
            string period = _words.Format(night ? "ui.nightCount" : "ui.dayCount", ("number", view.Day.ToString()));
            _pendingStatus = _words.Format("ui.runStatus", ("period", period), ("phase", ActionLabels.Phase(view.Phase, _words)),
                ("role", view.OwnRole.Name), ("faction", view.OwnRole.Faction.ToString()),
                ("state", _words.Format(view.Alive ? "ui.alive" : "ui.spectating")),
                ("uses", view.Charges < 0 ? _words.Format("ui.unlimited") : view.Charges.ToString()), ("seed", view.Seed.ToString()));
            _pendingStatus += " | " + _words.Format("ui.trialsStatus", ("used", view.TrialsUsed.ToString()), ("allowed", view.TrialsAllowed.ToString()));
            ClearChoices();
            _menuCategory = null; _menuPage = 0;
            foreach (ParticipantCard card in _cards.Values) card.SetSelectable(false);
            _status.text = "Listening…";
            _choiceScroll.gameObject.SetActive(false);
            _wasDisplaying = true;
            Pump(); UpdateChoiceState();
            _choiceScroll.verticalNormalizedPosition = 1;
            if (IsDisplaying && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_skip.gameObject);
        }

        public void ShowInputError(string message) => _status.text = message;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_information.IsOpen) _information.Close();
                else if (_displayOptionsPanel.gameObject.activeSelf) ToggleDisplayOptions();
                else if (_request?.CompositionPrompt != null && !IsDisplaying && _request.Options.Count > 0) SubmitOption(_request.Options[0].Id);
                else if (_menuCategory != null) { _menuCategory = null; _menuPage = 0; BuildMenu(); }
            }
            if (!_information.IsOpen && !_displayOptionsPanel.gameObject.activeSelf && keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame) ShowRoleReference();
                else if (IsDisplaying && keyboard.spaceKey.wasPressedThisFrame) SkipAll();
                else if (!IsDisplaying && _request != null && Time.frameCount > _choicesReadyFrame)
                    for (int seat = 0; seat < 9; seat++)
                        if (keyboard[(Key)((int)Key.Digit1 + seat)].wasPressedThisFrame) { SelectRosterParticipant(new CharacterId(seat)); break; }
            }
            _playback.Tick(Time.unscaledDeltaTime);
            Pump();
            if (_wasDisplaying && !IsDisplaying) UpdateChoiceState();
            if (_autoAdvance && !IsDisplaying && !_information.IsOpen && !_displayOptionsPanel.gameObject.activeSelf && _scrollFollower.Following && Time.unscaledTime >= _spectateAt)
            {
                _autoAdvance = false;
                if (_request != null && !_request.View.Alive && _request.View.Phase != GamePhase.JesterRevenge && _request.Options.Count == 1)
                    OptionChosen?.Invoke(_request.Options[0].Id);
            }
        }
        private void LateUpdate()
        {
            if (_request == null) return;
            _textEffects.Apply(_currentStart + _playback.VisibleUtf16, _playback.Frame, Time.unscaledTime);
            _screenEffects.Apply(_playback.Frame, _palette, _activeTone);
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
                string speakerName = observation.Speaker.HasValue ? _request.View.Characters.First(c => c.Id == observation.Speaker.Value).Name : null;
                line = ParticipantNameStyler.Dialogue(line, _request.View.Characters.Select(c => c.Name), speakerName, observation.Speaker?.Value ?? 0);
                line = ActionAppearance.Label(line, observation.Intent, _words);
                if (_shown.Length > 0) _shown.Append("\n\n");
                _currentStart = _shown.Length;
                _shown.Append(line.Text);
                foreach (SemanticSpan span in line.Spans) _spans.Add(new SemanticSpan(span.Start + _currentStart, span.Length, span.Style));
                _textEffects.SetDocument(new SemanticText(_shown.ToString(), _spans), _currentStart, _palette);
                EffectProfile profile = _profiles[_cues.Resolve(observation, _request.View)];
                _activeTone = profile.Tone;
                if (observation.Cue == GameCue.SpeechDelivered)
                {
                    _conversationHeading.text = observation.Intent.HasValue && _words.Entries.TryGetValue("intent." + observation.Intent.Value, out string action) ? action : _words.Format("ui.statement");
                    if (observation.Speaker.HasValue) _conversationHeading.text += " · " + _request.View.Characters.First(c => c.Id == observation.Speaker.Value).Name;
                    if (observation.Target.HasValue && observation.Target != observation.Speaker) _conversationHeading.text += " → " + _request.View.Characters.First(c => c.Id == observation.Target.Value).Name;
                    _conversationHeading.color = _palette.Foreground;
                }
                else { _conversationHeading.text = "CONVERSATION"; _conversationHeading.color = _palette.Foreground; }
                var pacing = new SpeechPacing(observation);
                _playback.Begin(observation.Sequence, line, profile, _settings, _onAcknowledged, pacing.Pause, pacing.Speed);
                RenderRoster(_next + 1);
                _castScroll.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                if (_scrollFollower.Following) _transcriptScroll.verticalNormalizedPosition = 0;
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
            if (_scrollFollower != null) _scrollFollower.Follow();
            if (_replyContext != null) _replyContext.gameObject.SetActive(false);
            if (_choiceScroll != null) _choiceScroll.viewport.offsetMax = new Vector2(-22, -48);
            _shown.Clear(); _spans.Clear(); _cues = new VisibleCueResolver(); _wasDisplaying = false;
            _autoAdvance = false; _displayDay = 1; _pendingStatus = null;
            if (_information != null && _information.IsOpen) _information.Close();
            foreach (ParticipantCard card in _cards.Values) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
            _cards.Clear();
            if (_castScroll != null) _castScroll.gameObject.SetActive(false);
            if (_choiceScroll != null) _choiceScroll.gameObject.SetActive(true);
            if (_screenEffects != null) _screenEffects.Apply(default, _palette);
        }
        private void ChangeSettings(bool? enabled = null, bool? reduced = null, bool? flashes = null, bool? instant = null, float? intensity = null, float? speed = null)
        {
            _settings = new EffectsSettings(enabled ?? _settings.Enabled, reduced ?? _settings.ReducedMotion,
                flashes ?? _settings.Flashes, intensity ?? _settings.Intensity, instant ?? _settings.InstantText, speed ?? _settings.ElementsPerSecond);
            _textEffects.SetSettings(_settings);
            _playback.Reconfigure(_settings);
            // Preferences must not acknowledge or consume the queued conversation.
            // Reconfigure preserves reveal position; the next Update pumps only completed lines.
            if (_request != null)
                _textEffects.Apply(_currentStart + _playback.VisibleUtf16, _playback.Frame, Time.unscaledTime);
            _screenEffects.Apply(_playback.Frame, _palette, _activeTone);
            SettingsChanged?.Invoke(_settings);
            RefreshSettingLabels();
        }
        private void RefreshSettingLabels()
        {
            SetControl(_effectsControl, "Effects: " + (_settings.Enabled ? "on" : "off"));
            SetControl(_motionControl, "Motion: " + (_settings.ReducedMotion ? "reduced" : "full"));
            SetControl(_flashControl, "Flash: " + (_settings.Flashes ? "on" : "off"));
            SetControl(_instantControl, "Text: " + (_settings.InstantText ? "instant" : "animated"));
            SetControl(_intensityControl, "Intensity: " + (_settings.Intensity * 100).ToString("0") + "%");
            _speedControl.Refresh(_settings);
        }
        private void ResetSettings()
        {
            var defaults = new EffectsSettings();
            ChangeSettings(defaults.Enabled, defaults.ReducedMotion, defaults.Flashes, defaults.InstantText,
                defaults.Intensity, defaults.ElementsPerSecond);
        }
        private void ToggleDisplayOptions()
        {
            bool open = !_displayOptionsPanel.gameObject.activeSelf;
            _displayOptionsPanel.gameObject.SetActive(open);
            SetControl(_optionsControl, open ? "Hide options" : "Display options");
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject((open ? _effectsControl : _optionsControl).gameObject);
        }

        private static void SetControl(Button button, string text) => button.GetComponent<ChoiceFocusIndicator>().SetText(text);
        private Button Control(Transform parent, string label, Action selected)
        {
            Button button = TextUiFactory.Button(parent, _font, label, 38);
            button.onClick.AddListener(() => selected()); _controls.Add(button); return button;
        }
        private void ApplyPalette(bool night, float progress = 0)
        {
            _palette = new TextPalette(night, progress);
            if (_backdrop != null) _backdrop.Apply(_palette, _request == null);
            if (_timeLabel != null)
            {
                _timeLabel.text = _words.Format(night ? "ui.nightCount" : "ui.dayCount", ("number", _displayDay.ToString()))
                    + (night ? "" : " · " + _words.Format(progress < 0.25f ? "ui.morning" : progress < 0.65f ? "ui.afternoon" : "ui.evening"));
                _timeLabel.color = _palette.Foreground;
                _timeFill.color = _palette.Accent(SemanticStyle.Evidence);
                _timeFill.rectTransform.anchorMax = new Vector2(night ? 1 : Mathf.Max(0.04f, progress), 0);
            }
            if (_canvasSurface != null) _canvasSurface.color = _palette.Background;
            foreach (Image surface in _surfaces)
            {
                surface.color = _palette.Panel;
                var outline = surface.GetComponent<Outline>();
                if (outline != null) outline.effectColor = _palette.Frame;
            }
            foreach (TextMeshProUGUI label in _labels) label.color = _palette.Foreground;
            if (_speedControl != null) _speedControl.Theme(_palette);
            foreach (Button button in _controls) ThemeButton(button);
            foreach (Button button in _buttons) ThemeButton(button);
            foreach (ParticipantCard card in _cards.Values) card.Theme(_palette);
            if (_roleBadge != null && _request != null) _roleBadge.color = _palette.RoleColor(_request.View.OwnRole.Faction);
            if (_transcript != null) _transcript.color = _palette.Foreground;
            if (_textEffects != null) _textEffects.SetPalette(_palette);
        }
        private void ThemeButton(Button button)
        {
            button.GetComponent<ChoiceFocusIndicator>().SetSurfaceColors(_palette.ButtonBackground, _palette.FocusBackground);
            button.GetComponent<Outline>().effectColor = _palette.Frame;
            foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                label.color = label.name == "Detail" ? _palette.Muted : _roleButtonFactions.TryGetValue(button, out Faction faction) ? _palette.RoleColor(faction) : _palette.Foreground;
        }
        private void AddButton(string label, string detail, Action selected)
        {
            Button button = TextUiFactory.Button(_choices, _font, label + (string.IsNullOrEmpty(detail) ? "" : "\n" + detail), string.IsNullOrEmpty(detail) ? 52 : 88);
            button.onClick.AddListener(() => selected()); _buttons.Add(button); ThemeButton(button);
        }
        private void ClearChoices()
        {
            foreach (Button button in _buttons) { button.interactable = false; button.gameObject.SetActive(false); Destroy(button.gameObject); }
            foreach (Button button in _buttons) _roleButtonFactions.Remove(button);
            _buttons.Clear();
        }
        private void UpdateChoiceState()
        {
            bool ready = !IsDisplaying;
            _choiceScroll.gameObject.SetActive(ready);
            _wasDisplaying = !ready;
            if (ready && _request != null)
            {
                if (_request.View.Phase != GamePhase.Ended)
                    ApplyPalette(Daylight.IsNight(_request.View.Phase), Mathf.Max(_palette.DayProgress, Daylight.Progress(_request.View.Phase, _request.View.TrialsUsed, _request.View.TrialsAllowed)));
                _status.text = _pendingStatus;
                _castScroll.gameObject.SetActive(true);
                RenderRoster();
                if (!_request.View.Alive && _request.View.Phase != GamePhase.Ended && _request.View.Phase != GamePhase.JesterRevenge)
                {
                    ClearChoices(); _choiceScroll.gameObject.SetActive(false);
                    _spectateAt = Time.unscaledTime + 0.8f; _autoAdvance = true;
                }
                else BuildMenu();
            }
        }
        private void BuildMenu()
        {
            // A skip/submit event must not choose a newly revealed option in the same frame.
            _choicesReadyFrame = Time.frameCount;
            ClearChoices();
            var options = _request.Options.AsEnumerable();
            var entries = new List<Tuple<string, string, Action>>();
            void Entry(string label, string detail, Action selected)
                => entries.Add(Tuple.Create(label, detail, selected));
            if (_request.CompositionPrompt != null)
            {
                _choiceHeading.text = _words.Format("fabricate.start");
                foreach (var option in options.Where(o => !o.Target.HasValue))
                { string id = option.Id; Entry(option.Label, option.Interpretation, () => SubmitOption(id)); }
            }
            else if (_menuCategory == null)
            {
                _choiceHeading.text = _request.View.Phase == GamePhase.JesterRevenge ? _words.Format("ui.revengePrompt")
                    : _request.View.RespondTo.HasValue ? _copy.DefendSelf : _words.Format("ui.actionFirst");
                foreach (PlayerOption option in options.Where(o => !o.HasSubmenu && (!o.Target.HasValue || o.Kind == SocialStim.Rules.Commands.CommandKind.Speak)))
                {
                    string id = option.Id;
                    Entry(option.Label, ReplyDescription(option), (Action)(() => SubmitOption(id)));
                }
                foreach (var group in options.Where(o => o.HasSubmenu).GroupBy(o => o.Category))
                {
                    string category = group.Key;
                    if (category == "Report" && group.Count() == 1)
                    {
                        string id = group.First().Id;
                        Entry(_words.Format("ui.giveReport"), "", (Action)(() => SubmitOption(id)));
                    }
                    else Entry(category == "Report" ? _words.Format("ui.giveReport") : _copy.Group(category), "", (Action)(() => { _menuCategory = category; _menuPage = 0; BuildMenu(); }));
                }
                if (options.Any(o => !o.HasSubmenu && o.Target.HasValue && o.Kind != SocialStim.Rules.Commands.CommandKind.Speak))
                    Entry(_words.Format("ui.selectTarget"), "", (Action)(() => { _menuCategory = "Targets"; _menuPage = 0; BuildMenu(); }));
                if (_request.View.Phase == GamePhase.Discussion && !_request.View.CounterClaimRole.HasValue && _request.View.Characters.First(c => c.Id == _request.View.PlayerId).ClaimedRole.HasValue
                    && !_request.View.CanReportAction)
                {
                    AddButton(_words.Format("ui.giveReport"), _words.Format(_request.View.LastNightAction == null ? "ui.reportPending" : "ui.reportDone"), () => { });
                    _buttons.Last().interactable = false;
                }
            }
            else
            {
                AddButton(_copy.Back, "", () => { _menuCategory = null; _menuPage = 0; BuildMenu(); });
                bool targeting = _menuCategory == "Targets" || options.Any(o => o.Category == _menuCategory && o.Target.HasValue);
                _choiceHeading.text = targeting ? _words.Format("ui.pickForAction", ("action", _menuCategory == "Targets" ? ActionLabels.Phase(_request.View.Phase) : _copy.Group(_menuCategory))) : _menuCategory == "Report" ? _words.Format("ui.giveReport") : _copy.Group(_menuCategory);
                foreach (PlayerOption option in options.Where(o => o.Category == _menuCategory && !o.Target.HasValue))
                {
                    string id = option.Id;
                    Entry(option.Label, ReplyDescription(option), (Action)(() => SubmitOption(id)));
                }
            }
            foreach (var entry in entries.Skip(_menuPage * 6).Take(6))
            {
                AddButton(entry.Item1, entry.Item2, entry.Item3);
                if (_menuCategory == "Claim Role")
                {
                    RoleSpec role = _roles.Values.FirstOrDefault(r => r.Name == entry.Item1);
                    if (role != null) { _roleButtonFactions[_buttons.Last()] = role.Faction; ThemeButton(_buttons.Last()); }
                }
            }
            if (_menuPage > 0) AddButton(_words.Format("ui.previous"), "", () => { _menuPage--; BuildMenu(); });
            if ((_menuPage + 1) * 6 < entries.Count) AddButton(_words.Format("ui.next"), "", () => { _menuPage++; BuildMenu(); });
            RenderRoster();
            _choiceScroll.verticalNormalizedPosition = 1;
            string context = _request.CompositionPrompt ?? ReplyContext();
            _replyContext.text = context; _replyContext.gameObject.SetActive(!string.IsNullOrEmpty(context));
            TextUiFactory.Fill(_replyContext.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(22, _request.CompositionPrompt == null ? -85 : -122), new Vector2(-22, -38));
            _choiceScroll.viewport.offsetMax = new Vector2(-22, _request.CompositionPrompt != null ? -134 : string.IsNullOrEmpty(context) ? -48 : -96);
            if (!_information.IsOpen) FocusChoicesOrTargets();
        }

        private void RenderRoster(int? visibleObservations = null)
        {
            if (_request == null) return;
            int visible = visibleObservations ?? Mathf.Min(_request.View.Transcript.Count, _next + (_playback.Busy ? 1 : 0));
            bool spectator = !RosterInteractions.VisibleParticipant(_request.View, _request.View.PlayerId, visible).Alive;
            foreach (var seat in _request.View.Characters)
            {
                var participant = RosterInteractions.VisibleParticipant(_request.View, seat.Id, visible);
                if (!_cards.TryGetValue(participant.Id, out ParticipantCard card))
                {
                    RectTransform root = TextUiFactory.Rect("Participant " + participant.Id.Value, _castPanel);
                    card = root.gameObject.AddComponent<ParticipantCard>(); card.Initialize(participant.Id, _font);
                    card.Selected += SelectRosterParticipant; _cards.Add(participant.Id, card);
                }
                var action = RosterInteractions.DirectChoice(_request, participant.Id, _menuCategory);
                bool selectable = !IsDisplaying && action != null;
                string annotation = selectable ? _words.Format("ui.selectable") : "";
                if (participant.IsTeammate && participant.TeamRole.HasValue && _roles.TryGetValue(participant.TeamRole.Value, out RoleSpec teamRole))
                    annotation = _words.Format("mafia.teamRole", ("role", teamRole.Name)) + (annotation.Length == 0 ? "" : " · " + annotation);
                RoleSpec claim = participant.ClaimedRole.HasValue && _roles.TryGetValue(participant.ClaimedRole.Value, out RoleSpec known) ? known : null;
                RoleSpec spectatorRole = spectator && _request.View.SpectatorRoles.TryGetValue(participant.Id, out RoleId actual)
                    && _roles.TryGetValue(actual, out RoleSpec actualRole) ? actualRole : null;
                card.Render(participant, participant.Id == _request.View.PlayerId, _palette, claim, selectable, false, annotation, _words, spectatorRole);
            }
        }
        private void SelectRosterParticipant(CharacterId target)
        {
            if (IsDisplaying || _information.IsOpen || _displayOptionsPanel.gameObject.activeSelf || Time.frameCount <= _choicesReadyFrame) return;
            var option = RosterInteractions.DirectChoice(_request, target, _menuCategory);
            if (option != null) SubmitOption(option.Id);
        }
        private string ReplyDescription(PlayerOption option)
        {
            if (option.Intent != SpeechIntent.Agree && option.Intent != SpeechIntent.Disagree) return option.Interpretation;
            string context = ReplyContext();
            return string.IsNullOrEmpty(context) ? option.Interpretation : _words.Format(option.Intent == SpeechIntent.Agree ? "ui.agreeContext" : "ui.disagreeContext", ("statement", context));
        }
        private string ReplyContext()
        {
            CharacterId? speaker = _request.View.FollowUpTo ?? _request.View.RespondTo;
            if (!speaker.HasValue) return "";
            var statement = RosterInteractions.ReplyStatement(_request.View.Transcript, speaker.Value, _request.View.Day);
            if (statement == null) return "";
            string name = _request.View.Characters.First(c => c.Id == statement.Speaker.Value).Name;
            string target = statement.Target.HasValue ? _request.View.Characters.First(c => c.Id == statement.Target.Value).Name : name;
            string key = statement.Intent == SpeechIntent.Defend ? "ui.contextSelfDefense" : statement.Intent == SpeechIntent.Vouch ? "ui.contextDefense"
                : statement.Intent == SpeechIntent.Deflect ? "ui.contextRefute" : "ui.contextAccuse";
            return _words.Format(key, ("speaker", name), ("target", target));
        }
        private void ShowRoleReference()
        {
            if (_displayOptionsPanel.gameObject.activeSelf) ToggleDisplayOptions();
            _information.Open(_words.Format("ui.roleGuide"), _roles.Values.OrderBy(r => r.Faction).ThenBy(r => r.Name, StringComparer.Ordinal)
                .Select(r => Tuple.Create(r.Name, _words.Entries.TryGetValue("role." + r.Id.Value, out string description)
                    ? description : r.Faction + " · " + ActionLabels.Ability(r.Ability), _palette.RoleColor(r.Faction))).Prepend(Tuple.Create(_words.Format("ui.legend"), _words.Format("ui.legendBody"), _palette.Foreground)), _palette);
        }
        private void ShowNightDebug()
        {
            if (_diagnostics == null) return;
            var records = _diagnostics();
            Func<CharacterId, string> name = id => _request?.View.Characters.FirstOrDefault(c => c.Id == id)?.Name
                ?? records.FirstOrDefault(r => r.Actor == id)?.Name ?? id.Value.ToString();
            var rows = records.Where(r => r.Actor.Value != 0).Select(r => Tuple.Create(r.Name,
                _words.Format("ui.debugRow", ("night", r.Action.Night.ToString()), ("name", r.Name),
                    ("action", ActionLabels.Ability(r.Action.Ability)), ("target", r.Action.Target.HasValue ? name(r.Action.Target.Value) : _words.Format("ui.pass")),
                    ("details", NightReportFormatter.Format(r.Action, _roles[r.Action.Role].Name, name, _words)
                        + "\n" + _words.Format(r.VisitTarget.HasValue ? "ui.visited" : "ui.noVisit")
                        + " · " + _words.Format(r.SpentCharge ? "ui.spent" : "ui.notSpent"))), _palette.Foreground)).ToList();
            if (rows.Count == 0) rows.Add(Tuple.Create("", _words.Format("ui.debugEmpty"), _palette.Muted));
            _information.Open(_words.Format("ui.debugTitle"), rows, _palette);
        }
        private static string Plain(string text)
        {
            try { return SemanticMarkup.Parse(text).Text; } catch (ArgumentException) { return text; }
        }

        private void SubmitOption(string id)
        {
            if (!IsDisplaying && !_information.IsOpen && !_displayOptionsPanel.gameObject.activeSelf && Time.frameCount > _choicesReadyFrame) OptionChosen?.Invoke(id);
        }

        private void FollowLatest() { _scrollFollower.Follow(); Canvas.ForceUpdateCanvases(); _transcriptScroll.verticalNormalizedPosition = 0; }
        private void FocusChoicesOrTargets()
        {
            if (EventSystem.current != null && (_menuCategory == "Accuse" || _menuCategory == "Defend" || _menuCategory == "Targets"
                || _request?.CompositionPrompt != null && _request.Options.Any(o => o.Target.HasValue)))
            {
                var card = _cards.Values.OrderBy(c => c.Id.Value).FirstOrDefault(c => c.GetComponent<Button>().interactable);
                if (card != null) { EventSystem.current.SetSelectedGameObject(card.gameObject); return; }
            }
            FocusFirstChoice();
        }
        private void FocusFirstChoice()
        {
            Canvas.ForceUpdateCanvases();
            if (_buttons.Count > 0 && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
        }
        private void OnDisable() { ResetDisplay(); }
    }
}
