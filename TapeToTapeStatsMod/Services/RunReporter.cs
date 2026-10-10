using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using TapeToTapeStatsMod.Clients;
using TapeToTapeStatsMod.Domain.DTOs;

namespace TapeToTapeStatsMod.Services;

/// <summary>
/// Sends runs to the API: snapshots while a run is played, then the finished run. Every payload is written to
/// <c>UserData/TapeToTapeStats/outbox/&lt;runId&gt;.json</c> first (replacing the run's previous one) and removed once
/// the API accepts it, so runs played offline or while the API is down are sent on a later update or launch.
/// </summary>
/// <remarks>
/// Sends go through one queue, so a run's payloads reach the API in order. Each send reads the run's latest payload
/// from the outbox rather than the one it was queued with, so a backlog of snapshots collapses into a single request.
/// </remarks>
public static class RunReporter
{
    private static readonly object OutboxLock = new();

    private static readonly object QueueLock = new();

    /// <summary>Runs reported as finished this session; later snapshots must not replace their final payload.</summary>
    private static readonly HashSet<string> FinishedRunIds = new();

    private static Task _sendQueue = Task.CompletedTask;

    private static TapeToTapeStatsClient? _client;

    private static string OutboxDirectory => Path.Combine(MelonEnvironment.UserDataDirectory, "TapeToTapeStats", "outbox");

    /// <summary>
    /// (Re)creates the client from the current preferences and retries anything left in the outbox.
    /// Safe to call again after they change.
    /// </summary>
    public static void Initialize()
    {
        _client = null;

        if (PreferenceService.ApiKey.Length == 0)
        {
            Melon<Core>.Logger.Warning(
                "No apiKey set in UserData/MelonPreferences.cfg [TapeToTapeStats]; runs will be saved but not reported.");
            return;
        }

        _client = new TapeToTapeStatsClient(PreferenceService.ApiBaseUrl, PreferenceService.ApiKey);
        Melon<Core>.Logger.Msg($"Reporting runs to {PreferenceService.ApiBaseUrl}.");
        RetryOutbox();
    }

    /// <summary>Sends the finished run in the background so the end-of-run flow is never blocked.</summary>
    public static void ReportFinished(RunInfoDto runInfo)
    {
        FinishedRunIds.Add(runInfo.RunId);
        Save(runInfo with { IsCompleted = true });
    }

    /// <summary>Sends a snapshot of a run that is still being played. Ignored once the run has finished.</summary>
    public static void ReportProgress(RunInfoDto runInfo)
    {
        if (FinishedRunIds.Contains(runInfo.RunId))
        {
            return;
        }

        Save(runInfo with { IsCompleted = false });
    }

    private static void Save(RunInfoDto runInfo)
    {
        var json = JsonConvert.SerializeObject(runInfo, TapeToTapeStatsClient.SerializerSettings);

        MelonDebug.Msg(json);

        if (WriteOutbox(runInfo.RunId, json))
        {
            Enqueue(client => SendLatest(client, runInfo.RunId));
        }
        else
        {
            // Without an outbox copy there is nothing to read back; send this payload as-is.
            Enqueue(client => Send(client, runInfo.RunId, json));
        }
    }

    private static void RetryOutbox()
    {
        if (_client == null || !Directory.Exists(OutboxDirectory))
        {
            return;
        }

        var pending = Directory.GetFiles(OutboxDirectory, "*.json");

        if (pending.Length == 0)
        {
            return;
        }

        Melon<Core>.Logger.Msg($"Retrying {pending.Length} unreported run(s).");

        foreach (var path in pending)
        {
            var runId = Path.GetFileNameWithoutExtension(path);

            Enqueue(client => SendLatest(client, runId));
        }
    }

    private static void Enqueue(Func<TapeToTapeStatsClient, Task> send)
    {
        var client = _client;

        if (client == null)
        {
            return;
        }

        lock (QueueLock)
        {
            _sendQueue = _sendQueue
                .ContinueWith(_ => send(client), TaskScheduler.Default)
                .Unwrap();
        }
    }

    /// <summary>Sends whatever the outbox holds for the run; nothing if an earlier send already delivered it.</summary>
    private static async Task SendLatest(TapeToTapeStatsClient client, string runId)
    {
        string? json;

        try
        {
            lock (OutboxLock)
            {
                var path = OutboxPath(runId);

                json = File.Exists(path) ? File.ReadAllText(path) : null;
            }
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Could not read run {runId} from the outbox: {e.Message}");
            return;
        }

        if (json == null)
        {
            return;
        }

        await Send(client, runId, json);
    }

    private static async Task Send(TapeToTapeStatsClient client, string runId, string json)
    {
        try
        {
            var result = await client.ReportRun(json);
            var outcome = result.Duplicate ? "updated" : "reported";

            DeleteOutbox(runId, json);
            Melon<Core>.Logger.Msg($"Run {runId} {outcome} (run {result.RunId}).");
        }
        catch (RunRejectedException e)
        {
            // The API will never accept this payload as-is; keep a copy for debugging but stop retrying.
            MoveToRejected(runId, json);
            Melon<Core>.Logger.Error($"Run {runId} was rejected: {e.Message}");
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error(
                $"Failed to report run {runId}; will retry with its next update or on next launch: {e.Message}");
        }
    }

    private static bool WriteOutbox(string runId, string json)
    {
        try
        {
            lock (OutboxLock)
            {
                Directory.CreateDirectory(OutboxDirectory);
                File.WriteAllText(OutboxPath(runId), json);
            }

            return true;
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Could not save run {runId} to the outbox: {e.Message}");
            return false;
        }
    }

    /// <summary>Only deletes the file if it still holds what was sent; a newer snapshot may have replaced it meanwhile.</summary>
    private static void DeleteOutbox(string runId, string sentJson)
    {
        lock (OutboxLock)
        {
            var path = OutboxPath(runId);

            if (File.Exists(path) && File.ReadAllText(path) == sentJson)
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>Like <see cref="DeleteOutbox"/>, leaves a newer snapshot in place to be sent on its own.</summary>
    private static void MoveToRejected(string runId, string rejectedJson)
    {
        lock (OutboxLock)
        {
            var path = OutboxPath(runId);

            if (!File.Exists(path) || File.ReadAllText(path) != rejectedJson)
            {
                return;
            }

            var rejectedDirectory = Path.Combine(OutboxDirectory, "rejected");

            Directory.CreateDirectory(rejectedDirectory);
            File.Move(path, Path.Combine(rejectedDirectory, Path.GetFileName(path)), overwrite: true);
        }
    }

    private static string OutboxPath(string runId)
    {
        var safeRunId = string.Concat(runId.Where(character => char.IsLetterOrDigit(character) || character == '-'));

        return Path.Combine(OutboxDirectory, $"{safeRunId}.json");
    }
}
