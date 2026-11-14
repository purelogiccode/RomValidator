using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace RomValidator.Services;

/// <summary>
/// Registers the application-wide F8 screenshot shortcut on a window.
/// </summary>
public static class ScreenshotShortcut
{
    /// <summary>Attaches the F8 shortcut to the specified window.</summary>
    /// <param name="window">The window that should handle F8.</param>
    /// <param name="onCaptured">Optional callback invoked with the saved screenshot path.</param>
    public static void Attach(Window window, Action<string?>? onCaptured = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.F8)
            {
                return;
            }

            e.Handled = true;

            try
            {
                onCaptured?.Invoke(ScreenshotService.CaptureActiveWindowScreenshot());
            }
            catch (Exception ex)
            {
                LoggerService.LogException("Screenshot", ex, "Error capturing screenshot");
            }
        }, RoutingStrategies.Tunnel);
    }
}
