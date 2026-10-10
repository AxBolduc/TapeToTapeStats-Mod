using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; // Use Newtonsoft.Json if System.Text.Json is still throwing errors
using TapeToTapeMods.Domain.DTOs;
using TapeToTapeMods.Services;

namespace TapeToTapeMods.Clients
{
    public class TapeToTapeStatsClient : ITapeToTapeStatsClient
    {
        private static readonly HttpClient Client = new HttpClient();
        private static TapeToTapeStatsClient _instance;
        private const string BaseUrl = "https://api.test.tapetotape.axbolduc.com";


        public static TapeToTapeStatsClient GetInstance()
        {

            var apiKey = PreferenceService.GetApiKey();
            return _instance ??= new TapeToTapeStatsClient(apiKey);
        }

        private TapeToTapeStatsClient(String apiKey)
        {
            // Configure base headers once
            Client.BaseAddress = new Uri(BaseUrl);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            
            // If you need custom headers (API Key), set them here
            Client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        public async Task<RunReportResultDto> ReportRun(RunInfoDto runInfo)
        {
            var json = JsonSerializer.Serialize(runInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Client.PostAsync("/v1/runs", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RunReportResultDto>(responseBody);
        }

        public async Task<List<RunInfoDto>> GetAllRuns()
        {
            var response = await Client.GetAsync("/v1/runs");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<RunInfoDto>>(responseBody);
        }
    }
}