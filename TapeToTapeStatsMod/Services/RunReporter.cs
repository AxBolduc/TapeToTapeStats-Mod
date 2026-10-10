using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using TapeToTapeStatsMod.Clients;
using TapeToTapeStatsMod.Domain.DTOs;

namespace TapeToTapeStatsMod.Services;

/// <summary>
/// Sends finished runs to the API. Every run is written to <c>UserData/TapeToTapeStats/outbox</c> first and
/// removed once the API accepts it, so runs finished offline or while the API is down are sent on a later launch.
/// </summary>
public static class RunReporter
{
    private static readonly object OutboxLock = new();

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

    /// <summary>
    /// Sends the run in the background so the end-of-run flow is never blocked.
    /// </summary>
    public static void Report(RunInfoDto runInfo)
    {
        var json = JsonConvert.SerializeObject(runInfo, TapeToTapeStatsClient.SerializerSettings);

        MelonDebug.Msg(json);
        WriteOutbox(runInfo.RunId, json);

        var client = _client;

        if (client == null)
        {
            return;
        }

        Task.Run(() => Send(client, runInfo.RunId, json));
    }

    private static void RetryOutbox()
    {
        var client = _client;

        if (client == null || !Directory.Exists(OutboxDirectory))
        {
            return;
        }

        var pending = Directory.GetFiles(OutboxDirectory, "*.json");

        if (pending.Length == 0)
        {
            return;
        }

        Melon<Core>.Logger.Msg($"Retrying {pending.Length} unreported run(s).");

        Task.Run(async () =>
        {
            foreach (var path in pending)
            {
                string json;

                lock (OutboxLock)
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    json = File.ReadAllText(path);
                }

                await Send(client, Path.GetFileNameWithoutExtension(path), json);
            }
        });
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
            MoveToRejected(runId);
            Melon<Core>.Logger.Error($"Run {runId} was rejected: {e.Message}");
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Failed to report run {runId}; will retry on next launch: {e.Message}");
        }
    }

    private static void WriteOutbox(string runId, string json)
    {
        try
        {
            lock (OutboxLock)
            {
                Directory.CreateDirectory(OutboxDirectory);
                File.WriteAllText(OutboxPath(runId), json);
            }
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Could not save run {runId} to the outbox: {e.Message}");
        }
    }

    /// <summary>Only deletes the file if it still holds what was sent; the run may have ended again meanwhile.</summary>
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

    private static void MoveToRejected(string runId)
    {
        lock (OutboxLock)
        {
            var path = OutboxPath(runId);

            if (!File.Exists(path))
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
