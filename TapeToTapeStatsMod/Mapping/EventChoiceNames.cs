using Il2CppTape2Tape.Hockey.UI;

namespace TapeToTapeStatsMod.Mapping;

public static class EventChoiceNames
{
    /// <summary>
    /// Describes one event card: its kind (<c>EEventChoiceType</c>), label, the skater it targets, and the
    /// talent/relic it grants, if any.
    /// </summary>
    public static Dictionary<string, object?>? Describe(EventChoiceItem? item)
    {
        if (item == null)
        {
            return null;
        }

        var choice = item.ChoiceData;
        var description = new Dictionary<string, object?>
        {
            ["choiceType"] = choice.ChoiceType.ToString(),
            ["label"] = string.IsNullOrEmpty(choice.translatedButtonName) ? choice.buttonName : choice.translatedButtonName,
            ["skater"] = GameObjectNames.Describe(choice.Skater),
            ["item"] = GameObjectNames.Describe(choice.PowerUpOrRelic),
            ["item2"] = GameObjectNames.Describe(choice.PowerUpOrRelic2),
            ["interactable"] = choice.interactable,
        };

        var customTexts = choice.CustomLocalizedTexts;

        if (customTexts != null && customTexts.Length > 0)
        {
            description["texts"] = customTexts.Where(text => !string.IsNullOrEmpty(text)).ToList();
        }

        return description;
    }
}
