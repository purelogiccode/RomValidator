namespace RomValidator.Services;

/// <summary>
/// Provides shared helpers for detecting common file-system error conditions.
/// </summary>
public static class FileSystemHelper
{
    private const int ErrorDiskFull = unchecked((int)0x80070070);

    /// <summary>
    /// Determines whether an exception indicates that the disk is full.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>True when the exception represents a disk-full condition.</returns>
    public static bool IsDiskFullError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.HResult == ErrorDiskFull)
        {
            return true;
        }

        var message = exception.Message;
        return message.Contains("not enough space", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("disk full", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("ERROR_DISK_FULL", StringComparison.OrdinalIgnoreCase);
    }
}
