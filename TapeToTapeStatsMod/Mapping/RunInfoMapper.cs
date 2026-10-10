using Il2Cpp;
using Il2CppData;
using Il2CppRogue;
using TapeToTapeStatsMod.Domain.DTOs;
using GameRunEndHandler = Il2CppTape2Tape.Hockey.UI.RunEndHandler;

namespace TapeToTapeStatsMod.Mapping;

/// <summary>
/// Copies game state into plain DTOs. Il2Cpp objects must only be read on the main
/// thread, so map synchronously before handing the result to a background task.
/// </summary>
public static class RunInfoMapper
{
    public static RunInfoDto FromRunEndHandler(GameRunEndHandler handler)
    {
        var campaignState = handler.m_CampaignState;
        var runData = campaignState.runData;

        return new RunInfoDto(
            RunId: runData.runID,
            DifficultyLevel: runData.difficultyLevel,
            CurrentAct: runData._currentAct,
            CurrentFloor: runData.currentFloor,
            IsVictory: handler.m_IsVictory,
            PlayerTeamData: ToTeamDto(runData.teamData),
            MatchScores: campaignState.runStatsState.runMatchScores.ToArray().Select(ToMatchScoreDto).ToList());
    }

    private static RunTeamDataDto ToTeamDto(TeamData teamData)
    {
        return new RunTeamDataDto(
            TeamId: teamData.id,
            TeamName: teamData.teamName,
            Center: ToSkaterDto(teamData.center),
            LeftWinger: ToSkaterDto(teamData.leftWinger),
            RightWinger: ToSkaterDto(teamData.rightWinger),
            LeftDefenceman: ToSkaterDto(teamData.leftDefensemen),
            RightDefenceMan: ToSkaterDto(teamData.rightDefensemen));
    }

    private static RunSkaterInfoDto? ToSkaterDto(ForwardData? skater)
    {
        if (skater == null)
        {
            return null;
        }

        return new RunSkaterInfoDto(
            FirstName: skater.firstName,
            LastName: skater.lastName,
            Number: skater.number,
            Powerups: skater.powerups.ToArray().Select(ToTalentDto).ToList(),
            ShotAccuracy: skater.shotAccuracy,
            ShotPower: skater.shotPower,
            Speed: skater.speed,
            SkaterSize: (int)skater.skaterSize);
    }

    private static TalentDto ToTalentDto(Talent talent)
    {
        return new TalentDto(
            Id: talent.id,
            Name: talent.localizedPowerupName,
            Level: talent.level,
            Rarity: (int)talent.rarity);
    }

    private static RunMatchScoreDto ToMatchScoreDto(MatchScore matchScore)
    {
        return new RunMatchScoreDto(
            TeamScore: matchScore.SelfScore,
            OpponentScore: matchScore.OtherScore,
            IsShootout: matchScore.Shootout,
            IsBoss: matchScore.IsBoss,
            NumAct: matchScore.act,
            OpponentId: matchScore.teamId);
    }
}
