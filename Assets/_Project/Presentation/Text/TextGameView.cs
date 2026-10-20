using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Application.Settings;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Single-speaker native view with shallow action/target/phrasing navigation and manual line advancement.</summary>
    public sealed class TextGameView : MonoBehaviour, IGameView, IPlayerInputProvider
    {
        private TMP_FontAsset _font;
        private TextMeshProUGUI _header, _speaker, _line, _hint;
        private RectTransform _choices, _actions, _roster;
        private Image _background;
        private Button _advance;
        private CanvasScaler _scaler;
        private SettingsPanel _settingsPanel;
        private AuxiliaryDrawer _drawer;
        private TextEffectRenderer _renderer;
        private ScreenEffectOverlay _overlay;
        private GamePreferences _preferences = new GamePreferences();
        private GlyphStyleCatalog _styles;
        private IReadOnlyDictionary<PresentationCue, EffectProfile> _profiles;
        private TextPalette _palette;
        private readonly DisplayPlayback _playback = new DisplayPlayback();
        private VisibleCueResolver _cues = new VisibleCueResolver();
        private readonly List<GameObservation> _delivered = new List<GameObservation>();
        private readonly List<Button> _choiceButtons = new List<Button>(), _actionButtons = new List<Button>();
        private readonly Dictionary<CharacterId, ParticipantCard> _cards = new Dictionary<CharacterId, ParticipantCard>();
        private readonly Dictionary<CharacterId, string> _claims = new Dictionary<CharacterId, string>();
        private readonly Dictionary<RoleId, string> _roleNames = new Dictionary<RoleId, string>();
        private readonly Dictionary<CharacterId, string> _marks = new Dictionary<CharacterId, string>();
        private PlayerInputRequest _request;
        private PlayerView _committedView;
        private GameObservation _current;
        private IReadOnlyList<RoleSpec> _setupRoles;
        private Action<RoleId?> _start;
        private int _next, _readyFrame;
        private bool _lineReady, _choosingTarget;
        private float _readyAt;
        private InteractionAction? _action;
        private CharacterId? _target;
        /// <summary>Emits only a revision-scoped application option/phrasing ID.</summary>
        public event Action<string> OptionChosen;
        /// <summary>Returns to the setup screen, discarding the current run through the composition root.</summary>
        public event Action RestartRequested;
        /// <summary>Compatibility event for display-policy consumers.</summary>
        public event Action<EffectsSettings> SettingsChanged;
        /// <summary>Persists detached preferences through the composition root.</summary>
        public event Action<GamePreferences> PreferencesChanged;
        /// <summary>Pending text remains input-blocking until explicitly advanced or auto-advanced.</summary>
        public bool IsDisplaying => _current != null || _request != null && _next < _request.View.Transcript.Count;
        /// <summary>Builds reusable screen surfaces; no scene YAML is needed.</summary>
        public void Initialize(TMP_FontAsset font, IReadOnlyDictionary<PresentationCue, EffectProfile> profiles = null,
            EffectsSettings initialSettings = null, GlyphStyleCatalog styles = null)
        {
            _font = font; _profiles = profiles ?? DefaultCueProfiles.Create(); _styles = styles ?? GlyphStyleCatalog.Default;
            _preferences = new GamePreferences(initialSettings);
            var canvas = new GameObject("Text Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _scaler = canvas.GetComponent<CanvasScaler>(); _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1280, 800); _scaler.matchWidthOrHeight = 0.5f;
            _background = canvas.AddComponent<Image>();
            _header = TextUiFactory.Text("Day and phase", canvas.transform, font, 20);
            TextUiFactory.Fill(_header.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(32, -65), new Vector2(-130, -24));
            Button menu = Button(canvas.transform, "≡", () => _drawer.Open(_committedView, _delivered.ToList().AsReadOnly(), _palette), 42);
            TextUiFactory.Fill((RectTransform)menu.transform, Vector2.one, Vector2.one, new Vector2(-90, -64), new Vector2(-32, -22));
            var rosterScroll = TextUiFactory.Scroll("Roster", canvas.transform, out _roster);
            TextUiFactory.Fill((RectTransform)rosterScroll.transform, new Vector2(0, 1), Vector2.one, new Vector2(32, -170), new Vector2(-32, -82));
            rosterScroll.horizontal = true; rosterScroll.vertical = false;
            TextUiFactory.Fill(rosterScroll.viewport, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4));
            _roster.anchorMin = Vector2.zero; _roster.anchorMax = new Vector2(0, 1); _roster.pivot = new Vector2(0, 0.5f); _roster.sizeDelta = Vector2.zero;
            _roster.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rosterLayout = _roster.gameObject.AddComponent<HorizontalLayoutGroup>(); rosterLayout.spacing = 8;
            rosterLayout.childForceExpandWidth = false; rosterLayout.childControlWidth = true; rosterLayout.childControlHeight = true;
            _speaker = TextUiFactory.Text("Speaker", canvas.transform, font, 20); _speaker.fontStyle = FontStyles.Bold;
            TextUiFactory.Fill(_speaker.rectTransform, new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.77f), Vector2.zero, Vector2.zero);
            _line = TextUiFactory.Text("Current line", canvas.transform, font, 22);
            TextUiFactory.Fill(_line.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.69f), Vector2.zero, Vector2.zero);
            _renderer = gameObject.AddComponent<TextEffectRenderer>(); _renderer.Initialize(_line);
            _advance = Button(canvas.transform, "Continue [Space]", AdvanceLine, 42);
            TextUiFactory.Fill((RectTransform)_advance.transform, new Vector2(0.7f, 0.31f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
            var choicesScroll = TextUiFactory.Scroll("Context choices", canvas.transform, out _choices);
            TextUiFactory.Fill((RectTransform)choicesScroll.transform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.30f), Vector2.zero, Vector2.zero);
            var choiceLayout = _choices.gameObject.AddComponent<VerticalLayoutGroup>(); choiceLayout.spacing = 6;
            choiceLayout.childControlHeight = true; choiceLayout.childControlWidth = true; choiceLayout.childForceExpandHeight = false;
            _choices.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _actions = TextUiFactory.Rect("Primary actions", canvas.transform);
            TextUiFactory.Fill(_actions, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.09f), Vector2.zero, Vector2.zero);
            var actionLayout = _actions.gameObject.AddComponent<HorizontalLayoutGroup>(); actionLayout.spacing = 8;
            actionLayout.childControlWidth = true; actionLayout.childControlHeight = true;
            _hint = TextUiFactory.Text("Context hint", canvas.transform, font, 14);
            TextUiFactory.Fill(_hint.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);
            var overlay = TextUiFactory.Rect("Effects Overlay", canvas.transform);
            TextUiFactory.Fill(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _overlay = overlay.gameObject.AddComponent<ScreenEffectOverlay>(); _overlay.raycastTarget = false;
            _settingsPanel = gameObject.AddComponent<SettingsPanel>();
            _settingsPanel.Initialize(canvas.transform, font, p => { ApplyPreferences(p); PreferencesChanged?.Invoke(p); });
            _drawer = gameObject.AddComponent<AuxiliaryDrawer>();
            _drawer.Initialize(canvas.transform, font, () => _settingsPanel.Toggle(_preferences), () => RestartRequested?.Invoke());
            Palette(false); ApplyPreferences(_preferences);
        }
        /// <summary>Shows role selection for a fresh run; no saved-run Continue action is implied.</summary>
        public void ShowSetup(IReadOnlyList<RoleSpec> roles, string description, Action<RoleId?> start)
        {
            ResetDisplay(); _setupRoles = roles; _start = start; _roleNames.Clear();
            foreach (RoleSpec role in roles) _roleNames.Add(role.Id, role.Name);
            _header.text = "SOCIALSTIM · NEW RUN"; _speaker.text = "Choose your role";
            _hint.text = description; Literal("Configure the next run in ≡ → Settings. Each run starts fresh.");
            Choice("Random role", () => start(null));
            foreach (var group in roles.GroupBy(r => r.Faction))
            { Faction faction = group.Key; Choice(faction + " roles", () => ShowRoles(faction)); }
            Choice("Quit", () => UnityEngine.Application.Quit()); FocusChoice();
        }
        private void ShowRoles(Faction faction)
        {
            Clear(_choiceButtons); Choice("Back", () => ShowSetup(_setupRoles, _hint.text, _start));
            foreach (RoleSpec role in _setupRoles.Where(r => r.Faction == faction).OrderBy(r => r.Name, StringComparer.Ordinal))
            { RoleId id = role.Id; Choice(role.Name, () => _start(id)); }
            FocusChoice();
        }
        /// <summary>Queues newly scoped observations without revealing final casualty/claim state before delivery.</summary>
        public void Render(PlayerInputRequest request)
        {
            _request = request; _action = null; _choosingTarget = false; Clear(_choiceButtons); Clear(_actionButtons);
            if (_committedView == null) { _committedView = request.View; RenderRoster(null); }
            StartLine();
        }
        /// <summary>Reports an actionable error without altering run state.</summary>
        public void ShowInputError(string message) { _hint.text = message; }
        /// <summary>Applies display changes immediately; new-run configuration stays separate.</summary>
        public void ApplyPreferences(GamePreferences preferences)
        {
            _preferences = preferences; _playback.UpdateSettings(preferences.Effects);
            if (_renderer != null) { _renderer.SetPolicy(preferences.Effects); _renderer.SetSemanticColors(preferences.Effects.Enabled && !preferences.Colorblind); }
            if (_line != null) _line.fontSize = preferences.TextSize;
            if (_scaler != null) _scaler.referenceResolution = new Vector2(1280, 800) / preferences.UiScale;
            _settingsPanel?.SetPreferences(preferences); SettingsChanged?.Invoke(preferences.Effects);
        }
        private void StartLine()
        {
            if (_request == null || _next >= _request.View.Transcript.Count) { CompleteBatch(); return; }
            _playback.Cancel(); _current = _request.View.Transcript[_next]; _lineReady = false;
            if (_current.Cue == GameCue.NightFalls) Palette(true);
            if (_current.Cue == GameCue.DawnBreaks) Palette(false);
            _speaker.text = _current.Speaker.HasValue ? _request.View.Characters.First(c => c.Id == _current.Speaker.Value).Name : "";
            string source = _current.Text;
            if (_current.Speaker.HasValue)
            { string prefix = _speaker.text + ": "; if (source.StartsWith(prefix, StringComparison.Ordinal)) source = source.Substring(prefix.Length); }
            SemanticText text;
            try { text = SemanticMarkup.Parse(source, _styles); } catch (ArgumentException) { text = SemanticText.Literal(source.Length > 2048 ? source.Substring(0, 2048) : source); }
            _renderer.SetDocument(text, 0, _palette); _renderer.SetPolicy(_preferences.Effects);
            RenderRoster(_current.Speaker);
            var pacing = new SpeechPacing(_current);
            _playback.Begin(_current.Sequence, text, _profiles[_cues.Resolve(_current, _request.View)], _preferences.Effects,
                ack => {
                    if (ack.Completion == DisplayCompletion.Cancelled || _current == null) return;
                    _lineReady = true; _readyAt = Time.unscaledTime; _delivered.Add(_current);
                    if (_current.Intent == SpeechIntent.ClaimRole && _current.Speaker.HasValue && _current.ClaimedRole.HasValue)
                        _claims[_current.Speaker.Value] = _roleNames.TryGetValue(_current.ClaimedRole.Value, out string role) ? role : _current.ClaimedRole.Value.Value;
                    _hint.text = "Space/click to continue";
                }, speedMultiplier: pacing.Speed);
            _advance.gameObject.SetActive(true); EventSystem.current?.SetSelectedGameObject(_advance.gameObject);
            _hint.text = _lineReady ? "Space/click to continue" : "Space/click reveals this line"; _readyFrame = Time.frameCount;
        }
        private void AdvanceLine()
        {
            if (_current == null || Time.frameCount <= _readyFrame || _drawer.IsOpen || _settingsPanel.IsOpen) return;
            if (!_lineReady) { _playback.Skip(); _readyFrame = Time.frameCount; return; }
            _current = null; _next++; StartLine();
        }
        private void CompleteBatch()
        {
            if (_request == null) return;
            _advance.gameObject.SetActive(false);
            _committedView = _request.View; RenderRoster(null); _readyFrame = Time.frameCount;
            _header.text = "DAY " + _committedView.Day + " · " + ActionLabels.Phase(_committedView.Phase).ToUpperInvariant()
                + (_committedView.Phase == GamePhase.Discussion ? " · ROUND " + _committedView.DiscussionRound + "/" + _committedView.DiscussionRoundsAllowed : "")
                + " · ALIVE " + _committedView.Characters.Count(c => c.Alive);
            _hint.text = _committedView.RespondTo.HasValue ? "Respond to " + _committedView.Characters.First(c => c.Id == _committedView.RespondTo.Value).Name : "";
            BuildActions();
        }
        private void BuildActions()
        {
            Clear(_choiceButtons); Clear(_actionButtons); _action = null; _choosingTarget = false;
            if (_request.View.Phase != GamePhase.Discussion || !_request.View.Alive)
            {
                foreach (PlayerOption option in _request.Options) { string id = option.Id; Choice(option.Label, () => Submit(id)); }
                if (_request.View.Phase == GamePhase.Ended) Choice("New run", () => RestartRequested?.Invoke());
                FocusChoice(); return;
            }
            if (_request.View.RespondTo.HasValue)
            {
                Primary("Defend yourself", () => Phrases(InteractionAction.Defend, null));
                Primary("Counter-accuse", () => Phrases(InteractionAction.Accuse, _request.View.RespondTo));
            }
            else
            {
                Primary("CHAT", () => Targets(InteractionAction.Chat)); Primary("ACCUSE", () => Targets(InteractionAction.Accuse));
                Primary("DEFEND", () => Targets(InteractionAction.Defend));
            }
            if (InteractionMenu.Available(_request, InteractionAction.Claim)) Primary("CLAIM ROLE", Claims);
            PlayerOption pass = _request.Options.FirstOrDefault(o => o.Intent == SpeechIntent.StaySilent);
            if (pass != null) Primary("Pass", () => Submit(pass.Id));
            FocusPrimary();
        }
        private void Targets(InteractionAction action)
        {
            _action = action; _choosingTarget = true; Clear(_choiceButtons); _hint.text = action + " · choose a highlighted roster target";
            if (InteractionMenu.Options(_request, action, null).Count > 0)
                Choice(action == InteractionAction.Defend ? "Yourself" : "Everyone", () => Phrases(action, null));
            Choice("Back", BuildActions); RenderRoster(null); FocusChoice();
        }
        private void Claims()
        {
            _action = InteractionAction.Claim; _choosingTarget = false; Clear(_choiceButtons);
            var claims = InteractionMenu.Options(_request, InteractionAction.Claim, null);
            if (claims.Count == 1) { Offer(claims); return; }
            foreach (PlayerOption option in claims) { PlayerOption chosen = option; Choice(option.Label, () => Offer(new[] { chosen })); }
            Choice("Back", BuildActions); FocusChoice();
        }
        private void Phrases(InteractionAction action, CharacterId? target)
        { _action = action; _target = target; _choosingTarget = false; RenderRoster(null); Offer(InteractionMenu.Options(_request, action, target)); }
        private void Offer(IReadOnlyList<PlayerOption> options)
        {
            Clear(_choiceButtons);
            var phrases = new List<(string Id, string Text)>();
            if (options.Count == 1)
            {
                PlayerOption option = options[0];
                foreach (DialogueSelection line in option.Phrasings) phrases.Add((option.Id + "|" + line.Id, "[" + Tone(option) + "] " + Plain(line.Text)));
                if (phrases.Count == 0) phrases.Add((option.Id, option.Label));
            }
            else foreach (PlayerOption option in options.OrderByDescending(o => o.Intent == SpeechIntent.ReportInvestigation))
            {
                var line = option.Phrasings.FirstOrDefault();
                phrases.Add((line == null ? option.Id : option.Id + "|" + line.Id, "[" + Tone(option) + "] " + (line == null ? option.Label : Plain(line.Text))));
            }
            if (_preferences.QuickMode && phrases.Count > 0) { Submit(phrases[0].Id); return; }
            foreach (var phrase in phrases.Take(3)) { string id = phrase.Id; Choice(phrase.Text, () => Submit(id), 64); }
            Choice("Back", () => { if (_action == InteractionAction.Claim) Claims(); else if (_request.View.RespondTo.HasValue) BuildActions(); else Targets(_action.Value); });
            FocusChoice();
        }
        private void Submit(string id) { if (!IsDisplaying && Time.frameCount > _readyFrame) OptionChosen?.Invoke(id); }
        private void RenderRoster(CharacterId? speaker)
        {
            if (_committedView == null) return;
            foreach (KnownCharacter character in _committedView.Characters)
            {
                if (!_cards.TryGetValue(character.Id, out ParticipantCard card))
                {
                    RectTransform root = TextUiFactory.Rect("Participant " + character.Id.Value, _roster);
                    card = root.gameObject.AddComponent<ParticipantCard>(); card.Initialize(character.Id, _font);
                    CharacterId id = character.Id;
                    card.Selected += selected => { if (IsDisplaying) return;
                        if (_choosingTarget && _action.HasValue && InteractionMenu.Targets(_request, _action.Value).Contains(selected)) Phrases(_action.Value, selected);
                        else if (!_choosingTarget) { string old = _marks.TryGetValue(id, out string mark) ? mark : ""; _marks[id] = old == "" ? "Trust" : old == "Trust" ? "Suspect" : old == "Suspect" ? "Confirmed" : ""; RenderRoster(null); }
                    };
                    _cards.Add(character.Id, card);
                }
                bool selectable = _choosingTarget && _action.HasValue && InteractionMenu.Targets(_request, _action.Value).Contains(character.Id);
                card.Render(character, character.Id == _committedView.PlayerId, _palette, speaker == character.Id, selectable,
                    _marks.TryGetValue(character.Id, out string marked) ? marked : "", _claims.TryGetValue(character.Id, out string claim) ? claim : "", _choosingTarget);
            }
        }
        private void Palette(bool night)
        {
            _palette = new TextPalette(night); if (_background != null) _background.color = _palette.Background;
            if (_header != null) _header.color = _palette.Foreground;
            if (_speaker != null) _speaker.color = _palette.Foreground;
            if (_hint != null) _hint.color = _palette.Muted;
            _renderer?.SetPalette(_palette); _settingsPanel?.SetPalette(_palette);
            foreach (var button in _choiceButtons.Concat(_actionButtons)) Theme(button);
        }
        private void Literal(string text) { _renderer.SetDocument(SemanticText.Literal(text), 0, _palette); _renderer.StopEffects(); }
        private void Update()
        {
            if (_request == null) return;
            bool modal = _drawer.IsOpen || _settingsPanel.IsOpen;
            if (!modal && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) AdvanceLine();
            if (!modal && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && _current != null
                && RectTransformUtility.RectangleContainsScreenPoint(_line.rectTransform, Mouse.current.position.ReadValue())) AdvanceLine();
            if (!modal && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame && _current != null
                && RectTransformUtility.RectangleContainsScreenPoint(_line.rectTransform, Touchscreen.current.primaryTouch.position.ReadValue())) AdvanceLine();
            if (!modal)
            {
                _playback.Tick(Time.unscaledDeltaTime);
                if (_current != null && _lineReady && _preferences.AutoAdvance && Time.unscaledTime - _readyAt >= _preferences.AdvanceDelay + _line.text.Length / 100f) AdvanceLine();
            }
            if (modal) _readyAt = Time.unscaledTime;
            if (_playback.Text != null) { _renderer.Apply(_playback.VisibleUtf16, _playback.Frame, _playback.Elapsed); _overlay.Apply(_playback.Frame, _palette); }
        }
        private Button Button(Transform parent, string title, Action selected, int height)
        { var button = TextUiFactory.Button(parent, _font, title, height); button.onClick.AddListener(() => selected()); Theme(button); return button; }
        private void Choice(string title, Action selected, int height = 42) => _choiceButtons.Add(Button(_choices, title, selected, height));
        private void Primary(string title, Action selected) => _actionButtons.Add(Button(_actions, title, selected, 44));
        private void Theme(Button button)
        { button.GetComponent<ChoiceFocusIndicator>().SetSurfaceColors(_palette.ButtonBackground, _palette.FocusBackground);
            foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true)) label.color = _palette.Foreground; }
        private static void Clear(List<Button> buttons)
        { foreach (Button b in buttons) { b.gameObject.SetActive(false); Destroy(b.gameObject); } buttons.Clear(); }
        private void FocusChoice() { Canvas.ForceUpdateCanvases(); if (_choiceButtons.Count > 0) EventSystem.current?.SetSelectedGameObject(_choiceButtons[0].gameObject); }
        private void FocusPrimary() { Canvas.ForceUpdateCanvases(); if (_actionButtons.Count > 0) EventSystem.current?.SetSelectedGameObject(_actionButtons[0].gameObject); }
        private static string Tone(PlayerOption option) => option.Intent == SpeechIntent.Accuse ? "Concern" : option.Intent == SpeechIntent.Defend ? "Self-defense"
            : option.Intent == SpeechIntent.Vouch ? "Support" : option.Intent == SpeechIntent.ClaimRole ? "Role" : option.Intent == SpeechIntent.ReportInvestigation ? "Result"
            : option.Intent == SpeechIntent.Disagree ? "Challenge" : option.Intent == SpeechIntent.Deflect ? "Counter" : "Conversation";
        private static string Plain(string text) { try { return SemanticMarkup.Parse(text).Text; } catch (ArgumentException) { return text; } }
        private void ResetDisplay()
        {
            if (_advance != null) _advance.gameObject.SetActive(false);
            _current = null; _playback.Cancel(); _request = null; _committedView = null; _next = 0; _delivered.Clear(); _claims.Clear(); _marks.Clear();
            _cues = new VisibleCueResolver(); Clear(_choiceButtons); Clear(_actionButtons); _renderer.StopEffects();
            foreach (var card in _cards.Values) { card.gameObject.SetActive(false); Destroy(card.gameObject); } _cards.Clear();
        }
        private void OnDisable() { if (_renderer != null) ResetDisplay(); }
    }
}
