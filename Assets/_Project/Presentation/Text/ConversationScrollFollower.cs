using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SocialStim.Presentation.Text
{
    /// <summary>Manual scrolling opts out of following; incoming messages never steal the reading position.</summary>
    public sealed class ConversationScrollFollower : MonoBehaviour, IScrollHandler, IBeginDragHandler, IEndDragHandler
    {
        private ScrollRect _scroll;
        private int _sampleFrames;
        private bool _dragging;
        public bool Following { get; private set; } = true;
        public void Initialize(ScrollRect scroll) { _scroll = scroll; }
        public void Follow() { Following = true; _sampleFrames = 0; _dragging = false; }
        public void OnScroll(PointerEventData data) { Following = false; _sampleFrames = 2; }
        public void OnBeginDrag(PointerEventData data) { _dragging = true; Following = false; }
        public void OnEndDrag(PointerEventData data) { _dragging = false; _sampleFrames = 2; }
        private void LateUpdate()
        {
            if (_dragging || _sampleFrames <= 0) return;
            if (--_sampleFrames == 0) Following = _scroll.verticalNormalizedPosition <= 0.02f;
        }
    }
}
