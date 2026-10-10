namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// Mirrors <c>runIngestResponseSchema</c> in <c>packages/contracts/src/runIngest.ts</c>.
/// </summary>
public record RunIngestResponseDto(bool Accepted, bool Duplicate, string RunId, string SubmissionId);
