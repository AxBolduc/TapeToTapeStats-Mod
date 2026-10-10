using Il2Cpp;
using Il2CppData;
using Il2CppRogue;
using Il2CppRogue.BenchPowers;
using Il2CppState;
using Il2CppTape2Tape.Hockey;
using MelonLoader;
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
        var runStats = campaignState.runStatsState;

        return new RunInfoDto(
            RunId: runData.runID,
            DifficultyLevel: runData.difficultyLevel,
            CurrentAct: runData._currentAct,
            CurrentFloor: runData.currentFloor,
            IsVictory: handler.m_IsVictory,
            PlayerTeamData: ToTeamDto(runData.teamData),
            MatchScores: runStats.runMatchScores.ToArray().Select(ToMatchScoreDto).ToList())
        {
            ModVersion = Core.ModVersion,
            SquadId = Safe(() => runData.squadId),
            GameRule = Safe(() => (int?)runData.gameRule),
            RunTimeSeconds = Safe(() => (float?)campaignState.GetTotalRunTime()),
            IsConceded = Safe(() => (bool?)runData.isRunConceded),
            IsTutorialRun = Safe(() => (bool?)runData.isTutorialRun),
            Relics = Safe(() => ToRelicDtos(runData.teamData)),
            Roster = Safe(() => ToRosterDtos(runData)),
            SkaterStats = Safe(() => runStats.runScorersStats.ToArray().Select(ToSkaterStatsDto).ToList()),
            GoalieStats = Safe(() => ToGoalieStatsDto(runStats.goalieStats)),
        };
    }

    /// <summary>
    /// v2 sections are best-effort: a game update that breaks one of them should cost that section,
    /// not the whole run report.
    /// </summary>
    private static T? Safe<T>(Func<T?> read)
    {
        try
        {
            return read();
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Skipping a run detail section: {e.Message}");
            return default;
        }
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
            OpponentId: matchScore.teamId)
        {
            IsWin = matchScore.Win,
            MapX = (int)matchScore.MapPoint.x,
            MapY = (int)matchScore.MapPoint.y,
            SkaterStats = Safe(() => ToMatchSkaterStats(matchScore)),
            GoalieStats = Safe(() => ToGoalieStatsDto(matchScore.goalieStats)),
        };
    }

    private static List<SkaterStatsDto> ToMatchSkaterStats(MatchScore matchScore)
    {
        var stats = new List<SkaterStatsDto>();

        if (matchScore.scorersStats == null)
        {
            return stats;
        }

        foreach (var entry in matchScore.scorersStats)
        {
            stats.Add(ToSkaterStatsDto(entry.Value));
        }

        return stats;
    }

    private static SkaterStatsDto ToSkaterStatsDto(ScorersStats stats)
    {
        return new SkaterStatsDto(
            SkaterId: stats.skaterId,
            DisplayName: stats.displayName ?? "",
            Goals: stats.goals,
            Assists: stats.assists,
            Shots: stats.shots,
            Hits: stats.hits,
            InjuriesReceived: stats.injuriesReceived);
    }

    private static GoalieStatsDto? ToGoalieStatsDto(GoalieStats? stats)
    {
        if (stats == null || string.IsNullOrEmpty(stats.skaterId))
        {
            return null;
        }

        return new GoalieStatsDto(
            SkaterId: stats.skaterId,
            DisplayName: stats.displayName ?? "",
            GamesPlayed: stats.gamesPlayed,
            GoalsAgainst: stats.goalsAgainst,
            ShotsAgainst: stats.shotsAgainst,
            Hits: stats.hits);
    }

    private static List<RelicDto> ToRelicDtos(TeamData teamData)
    {
        return teamData.relics.ToArray()
            .Where(relic => relic != null)
            .Select(GameObjectNames.ToRelicDto)
            .ToList();
    }

    private static List<RosterMemberDto> ToRosterDtos(RunData runData)
    {
        var team = runData.teamData;
        var roster = new List<RosterMemberDto>();
        var seenIds = new HashSet<string>();

        void Add(SkaterData? skater, string position)
        {
            if (skater == null || string.IsNullOrEmpty(skater.id) || !seenIds.Add(skater.id))
            {
                return;
            }

            roster.Add(ToRosterMemberDto(skater, position));
        }

        Add(team.center, "center");
        Add(team.leftWinger, "leftWinger");
        Add(team.rightWinger, "rightWinger");
        Add(team.leftDefensemen, "leftDefenceman");
        Add(team.rightDefensemen, "rightDefenceman");

        // Everyone on the team who isn't in the active lineup sits on the bench (other lines included).
        foreach (var forward in team.forwards.ToArray())
        {
            Add(forward, "bench");
        }

        Add(team.goalie, "goalie");

        if (runData.departedForwards != null)
        {
            foreach (var departed in runData.departedForwards.ToArray())
            {
                Add(departed, "departed");
            }
        }

        return roster;
    }

    private static RosterMemberDto ToRosterMemberDto(SkaterData skater, string position)
    {
        var talents = skater.powerups == null
            ? new List<RosterPowerupDto>()
            : skater.powerups.ToArray().Where(talent => talent != null).Select(GameObjectNames.ToPowerupDto).ToList();
        var forward = skater.TryCast<ForwardData>();

        return new RosterMemberDto(
            SkaterId: skater.id,
            Position: position,
            FirstName: skater.firstName ?? "",
            LastName: skater.lastName ?? "",
            Number: skater.number,
            Talents: talents)
        {
            ShotAccuracy = forward?.shotAccuracy,
            ShotPower = forward?.shotPower,
            Speed = forward?.speed,
            Checking = forward?.checking,
            Overall = forward == null ? null : Safe(() => (int?)forward.GetDisplayOverallStats()),
            SkaterSize = forward == null ? null : (int)forward.skaterSize,
            Rarity = forward == null ? null : (int)forward.forwardRarity,
            Category = forward == null ? null : (int)forward.skaterCategory,
            Ability = skater.ability == null ? null : GameObjectNames.ToPowerupDto(skater.ability),
            BenchPower = skater.benchPower == null ? null : ToBenchPowerDto(skater.benchPower),
        };
    }

    private static RosterPowerupDto ToBenchPowerDto(BenchPower benchPower)
    {
        return new RosterPowerupDto(benchPower.id, benchPower.benchPowerName ?? "", null, null);
    }
}
