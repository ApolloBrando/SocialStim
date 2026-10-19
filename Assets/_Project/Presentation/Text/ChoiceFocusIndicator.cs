using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SocialStim.Presentation.Text
{
    /// <summary>Plain textual focus marker so keyboard selection never relies on color.</summary>
    public sealed class ChoiceFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private TextMeshProUGUI _label;
        private string _text;

        /// <summary>Wires the existing button label without copying game state.</summary>
        public void Initialize(TextMeshProUGUI label, string text) { _label = label; _text = text; }
        /// <summary>Marks the currently selected choice.</summary>
        public void OnSelect(BaseEventData eventData) { if (_label != null) _label.text = "> " + _text; }
        /// <summary>Restores the authored choice label after focus leaves.</summary>
        public void OnDeselect(BaseEventData eventData) { if (_label != null) _label.text = _text; }
    }
}
