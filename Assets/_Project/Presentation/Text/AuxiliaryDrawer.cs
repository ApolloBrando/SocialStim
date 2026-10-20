using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Phases;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Secondary information behind one modal drawer; consumes only already-delivered public/scoped observations.</summary>
    public sealed class AuxiliaryDrawer : MonoBehaviour
    {
        private RectTransform _root;
        private TextMeshProUGUI _text;
        private Button _close;
        private GameObject _previousFocus;
        private IReadOnlyList<GameObservation> _history = Array.Empty<GameObservation>();
        private PlayerView _view;
        /// <summary>Builds history/ledger/role/help/review navigation with explicit external actions.</summary>
        public void Initialize(Transform parent, TMP_FontAsset font, Action settings, Action restart)
        {
            _root = TextUiFactory.Rect("Information Drawer", parent);
            TextUiFactory.Fill(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            var panel = TextUiFactory.Rect("Drawer", _root); panel.gameObject.AddComponent<Image>();
            TextUiFactory.Fill(panel, new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.95f), Vector2.zero, Vector2.zero);
            var actions = TextUiFactory.Rect("Sections", panel);
            TextUiFactory.Fill(actions, new Vector2(0, 0.78f), Vector2.one, new Vector2(16, 4), new Vector2(-16, -16));
            var grid = actions.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(164, 40); grid.spacing = new Vector2(8, 8);
            Add(actions, font, "History", () => Show(string.Join("\n\n", _history.Select(o => Plain(o.Text)))));
            Add(actions, font, "Claims ledger", () => Show(string.Join("\n\n", _history.Where(o => o.Intent == SocialStim.Rules.Dialogue.SpeechIntent.ClaimRole
                || o.Intent == SocialStim.Rules.Dialogue.SpeechIntent.ReportInvestigation || o.Intent == SocialStim.Rules.Dialogue.SpeechIntent.ReportAction)
                .Select(o => "Day " + o.Day + " · " + Plain(o.Text)))));
            Add(actions, font, "Your role", () => Show(_view == null ? "Start a run to receive a role." : _view.OwnRole.Name + " · " + _view.OwnRole.Faction
                + "\n\n" + Objective(_view) + "\n\nRemaining uses: " + (_view.Charges < 0 ? "Unlimited" : _view.Charges.ToString())));
            Add(actions, font, "After-action report", () => Show(_view == null || _view.AfterAction.Count == 0
                ? "Available after the run ends." : string.Join("\n", _view.AfterAction)));
            Add(actions, font, "Help", () => Show("Chat, accuse or defend, then select a valid roster target. Choose the words you want to say.\n\n"
                + "First Space/click reveals the line; the next advances. Pass leaves the floor. Back keeps your turn.\n\n"
                + "Claims are assertions, not proven roles. Your role and private results are only yours.\n\n"
                + "Settings change display immediately and game rules on the next run."));
            Add(actions, font, "Settings", () => { Close(); settings(); });
            Add(actions, font, "Copy seed", () => { if (_view != null) { GUIUtility.systemCopyBuffer = _view.Seed.ToString(); Show("Seed copied: " + _view.Seed); } });
            Add(actions, font, "New run", () => { Close(); restart(); });
            var scroll = TextUiFactory.Scroll("Information", panel, out RectTransform content);
            TextUiFactory.Fill((RectTransform)scroll.transform, Vector2.zero, new Vector2(1, 0.78f), new Vector2(16, 66), new Vector2(-16, -8));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = TextUiFactory.Text("Information Text", content, font, 18);
            _close = Add(panel, font, "Close drawer", Close);
            TextUiFactory.Fill((RectTransform)_close.transform, Vector2.zero, Vector2.zero, new Vector2(16, 16), new Vector2(196, 56));
            _root.gameObject.SetActive(false);
        }
        /// <summary>Opens with only observations the player has finished viewing.</summary>
        public void Open(PlayerView view, IReadOnlyList<GameObservation> history, TextPalette palette)
        {
            _view = view; _history = history; _previousFocus = EventSystem.current?.currentSelectedGameObject;
            _root.SetAsLastSibling(); _root.gameObject.SetActive(true); Show("History, claims and private role information.");
            foreach (Image image in _root.GetComponentsInChildren<Image>(true)) if (image.transform != _root) image.color = palette.Panel;
            foreach (TextMeshProUGUI label in _root.GetComponentsInChildren<TextMeshProUGUI>(true)) label.color = palette.Foreground;
            foreach (Button button in _root.GetComponentsInChildren<Button>(true)) button.GetComponent<ChoiceFocusIndicator>()?.SetSurfaceColors(palette.ButtonBackground, palette.FocusBackground);
            EventSystem.current?.SetSelectedGameObject(_close.gameObject);
        }
        /// <summary>Closes secondary information and restores usable prior focus.</summary>
        public void Close()
        { _root.gameObject.SetActive(false); if (_previousFocus != null && _previousFocus.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(_previousFocus); }
        /// <summary>Whether the modal currently owns input.</summary>
        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        private void Show(string text) => _text.text = string.IsNullOrEmpty(text) ? "No entries yet." : text;
        private static Button Add(Transform parent, TMP_FontAsset font, string title, Action chosen)
        { Button button = TextUiFactory.Button(parent, font, title, 40); button.onClick.AddListener(() => chosen()); return button; }
        private static string Plain(string text)
        { try { return SocialStim.Application.PresentationCues.SemanticMarkup.Parse(text).Text; } catch (ArgumentException) { return text; } }
        private static string Objective(PlayerView view) => view.OwnRole.Faction == SocialStim.Rules.Roles.Faction.Town
            ? "Eliminate the hostile factions." : view.OwnRole.Faction == SocialStim.Rules.Roles.Faction.Antagonist
            ? "Help your team take control." : view.OwnRole.Category == SocialStim.Rules.Roles.RoleCategory.NeutralEvil
            ? "Be eliminated by the daytime vote." : view.OwnRole.Category == SocialStim.Rules.Roles.RoleCategory.NeutralKilling
            ? "Eliminate the competing factions." : "Survive to the end.";
        private void Update()
        {
            if (!IsOpen) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(_root)) EventSystem.current?.SetSelectedGameObject(_close.gameObject);
        }
    }
}
