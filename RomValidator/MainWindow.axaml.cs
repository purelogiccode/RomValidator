using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RomValidator.Interfaces;
using RomValidator.Pages;
using RomValidator.Services;
using RomValidator.Views;

namespace RomValidator;

/// <summary>
/// Main application window. Hosts the navigation header, the active page
/// (Validate ROMs / Generate DAT), and the status bar.
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    private readonly ValidatePage _validatePage;
    private readonly GenerateDatPage _generateDatPage;

    /// <summary>Gets the bug report service for error tracking.</summary>
    public IBugReportService BugReportService { get; }

    /// <summary>Initializes a new instance of the MainWindow class.</summary>
    public MainWindow()
    {
        try
        {
            InitializeComponent();
            WindowThemeService.ApplyDarkTitleBar(this);

            BugReportService = (Application.Current as App)?.GetBugReportService()
                               ?? throw new InvalidOperationException(
                                   "BugReportService must be initialized before MainWindow.");

            _validatePage = new ValidatePage(this);
            _generateDatPage = new GenerateDatPage(this);

            MainContent.Content = _validatePage;
            UpdateActivePageIndicator(_validatePage);
            UpdateStatusBarMessage("Ready.");

            ScreenshotShortcut.Attach(this, filePath =>
            {
                if (filePath != null)
                {
                    _ = UpdateStatusBarMessageAsync($"Screenshot saved: {filePath}");
                }
            });
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error initializing MainWindow");
            throw;
        }
    }

    private void UpdateActivePageIndicator(object activePage)
    {
        SetMenuActive(ValidateRomsButton, ReferenceEquals(activePage, _validatePage));
        SetMenuActive(GenerateDatButton, ReferenceEquals(activePage, _generateDatPage));
    }

    private static void SetMenuActive(Button button, bool active)
    {
        if (active)
        {
            if (!button.Classes.Contains("active"))
            {
                button.Classes.Add("active");
            }
        }
        else
        {
            button.Classes.Remove("active");
        }
    }

    /// <summary>Updates the status bar message asynchronously from any thread.</summary>
    /// <param name="message">The message to display.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    public async Task UpdateStatusBarMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                StatusBarMessageTextBlock.Text = message;
            });
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled.
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error updating status bar");
        }
    }

    /// <summary>Updates the status bar message synchronously from any thread.</summary>
    /// <param name="message">The message to display.</param>
    public void UpdateStatusBarMessage(string message)
    {
        _ = UpdateStatusBarMessageAsync(message);
    }

    private void ValidateRoms_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!ReferenceEquals(MainContent.Content, _validatePage))
            {
                MainContent.Content = _validatePage;
                UpdateActivePageIndicator(_validatePage);
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error navigating to Validate page");
        }
    }

    private void GenerateDat_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!ReferenceEquals(MainContent.Content, _generateDatPage))
            {
                MainContent.Content = _generateDatPage;
                UpdateActivePageIndicator(_generateDatPage);
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error navigating to Generate DAT page");
        }
    }

    private void Donate_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.purelogiccode.com/donate",
                UseShellExecute = true
            });
            UpdateStatusBarMessage("Opened donation page: https://www.purelogiccode.com/donate");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error opening donation page");
            _ = DialogService.ShowAsync(this,
                "Could not open the donation page.\n\nYou can visit it here:\nhttps://www.purelogiccode.com/donate",
                "Donate", DialogButtons.Ok, DialogIcon.Info);
        }
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow();
            await aboutWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error opening About window");
        }
    }

    private void OpenAppData_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            AppPaths.EnsureCreated();
            Process.Start(new ProcessStartInfo(AppPaths.AppDataDirectory) { UseShellExecute = true });
            UpdateStatusBarMessage($"Opened data folder: {AppPaths.AppDataDirectory}");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error opening application data folder");
            _ = DialogService.ShowAsync(this,
                $"Could not open the data folder.\n\nYou can find it here:\n{AppPaths.AppDataDirectory}",
                "App Data Folder", DialogButtons.Ok, DialogIcon.Info);
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
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

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            Dispose();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error during window closing");
        }
    }

    /// <summary>Disposes resources used by the MainWindow.</summary>
    public void Dispose()
    {
        try
        {
            App.CancelAllOperations();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("MainWindow", ex, "Error during shutdown");
        }

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

        GC.SuppressFinalize(this);
    }
}
