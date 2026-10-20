using System;
using UnityEngine.InputSystem;

namespace SocialStim.Presentation.Text
{
    /// <summary>Owns isolated Input System actions, optionally cloned from inspector-authored references.</summary>
    public sealed class TextInputActions : IDisposable
    {
        private readonly InputAction _advance, _cancel;
        /// <summary>Creates independent actions so enabling/disabling the view cannot modify another action map.</summary>
        public TextInputActions(InputActionReference advance = null, InputActionReference cancel = null)
        {
            _advance = advance != null && advance.action != null ? advance.action.Clone()
                : new InputAction("Advance text", InputActionType.Button, "<Keyboard>/space");
            _cancel = cancel != null && cancel.action != null ? cancel.action.Clone()
                : new InputAction("Close information", InputActionType.Button, "<Keyboard>/escape");
            if (cancel == null || cancel.action == null) _cancel.AddBinding("<Gamepad>/buttonEast");
            _advance.performed += OnAdvance; _cancel.performed += OnCancel;
        }
        /// <summary>Requests reveal/advance; application commands remain separately gated.</summary>
        public event Action Advance;
        /// <summary>Requests closing the top information modal.</summary>
        public event Action Cancel;
        /// <summary>Activates owned actions when the view is enabled.</summary>
        public void Enable() { _advance.Enable(); _cancel.Enable(); }
        /// <summary>Deactivates only the owned clones when the view is disabled.</summary>
        public void Disable() { _advance.Disable(); _cancel.Disable(); }
        private void OnAdvance(InputAction.CallbackContext context) => Advance?.Invoke();
        private void OnCancel(InputAction.CallbackContext context) => Cancel?.Invoke();
        /// <summary>Releases native action state and callback subscriptions.</summary>
        public void Dispose()
        {
            Disable(); _advance.performed -= OnAdvance; _cancel.performed -= OnCancel;
            _advance.Dispose(); _cancel.Dispose();
        }
    }
}
