using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using TapeToTapeStatsMod.Domain.DTOs;

namespace TapeToTapeStatsMod.Clients;

public sealed class TapeToTapeStatsClient
{
    private const string IngestRunPath = "api/v1/ingest/runs";

    private readonly HttpClient _httpClient;

    public TapeToTapeStatsClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<RunIngestResponseDto> ReportRun(RunInfoDto runInfo)
    {
        var json = JsonConvert.SerializeObject(runInfo);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(IngestRunPath, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}");
        }

        return JsonConvert.DeserializeObject<RunIngestResponseDto>(responseBody)
            ?? throw new JsonException("Empty response body.");
    }
}
