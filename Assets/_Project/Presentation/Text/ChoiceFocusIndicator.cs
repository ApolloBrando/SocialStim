using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Plain textual focus marker so keyboard selection never relies on color.</summary>
    public sealed class ChoiceFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshProUGUI _label;
        private string _text;
        private ScrollRect _scroll;
        private Image _surface;
        private Color _normal;
        private Color _highlight;
        private bool _focused;
        private bool _hovered;
        public void SetSurfaceColors(Color normal, Color highlight)
        { _normal = normal; _highlight = highlight; RefreshSurface(); }
        private void RefreshSurface() { if (_surface != null) _surface.color = _focused || _hovered ? _highlight : _normal; }
        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; RefreshSurface(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; RefreshSurface(); }

        /// <summary>Wires the existing button label without copying game state.</summary>
        public void Initialize(TextMeshProUGUI label, string text) { _label = label; _text = text; _scroll = GetComponentInParent<ScrollRect>(); _surface = GetComponent<Image>(); }
        public void SetText(string text)
        {
            _text = text;
            if (_label != null) _label.text = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject ? "> " + _text : _text;
        }
        /// <summary>Marks the currently selected choice.</summary>
        public void OnSelect(BaseEventData eventData)
        {
            _focused = true; RefreshSurface();
            if (_label != null) _label.text = "> " + _text;
            if (_scroll == null || _scroll.viewport == null || _scroll.content == null) return;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_scroll.viewport, (RectTransform)transform);
            Rect viewport = _scroll.viewport.rect;
            float shift = bounds.max.y > viewport.yMax ? viewport.yMax - bounds.max.y
                : bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y : 0;
            float horizontal = _scroll.horizontal ? bounds.max.x > viewport.xMax ? viewport.xMax - bounds.max.x
                : bounds.min.x < viewport.xMin ? viewport.xMin - bounds.min.x : 0 : 0;
            if (!_scroll.vertical) shift = 0;
            if (shift != 0 || horizontal != 0) { _scroll.StopMovement(); _scroll.content.anchoredPosition += new Vector2(horizontal, shift); }
        }
        /// <summary>Restores the authored choice label after focus leaves.</summary>
        public void OnDeselect(BaseEventData eventData) { _focused = false; RefreshSurface(); if (_label != null) _label.text = _text; }
    }
}
