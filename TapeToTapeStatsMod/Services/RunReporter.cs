using MelonLoader;
using Newtonsoft.Json;
using TapeToTapeStatsMod.Clients;
using TapeToTapeStatsMod.Domain.DTOs;

namespace TapeToTapeStatsMod.Services;

public static class RunReporter
{
    private static TapeToTapeStatsClient? _client;

    /// <summary>
    /// (Re)creates the client from the current preferences. Safe to call again after they change.
    /// </summary>
    public static void Initialize()
    {
        _client = null;

        if (PreferenceService.ApiKey.Length == 0)
        {
            Melon<Core>.Logger.Warning(
                "No apiKey set in UserData/MelonPreferences.cfg [TapeToTapeStats]; runs will not be reported.");
            return;
        }

        _client = new TapeToTapeStatsClient(PreferenceService.ApiBaseUrl, PreferenceService.ApiKey);
        Melon<Core>.Logger.Msg($"Reporting runs to {PreferenceService.ApiBaseUrl}.");
    }

    /// <summary>
    /// Sends the run in the background so the end-of-run flow is never blocked.
    /// </summary>
    public static void Report(RunInfoDto runInfo)
    {
        MelonDebug.Msg(JsonConvert.SerializeObject(runInfo, Formatting.Indented));

        var client = _client;

        if (client == null)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                var result = await client.ReportRun(runInfo);
                var outcome = result.Duplicate ? "already reported" : "reported";
                Melon<Core>.Logger.Msg($"Run {runInfo.RunId} {outcome} (run {result.RunId}).");
            }
            catch (Exception e)
            {
                Melon<Core>.Logger.Error($"Failed to report run {runInfo.RunId}: {e.Message}");
            }
        });
    }
}
