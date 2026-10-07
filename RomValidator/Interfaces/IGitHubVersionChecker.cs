namespace RomValidator.Interfaces;

/// <summary>
/// Defines a service that checks the GitHub repository for a newer application release.
/// </summary>
public interface IGitHubVersionChecker : IDisposable
{
    /// <summary>
    /// Checks GitHub for a newer version of the application.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple containing:
    /// - IsNewVersionAvailable: True if a newer version is available;
    /// - ReleaseUrl: The URL to the latest release page;
    /// - LatestVersionTag: The version tag of the latest release.
    /// </returns>
    Task<(bool IsNewVersionAvailable, string? ReleaseUrl, string? LatestVersionTag)> CheckForNewVersionAsync(
        CancellationToken cancellationToken = default);
}
