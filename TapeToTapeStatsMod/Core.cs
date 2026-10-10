using MelonLoader;
using TapeToTapeMods.Clients;
using TapeToTapeMods.Services;

[assembly: MelonInfo(typeof(TapeToTapeMods.Core), "TapeToTapeStatsMod", "1.0.0", "Alex", null)]
[assembly: MelonGame("Excellent Rectangle", "Tape to Tape")]

namespace TapeToTapeMods
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            PreferenceService.Initialize();
            TapeToTapeStatsClient.GetInstance();
            
            LoggerInstance.Msg("Initialized.");
        }
    }
}