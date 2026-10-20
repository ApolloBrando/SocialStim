using SocialStim.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Catalog validation and direct navigation to its actual dialogue asset.</summary>
    [CustomEditor(typeof(GameCatalog)), CanEditMultipleObjects]
    public sealed class GameCatalogEditor : ValidatedDefinitionEditor
    {
        protected override void ValidateDefinition(UnityEngine.Object definition) => ContentValidation.ValidateCatalog((GameCatalog)definition);
        protected override void DrawActions()
        {
            if (targets.Length != 1) return;
            if (GUILayout.Button("Edit referenced dialogue"))
            {
                serializedObject.Update();
                DialogueEditorWindow.OpenAsset(serializedObject.FindProperty("_dialogue").objectReferenceValue as TextAsset);
            }
        }
    }
}
