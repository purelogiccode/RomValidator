using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using RomValidator.Interfaces;
using RomValidator.Models;

namespace RomValidator.Services;

/// <summary>
/// Service for checking GitHub releases to determine if a newer version of the application is available.
/// Provides version comparison and update notification functionality.
/// </summary>
public class GitHubVersionChecker : IGitHubVersionChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    /// <summary>
    /// Initializes a new instance of the GitHubVersionChecker class.
    /// </summary>
    /// <param name="repoOwner">The GitHub repository owner (username or organization).</param>
    /// <param name="repoName">The name of the GitHub repository.</param>
    public GitHubVersionChecker(string repoOwner, string repoName)
    {
        _apiBaseUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";

        _httpClient = new HttpClient();
        // GitHub API requires a User-Agent header
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RomValidator",
            GetCurrentApplicationVersion()?.ToString() ?? "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    /// <summary>
    /// Initializes a new instance of the GitHubVersionChecker class using a custom HTTP handler.
    /// Intended for unit tests so network calls can be simulated.
    /// </summary>
    /// <param name="repoOwner">The GitHub repository owner (username or organization).</param>
    /// <param name="repoName">The name of the GitHub repository.</param>
    /// <param name="httpMessageHandler">The HTTP message handler used to send requests.</param>
    internal GitHubVersionChecker(string repoOwner, string repoName, HttpMessageHandler httpMessageHandler)
        : this(repoOwner, repoName)
    {
        _httpClient = new HttpClient(httpMessageHandler);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RomValidator",
            GetCurrentApplicationVersion()?.ToString() ?? "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    /// <summary>
    /// Checks GitHub for a newer version of the application.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple containing:
    /// - IsNewVersionAvailable: True if a newer version is available;
    /// - ReleaseUrl: The URL to the latest release page;
    /// - LatestVersionTag: The version tag of the latest release.
    /// </returns>
    public async Task<(bool IsNewVersionAvailable, string? ReleaseUrl, string? LatestVersionTag)>
        CheckForNewVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_apiBaseUrl, cancellationToken);
            response.EnsureSuccessStatusCode(); // Throws an exception for HTTP error codes (4xx, 5xx)

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();

            if (release?.TagName == null || release.HtmlUrl == null)
            {
                LoggerService.LogWarning("GitHubVersionChecker", "Could not parse release info from API response.");
                return (false, null, null);
            }

            var currentVersion = GetCurrentApplicationVersion();
            if (currentVersion == null)
            {
                LoggerService.LogWarning("GitHubVersionChecker", "Could not determine current application version.");
                return (false, null, null);
            }

            var latestVersion = ParseVersionTag(release.TagName);
            if (latestVersion == null)
            {
                LoggerService.LogWarning("GitHubVersionChecker",
                    $"Could not parse latest version tag '{release.TagName}' from GitHub.");
                return (false, null, null);
            }

            if (latestVersion > currentVersion)
            {
                return (true, release.HtmlUrl, release.TagName);
            }

            return (false, null, null); // No new version available
        }
        catch (HttpRequestException httpEx)
        {
            // Network/SSL/TLS failures are environmental (no connectivity, proxy, or an
            // outdated OS that cannot negotiate a modern TLS handshake). These are not
            // application bugs, so log at Information level and do not send a bug report.
            LoggerService.LogInfo("GitHubVersionChecker",
                $"HTTP request error checking for updates: {httpEx.Message}");
            return (false, null, null);
        }
        catch (TaskCanceledException tcEx)
        {
            // Request timed out or was cancelled - also an environmental/connectivity issue.
            LoggerService.LogInfo("GitHubVersionChecker",
                $"Update check timed out or was cancelled: {tcEx.Message}");
            return (false, null, null);
        }
        catch (Exception ex)
        {
            // LogException forwards the report to the bug report API through the Serilog sink.
            LoggerService.LogException("GitHubVersionChecker", ex, "General error checking for updates");
            return (false, null, null);
        }
    }

    /// <summary>
    /// Parses a GitHub release tag (e.g. "v1.2.3" or "release_1.2.3") into a <see cref="Version"/>.
    /// </summary>
    /// <param name="tagName">The raw tag name from the GitHub release.</param>
    /// <returns>The parsed version, or null when the tag is not a valid version.</returns>
    internal static Version? ParseVersionTag(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        // Clean the GitHub tag name to be parseable by System.Version
        // e.g., "release_1.0.0" -> "1.0.0", "v1.0.0" -> "1.0.0", "V1.0.0" -> "1.0.0"
        var cleanedTag = tagName.Replace("release_", "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('v', 'V');

        return Version.TryParse(cleanedTag, out var version) ? version : null;
    }

    /// <summary>
    /// Gets the currently executing application version from the assembly metadata.
    /// </summary>
    /// <returns>The current application version, or null when it cannot be determined.</returns>
    private static Version? GetCurrentApplicationVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
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
