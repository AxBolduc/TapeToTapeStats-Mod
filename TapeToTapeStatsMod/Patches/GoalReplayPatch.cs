using Il2Cpp;

namespace TapeToTapeStatsMod.Patches;

[HarmonyLib.HarmonyPatch(typeof(GoalReplay), nameof(GoalReplay.StartReplaySequence))]
public class GoalReplayPatch
{
    static void Postfix(GoalReplay __instance)
    {
        __instance.SkipBeforeStarting();
    }
}