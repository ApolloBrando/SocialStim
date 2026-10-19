using System;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Public phase progress used only for the daytime palette.</summary>
    public static class Daylight
    {
        public static bool IsNight(GamePhase phase) => phase == GamePhase.NightSelection || phase == GamePhase.NightResolution;
        public static float Progress(GamePhase phase, int usedTrials, int allowedTrials)
        {
            if (phase == GamePhase.Dawn || phase == GamePhase.Discussion) return 0;
            float trial = Math.Min(1f, Math.Max(0f, (float)usedTrials / Math.Max(1, allowedTrials)));
            return phase == GamePhase.LastWords || phase == GamePhase.Execution ? 1f : 0.35f + 0.55f * trial;
        }
    }
}
