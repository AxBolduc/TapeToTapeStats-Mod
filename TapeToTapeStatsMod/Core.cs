using MelonLoader;
using TapeToTapeStatsMod.Services;

[assembly: MelonInfo(typeof(TapeToTapeStatsMod.Core), "TapeToTapeStatsMod", "1.0.0", "Alex")]
[assembly: MelonGame("Excellent Rectangle", "Tape to Tape")]

namespace TapeToTapeStatsMod;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        PreferenceService.Initialize();
        RunReporter.Initialize();

        LoggerInstance.Msg("Initialized.");
    }
}
