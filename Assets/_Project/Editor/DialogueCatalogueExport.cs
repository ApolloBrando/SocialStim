using System;
using System.IO;
using System.Text;
using SocialStim.Data.Dialogue;
using UnityEditor;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Refreshes the complete reference inside Unity without requiring an external Python process.</summary>
    public static class DialogueCatalogueExport
    {
        /// <summary>Generates the same reference checked by CI from the two authoritative JSON sources.</summary>
        [MenuItem("Tools/SocialStim/Refresh Dialogue Catalogue")]
        public static void Refresh()
        {
            string output = DialogueCatalogue.Render(File.ReadAllText("Assets/_Project/Data/Dialogue/StarterDialogue.json"),
                File.ReadAllText("Assets/_Project/Data/Dialogue/Resources/SocialStimText.json"));
            string path = "Docs/DIALOGUE_CATALOGUE.md";
            if (!File.Exists(path) || File.ReadAllText(path) != output) File.WriteAllText(path, output, new UTF8Encoding(false));
        }
        /// <summary>Updates the repository reference when its canonical source is saved from the Unity editor.</summary>
        public static void RefreshFor(string source)
        {
            if (source == "Assets/_Project/Data/Dialogue/StarterDialogue.json" || source == "Assets/_Project/Data/Dialogue/Resources/SocialStimText.json")
            {
                try { Refresh(); }
                catch (Exception exception) { Debug.LogError("Source was saved, but the dialogue reference could not be refreshed: " + exception.Message); }
            }
        }
    }
}
