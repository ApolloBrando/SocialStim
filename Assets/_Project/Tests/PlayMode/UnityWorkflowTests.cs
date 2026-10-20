using NUnit.Framework;
using System.Linq;
using SocialStim.Presentation.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace SocialStim.Tests.PlayMode
{
    /// <summary>Native Input System lifecycle and EventSystem reuse. Requires the Unity runner.</summary>
    public sealed class UnityWorkflowTests
    {
        [Test]
        public void TextActionsUseOwnedClonesAndStopDispatchWhenDisabled()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Text"); var source = map.AddAction("Advance", InputActionType.Button, "<Keyboard>/space");
            var reference = InputActionReference.Create(source); var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                using (var input = new TextInputActions(reference))
                {
                    int calls = 0; input.Advance += () => calls++; input.Enable();
                    Assert.That(source.enabled, Is.False, "An isolated view must not enable the shared source action.");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update();
                    Assert.That(calls, Is.EqualTo(1)); input.Disable();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update();
                    Assert.That(calls, Is.EqualTo(1)); Assert.That(source.enabled, Is.False);
                }
            }
            finally { InputSystem.RemoveDevice(keyboard); Object.DestroyImmediate(reference); Object.DestroyImmediate(asset); }
        }
        [Test]
        public void ExistingCompatibleEventSystemIsReusedWithoutCreatingASecondDispatcher()
        {
            var enabled = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(system => system.enabled).ToArray();
            foreach (var system in enabled) system.enabled = false;
            var owner = new GameObject("Owner");
            var existing = new GameObject("Existing", typeof(EventSystem), typeof(InputSystemUIInputModule));
            try
            {
                Assert.That(UiEventSystem.Ensure(owner.transform), Is.SameAs(existing.GetComponent<EventSystem>()));
                Assert.That(UiEventSystem.Ensure(owner.transform), Is.SameAs(existing.GetComponent<EventSystem>()));
                Assert.That(owner.transform.childCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(existing); Object.DestroyImmediate(owner);
                foreach (var system in enabled) if (system != null) system.enabled = true;
            }
        }
    }
}
