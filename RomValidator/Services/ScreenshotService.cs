using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RomValidator.Services;

public static class ScreenshotService
{
    public static string CaptureWindowScreenshot(Window window)
    {
        // Save to the per-user AppData folder (same root as the logs) so captures
        // also work when the app is installed under Program Files.
        var screenshotsDir = AppPaths.ScreenshotsDirectory;
        Directory.CreateDirectory(screenshotsDir);

        var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var filePath = Path.Combine(screenshotsDir, filename);

        // Capture at the window's actual DPI so the PNG matches the physical screen size.
        var dpi = VisualTreeHelper.GetDpi(window);
        var width = (int)Math.Max(window.ActualWidth * dpi.DpiScaleX, 1);
        var height = (int)Math.Max(window.ActualHeight * dpi.DpiScaleY, 1);

        var renderTarget = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        renderTarget.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using var stream = File.Create(filePath);
        encoder.Save(stream);

        LoggerService.LogInfo("Screenshot", $"Screenshot saved: {filePath}");

        return filePath;
    }
}
