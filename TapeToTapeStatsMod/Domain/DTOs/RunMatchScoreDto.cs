namespace TapeToTapeStatsMod.Domain.DTOs;

public record RunMatchScoreDto(
    int TeamScore,
    int OpponentScore,
    bool IsShootout,
    bool IsBoss,
    int NumAct,
    string OpponentId)
{
    public bool? IsWin { get; init; }

    /// <summary>Map node the match was played on.</summary>
    public int? MapX { get; init; }

    public int? MapY { get; init; }

    public List<SkaterStatsDto>? SkaterStats { get; init; }

    public GoalieStatsDto? GoalieStats { get; init; }
}
