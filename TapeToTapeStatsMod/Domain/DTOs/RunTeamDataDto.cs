namespace TapeToTapeStatsMod.Domain.DTOs;

public record RunTeamDataDto(
    string TeamId,
    string TeamName,
    RunSkaterInfoDto? Center,
    RunSkaterInfoDto? LeftWinger,
    RunSkaterInfoDto? RightWinger,
    RunSkaterInfoDto? LeftDefenceman,
    RunSkaterInfoDto? RightDefenceMan);
