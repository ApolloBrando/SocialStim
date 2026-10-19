using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Reusable native uGUI surfaces and controls, separate from gameplay.</summary>
    internal static class TextUiFactory
    {
        private static Sprite _rounded;
        private static Texture2D _roundedTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGeneratedAssets()
        {
            if (_rounded != null) Object.Destroy(_rounded);
            if (_roundedTexture != null) Object.Destroy(_roundedTexture);
            _rounded = null; _roundedTexture = null;
        }

        private static Sprite Rounded()
        {
            if (_rounded != null) return _rounded;
            const int size = 64;
            const float radius = 12;
            _roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "SocialStim rounded UI", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0);
                    float alpha = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                }
            _roundedTexture.SetPixels32(pixels); _roundedTexture.Apply(false, false);
            _rounded = Sprite.Create(_roundedTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
            _rounded.name = "SocialStim rounded surface"; _rounded.hideFlags = HideFlags.DontSave;
            return _rounded;
        }

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
            text.color = new Color(0.16f, 0.14f, 0.13f); text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            Fill(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return text;
        }

        internal static ScrollRect Scroll(string name, Transform parent, out RectTransform content)
        {
            RectTransform root = Rect(name, parent);
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.98f, 0.97f, 0.93f); Border(root);
            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.12f, 0.09f, 0.06f, 0.08f); shadow.effectDistance = new Vector2(0, -2);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 32; scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform viewport = Rect("Viewport", root);
            Fill(viewport, Vector2.zero, Vector2.one, new Vector2(22, 20), new Vector2(-22, -48));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport; scroll.content = content;
            return scroll;
        }

        internal static void Border(RectTransform root)
        {
            Image image = root.GetComponent<Image>();
            image.sprite = Rounded(); image.type = Image.Type.Sliced;
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.48f, 0.39f, 0.30f, 0.25f); outline.effectDistance = new Vector2(1, -1);
        }

        internal static Button Button(Transform parent, TMP_FontAsset font, string label, int height)
        {
            RectTransform rect = Rect("Choice", parent);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.98f, 0.97f, 0.93f);
            Border(rect);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var layout = rect.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = height; layout.minHeight = height;
            if (height < 70) layout.preferredWidth = 144;
            int separator = label.IndexOf('\n');
            string title = separator < 0 ? label : label.Substring(0, separator);
            string detail = separator < 0 ? "" : label.Substring(separator + 1);
            var text = Text("Label", rect, font, height >= 70 ? 18 : 14); text.text = title;
            text.fontStyle = FontStyles.Bold;
            if (separator >= 0)
            {
                Fill(text.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(16, -35), new Vector2(-16, -10));
                var secondary = Text("Detail", rect, font, 14); secondary.text = detail;
                Fill(secondary.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -39));
            }
            else
            {
                text.alignment = TextAlignmentOptions.Center;
                Fill(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 6), new Vector2(-8, -6));
            }
            rect.gameObject.AddComponent<ChoiceFocusIndicator>().Initialize(text, title);
            return button;
        }
    }
}
