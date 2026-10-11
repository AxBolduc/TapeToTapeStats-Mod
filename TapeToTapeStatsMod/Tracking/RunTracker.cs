using Il2CppState;
using MelonLoader;
using UnityEngine;

namespace TapeToTapeStatsMod.Tracking;

/// <summary>
/// Entry point for patches that record decisions. Resolves the current run from the campaign state and
/// never lets a tracking failure reach the game.
/// </summary>
public static class RunTracker
{
    private static CampaignState? _campaignState;

    /// <summary>Remembers the campaign state seen by any patch, for patches whose menus don't carry one.</summary>
    public static void Observe(CampaignState? campaignState)
    {
        if (campaignState != null)
        {
            _campaignState = campaignState;
        }
    }

    public static CampaignState? CurrentCampaignState
    {
        get
        {
            if (_campaignState != null && _campaignState.WasCollected)
            {
                _campaignState = null;
            }

            if (_campaignState == null)
            {
                var found = Resources.FindObjectsOfTypeAll<CampaignState>();

                if (found.Length > 0)
                {
                    _campaignState = found[0];
                }
            }

            return _campaignState;
        }
    }

    /// <summary>
    /// Records an event against the run in <paramref name="campaignState"/> (or the last one seen).
    /// <paramref name="buildData"/> runs inside the guard, so it may read game objects freely.
    /// </summary>
    public static void Record(string type, Func<Dictionary<string, object?>> buildData, CampaignState? campaignState = null)
    {
        try
        {
            Observe(campaignState);

            var state = campaignState ?? CurrentCampaignState;
            var runData = state?.runData;

            if (runData == null || string.IsNullOrEmpty(runData.runID))
            {
                MelonDebug.Msg($"Not recording {type}: no active run.");
                return;
            }

            RecordForRun(runData.runID, type, runData._currentAct, runData.currentFloor, buildData);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Failed to record {type}: {e.Message}");
        }
    }

    /// <summary>Records an event against an explicit run id (the game's analytics events carry one).</summary>
    public static void RecordForRun(
        string runId,
        string type,
        int? act,
        int? floor,
        Func<Dictionary<string, object?>> buildData)
    {
        try
        {
            if (string.IsNullOrEmpty(runId))
            {
                return;
            }

            RunEventLog.Append(runId, type, act, floor, buildData());
            RunSnapshotScheduler.MarkChanged(runId);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Failed to record {type}: {e.Message}");
        }
    }

    /// <summary>Current act and floor of the active run, for events that only know the run id.</summary>
    public static (int? Act, int? Floor) CurrentPosition()
    {
        try
        {
            var runData = CurrentCampaignState?.runData;

            return runData == null ? (null, null) : (runData._currentAct, runData.currentFloor);
        }
        catch
        {
            return (null, null);
        }
    }
}
