namespace TapeToTapeStatsMod.Domain.DTOs;

public record RunMatchScoreDto(
    int TeamScore,
    int OpponentScore,
    bool IsShootout,
    bool IsBoss,
    int NumAct,
    string OpponentId);
