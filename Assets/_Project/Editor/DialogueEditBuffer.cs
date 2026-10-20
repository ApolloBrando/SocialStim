using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Undo records contain only staged text, never the source revision used for conflict detection.</summary>
    public sealed class DialogueEditBuffer : ScriptableObject
    {
        [SerializeField] private string _json = "";
        /// <summary>The complete staged JSON source.</summary>
        public string Json => _json;
        /// <summary>Updates staging; the editor window records Undo before calling this method.</summary>
        public void SetText(string json) { _json = json ?? ""; }
    }
}
