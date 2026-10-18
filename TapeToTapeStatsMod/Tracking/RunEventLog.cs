using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using TapeToTapeStatsMod.Domain.DTOs;

namespace TapeToTapeStatsMod.Tracking;

/// <summary>
/// Append-only event log per run, one JSON object per line in
/// <c>UserData/TapeToTapeStats/events/&lt;runId&gt;.jsonl</c>. Runs span game sessions (save, quit, resume),
/// so events are kept on disk until the run is reported. Only call from the main thread.
/// </summary>
public static class RunEventLog
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    private static readonly Dictionary<string, RunLogState> States = new();

    private static string Directory => Path.Combine(MelonEnvironment.UserDataDirectory, "TapeToTapeStats", "events");

    public static void Append(string runId, string type, int? act, int? floor, Dictionary<string, object?> data)
    {
        var state = GetState(runId);
        var entry = new RunEventDto(
            Sequence: state.NextSequence,
            Type: type,
            OccurredAt: DateTime.UtcNow.ToString("O"),
            Act: act,
            Floor: floor,
            Data: data);

        System.IO.Directory.CreateDirectory(Directory);
        File.AppendAllText(PathFor(runId), JsonConvert.SerializeObject(entry) + "\n");

        state.NextSequence++;
        state.LastByType[type] = entry;
        MelonDebug.Msg($"[{type}] {JsonConvert.SerializeObject(data)}");
    }

    /// <summary>The most recent event of a type in this run, used to skip duplicates.</summary>
    public static RunEventDto? Last(string runId, string type)
    {
        return GetState(runId).LastByType.TryGetValue(type, out var entry) ? entry : null;
    }

    public static List<RunEventDto> ReadAll(string runId)
    {
        var events = new List<RunEventDto>();
        var path = PathFor(runId);

        if (!File.Exists(path))
        {
            return events;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonConvert.DeserializeObject<RunEventDto>(line);

                if (entry != null)
                {
                    events.Add(entry);
                }
            }
            catch (JsonException e)
            {
                // A torn last line (the game was killed mid-write) shouldn't lose the rest of the run.
                Melon<Core>.Logger.Warning($"Skipping unreadable event in {path}: {e.Message}");
            }
        }

        return events;
    }

    /// <summary>The run's events for a report, or null (logged) if the log can't be read.</summary>
    public static List<RunEventDto>? TryReadAll(string runId)
    {
        try
        {
            return ReadAll(runId);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Warning($"Could not read the event log for run {runId}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes logs for runs that are long finished. Logs are kept after reporting because the game can
    /// end the same run more than once, and after a restart a run's next report resends its whole timeline.
    /// </summary>
    public static void PruneOldLogs()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return;
        }

        foreach (var path in System.IO.Directory.GetFiles(Directory, "*.jsonl"))
        {
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > Retention)
            {
                File.Delete(path);
            }
        }
    }

    private static RunLogState GetState(string runId)
    {
        if (States.TryGetValue(runId, out var state))
        {
            return state;
        }

        state = new RunLogState();

        foreach (var entry in ReadAll(runId))
        {
            state.NextSequence = Math.Max(state.NextSequence, entry.Sequence + 1);
            state.LastByType[entry.Type] = entry;
        }

        States[runId] = state;
        return state;
    }

    private static string PathFor(string runId)
    {
        var safeRunId = string.Concat(runId.Where(character => char.IsLetterOrDigit(character) || character == '-'));

        return Path.Combine(Directory, $"{safeRunId}.jsonl");
    }

    private sealed class RunLogState
    {
        public int NextSequence { get; set; }

        public Dictionary<string, RunEventDto> LastByType { get; } = new();
    }
}
