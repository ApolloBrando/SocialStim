using System;
using System.Collections.Generic;
using System.IO;
using SocialStim.Data.Definitions;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Application.PresentationCues;
using SocialStim.Presentation.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Shared validation for menus, inspectors, content imports and the player build pipeline.</summary>
    public static class ContentValidation
    {
        /// <summary>Validates every project catalog and authoring definition rather than only one hardcoded starter asset.</summary>
        [MenuItem("Tools/SocialStim/Validate All Content")]
        public static void ValidateAllContent()
        {
            var failures = new List<string>();
            string[] catalogs = AssetDatabase.FindAssets("t:GameCatalog", new[] { "Assets/_Project" });
            if (catalogs.Length == 0) failures.Add("No GameCatalog asset found under Assets/_Project.");
            foreach (string guid in catalogs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Check(path, () => ValidateCatalog(AssetDatabase.LoadAssetAtPath<GameCatalog>(path)), failures);
            }
            Check("Resources/SocialStimText.json", () => GameText.Load(), failures);
            ValidateAssets<RoleDefinition>(asset => asset.ToSpec(), failures);
            ValidateAssets<RoleListDefinition>(asset => asset.Load(), failures);
            ValidateAssets<ReasoningDefinition>(asset => asset.ToSettings(), failures);
            ValidateAssets<CueProfileDefinition>(asset => asset.Load(), failures);
            ValidateAssets<TextStyleDefinition>(asset => asset.Load(), failures);
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/_Project/Data/Dialogue" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetExtension(path) == ".json") Check(path, () => ValidateText(File.ReadAllText(path)), failures);
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
        }
        /// <summary>Compatibility entry point retained for the documented starter-content menu.</summary>
        [MenuItem("Tools/SocialStim/Validate Starter Content")]
        public static void ValidateStarterContent()
        { ValidateAllContent(); Debug.Log("SocialStim content definitions, text, markup and setup constraints are valid."); }
        /// <summary>Validates all data referenced by one catalog, including configured exact slots and selectable roles.</summary>
        public static void ValidateCatalog(GameCatalog catalog)
        {
            if (catalog == null) throw new InvalidOperationException("Missing game catalog.");
            var roles = catalog.LoadRoles(); var names = catalog.LoadNames(); catalog.LoadReasoning();
            var setup = catalog.LoadSetup();
            if (setup != null)
            {
                if (names.Count < setup.Slots.Count) throw new InvalidOperationException("Role list needs more cast names.");
                foreach (var role in roles) setup.Resolve(roles, role.Id, new SeededRandom(1));
            }
            foreach (var line in catalog.LoadDialogue()) ValidateLine(line.Id, line.Text);
        }
        /// <summary>Validates standalone spoken/copy JSON, including semantic markup and available template variables.</summary>
        public static void ValidateText(string json)
        {
            var document = new EditableTextDocument(json); document.Validate();
            if (!document.IsCopy) foreach (var line in DialogueDocument.Parse(json)) ValidateLine(line.Id, line.Text);
        }
        private static void ValidateLine(string id, string text)
        { try { SemanticMarkup.Parse(text); } catch (ArgumentException exception) { throw new ArgumentException(id + ": " + exception.Message, exception); } }
        private static void ValidateAssets<T>(Action<T> validate, List<string> failures) where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Check(path, () => validate(AssetDatabase.LoadAssetAtPath<T>(path)), failures);
            }
        }
        private static void Check(string path, Action validate, List<string> failures)
        { try { validate(); } catch (Exception exception) { failures.Add(path + ": " + exception.Message); } }
        /// <summary>Opens the committed scene after Unity offers to save current scene work.</summary>
        [MenuItem("Tools/SocialStim/Open Game Scene")]
        public static void OpenGameScene()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity"); }
    }
}
