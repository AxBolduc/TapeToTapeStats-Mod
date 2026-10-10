using HarmonyLib;
using Il2CppState;
using Il2CppSTS.Map;
using Il2CppTape2Tape.Hockey.UI;
using TapeToTapeStatsMod.Mapping;
using TapeToTapeStatsMod.Tracking;

namespace TapeToTapeStatsMod.Patches;

/// <summary>The player picks the next map node.</summary>
[HarmonyPatch(typeof(CampaignState), nameof(CampaignState.SetCurrentNode))]
public static class MapNodeSelectedPatch
{
    private static void Postfix(CampaignState __instance, MapNode mapNode)
    {
        RunTracker.Observe(__instance);

        if (mapNode == null)
        {
            return;
        }

        var runId = __instance.runData?.runID;

        if (string.IsNullOrEmpty(runId))
        {
            return;
        }

        var point = mapNode.Node?.point;
        var nodeKey = $"{mapNode.act}:{point?.x}:{point?.y}";

        // Resuming a saved run re-applies the current node; that isn't a new decision.
        var last = RunEventLog.Last(runId, RunEventTypes.NodeSelected);

        if (last != null && last.Data.TryGetValue("nodeKey", out var lastKey) && lastKey?.ToString() == nodeKey)
        {
            return;
        }

        RunTracker.Record(RunEventTypes.NodeSelected, () => new Dictionary<string, object?>
        {
            ["nodeKey"] = nodeKey,
            ["nodeType"] = mapNode.NodeType.ToString(),
            ["act"] = mapNode.act,
            ["floor"] = mapNode.floor,
            ["x"] = point?.x,
            ["y"] = point?.y,
        }, __instance);
    }
}

/// <summary>A post-match talent, ability, or bench power is assigned to a skater.</summary>
[HarmonyPatch(typeof(SkaterPowerupRewardMenu), nameof(SkaterPowerupRewardMenu.ApplyCurrentSelection))]
public static class PowerupRewardAppliedPatch
{
    // Prefix: the menu clears its selection while applying.
    private static void Prefix(SkaterPowerupRewardMenu __instance)
    {
        RunTracker.Record(RunEventTypes.PowerupRewardApplied, () =>
        {
            var offered = Il2CppCollections.ToManagedList(__instance.m_PowerUpRewards)
                .Select(reward => GameObjectNames.Describe(reward))
                .Concat(Il2CppCollections.ToManagedList(__instance.m_BenchRewards)
                    .Select(reward => GameObjectNames.Describe(reward)))
                .ToList();
            var skaterChoices = Il2CppCollections.ToManagedList(__instance.m_Skaters)
                .Select(skater => GameObjectNames.Describe(skater))
                .ToList();

            return new Dictionary<string, object?>
            {
                ["chosen"] = GameObjectNames.Describe(__instance.GetSelectedPower()),
                ["skater"] = GameObjectNames.Describe(__instance.GetSelectedSkater()),
                ["offered"] = offered,
                ["skaterChoices"] = skaterChoices,
                ["rerollCount"] = __instance.m_RerollCount,
            };
        }, __instance.m_CampaignState);
    }
}

/// <summary>A relic reward is taken.</summary>
[HarmonyPatch(typeof(RelicRewardMenu), nameof(RelicRewardMenu.ApplyCurrentSelection))]
public static class RelicRewardAppliedPatch
{
    private static void Prefix(RelicRewardMenu __instance)
    {
        RunTracker.Record(RunEventTypes.RelicRewardApplied, () =>
        {
            var offered = Il2CppCollections.ToManagedList(__instance.m_RelicRewards);
            var index = __instance.m_CurrentRelicChoice;
            var chosen = index >= 0 && index < offered.Count ? offered[index] : null;

            return new Dictionary<string, object?>
            {
                ["chosen"] = GameObjectNames.Describe(chosen),
                ["chosenIndex"] = index,
                ["offered"] = offered.Select(reward => GameObjectNames.Describe(reward)).ToList(),
                ["category"] = (int)__instance.m_Category,
                ["rerollCount"] = __instance.m_RerollCount,
            };
        }, __instance.m_CampaignState);
    }
}

/// <summary>A choice is made at an event node. Multi-step events (e.g. pick a skater, then a procedure) record each step.</summary>
[HarmonyPatch(typeof(EventChoiceMenu), nameof(EventChoiceMenu.Choose))]
public static class EventChoiceMadePatch
{
    private static void Prefix(EventChoiceMenu __instance, EventChoiceItem choice)
    {
        RunTracker.Record(RunEventTypes.EventChoiceMade, () =>
        {
            var offered = new List<Dictionary<string, object?>?>();
            var items = __instance.m_Items;

            if (items != null)
            {
                for (var index = 0; index < items.Count; index++)
                {
                    offered.Add(EventChoiceNames.Describe(items[index]));
                }
            }

            return new Dictionary<string, object?>
            {
                ["nodeType"] = __instance.m_CampaignState?.CurrentNodeType.ToString(),
                ["chosen"] = EventChoiceNames.Describe(choice),
                ["offered"] = offered,
                ["choicesMade"] = __instance.m_ChoicesMade,
                ["rerollCount"] = __instance.m_RerollCount,
            };
        }, __instance.m_CampaignState);
    }
}

/// <summary>The player leaves an event node without taking anything.</summary>
[HarmonyPatch(typeof(EventChoiceMenu), nameof(EventChoiceMenu.LogRewardSkippedAnalyticsEvent))]
public static class EventChoiceSkippedPatch
{
    // Hooked on the analytics call rather than ExitWithNoChoice, which runs before the confirm prompt.
    private static void Prefix(EventChoiceMenu __instance)
    {
        RunTracker.Record(RunEventTypes.EventChoiceSkipped, () => new Dictionary<string, object?>
        {
            ["nodeType"] = __instance.m_CampaignState?.CurrentNodeType.ToString(),
            ["choicesMade"] = __instance.m_ChoicesMade,
        }, __instance.m_CampaignState);
    }
}

[HarmonyPatch]
public static class ChoicesRerolledPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SkaterPowerupRewardMenu), nameof(SkaterPowerupRewardMenu.RerollChoicesNow))]
    private static void PowerupRerolled(SkaterPowerupRewardMenu __instance)
    {
        RunTracker.Record(RunEventTypes.ChoicesRerolled, () => new Dictionary<string, object?>
        {
            ["menu"] = "powerupReward",
            ["rerollCount"] = __instance.m_RerollCount,
        }, __instance.m_CampaignState);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RelicRewardMenu), nameof(RelicRewardMenu.RerollChoicesNow))]
    private static void RelicRerolled(RelicRewardMenu __instance)
    {
        RunTracker.Record(RunEventTypes.ChoicesRerolled, () => new Dictionary<string, object?>
        {
            ["menu"] = "relicReward",
            ["rerollCount"] = __instance.m_RerollCount,
        }, __instance.m_CampaignState);
    }

    // RerollChoices can bail out when rerolling isn't allowed; the in-place coroutine only starts for a real reroll.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EventChoiceMenu), nameof(EventChoiceMenu.RerollChoicesInPlace))]
    private static void EventRerolled(EventChoiceMenu __instance)
    {
        RunTracker.Record(RunEventTypes.ChoicesRerolled, () => new Dictionary<string, object?>
        {
            ["menu"] = "event",
            ["nodeType"] = __instance.m_CampaignState?.CurrentNodeType.ToString(),
            ["rerollCount"] = __instance.m_RerollCount,
        }, __instance.m_CampaignState);
    }
}
