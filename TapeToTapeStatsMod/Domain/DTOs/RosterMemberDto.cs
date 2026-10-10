namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// One member of the player's roster at the end of the run. <c>Position</c> is one of the five lineup slots
/// (<c>center</c>, <c>leftWinger</c>, <c>rightWinger</c>, <c>leftDefenceman</c>, <c>rightDefenceman</c>),
/// <c>bench</c>, <c>goalie</c>, or <c>departed</c>. Skater-only stats are null for the goalie.
/// </summary>
public record RosterMemberDto(
    string SkaterId,
    string Position,
    string FirstName,
    string LastName,
    int Number,
    List<RosterPowerupDto> Talents)
{
    public int? ShotAccuracy { get; init; }

    public int? ShotPower { get; init; }

    public int? Speed { get; init; }

    public int? Checking { get; init; }

    public int? Overall { get; init; }

    public int? SkaterSize { get; init; }

    /// <summary><c>ForwardRarity</c>: 0 vanilla, 1 elite.</summary>
    public int? Rarity { get; init; }

    /// <summary><c>SkaterCategory</c>.</summary>
    public int? Category { get; init; }

    public RosterPowerupDto? Ability { get; init; }

    public RosterPowerupDto? BenchPower { get; init; }
}

/// <summary>A talent, ability, or bench power. Bench powers have no level or rarity.</summary>
public record RosterPowerupDto(string Id, string Name, int? Level, int? Rarity);
