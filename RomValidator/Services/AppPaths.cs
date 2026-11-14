
namespace RomValidator.Services;

/// <summary>
/// Central location for the application's folders:
/// the "Screenshot" folder next to the application executable (primary) and the
/// per-user data folder (for example <c>%LOCALAPPDATA%\ROM Validator</c> on Windows or
/// <c>~/Library/Application Support/ROM Validator</c> on macOS) used for logs and as a
/// fallback for screenshots when the application folder is not writable.
/// </summary>
public static class AppPaths
{
    /// <summary>Gets the name of the application folder used under AppData.</summary>
    public const string ApplicationName = "ROM Validator";

    /// <summary>Gets the platform-specific per-user application data folder.</summary>
    public static string AppDataDirectory { get; } = GetAppDataDirectory();

    private static string GetAppDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                ApplicationName);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationName);
    }

    /// <summary>Gets the folder where the Serilog rolling log files are written.</summary>
    public static string LogsDirectory => Path.Combine(AppDataDirectory, "Logs");

    /// <summary>
    /// Gets the primary folder where F8 screenshots are saved:
    /// the "Screenshot" folder inside the application folder.
    /// </summary>
    public static string ScreenshotsDirectory => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Screenshot");

    /// <summary>
    /// Gets the fallback folder where screenshots are saved when the application
    /// folder is not writable (e.g. installed under Program Files).
    /// </summary>
    public static string FallbackScreenshotsDirectory => Path.Combine(AppDataDirectory, "Screenshot");

    /// <summary>
    /// Creates the data, logs, and screenshot folders if they do not exist yet.
    /// Creation of the primary screenshot folder is best-effort because the
    /// application folder may be read-only.
    /// </summary>
    public static void EnsureCreated()
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(FallbackScreenshotsDirectory);

            try
            {
                Directory.CreateDirectory(ScreenshotsDirectory);
            }
            catch (Exception ex)
            {
                LoggerService.LogDebug("AppPaths",
                    $"Primary screenshot folder is not writable ({ScreenshotsDirectory}): {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AppPaths", ex, "Error creating application data folders");
            throw;
        }
    }
}
