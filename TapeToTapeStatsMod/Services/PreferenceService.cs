using MelonLoader;

namespace TapeToTapeMods.Services;

public class PreferenceService
{
    private static MelonPreferences_Category _category;
    
    public static void Initialize()
    {
        if (_category != null)
        {
            Melon<Core>.Logger.Warning("Preference Service Already Initialized");
        }
        
        _category = MelonPreferences.CreateCategory("TapeToTapeStats");

        _category.CreateEntry("apiKey", "REPLACE");
        _category.SaveToFile();
    }

    public static string GetApiKey()
    {
        var entry =  _category.GetEntry<string>("apiKey");
        
        return entry.Value;
    }
}