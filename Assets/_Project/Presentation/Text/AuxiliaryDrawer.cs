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
        private SocialStim.Rules.Dialogue.TextTemplates _copy;
        private TextMeshProUGUI _text;
        private Button _close;
        private GameObject _previousFocus;
        private IReadOnlyList<GameObservation> _history = Array.Empty<GameObservation>();
        private PlayerView _view;
        /// <summary>Builds history/ledger/role/help/review navigation with explicit external actions.</summary>
        public void Initialize(Transform parent, TMP_FontAsset font, SocialStim.Rules.Dialogue.TextTemplates copy, Action settings, Action restart)
        {
            _copy = copy;
            _root = TextUiFactory.Rect("Information Drawer", parent);
            TextUiFactory.Fill(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            var panel = TextUiFactory.Rect("Drawer", _root); panel.gameObject.AddComponent<Image>();
            TextUiFactory.Fill(panel, new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.95f), Vector2.zero, Vector2.zero);
            var actions = TextUiFactory.Rect("Sections", panel);
            TextUiFactory.Fill(actions, new Vector2(0, 0.78f), Vector2.one, new Vector2(16, 4), new Vector2(-16, -16));
            var grid = actions.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(164, 40); grid.spacing = new Vector2(8, 8);
            Add(actions, font, _copy.Render("drawer.history"), () => Show(string.Join("\n\n", _history.Select(o => Plain(o.Text)))));
            Add(actions, font, _copy.Render("drawer.claims"), () => Show(string.Join("\n\n", _history.Where(o => o.Intent == SocialStim.Rules.Dialogue.SpeechIntent.ClaimRole
                || o.Intent == SocialStim.Rules.Dialogue.SpeechIntent.ReportInvestigation || o.Intent == SocialStim.Rules.Dialogue.SpeechIntent.ReportAction)
                .Select(o => _copy.Render("drawer.ledgerEntry", ("day", o.Day), ("line", Plain(o.Text)))))));
            Add(actions, font, _copy.Render("drawer.role"), () => Show(_view == null ? _copy.Render("drawer.noRole") : _copy.Render("drawer.roleInfo", ("role", _view.OwnRole.Name), ("faction", _copy.Render("faction." + _view.OwnRole.Faction)), ("objective", Objective(_view)), ("uses", _view.Charges < 0 ? _copy.Render("drawer.unlimited") : _view.Charges.ToString()))));
            Add(actions, font, _copy.Render("drawer.review"), () => Show(_view == null || _view.AfterAction.Count == 0
                ? _copy.Render("drawer.noReview") : string.Join("\n", _view.AfterAction)));
            Add(actions, font, _copy.Render("drawer.helpLabel"), () => Show(_copy.Render("drawer.help")));
            Add(actions, font, _copy.Render("drawer.settings"), () => { Close(); settings(); });
            Add(actions, font, _copy.Render("drawer.copySeed"), () => { if (_view != null) { GUIUtility.systemCopyBuffer = _view.Seed.ToString(); Show(_copy.Render("drawer.seedCopied", ("seed", _view.Seed))); } });
            Add(actions, font, _copy.Render("drawer.newRun"), () => { Close(); restart(); });
            var scroll = TextUiFactory.Scroll("Information", panel, out RectTransform content);
            TextUiFactory.Fill((RectTransform)scroll.transform, Vector2.zero, new Vector2(1, 0.78f), new Vector2(16, 66), new Vector2(-16, -8));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = TextUiFactory.Text("Information Text", content, font, 18);
            _close = Add(panel, font, _copy.Render("drawer.close"), Close);
            TextUiFactory.Fill((RectTransform)_close.transform, Vector2.zero, Vector2.zero, new Vector2(16, 16), new Vector2(196, 56));
            _root.gameObject.SetActive(false);
        }
        /// <summary>Opens with only observations the player has finished viewing.</summary>
        public void Open(PlayerView view, IReadOnlyList<GameObservation> history, TextPalette palette)
        {
            _view = view; _history = history; _previousFocus = EventSystem.current?.currentSelectedGameObject;
            _root.SetAsLastSibling(); _root.gameObject.SetActive(true); Show(_copy.Render("drawer.intro"));
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
        private void Show(string text) => _text.text = string.IsNullOrEmpty(text) ? _copy.Render("drawer.empty") : text;
        private static Button Add(Transform parent, TMP_FontAsset font, string title, Action chosen)
        { Button button = TextUiFactory.Button(parent, font, title, 40); button.onClick.AddListener(() => chosen()); return button; }
        private static string Plain(string text)
        { try { return SocialStim.Application.PresentationCues.SemanticMarkup.Parse(text).Text; } catch (ArgumentException) { return text; } }
        private string Objective(PlayerView view) => view.OwnRole.Faction == SocialStim.Rules.Roles.Faction.Town
            ? _copy.Render("objective.Town") : view.OwnRole.Faction == SocialStim.Rules.Roles.Faction.Antagonist
            ? _copy.Render("objective.Antagonist") : view.OwnRole.Category == SocialStim.Rules.Roles.RoleCategory.NeutralEvil
            ? _copy.Render("objective.NeutralEvil") : view.OwnRole.Category == SocialStim.Rules.Roles.RoleCategory.NeutralKilling
            ? _copy.Render("objective.NeutralKilling") : _copy.Render("objective.NeutralBenign");
        private void Update()
        {
            if (!IsOpen) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(_root)) EventSystem.current?.SetSelectedGameObject(_close.gameObject);
        }
    }
}
