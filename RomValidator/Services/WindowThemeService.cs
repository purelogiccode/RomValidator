using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace RomValidator.Services;

/// <summary>
/// Applies the dark native window chrome on Windows so the system title bar matches
/// the application theme. No-op on Linux and macOS, where the window manager follows
/// the application theme automatically.
/// </summary>
public static class WindowThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    // COLORREF values are 0x00BBGGRR.
    private const int CaptionColor = 0x0016110E; // #0E1116
    private const int TextColor = 0x00F7F3F1; // #F1F3F7

    /// <summary>Applies dark native window chrome when the window is opened.</summary>
    /// <param name="window">The window to theme.</param>
    public static void ApplyDarkTitleBar(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        window.Opened += (_, _) => Apply(window);
    }

    private static void Apply(Window window)
    {
        try
        {
            var handle = window.TryGetPlatformHandle()?.Handle;
            if (handle is null || handle == IntPtr.Zero)
            {
                return;
            }

            var enabled = 1;
            if (DwmSetWindowAttribute(handle.Value, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
            {
                _ = DwmSetWindowAttribute(handle.Value, DwmwaUseImmersiveDarkModeLegacy, ref enabled, sizeof(int));
            }

            var caption = CaptionColor;
            _ = DwmSetWindowAttribute(handle.Value, DwmwaCaptionColor, ref caption, sizeof(int));
            var text = TextColor;
            _ = DwmSetWindowAttribute(handle.Value, DwmwaTextColor, ref text, sizeof(int));
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("WindowTheme", $"Could not apply dark title bar: {ex.Message}");
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
