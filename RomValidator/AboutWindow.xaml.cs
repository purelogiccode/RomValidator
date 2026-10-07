using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using RomValidator.Interfaces;
using RomValidator.Services;

namespace RomValidator;

/// <summary>
/// About window that displays application information, version, and useful links.
/// </summary>
public partial class AboutWindow
{
    /// <summary>Gets the bug report service for error tracking.</summary>
    public IBugReportService BugReportService { get; }

    /// <summary>
    /// Initializes a new instance of the AboutWindow class.
    /// </summary>
    /// <param name="bugReportService">The bug report service for error tracking.</param>
    public AboutWindow(IBugReportService bugReportService)
    {
        try
        {
            BugReportService = bugReportService;
            InitializeComponent();
            AppVersionTextBlock.Text = $"Version: {GetApplicationVersion()}";
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AboutWindow", ex, "Error initializing About window");
            throw;
        }
    }

    /// <summary>
    /// Handles the Close button: closes the About window.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
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

    /// <summary>
    /// Handles hyperlink navigation: opens the target URL in the default browser.
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LoggerService.LogException("AboutWindow", ex, $"Error opening browser for URL: {e.Uri.AbsoluteUri}");
            MessageBox.Show($"Unable to open browser: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Gets the currently executing application version from the assembly metadata.
    /// </summary>
    /// <returns>The application version, or "Unknown" when it cannot be determined.</returns>
    private static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "Unknown";
    }
}
