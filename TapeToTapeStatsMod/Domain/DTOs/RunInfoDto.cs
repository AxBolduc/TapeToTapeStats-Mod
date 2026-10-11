namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// Run payload sent to <c>POST /api/v1/ingest/runs</c>. Property names must match
/// <c>runDtoSchema</c> in <c>packages/contracts/src/runIngest.ts</c>.
/// </summary>
/// <remarks>
/// The positional properties are the original (v1) payload. The init-only properties are the
/// optional v2 sections; the API drops any v2 section it can't validate instead of rejecting the run.
/// </remarks>
public record RunInfoDto(
    string RunId,
    int DifficultyLevel,
    int CurrentAct,
    int CurrentFloor,
    bool IsVictory,
    RunTeamDataDto PlayerTeamData,
    List<RunMatchScoreDto> MatchScores)
{
    /// <summary>False for the snapshots sent while the run is still being played.</summary>
    public bool? IsCompleted { get; init; }

    public string? ModVersion { get; init; }

    public string? SquadId { get; init; }

    public int? GameRule { get; init; }

    public float? RunTimeSeconds { get; init; }

    public bool? IsConceded { get; init; }

    public bool? IsTutorialRun { get; init; }

    public List<RelicDto>? Relics { get; init; }

    public List<RosterMemberDto>? Roster { get; init; }

    public List<SkaterStatsDto>? SkaterStats { get; init; }

    public GoalieStatsDto? GoalieStats { get; init; }

    public List<RunEventDto>? Events { get; init; }
}
