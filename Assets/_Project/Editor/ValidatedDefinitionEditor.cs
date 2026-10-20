using System;
using UnityEditor;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Preserves Unity's serialized inspector/Undo workflow and caches validation feedback between edits.</summary>
    public abstract class ValidatedDefinitionEditor : UnityEditor.Editor
    {
        private string _error;
        protected virtual void OnEnable() { ValidateSelection(); EditorApplication.projectChanged += ValidateSelection; }
        protected virtual void OnDisable() => EditorApplication.projectChanged -= ValidateSelection;
        /// <summary>Draws Unity-native properties and validates only when data changes or validation is requested.</summary>
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector()) ValidateSelection();
            if (_error != null) EditorGUILayout.HelpBox(_error, MessageType.Error);
            if (GUILayout.Button("Validate definition")) ValidateSelection();
            DrawActions();
        }
        /// <summary>Checks one target using its runtime conversion contract.</summary>
        protected abstract void ValidateDefinition(UnityEngine.Object definition);
        /// <summary>Optional asset-specific navigation.</summary>
        protected virtual void DrawActions() { }
        private void ValidateSelection()
        {
            try { foreach (var definition in targets) ValidateDefinition(definition); _error = null; }
            catch (Exception exception) { _error = exception.Message; }
            Repaint();
        }
    }
}
