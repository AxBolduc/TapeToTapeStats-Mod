namespace TapeToTapeStatsMod.Domain.DTOs;

/// <summary>
/// Mirrors <c>runIngestResponseSchema</c> in <c>packages/contracts/src/runIngest.ts</c>.
/// <c>LastEventSequence</c> is the highest event sequence the API has stored for the run (null before its first).
/// </summary>
public record RunIngestResponseDto(bool Accepted, bool Duplicate, int? LastEventSequence, string RunId, string SubmissionId);
