using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Reuses a compatible scene EventSystem instead of silently creating duplicate input dispatchers.</summary>
    public static class UiEventSystem
    {
        /// <summary>Creates one owned system when absent; reports conflicting systems/modules without rewriting the scene.</summary>
        public static EventSystem Ensure(Transform owner)
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(system => system.enabled).ToArray();
            if (systems.Length > 1) throw new InvalidOperationException("Keep one enabled EventSystem in the scene.");
            if (systems.Length == 0)
            {
                var created = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                created.transform.SetParent(owner, false); return created.GetComponent<EventSystem>();
            }
            EventSystem existing = systems[0];
            if (existing.GetComponents<BaseInputModule>().Any(module => module.enabled && !(module is InputSystemUIInputModule)))
                throw new InvalidOperationException("Use an InputSystemUIInputModule on the existing EventSystem.");
            var input = existing.GetComponent<InputSystemUIInputModule>();
            if (input == null) existing.gameObject.AddComponent<InputSystemUIInputModule>();
            else if (!input.enabled) throw new InvalidOperationException("Enable the existing InputSystemUIInputModule.");
            return existing;
        }
    }
}
