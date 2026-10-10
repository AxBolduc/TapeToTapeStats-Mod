using MelonLoader;
using TapeToTapeStatsMod.Mapping;
using TapeToTapeStatsMod.Services;

namespace TapeToTapeStatsMod.Tracking;

/// <summary>
/// Reports the active run while it is played, not only when it ends. Every recorded event (a node picked, a match
/// finished, a reward or event choice, ...) marks the run changed; once things have been quiet for
/// <see cref="QuietPeriod"/>, the whole run is snapshotted and sent.
/// </summary>
/// <remarks>
/// Events are recorded from prefixes, before the game applies the change they describe, and usually arrive in bursts
/// (a reward screen logs several). Waiting for a quiet moment captures the state after the change and sends one
/// snapshot per burst. Snapshots are taken in <see cref="Tick"/> because game state must be read on the main thread.
/// </remarks>
public static class RunSnapshotScheduler
{
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(2);

    private static string? _pendingRunId;

    private static DateTime _dueAt;

    public static void MarkChanged(string runId)
    {
        _pendingRunId = runId;
        _dueAt = DateTime.UtcNow + QuietPeriod;
    }

    /// <summary>Drops a pending snapshot; the run has ended and its final report supersedes it.</summary>
    public static void Cancel(string runId)
    {
        if (_pendingRunId == runId)
        {
            _pendingRunId = null;
        }
    }

    /// <summary>Call every frame from the main thread.</summary>
    public static void Tick()
    {
        if (_pendingRunId == null || DateTime.UtcNow < _dueAt)
        {
            return;
        }

        var runId = _pendingRunId;

        _pendingRunId = null;

        try
        {
            var campaignState = RunTracker.CurrentCampaignState;

            if (campaignState?.runData == null || campaignState.runData.runID != runId)
            {
                MelonDebug.Msg($"Not snapshotting run {runId}: it is no longer the active run.");
                return;
            }

            var runInfo = RunInfoMapper.FromCampaignState(campaignState, isVictory: false);

            RunReporter.ReportProgress(runInfo with { Events = RunEventLog.TryReadAll(runId) });
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Could not snapshot run {runId}: {e.Message}");
        }
    }
}
