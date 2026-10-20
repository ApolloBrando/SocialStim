using System;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Dialogue;
using UnityEngine;

namespace SocialStim.Data.Definitions
{
    /// <summary>Unity adapter for the explicitly named, editable copy asset.</summary>
    public static class GameText
    {
        /// <summary>Loads authored copy. A missing or invalid asset stops setup rather than using hardcoded prose.</summary>
        public static TextTemplates Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("SocialStimText");
            if (asset == null) throw new InvalidOperationException("Missing Resources/SocialStimText.json.");
            TextTemplates text = TextDocument.Parse(asset.text);
            TextCatalogContract.Validate(text);
            return text;
        }
    }
}
