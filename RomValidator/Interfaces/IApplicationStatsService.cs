namespace RomValidator.Interfaces;

/// <summary>
/// Defines a service that records anonymous application usage statistics
/// to the remote Application Stats API.
/// </summary>
public interface IApplicationStatsService : IDisposable
{
    /// <summary>
    /// Records application usage statistics to the remote API.
    /// This is called once per application launch to track usage.
    /// </summary>
    /// <returns>True if the usage was recorded successfully, false otherwise.</returns>
    Task<bool> RecordUsageAsync();
}
