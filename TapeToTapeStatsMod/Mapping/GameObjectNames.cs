using Il2CppData;
using Il2CppRogue;
using Il2CppRogue.BenchPowers;
using Il2CppRogue.Rewards;
using TapeToTapeStatsMod.Domain.DTOs;
using UnityEngine;

namespace TapeToTapeStatsMod.Mapping;

/// <summary>
/// Turns the game's reward objects (talents, abilities, relics, bench powers, skaters) into small,
/// JSON-friendly descriptions for DTOs and decision events.
/// </summary>
public static class GameObjectNames
{
    public static RelicDto ToRelicDto(Relic relic)
    {
        return new RelicDto(
            Id: relic.id,
            Name: SafeName(() => relic.localizedRelicName, relic.relicName),
            Level: relic.level,
            Rarity: (int)relic.rarity,
            Category: (int)relic.category);
    }

    public static RosterPowerupDto ToPowerupDto(PowerUp powerUp)
    {
        return new RosterPowerupDto(
            Id: powerUp.id,
            Name: SafeName(() => powerUp.localizedPowerupName, powerUp.powerupName),
            Level: powerUp.level,
            Rarity: (int)powerUp.rarity);
    }

    /// <summary>
    /// Describes whatever a reward card or event choice holds. The game types these slots as
    /// <c>ScriptableObject</c>, so check each concrete type it can be.
    /// </summary>
    public static Dictionary<string, object?>? Describe(ScriptableObject? item)
    {
        if (item == null)
        {
            return null;
        }

        var relic = item.TryCast<Relic>();

        if (relic != null)
        {
            return new Dictionary<string, object?>
            {
                ["kind"] = "relic",
                ["id"] = relic.id,
                ["name"] = SafeName(() => relic.localizedRelicName, relic.relicName),
                ["level"] = relic.level,
                ["rarity"] = (int)relic.rarity,
                ["category"] = (int)relic.category,
            };
        }

        var powerUp = item.TryCast<PowerUp>();

        if (powerUp != null)
        {
            return new Dictionary<string, object?>
            {
                ["kind"] = powerUp.TryCast<Ability>() != null ? "ability" : "talent",
                ["id"] = powerUp.id,
                ["name"] = SafeName(() => powerUp.localizedPowerupName, powerUp.powerupName),
                ["level"] = powerUp.level,
                ["rarity"] = (int)powerUp.rarity,
            };
        }

        var benchPower = item.TryCast<BenchPower>();

        if (benchPower != null)
        {
            return new Dictionary<string, object?>
            {
                ["kind"] = "benchPower",
                ["id"] = benchPower.id,
                ["name"] = benchPower.benchPowerName,
            };
        }

        var skater = item.TryCast<SkaterData>();

        if (skater != null)
        {
            return Describe(skater);
        }

        return new Dictionary<string, object?>
        {
            ["kind"] = "unknown",
            ["type"] = item.GetIl2CppType().Name,
            ["name"] = item.name,
        };
    }

    public static Dictionary<string, object?>? Describe(SkaterData? skater)
    {
        if (skater == null)
        {
            return null;
        }

        var description = new Dictionary<string, object?>
        {
            ["kind"] = skater.TryCast<GoaltenderData>() != null ? "goalie" : "skater",
            ["id"] = skater.id,
            ["name"] = $"{skater.firstName} {skater.lastName}".Trim(),
            ["number"] = skater.number,
        };

        var forward = skater.TryCast<ForwardData>();

        if (forward != null)
        {
            description["overall"] = SafeInt(forward.GetDisplayOverallStats);
            description["rarity"] = (int)forward.forwardRarity;
        }

        return description;
    }

    public static Dictionary<string, object?>? Describe(Reward? reward)
    {
        if (reward == null)
        {
            return null;
        }

        var relicReward = reward.TryCast<RelicReward>();

        if (relicReward != null)
        {
            return Describe(relicReward.relic);
        }

        var powerUpReward = reward.TryCast<PowerUpReward>();

        if (powerUpReward != null)
        {
            return Describe(powerUpReward.powerUp);
        }

        var benchPowerReward = reward.TryCast<BenchPowerReward>();

        if (benchPowerReward != null)
        {
            return Describe(benchPowerReward.benchPower);
        }

        return new Dictionary<string, object?> { ["kind"] = "unknown", ["type"] = reward.GetIl2CppType().Name };
    }

    private static string SafeName(Func<string?> localized, string? fallback)
    {
        try
        {
            var name = localized();

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }
        catch
        {
            // Localization can throw before the language tables load; the internal name is fine then.
        }

        return fallback ?? "";
    }

    private static int? SafeInt(Func<int> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return null;
        }
    }
}
