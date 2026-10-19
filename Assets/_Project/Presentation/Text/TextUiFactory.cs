using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Scene adapter creating plain uGUI geometry; contains no game rules.</summary>
    internal static class TextUiFactory
    {
        internal static RectTransform Rect(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            value.SetParent(parent, false);
            return value;
        }

        internal static void Fill(RectTransform rect, Vector2 minimum, Vector2 maximum, Vector2 offsetMinimum, Vector2 offsetMaximum)
        { rect.anchorMin = minimum; rect.anchorMax = maximum; rect.offsetMin = offsetMinimum; rect.offsetMax = offsetMaximum; }

        internal static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, int size)
        {
            RectTransform rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.richText = false;
            text.color = new Color(0.90f, 0.91f, 0.93f); text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            Fill(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return text;
        }

        internal static ScrollRect Scroll(string name, Transform parent, out RectTransform content)
        {
            RectTransform root = Rect(name, parent);
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.055f, 0.065f, 0.08f);
            Border(root);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 32; scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform viewport = Rect("Viewport", root);
            Fill(viewport, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport; scroll.content = content;
            return scroll;
        }

        internal static void Border(RectTransform root)
        {
            for (int edge = 0; edge < 4; edge++)
            {
                RectTransform line = Rect("Frame " + edge, root);
                var image = line.gameObject.AddComponent<Image>();
                image.color = new Color(0.45f, 0.42f, 0.46f, 0.85f); image.raycastTarget = false;
                if (edge < 2)
                    Fill(line, new Vector2(0, edge), new Vector2(1, edge), new Vector2(0, edge == 0 ? 0 : -2), new Vector2(0, edge == 0 ? 2 : 0));
                else
                    Fill(line, new Vector2(edge - 2, 0), new Vector2(edge - 2, 1), new Vector2(edge == 2 ? 0 : -2, 0), new Vector2(edge == 2 ? 2 : 0, 0));
            }
        }

        internal static Button Button(Transform parent, TMP_FontAsset font, string label, int height)
        {
            RectTransform rect = Rect("Choice", parent);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.13f, 0.16f, 0.20f);
            Border(rect);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var layout = rect.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = height; layout.minHeight = height;
            var text = Text("Label", rect, font, 16); text.text = label;
            Fill(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8));
            rect.gameObject.AddComponent<ChoiceFocusIndicator>().Initialize(text, label);
            return button;
        }
    }
}
