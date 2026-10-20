using System;
using SocialStim.Data.Dialogue;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Tests.PlayMode
{
    internal static class CopyFixture
    {
        internal static TextTemplates Load() => TextDocument.Parse(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Resources.Load<UnityEngine.TextAsset>("SocialStimText").text
#else
            System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "SocialStimText.json"))
#endif
        );
    }
}
