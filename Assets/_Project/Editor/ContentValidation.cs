using SocialStim.Data.Definitions;
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
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/Catalogs/StarterCatalog.asset");
            if (catalog == null) throw new System.InvalidOperationException("StarterCatalog.asset is missing.");
            int roles = catalog.LoadRoles().Count;
            int lines = catalog.LoadDialogue().Count;
            int names = catalog.LoadNames().Count;
            Debug.Log($"SocialStim content valid: {roles} roles, {lines} placeholder lines, {names} names.");
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
