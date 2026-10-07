using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using RomValidator.Interfaces;
using RomValidator.Pages;
using RomValidator.Services;

namespace RomValidator;

/// <summary>
/// Main application window. Hosts the navigation header, the page frame
/// (Validate ROMs / Generate DAT), and the status bar.
/// </summary>
public partial class MainWindow : IDisposable
{
    // Cached brushes for UI consistency (PERF fix)
    private static readonly SolidColorBrush SActiveBrush = new(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly SolidColorBrush SActiveBackgroundBrush = new(Color.FromRgb(0x00, 0x78, 0xD4));
    private static readonly Brush SInactiveBrush = Brushes.Transparent;

    // Services
    /// <summary>Gets the bug report service for error tracking.</summary>
    public IBugReportService BugReportService { get; }

    // Pages
    private readonly ValidatePage _validatePage;
    private readonly GenerateDatPage _generateDatPage;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// Sets up services, exception handling, and application pages.
    /// </summary>
    public MainWindow()
    {
        try
        {
            InitializeComponent();

            // Reuse the BugReportService from App to avoid duplicate HttpClient instances
            BugReportService = ((App)Application.Current).GetBugReportService()
                               ?? throw new InvalidOperationException(
                                   "BugReportService must be initialized before MainWindow.");

            // Initialize Pages
            _validatePage = new ValidatePage(this);
            _generateDatPage = new GenerateDatPage(this);

            // Load initial page
            MainContentFrame.Navigate(_validatePage);
            UpdateActivePageIndicator(_validatePage);
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error initializing MainWindow");
            throw;
        }
    }

    /// <summary>
    /// Highlights the header button that corresponds to the currently displayed page.
    /// </summary>
    /// <param name="activePage">The page currently shown in the content frame.</param>
    private void UpdateActivePageIndicator(object activePage)
    {
        if (Equals(activePage, _validatePage))
        {
            ValidateRomsButton.BorderThickness = new Thickness(0, 0, 0, 3);
            ValidateRomsButton.BorderBrush = SActiveBrush;
            ValidateRomsButton.Background = SActiveBackgroundBrush;
            GenerateDatButton.BorderThickness = new Thickness(0);
            GenerateDatButton.BorderBrush = SInactiveBrush;
            GenerateDatButton.Background = SInactiveBrush;
        }
        else if (Equals(activePage, _generateDatPage))
        {
            GenerateDatButton.BorderThickness = new Thickness(0, 0, 0, 3);
            GenerateDatButton.BorderBrush = SActiveBrush;
            GenerateDatButton.Background = SActiveBackgroundBrush;
            ValidateRomsButton.BorderThickness = new Thickness(0);
            ValidateRomsButton.BorderBrush = SInactiveBrush;
            ValidateRomsButton.Background = SInactiveBrush;
        }
    }

    /// <summary>
    /// Updates the status bar message asynchronously from any thread.
    /// </summary>
    /// <param name="message">The message to display in the status bar.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    public async Task UpdateStatusBarMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                StatusBarMessageTextBlock.Text = message;
            });
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled, ignore
        }
        catch (Exception ex)
        {
            // Log the exception but don't crash the application
            LoggerService.LogException("MainWindow", ex, "Error updating status bar");
        }
    }

    /// <summary>
    /// Updates the status bar message synchronously from any thread.
    /// </summary>
    /// <param name="message">The message to display in the status bar.</param>
    public void UpdateStatusBarMessage(string message)
    {
        // Keep the synchronous version for backward compatibility
        // but implement it using the async version with a default cancellation token
        _ = UpdateStatusBarMessageAsync(message);
    }

    /// <summary>
    /// Handles the "Validate ROMs" header button: navigates to the validation page.
    /// </summary>
    private void ValidateRoms_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Equals(MainContentFrame.Content, _validatePage))
            {
                MainContentFrame.Navigate(_validatePage);
                UpdateActivePageIndicator(_validatePage);
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error navigating to Validate page");
        }
    }

    /// <summary>
    /// Handles the "Generate DAT" header button: navigates to the DAT generation page.
    /// </summary>
    private void GenerateDat_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Equals(MainContentFrame.Content, _generateDatPage))
            {
                MainContentFrame.Navigate(_generateDatPage);
                UpdateActivePageIndicator(_generateDatPage);
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error navigating to Generate DAT page");
        }
    }

    /// <summary>
    /// Handles the "About" header button: opens the About window.
    /// </summary>
    private void About_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow(BugReportService) { Owner = this };
            aboutWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error opening About window");
        }
    }

    /// <summary>
    /// Handles the "App Data" header button: opens the per-user data folder in Explorer.
    /// </summary>
    private void OpenAppData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Make sure the data/logs/screenshot folders exist before opening them.
            AppPaths.EnsureCreated();
            Process.Start(new ProcessStartInfo(AppPaths.AppDataDirectory) { UseShellExecute = true });
            UpdateStatusBarMessage($"Opened data folder: {AppPaths.AppDataDirectory}");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error opening application data folder");
            MessageBox.Show(
                $"Could not open the data folder.\n\nYou can find it here:\n{AppPaths.AppDataDirectory}",
                "App Data Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Handles the "Exit" header button: closes the application.
    /// </summary>
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error closing application");
        }
    }

    /// <summary>
    /// Handles window closing: disposes resources owned by the main window.
    /// </summary>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            // Don't cancel the closing event, just ensure proper cleanup
            Dispose();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error during window closing");
        }
    }

    /// <summary>
    /// Disposes of resources used by the MainWindow.
    /// Cancels all ongoing operations and disposes of pages and services.
    /// </summary>
    public void Dispose()
    {
        // Cancel all ongoing operations first
        try
        {
            App.CancelAllOperations();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error during shutdown");
        }

        // Dispose pages while BugReportService is still alive for error reporting
        try
        {
            _validatePage.Dispose();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error disposing ValidatePage");
        }

        try
        {
            _generateDatPage.Dispose();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error disposing GenerateDatPage");
        }

        // BugReportService is owned by App and disposed in App.OnExit (after the
        // Serilog logger is flushed), so it stays alive for all error reporting above.

        GC.SuppressFinalize(this);
    }
}
