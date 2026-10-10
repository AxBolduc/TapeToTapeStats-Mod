using HarmonyLib;
using Il2CppTape2Tape.Analytics;
using Il2CppTape2Tape.Analytics.Events;
using Newtonsoft.Json.Linq;
using TapeToTapeStatsMod.Mapping;
using TapeToTapeStatsMod.Tracking;

namespace TapeToTapeStatsMod.Patches;

/// <summary>
/// Mirrors the game's own analytics events into the run's event log. These are virtual methods called
/// through <c>IAnalyticsService</c>, so the native calls can't be inlined past the patch, and the game
/// fills them in regardless of analytics consent. Prefixes only read the args.
/// </summary>
[HarmonyPatch(typeof(UnityAnalyticsService))]
public static class AnalyticsPatches
{
    // Team stat strings are small today; cap them so a game change can't bloat the payload.
    private const int MaxRawStringLength = 8000;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(UnityAnalyticsService.LogEndOfGameEvent))]
    private static void LogEndOfGameEvent(EndOfGameEventArgs args)
    {
        RecordCampaignEvent(args, RunEventTypes.MatchEnded, () =>
        {
            var data = new Dictionary<string, object?>
            {
                ["gameMode"] = args.GameMode,
                ["gameType"] = args.GameType,
                ["ruleType"] = args.RuleType,
                ["isConceded"] = args.IsConceded,
                ["homeTeam"] = args.HomeTeam,
                ["visitorTeam"] = args.VisitorTeam,
                ["winningTeam"] = args.WinningTeam,
                ["homeScore"] = args.HomeScore,
                ["visitorScore"] = args.VisitorScore,
                ["homeShots"] = args.HomeShots,
                ["visitorShots"] = args.VisitorShots,
                ["homeStats"] = ParseMaybeJson(args.HomeStats),
                ["visitorStats"] = ParseMaybeJson(args.VisitorStats),
                ["periodsPlayed"] = args.NumberOfPeriodsPlayed,
                ["saveScummingCount"] = args.SaveScummingCount,
                ["playerCount"] = args.PlayerCount,
            };

            AddTeamState(data);
            return data;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(UnityAnalyticsService.LogGameRewardSelectedEvent))]
    private static void LogGameRewardSelectedEvent(GameRewardSelectedEventArgs args)
    {
        RecordCampaignEvent(args, RunEventTypes.GameRewardSelected, () => new Dictionary<string, object?>
        {
            ["rewardType"] = args.RewardType,
            ["chosen"] = args.ChosenReward,
            ["choices"] = Il2CppCollections.ToManagedList(args.Choices),
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(UnityAnalyticsService.LogEventRewardSelectedEvent))]
    private static void LogEventRewardSelectedEvent(EventRewardSelectedEventArgs args)
    {
        RecordCampaignEvent(args, RunEventTypes.EventRewardSelected, () => new Dictionary<string, object?>
        {
            ["nodeType"] = args.NodeType,
            ["rewardType"] = args.RewardType,
            ["chosen"] = ParseMaybeJson(args.ChosenReward),
            ["choices"] = Il2CppCollections.ToManagedList(args.Choices).Select(ParseMaybeJson).ToList(),
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(UnityAnalyticsService.LogRunCompletedEvent))]
    private static void LogRunCompletedEvent(RunCompletedEventArgs args)
    {
        RecordCampaignEvent(args, RunEventTypes.RunCompleted, () => new Dictionary<string, object?>
        {
            ["won"] = args.Won,
            ["timeTakenSeconds"] = args.TimeTakenInSeconds,
            ["playerCount"] = args.PlayerCount,
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(UnityAnalyticsService.LogUnlockEarnedEvent))]
    private static void LogUnlockEarnedEvent(UnlockEarnedEventArgs args)
    {
        RecordCampaignEvent(args, RunEventTypes.UnlockEarned, () => new Dictionary<string, object?>
        {
            ["unlockId"] = args.UnlockId,
            ["unlockName"] = args.UnlockName,
            ["unlockCategory"] = args.UnlockCategory,
            ["unlockSource"] = args.UnlockSource,
            ["unlockTiming"] = args.UnlockTiming,
            ["progress"] = args.Progress,
            ["requirement"] = args.Requirement,
        });
    }

    private static void RecordCampaignEvent(
        CampaignEventArgs args,
        string type,
        Func<Dictionary<string, object?>> buildData)
    {
        string? runId;

        try
        {
            runId = args.CampaignRunId;
        }
        catch
        {
            return;
        }

        // Exhibition, practice, and tournament games have no campaign run.
        if (string.IsNullOrEmpty(runId))
        {
            return;
        }

        var (act, floor) = RunTracker.CurrentPosition();

        RunTracker.RecordForRun(runId, type, act, floor, () =>
        {
            var data = buildData();

            data["difficulty"] = args.CurrentDifficulty;
            data["squad"] = args.MetaName;
            return data;
        });
    }

    /// <summary>The lineup and relics the match was played with, so per-match decisions can be compared.</summary>
    private static void AddTeamState(Dictionary<string, object?> data)
    {
        var team = RunTracker.CurrentCampaignState?.runData?.teamData;

        if (team == null)
        {
            return;
        }

        data["lineup"] = new Dictionary<string, object?>
        {
            ["center"] = team.center?.id,
            ["leftWinger"] = team.leftWinger?.id,
            ["rightWinger"] = team.rightWinger?.id,
            ["leftDefenceman"] = team.leftDefensemen?.id,
            ["rightDefenceman"] = team.rightDefensemen?.id,
            ["goalie"] = team.goalie?.id,
        };
        data["relicIds"] = team.relics?.ToArray().Where(relic => relic != null).Select(relic => relic.id).ToList();
    }

    /// <summary>
    /// Some analytics fields are JSON the game serialized itself; keep them structured when they parse.
    /// </summary>
    private static object? ParseMaybeJson(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var trimmed = value.TrimStart();

        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            try
            {
                return JToken.Parse(value);
            }
            catch
            {
                // Not JSON after all; fall through to the raw string.
            }
        }

        return value.Length > MaxRawStringLength ? value[..MaxRawStringLength] : value;
    }
}
