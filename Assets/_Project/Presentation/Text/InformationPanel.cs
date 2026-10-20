using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Reusable native modal for role reference, public history and explicitly privileged diagnostics.</summary>
    public sealed class InformationPanel : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private RectTransform _content;
        private TextMeshProUGUI _title;
        private ScrollRect _scroll;
        private Button _close;
        private GameObject _previousFocus;
        private Image _panel;
        public bool IsOpen => gameObject.activeSelf;
        public void Initialize(TMP_FontAsset font, string closeLabel)
        {
            _font = font;
            var dim = gameObject.AddComponent<Image>(); dim.color = new Color(0, 0, 0, 0.58f);
            _scroll = TextUiFactory.Scroll("Information", transform, out _content);
            TextUiFactory.Fill((RectTransform)_scroll.transform, new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.90f), Vector2.zero, Vector2.zero);
            _panel = _scroll.GetComponent<Image>();
            var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _title = TextUiFactory.Text("Title", _scroll.transform, font, 20); _title.fontStyle = FontStyles.Bold;
            TextUiFactory.Fill(_title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(22, -36), new Vector2(-22, -10));
            _close = TextUiFactory.Button(transform, font, closeLabel, 40);
            TextUiFactory.Fill((RectTransform)_close.transform, new Vector2(0.40f, 0.04f), new Vector2(0.60f, 0.10f), Vector2.zero, Vector2.zero);
            _close.onClick.AddListener(Close);
            RectTransform barRoot = TextUiFactory.Rect("Vertical scrollbar", _scroll.transform);
            TextUiFactory.Fill(barRoot, new Vector2(1, 0), Vector2.one, new Vector2(-18, 20), new Vector2(-6, -48));
            var bar = barRoot.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
            var handle = TextUiFactory.Rect("Handle", barRoot); TextUiFactory.Fill(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = handle.gameObject.AddComponent<Image>(); image.color = new Color(0.45f, 0.55f, 0.65f);
            bar.handleRect = handle; bar.targetGraphic = image;
            _scroll.verticalScrollbar = bar; _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = bar, selectOnDown = bar };
            _close.navigation = navigation;
            bar.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _close, selectOnRight = _close };
            gameObject.SetActive(false);
        }
        public void Open(string title, IEnumerable<Tuple<string, string, Color>> rows, TextPalette palette, IEnumerable<string> names = null)
        {
            string[] highlights = names?.ToArray();
            if (!IsOpen && EventSystem.current != null) _previousFocus = EventSystem.current.currentSelectedGameObject;
            foreach (Transform child in _content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _title.text = title; _title.color = palette.Foreground; _panel.color = palette.Panel;
            foreach (var row in rows)
            {
                var heading = TextUiFactory.Text("Heading", _content, _font, 18); heading.text = row.Item1;
                heading.textWrappingMode = TextWrappingModes.Normal;
                heading.gameObject.AddComponent<LayoutElement>().minWidth = 0;
                heading.fontStyle = FontStyles.Bold; heading.color = row.Item3;
                var body = TextUiFactory.Text("Body", _content, _font, 16); body.text = row.Item2; body.color = palette.Foreground;
                body.textWrappingMode = TextWrappingModes.Normal; body.overflowMode = TextOverflowModes.Overflow;
                body.gameObject.AddComponent<LayoutElement>().minWidth = 0;
                if (highlights != null)
                {
                    var renderer = body.gameObject.AddComponent<TextEffectRenderer>(); renderer.Initialize(body);
                    renderer.SetDocument(ParticipantNameStyler.Highlight(SemanticText.Literal(row.Item2), highlights), int.MaxValue, palette);
                    renderer.Apply(int.MaxValue, default, 0);
                }
            }
            _close.GetComponent<ChoiceFocusIndicator>().SetSurfaceColors(palette.ButtonBackground, palette.FocusBackground);
            foreach (var label in _close.GetComponentsInChildren<TextMeshProUGUI>()) label.color = palette.Foreground;
            gameObject.SetActive(true); transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases(); _scroll.verticalNormalizedPosition = 1;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_close.gameObject);
        }
        public void Close()
        {
            gameObject.SetActive(false);
            if (EventSystem.current != null && _previousFocus != null && _previousFocus.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(_previousFocus);
        }
    }
}
