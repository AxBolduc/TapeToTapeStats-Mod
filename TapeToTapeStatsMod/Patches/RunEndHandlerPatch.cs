using HarmonyLib;
using MelonLoader;
using TapeToTapeStatsMod.Mapping;
using TapeToTapeStatsMod.Services;
using GameRunEndHandler = Il2CppTape2Tape.Hockey.UI.RunEndHandler;

namespace TapeToTapeStatsMod.Patches;

[HarmonyPatch(typeof(GameRunEndHandler), nameof(GameRunEndHandler.EndRun))]
public static class RunEndHandlerPatch
{
    // Prefix so run state is captured before the game tears it down.
    private static void Prefix(GameRunEndHandler __instance)
    {
        try
        {
            RunReporter.Report(RunInfoMapper.FromRunEndHandler(__instance));
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Failed to capture run data: {e}");
        }
    }
}
