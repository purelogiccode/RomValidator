using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;

namespace RomValidator.Services;

/// <summary>
/// Service that captures screenshots of the active application window.
/// Screenshots are saved to the "Screenshot" folder inside the application folder;
/// when that folder is not writable, they fall back to the per-user data folder.
/// </summary>
public static class ScreenshotService
{
    /// <summary>
    /// Captures a screenshot of the currently active application window (or the main
    /// window when no window reports itself as active) and saves it as a PNG file.
    /// </summary>
    /// <returns>The full path of the saved screenshot, or null when capture failed.</returns>
    public static string? CaptureActiveWindowScreenshot()
    {
        try
        {
            var window = GetActiveWindow();
            if (window == null)
            {
                LoggerService.LogInfo("Screenshot", "No active window found to capture.");
                return null;
            }

            return CaptureWindowScreenshot(window);
        }
        catch (Exception ex)
        {
            LoggerService.LogException("Screenshot", ex, "Error capturing active window screenshot");
            return null;
        }
    }

    /// <summary>Captures a screenshot of the specified window and saves it as a PNG file.</summary>
    /// <param name="window">The window to capture.</param>
    /// <returns>The full path of the saved screenshot.</returns>
    public static string CaptureWindowScreenshot(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";

        var primaryDirectory = AppPaths.ScreenshotsDirectory;
        try
        {
            Directory.CreateDirectory(primaryDirectory);
            var primaryPath = Path.Combine(primaryDirectory, filename);
            SaveWindowToPng(window, primaryPath);
            LoggerService.LogInfo("Screenshot", $"Screenshot saved: {primaryPath}");
            return primaryPath;
        }
        catch (Exception primaryEx)
        {
            LoggerService.LogInfo("Screenshot",
                $"Could not save screenshot to '{primaryDirectory}': {primaryEx.Message}. Falling back to AppData.");

            var fallbackDirectory = AppPaths.FallbackScreenshotsDirectory;
            Directory.CreateDirectory(fallbackDirectory);
            var fallbackPath = Path.Combine(fallbackDirectory, filename);
            SaveWindowToPng(window, fallbackPath);
            LoggerService.LogInfo("Screenshot", $"Screenshot saved to fallback location: {fallbackPath}");
            return fallbackPath;
        }
    }

    /// <summary>Returns the currently active application window, falling back to the main window.</summary>
    /// <returns>The active window, or null when no window is available.</returns>
    private static Window? GetActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        foreach (var window in desktop.Windows)
        {
            if (window.IsActive)
            {
                return window;
            }
        }

        return desktop.MainWindow;
    }

    /// <summary>Renders the specified window to a PNG file at the window's actual scaling.</summary>
    /// <param name="window">The window to render.</param>
    /// <param name="filePath">The destination PNG file path.</param>
    private static void SaveWindowToPng(Window window, string filePath)
    {
        var scaling = window.RenderScaling;
        var width = Math.Max((int)(window.Bounds.Width * scaling), 1);
        var height = Math.Max((int)(window.Bounds.Height * scaling), 1);

        using var renderTarget = new RenderTargetBitmap(
            new PixelSize(width, height),
            new Vector(96 * scaling, 96 * scaling));
        renderTarget.Render(window);
        renderTarget.Save(filePath, PngBitmapEncoderOptions.Default);
    }
}
