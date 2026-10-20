using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SocialStim.Editor
{
    /// <summary>Runs the same content checks for editor-menu and automated Unity builds.</summary>
    public sealed class ContentBuildValidation : IPreprocessBuildWithReport
    {
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            try { ContentValidation.ValidateAllContent(); }
            catch (System.Exception exception) { throw new BuildFailedException(exception.Message); }
        }
    }
}
