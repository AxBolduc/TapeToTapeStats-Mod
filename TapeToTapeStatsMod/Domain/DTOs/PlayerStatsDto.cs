namespace TapeToTapeStatsMod.Domain.DTOs;

public record SkaterStatsDto(
    string SkaterId,
    string DisplayName,
    int Goals,
    int Assists,
    int Shots,
    int Hits,
    int InjuriesReceived);

public record GoalieStatsDto(
    string SkaterId,
    string DisplayName,
    int GamesPlayed,
    int GoalsAgainst,
    int ShotsAgainst,
    int Hits);
