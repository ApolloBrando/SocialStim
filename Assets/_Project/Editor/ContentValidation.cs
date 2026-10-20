using System;
using System.Collections.Generic;
using SocialStim.Application.PresentationCues;
using SocialStim.Data.Definitions;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Randomness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Shared editor/build content checks; never starts or modifies a run.</summary>
    public static class ContentValidation
    {
        /// <summary>Retains the existing menu entry while validating every project content asset.</summary>
        [MenuItem("Tools/SocialStim/Validate Starter Content")]
        public static void ValidateStarterContent()
        {
            ValidateAllContent();
            Debug.Log("SocialStim project content is valid.");
        }

        /// <summary>Fails with asset paths for invalid definitions, setup references or dialogue markup.</summary>
        public static void ValidateAllContent()
        {
#if !ENABLE_INPUT_SYSTEM
            throw new InvalidOperationException("Enable Input System Package (New) in Player Settings and restart Unity.");
#else
            var failures = new List<string>();
            int catalogs = ValidateAssets<GameCatalog>(catalog =>
            {
                var roles = catalog.LoadRoles();
                var names = catalog.LoadNames();
                var setup = catalog.LoadSetup();
                if (setup != null)
                {
                    if (names.Count < setup.Slots.Count)
                        throw new InvalidOperationException("The role list needs more cast names.");
                    // A custom list may intentionally exclude roles from the public catalogue.
                    // Validate its actual constraints, rather than forcing every role into it.
                    setup.Resolve(roles, null, new SeededRandom(1));
                }
                foreach (var line in catalog.LoadDialogue())
                {
                    try { SemanticMarkup.Parse(line.Text); }
                    catch (ArgumentException exception)
                    { throw new ArgumentException(line.Id + ": " + exception.Message, exception); }
                }
                catalog.LoadReasoning();
            }, failures);
            if (catalogs == 0) failures.Add("No GameCatalog assets found in Assets/_Project.");
            ValidateAssets<RoleDefinition>(asset => asset.ToSpec(), failures);
            ValidateAssets<RoleListDefinition>(asset => asset.Load(), failures);
            ValidateAssets<ReasoningDefinition>(asset => asset.ToSettings(), failures);
            ValidateAssets<CueProfileDefinition>(asset => asset.Load(), failures);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
#endif
        }

        private static int ValidateAssets<T>(Action<T> validate, List<string> failures) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Project" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                try
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset == null) throw new InvalidOperationException("Asset could not be loaded.");
                    validate(asset);
                }
                catch (Exception exception) { failures.Add(path + ": " + exception.Message); }
            }
            return guids.Length;
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
