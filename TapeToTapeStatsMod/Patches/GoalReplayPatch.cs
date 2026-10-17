using Il2Cpp;
using TapeToTapeStatsMod.Services;

namespace TapeToTapeStatsMod.Patches;

[HarmonyLib.HarmonyPatch(typeof(GoalReplay), nameof(GoalReplay.StartReplaySequence))]
public class GoalReplayPatch
{
    static void Postfix(GoalReplay __instance)
    {
        if (!PreferenceService.SkipGoalReplays)
        {
            return;
        }

        __instance.SkipBeforeStarting();
    }
}