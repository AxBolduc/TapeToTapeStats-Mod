using MelonLoader;
using Newtonsoft.Json;
using TapeToTapeMods.Domain.DTOs;

namespace TapeToTapeMods.Patches;

[HarmonyLib.HarmonyPatch(typeof(Il2CppTape2Tape.Hockey.UI.RunEndHandler),
    nameof(Il2CppTape2Tape.Hockey.UI.RunEndHandler.EndRun))]
public class RunEndHandler
{

    // Use a Postfix so you don't block the actual EndRun logic
    static void Prefix(Il2CppTape2Tape.Hockey.UI.RunEndHandler __instance)
    {
        // Capture everything we need synchronously
        var runStats = __instance.m_CampaignState.runStatsState;
        var runData = __instance.m_CampaignState.runData;

        var runInfo = new RunInfoDto()
        {
            RunId = runData.runID,
            DifficultyLevel = runData.difficultyLevel,
            CurrentAct = runData._currentAct,
            CurrentFloor = runData.currentFloor,
            IsVictory = __instance.m_IsVictory,
            PlayerTeamData = new RunTeamDataDto(runData.teamData),
            MatchScores = __instance.m_CampaignState.runStatsState.runMatchScores.ToArray()
                .Select((
                    matchData) => new RunMatchScoreDto(matchData))
                .ToList()
        };

        string jsonOutput = JsonConvert.SerializeObject(runInfo, Formatting.Indented);
        
        Melon<Core>.Logger.Msg("OUTPUT JSON");
        Melon<Core>.Logger.Msg(jsonOutput);
        Melon<Core>.Logger.Msg("OUTPUT JSON END");


        // Fire and forget, but safely
        /*Task.Run(async () =>
        {
            try
            {
                var response = await TapeToTapeStatsClient.GetInstance().GetAllRuns();
                Melon<Core>.Logger.Msg("Successfully sent run data.");
            }
            catch (Exception e)
            {
                Melon<Core>.Logger.Error($"Network request failed: {e.Message}");
            }
        });*/
    }
}