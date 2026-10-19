using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocialStim.Application.Council;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Council;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialStim.Presentation.Council
{
    /// <summary>Thin screen/input adapter around the portable controller and one-line display player.</summary>
    public sealed class CouncilScreen : MonoBehaviour
    {
        private CouncilLayout _layout;
        private CouncilDialoguePlayer _dialogue;
        private CouncilController _controller;
        private EffectsSettings _settings;
        private TextPalette _palette;
        private readonly HashSet<CharacterId> _deliveredDeaths = new HashSet<CharacterId>();
        private readonly Dictionary<CharacterId, RoleId> _deliveredClaims = new Dictionary<CharacterId, RoleId>();
        private readonly Dictionary<CharacterId, string> _marks = new Dictionary<CharacterId, string>();
        private readonly List<CouncilEvent> _delivered = new List<CouncilEvent>();
        private CharacterId? _speaker;
        private int _readyFrame;
        private bool _drawer;
        public event Action NewRunRequested;
        public event Action SettingsRequested;
        public bool HasRun => _controller != null;
        public bool IsDisplaying => _dialogue != null && _dialogue.Pending;
        public CouncilController Controller => _controller;

        public void Initialize(TMP_FontAsset font, EffectsSettings settings, IReadOnlyDictionary<PresentationCue, EffectProfile> profiles = null)
        {
            _settings = settings; _layout = new CouncilLayout(transform, font); _palette = new TextPalette(false);
            var renderer = gameObject.AddComponent<TextEffectRenderer>();
            _dialogue = new CouncilDialoguePlayer(renderer, _layout.Body, settings, profiles ?? DefaultCueProfiles.Create());
            _dialogue.Started += StartLine;
            _dialogue.Delivered += record => {
                _delivered.Add(record);
                if (record.Kind == CouncilEventKind.Death || record.Kind == CouncilEventKind.Elimination)
                    if (record.Target.HasValue) _deliveredDeaths.Add(record.Target.Value);
                if (record.Kind == CouncilEventKind.Claim && record.Actor.HasValue && record.Claim.HasValue)
                    _deliveredClaims[record.Actor.Value] = record.Claim.Value;
            };
            _dialogue.Completed += ShowInputs;
            var lineClick = _layout.Body.gameObject.AddComponent<Button>();
            _layout.Body.raycastTarget = true; lineClick.targetGraphic = _layout.Body;
            lineClick.transition = Selectable.Transition.None; lineClick.navigation = new Navigation { mode = Navigation.Mode.None };
            lineClick.onClick.AddListener(() => { if (!_drawer && _dialogue.Pending) { _dialogue.Advance(); if (_dialogue.Pending) ShowAdvance(); } });
            _layout.Menu.onClick.AddListener(() => { if (_drawer && _controller != null) Resume(); else ShowDrawer(); });
            Canvas.ForceUpdateCanvases(); _layout.Resize(); ShowMainMenu();
        }

        public void Begin(CouncilSession session, bool quickMode = false)
        {
            _controller = new CouncilController(session) { QuickMode = quickMode };
            _delivered.Clear(); _deliveredDeaths.Clear(); _deliveredClaims.Clear(); _marks.Clear(); _speaker = null;
            _dialogue.Reset(); Resume(); Render();
        }
        public void ApplySettings(EffectsSettings settings)
        { _settings = settings; _dialogue.Settings(settings); }
        public void ShowMainMenu()
        {
            OpenPanel("SocialStim", "");
            if (_controller != null)
                PanelButton(_controller.Snapshot.Phase == CouncilPhase.Ended ? "After-action report" : "Continue",
                    _controller.Snapshot.Phase == CouncilPhase.Ended ? (Action)ShowAfterAction : Resume);
            PanelButton("New Run", () => NewRunRequested?.Invoke());
            if (SettingsRequested != null) PanelButton("Settings", () => SettingsRequested?.Invoke());
            PanelButton("Quit", ApplicationQuit);
        }
        public void ShowSetup(IReadOnlyList<RoleSpec> roles, string description, Action<RoleId?, Faction?> begin)
        {
            OpenPanel("New Run", description);
            PanelButton("Random role", () => begin(null, null));
            foreach (Faction faction in new[] { Faction.Town, Faction.Antagonist, Faction.Neutral })
            {
                var side = faction;
                PanelButton(side == Faction.Antagonist ? "Evil" : side.ToString(), () => {
                    OpenPanel("Choose a role", description);
                    PanelButton("Random " + (side == Faction.Antagonist ? "Evil" : side.ToString()), () => begin(null, side));
                    foreach (var role in roles.Where(r => r.Faction == side))
                    { var id = role.Id; PanelButton(role.Name, () => begin(id, null)); }
                    PanelButton("Back", () => ShowSetup(roles, description, begin));
                });
            }
            PanelButton("Back", ShowMainMenu);
        }
        private void Update()
        {
            if (_dialogue == null || _drawer) return;
            _dialogue.Tick(Time.unscaledDeltaTime);
            if (_dialogue.Pending && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            { _dialogue.Advance(); if (_dialogue.Pending) ShowAdvance(); }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ShowDrawer();
        }
        private void OnRectTransformDimensionsChange() { if (_layout != null) _layout.Resize(); }
        private void Render()
        {
            if (_controller == null) return;
            _drawer = false; _layout.Drawer.gameObject.SetActive(false); _layout.Main.gameObject.SetActive(true);
            var snapshot = _controller.Snapshot;
            _dialogue.Render(snapshot.Events, _palette);
            if (_dialogue.Pending) ShowAdvance(); else ShowInputs();
        }
        private void StartLine(CouncilEvent record)
        {
            _speaker = record.Actor;
            var view = _controller.Snapshot;
            bool night = record.Kind == CouncilEventKind.Phase && record.Text.StartsWith("Night", StringComparison.Ordinal);
            bool morning = record.Kind == CouncilEventKind.Death || record.Kind == CouncilEventKind.Morning || record.Kind == CouncilEventKind.PrivateResult
                || record.Kind == CouncilEventKind.RoleReveal && record.Reason == ReasonType.NightPattern;
            float progress = morning ? 0 : record.Kind == CouncilEventKind.Vote || record.Kind == CouncilEventKind.Tally ? 0.8f
                : record.Kind == CouncilEventKind.Elimination ? 1 : Mathf.Clamp01((record.Round - 1f) / Mathf.Max(1, view.Rounds));
            _palette = new TextPalette(night, progress); _layout.Theme(_palette); _dialogue.Palette(_palette);
            string name = record.Actor.HasValue ? view.Knowledge.Characters.Single(c => c.Id == record.Actor.Value).Name : "";
            _layout.Speaker.text = (record.Recipient.HasValue ? "PRIVATE · " : "") + name
                + (record.Kind == CouncilEventKind.Claim ? " · CLAIM" : "");
            _layout.Header.text = "DAY " + (record.Day + (morning ? 1 : 0)) + " · " + EventStage(record, view.Phase)
                + " · ROUND " + record.Round + "/" + view.Rounds + "       ALIVE " + (view.Knowledge.Characters.Count - _deliveredDeaths.Count);
            RenderRoster(false); ShowAdvance();
        }
        private void ShowAdvance()
        {
            _readyFrame = Time.frameCount; _layout.Clear(_layout.Actions);
            _layout.Prompt.text = "";
            var button = _layout.Button(_layout.Actions, "Next [Space]", "", 76);
            button.onClick.AddListener(() => {
                if (Time.frameCount <= _readyFrame) return;
                _dialogue.Advance(); if (_dialogue.Pending) ShowAdvance();
            });
            Focus(button.gameObject);
        }
        private void ShowInputs()
        {
            if (_drawer || _controller == null || _dialogue.Pending) return;
            _readyFrame = Time.frameCount; var view = _controller.Snapshot; _layout.Clear(_layout.Actions);
            _palette = new TextPalette(view.Phase == CouncilPhase.Night,
                view.Phase == CouncilPhase.Vote || view.Phase == CouncilPhase.Elimination ? 0.9f : Mathf.Clamp01((view.Round - 1f) / view.Rounds));
            _layout.Theme(_palette); _dialogue.Palette(_palette);
            _layout.Header.text = "DAY " + (view.Phase == CouncilPhase.Morning ? view.Day + 1 : view.Day)
                + " · " + view.Phase.ToString().ToUpperInvariant()
                + (view.Phase == CouncilPhase.Discussion ? " · ROUND " + view.Round + "/" + view.Rounds : "")
                + "       ALIVE " + view.Knowledge.Characters.Count(c => c.Alive);
            _layout.Prompt.text = view.Respond ? "RESPOND" : _controller.Step == InteractionStep.Target ? "Choose a name on the roster."
                : view.Phase == CouncilPhase.Vote ? "Choose your vote on the roster." : view.Phase == CouncilPhase.Night ? "Choose a night target."
                : _controller.Step == InteractionStep.Phrasing ? "Choose your words." : "";
            RenderRoster(true);
            var options = _controller.Options();
            foreach (var option in options)
            {
                string id = option.Id;
                var button = _layout.Button(_layout.Actions, option.Title, option.Detail, 76);
                button.onClick.AddListener(() => {
                    if (_dialogue.Pending || Time.frameCount <= _readyFrame) return;
                    if (!_controller.Choose(id, out string error)) { _layout.Prompt.text = error; return; }
                    Render();
                });
            }
            if (view.Phase == CouncilPhase.Ended)
            {
                var report = _layout.Button(_layout.Actions, "After-action report");
                report.onClick.AddListener(ShowAfterAction);
                var menu = _layout.Button(_layout.Actions, "Main menu"); menu.onClick.AddListener(ShowMainMenu);
            }
            Canvas.ForceUpdateCanvases(); _layout.Resize(); _layout.ActionScroll.verticalNormalizedPosition = 1;
            if (_layout.Actions.childCount > 0) Focus(_layout.Actions.GetChild(0).gameObject);
            else
            {
                var target = _layout.Roster.GetComponentsInChildren<Button>().FirstOrDefault(b => b.interactable);
                if (target != null) Focus(target.gameObject);
            }
        }
        private void RenderRoster(bool selectable)
        {
            if (_controller == null) return;
            _layout.Clear(_layout.Roster); var view = _controller.Snapshot; var targets = _controller.Targets;
            foreach (var actor in view.Knowledge.Characters)
            {
                var id = actor.Id;
                string badge = _deliveredClaims.TryGetValue(id, out RoleId claim) ? view.Knowledge.Catalog.Single(r => r.Id == claim).Name : "";
                string mark = _marks.TryGetValue(id, out string value) ? value : "";
                string state = _deliveredDeaths.Contains(id) ? "DEAD" : actor.IsTeammate ? "TEAM" : id == view.Knowledge.Self ? "YOU" : "";
                string label = (_speaker == id ? "> " : "") + actor.Name + "\n" + state + (mark.Length > 0 ? " · " + mark : "")
                    + (badge.Length > 0 ? "\n" + badge : "");
                var button = _layout.Button(_layout.Roster, label, "", 60);
                button.interactable = selectable && targets.Contains(id);
                int revision = view.Revision;
                button.onClick.AddListener(() => {
                    if (_dialogue.Pending || Time.frameCount <= _readyFrame) return;
                    if (!_controller.ChooseTarget(id, revision, out string error)) { _layout.Prompt.text = error; return; }
                    Render();
                });
            }
            Canvas.ForceUpdateCanvases(); _layout.Resize();
        }
        private void ShowDrawer()
        {
            if (_controller == null) { ShowMainMenu(); return; }
            OpenPanel("Menu", "");
            PanelButton("History", () => ShowRecords("History", _delivered));
            PanelButton("Claims · unverified", () => ShowRecords("Claims · unverified",
                _delivered.Where(e => e.Kind == CouncilEventKind.Claim || e.Kind == CouncilEventKind.Report)));
            PanelButton("My role", ShowOwnRole);
            PanelButton("Player marks", ShowMarks);
            PanelButton("Help", () => { OpenPanel("Help", "Chat, accuse or defend, then choose a roster target and phrasing.\nSpace: reveal the current line, then advance.\nClaims are assertions; readouts can be forged. Vote to eliminate one person per day."); PanelButton("Back", ShowDrawer); });
            if (SettingsRequested != null) PanelButton("Settings", () => SettingsRequested?.Invoke());
            PanelButton("Main menu", ShowMainMenu); PanelButton("Close", Resume);
        }
        private void ShowRecords(string title, IEnumerable<CouncilEvent> records)
        {
            var view = _controller.Snapshot;
            OpenPanel(title, string.Join("\n\n", records.Select(e => "Day " + e.Day + " · "
                + (e.Actor.HasValue ? view.Knowledge.Characters.Single(c => c.Id == e.Actor.Value).Name + ": " : "") + Plain(e.Text))));
            PanelButton("Back", ShowDrawer);
        }
        private void ShowOwnRole()
        {
            var view = _controller.Snapshot; var role = view.Knowledge.OwnRole;
            string objective = role.Objective == RoleObjective.LastSurvivor ? "Be the last survivor."
                : role.Objective == RoleObjective.StealParity ? "Survive until Evil reaches parity."
                : role.Faction == Faction.Town ? "Eliminate Evil and Anarchy." : "Reach parity and eliminate Anarchy.";
            OpenPanel(role.Name, (role.Faction == Faction.Antagonist ? "Evil" : role.Faction.ToString()) + "\n" + objective
                + "\n\nUses: " + (view.Knowledge.Charges < 0 ? "Unlimited" : view.Knowledge.Charges.ToString()) + "\nSeed: " + view.Seed);
            PanelButton("Copy seed", () => GUIUtility.systemCopyBuffer = view.Seed.ToString()); PanelButton("Back", ShowDrawer);
        }
        private void ShowMarks()
        {
            var view = _controller.Snapshot; OpenPanel("Player marks", "Your notes; they do not establish anyone's role.");
            foreach (var actor in view.Knowledge.Characters.Where(c => c.Id != view.Knowledge.Self))
            {
                var id = actor.Id; string current = _marks.TryGetValue(id, out string mark) ? mark : "";
                PanelButton(actor.Name + (current.Length > 0 ? " · " + current : ""), () => {
                    _marks[id] = current == "" ? "Trust" : current == "Trust" ? "Suspect" : current == "Suspect" ? "Confirmed" : "";
                    ShowMarks();
                });
            }
            PanelButton("Back", ShowDrawer);
        }
        private void ShowAfterAction()
        {
            var view = _controller.Snapshot; if (view.Phase != CouncilPhase.Ended) return;
            var text = new StringBuilder();
            foreach (var role in view.FinalRoles)
                text.Append(view.Knowledge.Characters.Single(c => c.Id == role.Key).Name).Append(" · ")
                    .Append(view.Knowledge.Catalog.Single(r => r.Id == role.Value).Name).Append('\n');
            foreach (var record in view.AfterAction.Where(e => e.Kind == CouncilEventKind.NightOrder || e.Kind == CouncilEventKind.Vote))
                text.Append("\nDay ").Append(record.Day).Append(" · ").Append(record.Text);
            OpenPanel("After-action report", text.ToString()); PanelButton("Close", Resume);
        }
        private void OpenPanel(string title, string text)
        {
            _drawer = true; _layout.Main.gameObject.SetActive(false); _layout.Drawer.gameObject.SetActive(true);
            _layout.DrawerTitle.text = title; _layout.DrawerText.text = text; _layout.Clear(_layout.DrawerButtons);
            Canvas.ForceUpdateCanvases(); _layout.Resize();
        }
        private void PanelButton(string title, Action action)
        { var button = _layout.Button(_layout.DrawerButtons, title, "", 48); button.onClick.AddListener(() => action());
            if (_layout.DrawerButtons.childCount == 1) Focus(button.gameObject); }
        private void Resume() { _drawer = false; _layout.Drawer.gameObject.SetActive(false); _layout.Main.gameObject.SetActive(true); if (_controller != null) Render(); else ShowMainMenu(); }
        private static void Focus(GameObject target) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target); }
        private static string Plain(string text) { try { return SemanticMarkup.Parse(text).Text; } catch (ArgumentException) { return text; } }
        private static string EventStage(CouncilEvent e, CouncilPhase phase) => e.Kind == CouncilEventKind.Vote || e.Kind == CouncilEventKind.Tally || e.Kind == CouncilEventKind.Tie
            ? "VOTE" : e.Kind == CouncilEventKind.RoleReveal && e.Reason == ReasonType.NightPattern ? "MORNING"
            : e.Kind == CouncilEventKind.Elimination || e.Kind == CouncilEventKind.RoleReveal || e.Intent == SpeechIntent.LastWords ? "ELIMINATION"
            : e.Kind == CouncilEventKind.Death || e.Kind == CouncilEventKind.Morning || e.Kind == CouncilEventKind.PrivateResult ? "MORNING"
            : e.Kind == CouncilEventKind.GameOver ? "GAME OVER" : phase == CouncilPhase.Night ? "NIGHT" : "DISCUSSION";
        private static void ApplicationQuit() { UnityEngine.Application.Quit(); }
    }
}
