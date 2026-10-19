using SocialStim.Data.Definitions;
using SocialStim.Application.PresentationCues;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Editor-only content validation and scene entry; no play rules.</summary>
    public static class ContentValidation
    {
        /// <summary>Loads and validates the catalog and authored dialogue without entering Play Mode.</summary>
        [MenuItem("Tools/SocialStim/Validate Starter Content")]
        public static void ValidateStarterContent()
        {
#if !ENABLE_INPUT_SYSTEM
            throw new System.InvalidOperationException("Enable Input System Package (New) in Edit > Project Settings > Player > Other Settings > Active Input Handling, then restart Unity.");
#else
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/Catalogs/StarterCatalog.asset");
            if (catalog == null) throw new System.InvalidOperationException("StarterCatalog.asset is missing.");
            int roles = catalog.LoadRoles().Count;
            var dialogue = catalog.LoadDialogue();
            foreach (var line in dialogue) SemanticMarkup.Parse(line.Text);
            int lines = dialogue.Count;
            int names = catalog.LoadNames().Count;
            catalog.LoadReasoning();
            Debug.Log($"SocialStim content valid: {roles} roles, {lines} placeholder lines, {names} names.");
#endif
        }

        /// <summary>Opens the committed scene after allowing the editor to save existing scene work.</summary>
        [MenuItem("Tools/SocialStim/Open Game Scene")]
        public static void OpenGameScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity");
        }
    }
}
