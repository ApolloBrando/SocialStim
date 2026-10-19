using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Plain textual focus marker so keyboard selection never relies on color.</summary>
    public sealed class ChoiceFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private TextMeshProUGUI _label;
        private string _text;
        private ScrollRect _scroll;

        /// <summary>Wires the existing button label without copying game state.</summary>
        public void Initialize(TextMeshProUGUI label, string text) { _label = label; _text = text; _scroll = GetComponentInParent<ScrollRect>(); }
        public void SetText(string text)
        {
            _text = text;
            if (_label != null) _label.text = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject ? "> " + _text : _text;
        }
        /// <summary>Marks the currently selected choice.</summary>
        public void OnSelect(BaseEventData eventData)
        {
            if (_label != null) _label.text = "> " + _text;
            if (_scroll == null || _scroll.viewport == null || _scroll.content == null) return;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_scroll.viewport, (RectTransform)transform);
            Rect viewport = _scroll.viewport.rect;
            float shift = bounds.max.y > viewport.yMax ? viewport.yMax - bounds.max.y
                : bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y : 0;
            if (shift != 0) { _scroll.StopMovement(); _scroll.content.anchoredPosition += new Vector2(0, shift); }
        }
        /// <summary>Restores the authored choice label after focus leaves.</summary>
        public void OnDeselect(BaseEventData eventData) { if (_label != null) _label.text = _text; }
    }
}
