using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SocialStim.Editor
{
    /// <summary>Coalesces relevant imports/moves/deletions and validates after the AssetDatabase finishes its import batch.</summary>
    public sealed class ContentImportValidation : AssetPostprocessor
    {
        private static bool _queued;
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous, bool didDomainReload)
        {
            if (!didDomainReload && !imported.Concat(deleted).Concat(moved).Concat(previous).Any(Relevant)) return;
            if (_queued) return; _queued = true; EditorApplication.delayCall += ValidateLater;
        }
        private static bool Relevant(string path) => path.StartsWith("Assets/_Project/", StringComparison.Ordinal)
            && (path.EndsWith(".json", StringComparison.Ordinal) || path.EndsWith(".asset", StringComparison.Ordinal));
        private static void ValidateLater()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += ValidateLater; return; }
            _queued = false;
            try { ContentValidation.ValidateAllContent(); }
            catch (Exception exception) { Debug.LogError("SocialStim content validation:\n" + exception.Message); }
        }
    }
}
