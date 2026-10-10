namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// Something the player decided or the game reported during a run, in the order it happened.
/// <c>Type</c> is one of the constants in <see cref="Tracking.RunEventTypes"/>; <c>Data</c> is type-specific.
/// </summary>
public record RunEventDto(
    int Sequence,
    string Type,
    string OccurredAt,
    int? Act,
    int? Floor,
    Dictionary<string, object?> Data);
