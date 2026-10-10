namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// Run payload sent to <c>POST /api/v1/ingest/runs</c>. Property names must match
/// <c>runDtoSchema</c> in <c>packages/contracts/src/runIngest.ts</c>.
/// </summary>
public record RunInfoDto(
    string RunId,
    int DifficultyLevel,
    int CurrentAct,
    int CurrentFloor,
    bool IsVictory,
    RunTeamDataDto PlayerTeamData,
    List<RunMatchScoreDto> MatchScores);
