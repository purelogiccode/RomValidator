using System.IO;

namespace RomValidator.Services;

/// <summary>
/// Central location for the application's per-user data folder
/// (%LOCALAPPDATA%\ROM Validator). Logs and screenshots are stored here so they
/// stay writable even when the app is installed under Program Files, and users
/// can find them in one place via the "App Data" button in the main window.
/// </summary>
public static class AppPaths
{
    /// <summary>Gets the application data folder (e.g. %LOCALAPPDATA%\ROM Validator).</summary>
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ROM Validator");

    /// <summary>Gets the folder where the Serilog rolling log files are written.</summary>
    public static string LogsDirectory => Path.Combine(AppDataDirectory, "Logs");

    /// <summary>Gets the folder where F8 screenshots are saved.</summary>
    public static string ScreenshotsDirectory => Path.Combine(AppDataDirectory, "Screenshot");

    /// <summary>
    /// Creates the data, logs, and screenshot folders if they do not exist yet.
    /// </summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ScreenshotsDirectory);
    }
}
