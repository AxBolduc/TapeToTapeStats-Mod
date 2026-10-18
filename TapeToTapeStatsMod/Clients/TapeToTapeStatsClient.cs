using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using TapeToTapeStatsMod.Domain.DTOs;

namespace TapeToTapeStatsMod.Clients;

public sealed class TapeToTapeStatsClient
{
    private const string IngestRunPath = "api/v1/ingest/runs";

    /// <summary>Unset v2 fields are omitted rather than sent as null.</summary>
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

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
        // Lets the API's logs tell which mod build sent a request.
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TapeToTapeStatsMod", Core.ModVersion));
    }

    /// <summary>Posts an already-serialized <see cref="RunInfoDto"/>.</summary>
    public async Task<RunIngestResponseDto> ReportRun(string runInfoJson)
    {
        using var content = new StringContent(runInfoJson, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(IngestRunPath, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new RunRejectedException(DescribeFailure(response, responseBody));
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(DescribeFailure(response, responseBody));
        }

        return JsonConvert.DeserializeObject<RunIngestResponseDto>(responseBody)
            ?? throw new JsonException("Empty response body.");
    }

    /// <summary>Includes Cloudflare's ray ID, which finds the request in the API's logs.</summary>
    private static string DescribeFailure(HttpResponseMessage response, string responseBody)
    {
        var rayId = response.Headers.TryGetValues("CF-Ray", out var values) ? string.Join(",", values) : "none";

        return $"{(int)response.StatusCode} {response.ReasonPhrase} (ray {rayId}): {responseBody}";
    }
}

/// <summary>The API refused the payload itself (400), so sending it again won't help.</summary>
public sealed class RunRejectedException : Exception
{
    public RunRejectedException(string message) : base(message)
    {
    }
}
