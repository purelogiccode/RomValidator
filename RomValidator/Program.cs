using Avalonia;

namespace RomValidator;

/// <summary>
/// Application entry point. Do not use Avalonia or synchronization-context-dependent
/// APIs before <see cref="BuildAvaloniaApp"/> starts the application.
/// </summary>
internal static class Program
{
    /// <summary>Starts the desktop application.</summary>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Builds the Avalonia application used by the desktop host and designer.</summary>
    /// <returns>The configured <see cref="AppBuilder"/>.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
    }
}
