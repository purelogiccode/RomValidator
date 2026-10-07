using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RomValidator.Services;

/// <summary>
/// Service that captures screenshots of the active application window.
/// Screenshots are saved to the "Screenshot" folder inside the application folder;
/// when that folder is not writable (e.g. the app is installed under Program Files),
/// they fall back to %LOCALAPPDATA%\ROM Validator\Screenshot.
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

    /// <summary>
    /// Captures a screenshot of the specified window and saves it as a PNG file.
    /// </summary>
    /// <param name="window">The window to capture.</param>
    /// <returns>The full path of the saved screenshot.</returns>
    public static string CaptureWindowScreenshot(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";

        // Primary location: "Screenshot" folder inside the application folder.
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
            // Fallback location: AppData\ROM Validator\Screenshot. The application folder
            // being read-only is an environmental condition, so log at Information level.
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

    /// <summary>
    /// Returns the currently active application window, falling back to the main window.
    /// </summary>
    /// <returns>The active window, or null when no window is available.</returns>
    private static Window? GetActiveWindow()
    {
        var application = Application.Current;
        if (application == null)
        {
            return null;
        }

        foreach (Window window in application.Windows)
        {
            if (window.IsActive)
            {
                return window;
            }
        }

        return application.MainWindow;
    }

    /// <summary>
    /// Renders the specified window to a PNG file at the window's actual DPI.
    /// </summary>
    /// <param name="window">The window to render.</param>
    /// <param name="filePath">The destination PNG file path.</param>
    private static void SaveWindowToPng(Window window, string filePath)
    {
        // Capture at the window's actual DPI so the PNG matches the physical screen size.
        var dpi = VisualTreeHelper.GetDpi(window);
        var width = (int)Math.Max(window.ActualWidth * dpi.DpiScaleX, 1);
        var height = (int)Math.Max(window.ActualHeight * dpi.DpiScaleY, 1);

        var renderTarget =
            new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        renderTarget.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }
}
