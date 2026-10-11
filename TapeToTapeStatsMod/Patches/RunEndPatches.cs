using HarmonyLib;
using Il2CppState;
using Il2CppState.EndOfGame;
using MelonLoader;
using TapeToTapeStatsMod.Mapping;
using TapeToTapeStatsMod.Services;
using TapeToTapeStatsMod.Tracking;
using GameCampaignEndStep = Il2CppState.EndOfGame.Steps.CampaignEndStep;
using GameRunEndHandler = Il2CppTape2Tape.Hockey.UI.RunEndHandler;

namespace TapeToTapeStatsMod.Patches;

/// <summary>
/// The game ends a run on one of two paths, and both are needed:
/// <list type="bullet">
///   <item>A run that ends in a match (won or lost) goes through the end-of-game pipeline, whose
///   <c>CampaignEndStep</c> calls <c>RunEndHelper.EndRun</c> directly. <c>RunEndHandler</c> is not involved.</item>
///   <item>A run that ends outside a match (e.g. abandoned from the map) goes through <c>RunEndHandler.EndRun</c>.</item>
/// </list>
/// <c>RunEndHelper.EndRun</c> can't be patched instead: <c>CampaignEndStep</c>'s call to it is inlined.
/// Prefixes so run state is captured before the game tears it down.
/// </summary>
[HarmonyPatch]
public static class RunEndPatches
{
    private static string? _lastCapturedRunId;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameCampaignEndStep), nameof(GameCampaignEndStep.Execute))]
    private static void CampaignEndStepExecute(EndOfGameContext ctx)
    {
        try
        {
            Capture(ctx.CampaignState, ctx.IsRunWin);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Failed to capture run data: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameRunEndHandler), nameof(GameRunEndHandler.EndRun))]
    private static void RunEndHandlerEndRun(GameRunEndHandler __instance)
    {
        try
        {
            Capture(__instance.m_CampaignState, __instance.m_IsVictory);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Failed to capture run data: {e}");
        }
    }

    private static void Capture(CampaignState campaignState, bool isVictory)
    {
        RunTracker.Observe(campaignState);

        var runInfo = RunInfoMapper.FromCampaignState(campaignState, isVictory);

        // CampaignEndStep also raises the events RunEndHandler listens to, so a run can reach both hooks.
        if (runInfo.RunId == _lastCapturedRunId)
        {
            return;
        }

        _lastCapturedRunId = runInfo.RunId;
        RunSnapshotScheduler.Cancel(runInfo.RunId);
        RunReporter.ReportFinished(runInfo with { Events = RunEventLog.TryReadAll(runInfo.RunId) });
    }
}
