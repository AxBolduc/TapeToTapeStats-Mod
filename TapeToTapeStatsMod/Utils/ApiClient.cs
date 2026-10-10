using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MelonLoader;

namespace TapeToTapeMods.Utils;

public class ApiClient
{
    [Serializable]
    public class RunEndData(int wins)
    {
        public int Wins = wins;
    }
    
    private static readonly HttpClient client = new HttpClient();

    public static async Task PostRunData(RunEndData runData)
    {
        try
        {
            var response = await client.PostAsJsonAsync("http://localhost:8080/data", runData);

            if (!response.IsSuccessStatusCode)
            {
                Melon<Core>.Logger.Error($"Failed to send run data: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Failed to send run data: {e.Message}");
        }
    }
}