using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SocialStim.Editor
{
    /// <summary>Stops a player build before invalid authored definitions or text can ship.</summary>
    public sealed class ContentBuildValidation : IPreprocessBuildWithReport
    {
        /// <summary>Runs before other project build processors.</summary>
        public int callbackOrder => -1000;
        /// <summary>Uses the same content contracts as imports/inspectors and reports a build failure with asset paths.</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
#if !ENABLE_INPUT_SYSTEM
            throw new BuildFailedException("SocialStim requires the Input System backend in Player Settings.");
#else
            try { ContentValidation.ValidateAllContent(); }
            catch (Exception exception) { throw new BuildFailedException("SocialStim content validation failed:\n" + exception.Message); }
#endif
        }
    }
}
