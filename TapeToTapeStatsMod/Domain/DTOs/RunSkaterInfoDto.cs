namespace TapeToTapeStatsMod.Domain.DTOs;

public record RunSkaterInfoDto(
    string FirstName,
    string LastName,
    int Number,
    List<TalentDto> Powerups,
    int ShotAccuracy,
    int ShotPower,
    int Speed,
    int SkaterSize);
