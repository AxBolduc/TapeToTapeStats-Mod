using HarmonyLib;
using MelonLoader;
using TapeToTapeStatsMod.Mapping;
using TapeToTapeStatsMod.Services;
using TapeToTapeStatsMod.Tracking;
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
            RunTracker.Observe(__instance.m_CampaignState);

            var runInfo = RunInfoMapper.FromRunEndHandler(__instance);

            RunReporter.Report(runInfo with { Events = ReadEvents(runInfo.RunId) });
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Failed to capture run data: {e}");
        }
    }

    private static List<Domain.DTOs.RunEventDto>? ReadEvents(string runId)
    {
        try
        {
            return RunEventLog.ReadAll(runId);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Could not read the event log for run {runId}: {e.Message}");
            return null;
        }
    }
}
