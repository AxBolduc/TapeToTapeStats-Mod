using Il2CppData;

namespace TapeToTapeMods.Domain.DTOs;

public record RunTeamDataDto
{
    public RunTeamDataDto(string TeamId,
        string TeamName,
        RunSkaterInfoDto Center,
        RunSkaterInfoDto LeftWinger,
        RunSkaterInfoDto RightWinger,
        RunSkaterInfoDto LeftDefenceman,
        RunSkaterInfoDto RightDefenceMan,
        GoaltenderData Goalie)
    {
        this.TeamId = TeamId;
        this.TeamName = TeamName;
        this.Center = Center;
        this.LeftWinger = LeftWinger;
        this.RightWinger = RightWinger;
        this.LeftDefenceman = LeftDefenceman;
        this.RightDefenceMan = RightDefenceMan;
        // this.Goalie = Goalie;
    }

    public RunTeamDataDto(TeamData teamData)
    {
        this.TeamId = teamData.id;
        this.TeamName = teamData.teamName;
        this.Center = teamData.center != null ? new RunSkaterInfoDto(teamData.center) : null;
        this.LeftWinger = teamData.center != null ? new RunSkaterInfoDto(teamData.leftWinger) : null;
        this.RightWinger = teamData.rightWinger != null ? new RunSkaterInfoDto(teamData.rightWinger) : null;
        this.LeftDefenceman = teamData.leftDefensemen != null ? new RunSkaterInfoDto(teamData.leftDefensemen)  : null;
        this.RightDefenceMan = teamData.leftDefensemen != null ? new RunSkaterInfoDto(teamData.rightDefensemen) : null;
        // this.Goalie = teamData.goalie;
    }

    public string TeamId { get; init; }
    public string TeamName { get; init; }
    public RunSkaterInfoDto? Center { get; init; }
    public RunSkaterInfoDto? LeftWinger { get; init; }
    public RunSkaterInfoDto? RightWinger { get; init; }
    public RunSkaterInfoDto? LeftDefenceman { get; init; }
    public RunSkaterInfoDto? RightDefenceMan { get; init; }
    // public GoaltenderData Goalie { get; init; }
}