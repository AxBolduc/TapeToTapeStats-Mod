using MelonLoader;

namespace TapeToTapeStatsMod.Services;

/// <summary>
/// Settings stored in <c>UserData/MelonPreferences.cfg</c> under <c>[TapeToTapeStats]</c>.
/// </summary>
public static class PreferenceService
{
    private const string DefaultApiBaseUrl = "https://api.tapetotape.axbolduc.com";

    private static MelonPreferences_Entry<string>? _apiKey;
    private static MelonPreferences_Entry<string>? _apiBaseUrl;

    public static void Initialize()
    {
        if (_apiKey != null)
        {
            return;
        }

        var category = MelonPreferences.CreateCategory("TapeToTapeStats");

        _apiKey = category.CreateEntry("apiKey", "", description: "API key from your Tape to Tape Stats account.");
        _apiBaseUrl = category.CreateEntry("apiBaseUrl", DefaultApiBaseUrl, description: "Base URL of the Tape to Tape Stats API.");
        category.SaveToFile(false);
    }

    public static string ApiKey => _apiKey?.Value.Trim() ?? "";

    public static string ApiBaseUrl => _apiBaseUrl?.Value.Trim().TrimEnd('/') ?? DefaultApiBaseUrl;

    public static void SetApiKey(string apiKey)
    {
        if (_apiKey == null)
        {
            return;
        }

        _apiKey.Value = apiKey.Trim();
        MelonPreferences.Save();
    }

    public static void SetApiBaseUrl(string apiBaseUrl)
    {
        if (_apiBaseUrl == null)
        {
            return;
        }

        _apiBaseUrl.Value = apiBaseUrl.Trim();
        MelonPreferences.Save();
    }
}
