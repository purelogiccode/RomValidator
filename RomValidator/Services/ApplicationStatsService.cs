using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using RomValidator.Interfaces;

namespace RomValidator.Services;

/// <summary>
/// Service for recording application usage statistics to a remote API.
/// Tracks application launches and usage for analytics purposes.
/// </summary>
public class ApplicationStatsService : IApplicationStatsService
{
    private readonly HttpClient _httpClient;
    private readonly string _statsUrl;
    private readonly string _apiKey;
    private readonly string _applicationId;
    private int _hasRecordedUsage;

    /// <summary>
    /// Initializes a new instance of the ApplicationStatsService class.
    /// </summary>
    /// <param name="baseUrl">Base URL of the Application Stats API (e.g. https://www.purelogiccode.com/ApplicationStats).</param>
    /// <param name="apiKey">The API key (Bearer token) used to authenticate with the stats API.</param>
    /// <param name="applicationId">The unique application identifier used by the stats API.</param>
    public ApplicationStatsService(string baseUrl, string apiKey, string applicationId)
    {
        _httpClient = new HttpClient();
        _statsUrl = $"{baseUrl.TrimEnd('/')}/stats";
        _apiKey = apiKey;
        _applicationId = applicationId;
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationStatsService class using a custom HTTP handler.
    /// Intended for unit tests so network calls can be simulated.
    /// </summary>
    /// <param name="baseUrl">Base URL of the Application Stats API.</param>
    /// <param name="apiKey">The API key (Bearer token) used to authenticate with the stats API.</param>
    /// <param name="applicationId">The unique application identifier used by the stats API.</param>
    /// <param name="httpMessageHandler">The HTTP message handler used to send requests.</param>
    internal ApplicationStatsService(string baseUrl, string apiKey, string applicationId,
        HttpMessageHandler httpMessageHandler)
        : this(baseUrl, apiKey, applicationId)
    {
        _httpClient = new HttpClient(httpMessageHandler);
    }

    /// <summary>
    /// Records application usage statistics to the remote API.
    /// This method is called once per application launch to track usage.
    /// </summary>
    /// <returns>True if the usage was recorded successfully, false otherwise.</returns>
    public async Task<bool> RecordUsageAsync()
    {
        // Mark as attempted immediately to prevent duplicate calls per launch.
        // Interlocked makes the check-and-set atomic so concurrent callers cannot race.
        if (Interlocked.Exchange(ref _hasRecordedUsage, 1) == 1)
        {
            return true; // Already recorded
        }

        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

            var payload = new
            {
                applicationId = _applicationId,
                version
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _statsUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // Don't log an error for Rate Limit (429) to avoid bug reports (user feedback Apr 11, 2026)
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return false;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            // Stats API failures are server/environmental issues, not application bugs,
            // so log at Information level and do not send a bug report.
            LoggerService.LogInfo("ApplicationStatsService",
                $"Stats API call failed with HTTP status {response.StatusCode}. Content: {errorContent}");

            return false;
        }
        catch (Exception ex)
        {
            LoggerService.LogInfo("ApplicationStatsService",
                $"Exception while recording application stats: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disposes of the HTTP client used by the service.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
