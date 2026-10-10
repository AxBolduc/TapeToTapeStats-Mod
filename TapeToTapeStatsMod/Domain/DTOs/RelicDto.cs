namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// A team relic. <c>Category</c> is the game's <c>RelicCategory</c> flags and <c>Rarity</c> its <c>PowerUpRarity</c>.
/// </summary>
public record RelicDto(string Id, string Name, int Level, int Rarity, int Category);
