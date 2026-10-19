using System.Collections.Generic;
using SocialStim.Presentation.Effects;
using SocialStim.Presentation.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Council
{
    /// <summary>Native uGUI construction/theme adapter. Owns no rules or interaction decisions.</summary>
    internal sealed class CouncilLayout
    {
        private readonly TMP_FontAsset _font;
        private readonly List<Image> _surfaces = new List<Image>();
        private readonly List<TextMeshProUGUI> _labels = new List<TextMeshProUGUI>();
        private readonly List<Button> _buttons = new List<Button>();
        internal CouncilLayout(Transform parent, TMP_FontAsset font)
        {
            _font = font;
            Root = TextUiFactory.Rect("Council", parent);
            var canvas = Root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = Root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 0.5f;
            Root.gameObject.AddComponent<GraphicRaycaster>();
            Background = Root.gameObject.AddComponent<Image>();
            Header = Label("Header", Root, 20, new Vector2(32, -65), new Vector2(-170, -24), new Vector2(0, 1), Vector2.one);
            Menu = Button(Root, "≡", "", 44);
            TextUiFactory.Fill((RectTransform)Menu.transform, Vector2.one, Vector2.one, new Vector2(-126, -65), new Vector2(-32, -21));
            Main = TextUiFactory.Rect("Discussion", Root); TextUiFactory.Fill(Main, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RosterScroll = TextUiFactory.Scroll("Roster", Main, out RectTransform roster);
            Roster = roster; _surfaces.Add(RosterScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)RosterScroll.transform, new Vector2(0, 1), Vector2.one, new Vector2(32, -226), new Vector2(-32, -84));
            TextUiFactory.Fill(RosterScroll.viewport, Vector2.zero, Vector2.one, new Vector2(8, 6), new Vector2(-8, -6));
            RosterGrid = roster.gameObject.AddComponent<GridLayoutGroup>();
            RosterGrid.cellSize = new Vector2(112, 60); RosterGrid.spacing = new Vector2(8, 8);
            RosterGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; RosterGrid.constraintCount = 10;
            roster.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Speaker = Label("Speaker", Main, 18, new Vector2(40, -272), new Vector2(-40, -241), new Vector2(0, 1), Vector2.one);
            Speaker.fontStyle = FontStyles.Bold;
            Body = Label("Current line", Main, 25, new Vector2(40, 252), new Vector2(-40, -288), Vector2.zero, Vector2.one);
            Body.lineSpacing = 4;
            Prompt = Label("Prompt", Main, 15, new Vector2(36, 219), new Vector2(-36, 244), Vector2.zero, new Vector2(1, 0));
            ActionScroll = TextUiFactory.Scroll("Actions", Main, out RectTransform actions); Actions = actions;
            _surfaces.Add(ActionScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)ActionScroll.transform, Vector2.zero, new Vector2(1, 0), new Vector2(32, 24), new Vector2(-32, 210));
            TextUiFactory.Fill(ActionScroll.viewport, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            ActionGrid = actions.gameObject.AddComponent<GridLayoutGroup>(); ActionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            ActionGrid.constraintCount = 3; ActionGrid.cellSize = new Vector2(390, 76); ActionGrid.spacing = new Vector2(8, 8);
            actions.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Drawer = TextUiFactory.Rect("Drawer", Root); var surface = Drawer.gameObject.AddComponent<Image>(); TextUiFactory.Border(Drawer); _surfaces.Add(surface);
            TextUiFactory.Fill(Drawer, Vector2.zero, Vector2.one, new Vector2(32, 24), new Vector2(-32, -84));
            DrawerTitle = Label("Drawer title", Drawer, 25, new Vector2(24, -64), new Vector2(-24, -24), new Vector2(0, 1), Vector2.one);
            var drawerScroll = TextUiFactory.Scroll("Drawer text", Drawer, out RectTransform textContent); _surfaces.Add(drawerScroll.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)drawerScroll.transform, Vector2.zero, Vector2.one, new Vector2(20, 220), new Vector2(-20, -76));
            TextUiFactory.Fill(drawerScroll.viewport, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            DrawerText = TextUiFactory.Text("Details", textContent, _font, 18); _labels.Add(DrawerText);
            var textLayout = textContent.gameObject.AddComponent<VerticalLayoutGroup>(); textLayout.childControlWidth = true; textLayout.childControlHeight = true; textLayout.childForceExpandHeight = false;
            textContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var controls = TextUiFactory.Scroll("Drawer controls", Drawer, out RectTransform drawerButtons);
            DrawerButtons = drawerButtons; _surfaces.Add(controls.GetComponent<Image>());
            TextUiFactory.Fill((RectTransform)controls.transform, Vector2.zero, new Vector2(1, 0), new Vector2(20, 20), new Vector2(-20, 200));
            TextUiFactory.Fill(controls.viewport, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            DrawerGrid = DrawerButtons.gameObject.AddComponent<GridLayoutGroup>(); DrawerGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            DrawerGrid.constraintCount = 3; DrawerGrid.cellSize = new Vector2(375, 48); DrawerGrid.spacing = new Vector2(8, 8);
            DrawerButtons.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Drawer.gameObject.SetActive(false);
            Theme(new TextPalette(false));
        }
        internal RectTransform Root { get; }
        internal RectTransform Main { get; }
        internal Image Background { get; }
        internal TextMeshProUGUI Header { get; }
        internal Button Menu { get; }
        internal RectTransform Roster { get; }
        internal ScrollRect RosterScroll { get; }
        internal GridLayoutGroup RosterGrid { get; }
        internal TextMeshProUGUI Speaker { get; }
        internal TextMeshProUGUI Body { get; }
        internal TextMeshProUGUI Prompt { get; }
        internal RectTransform Actions { get; }
        internal ScrollRect ActionScroll { get; }
        internal GridLayoutGroup ActionGrid { get; }
        internal RectTransform Drawer { get; }
        internal TextMeshProUGUI DrawerTitle { get; }
        internal TextMeshProUGUI DrawerText { get; }
        internal RectTransform DrawerButtons { get; }
        internal GridLayoutGroup DrawerGrid { get; }
        private TextPalette _palette;

        internal Button Button(Transform parent, string title, string detail = "", int height = 76)
        {
            var button = TextUiFactory.Button(parent, _font, string.IsNullOrEmpty(detail) ? title : title + "\n" + detail, height);
            _buttons.Add(button); ThemeButton(button); return button;
        }
        internal void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i); var button = child.GetComponent<Button>();
                if (button != null) { button.interactable = false; _buttons.Remove(button); }
                child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject);
            }
        }
        internal void Theme(TextPalette palette)
        {
            _palette = palette; Background.color = palette.Background;
            foreach (var surface in _surfaces) { surface.color = palette.Panel; var border = surface.GetComponent<Outline>(); if (border != null) border.effectColor = palette.Frame; }
            foreach (var label in _labels) label.color = palette.Foreground;
            foreach (var button in _buttons) if (button != null) ThemeButton(button);
        }
        private void ThemeButton(Button button)
        {
            button.GetComponent<ChoiceFocusIndicator>().SetSurfaceColors(_palette.ButtonBackground, _palette.FocusBackground);
            var border = button.GetComponent<Outline>(); if (border != null) border.effectColor = _palette.Frame;
            foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                label.color = label.name == "Detail" ? _palette.Muted : _palette.Foreground;
        }
        internal void Resize()
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt(RosterScroll.viewport.rect.width / 120));
            RosterGrid.constraintCount = columns;
            float width = ActionScroll.viewport.rect.width;
            ActionGrid.cellSize = new Vector2(Mathf.Max(90, (width - 16) / 3), ActionGrid.cellSize.y);
            DrawerGrid.cellSize = new Vector2(Mathf.Max(90, (DrawerButtons.rect.width - 16) / 3), 48);
        }
        private TextMeshProUGUI Label(string name, Transform parent, int size, Vector2 min, Vector2 max, Vector2 anchorsMin, Vector2 anchorsMax)
        {
            var label = TextUiFactory.Text(name, parent, _font, size); _labels.Add(label);
            TextUiFactory.Fill(label.rectTransform, anchorsMin, anchorsMax, min, max); return label;
        }
    }
}
