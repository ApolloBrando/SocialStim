using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SocialStim.Presentation.Text
{
    /// <summary>Uses uGUI raycasting for mouse/touch line clicks, respecting modal blockers and pointer routing.</summary>
    public sealed class CurrentLinePointer : MonoBehaviour, IPointerClickHandler
    {
        private Action _advance;
        /// <summary>Attaches the presentation callback; no game commands are produced here.</summary>
        public void Initialize(Action advance) { _advance = advance; }
        /// <summary>Handles primary mouse/touch clicks delivered by the EventSystem.</summary>
        public void OnPointerClick(PointerEventData eventData)
        { if (eventData.button == PointerEventData.InputButton.Left) _advance?.Invoke(); }
    }
}
