using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RomValidator.Services;
using RomValidator.Views;

namespace RomValidator;

/// <summary>
/// About window that displays application information, version, and useful links.
/// </summary>
public partial class AboutWindow : Window
{
    private const string WebsiteUrl = "https://www.purelogiccode.com";
    private const string GitHubUrl = "https://github.com/purelogiccode/RomValidator";

    /// <summary>Initializes a new instance of the AboutWindow class.</summary>
    public AboutWindow()
    {
        try
        {
            InitializeComponent();
            WindowThemeService.ApplyDarkTitleBar(this);
            AppVersionTextBlock.Text = $"Version: {GetApplicationVersion()}";
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AboutWindow", ex, "Error initializing About window");
            throw;
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AboutWindow", ex, "Error closing About window");
        }
    }

    private void Website_Click(object? sender, RoutedEventArgs e) => OpenUrl(WebsiteUrl);

    private void GitHub_Click(object? sender, RoutedEventArgs e) => OpenUrl(GitHubUrl);

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AboutWindow", ex, $"Error opening browser for URL: {url}");
            _ = DialogService.ShowAsync(this, $"Unable to open browser: {ex.Message}", "Error",
                DialogButtons.Ok, DialogIcon.Error);
        }
    }

    private static string GetApplicationVersion()
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AboutWindow", ex, "Error reading application version");
            return "Unknown";
        }
    }
}
