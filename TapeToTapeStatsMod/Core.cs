using MelonLoader;
using TapeToTapeStatsMod.Services;
using TapeToTapeStatsMod.Tracking;

[assembly: MelonInfo(typeof(TapeToTapeStatsMod.Core), "TapeToTapeStatsMod", TapeToTapeStatsMod.Core.ModVersion, "Alex")]
[assembly: MelonGame("Excellent Rectangle", "Tape to Tape")]

namespace TapeToTapeStatsMod;

public class Core : MelonMod
{
    /// <summary>Sent with every run so the API can tell which payload version a mod build produces.</summary>
    public const string ModVersion = "1.4.0";

    public override void OnInitializeMelon()
    {
        PreferenceService.Initialize();
        RunReporter.Initialize();

        try
        {
            RunEventLog.PruneOldLogs();
        }
        catch (Exception e)
        {
            LoggerInstance.Warning($"Could not prune old event logs: {e.Message}");
        }

        LoggerInstance.Msg("Initialized.");
    }

    public override void OnUpdate()
    {
        RunSnapshotScheduler.Tick();
    }
}
