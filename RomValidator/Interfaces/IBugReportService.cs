namespace RomValidator.Interfaces;

/// <summary>
/// Defines a service that submits bug reports (errors, warnings, and exceptions)
/// to the remote bug report API.
/// </summary>
public interface IBugReportService : IDisposable
{
    /// <summary>
    /// Sends a bug report to the API with comprehensive environment and error details.
    /// </summary>
    /// <param name="context">Context or location where the error occurred.</param>
    /// <param name="exception">The exception that occurred (optional).</param>
    /// <param name="additionalInfo">Additional information about the error (optional).</param>
    /// <returns>True if the report was sent successfully, false otherwise.</returns>
    Task<bool> SendBugReportAsync(string context, Exception? exception = null, string? additionalInfo = null);

    /// <summary>
    /// Sends a bug report to the API with comprehensive environment and error details.
    /// </summary>
    /// <param name="context">Context or location where the error occurred.</param>
    /// <param name="exception">The exception that occurred (optional).</param>
    /// <param name="additionalInfo">Additional information about the error (optional).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the report was sent successfully, false otherwise.</returns>
    Task<bool> SendBugReportAsync(string context, Exception? exception, string? additionalInfo,
        CancellationToken cancellationToken);
}
