using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RomValidator.Services;
using Serilog;

namespace RomValidator;

/// <summary>
/// Application class with global exception handling to ensure all bugs are reported.
/// </summary>
public partial class App
{
    private BugReportService? _bugReportService;
    private ApplicationStatsService? _applicationStatsService;
    private GitHubVersionChecker? _versionChecker;
    private BugReportSink? _bugReportSink;
    private static CancellationTokenSource? _globalCancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the App class.
    /// Sets up global cancellation tokens, services, and exception handling.
    /// </summary>
    public App()
    {
        // Bootstrap logging first so failures during service initialization are
        // captured even before the full Serilog configuration is in place.
        InitializeBootstrapLogging();

        try
        {
            // Check if running from a temp directory (e.g., extracted from a compressed archive)
            CheckIfRunningFromTempDirectory();

            // Initialize global cancellation token source
            _globalCancellationTokenSource = new CancellationTokenSource();

            // Initialize bug report service first so we can report any initialization issues
            InitializeBugReportService();

            // Initialize the GitHub version checker (owned by App so the startup update
            // check is not tied to the lifetime of any particular page)
            InitializeVersionChecker();

            // Initialize Serilog logging (depends on BugReportService for the custom sink)
            InitializeLogging();

            // Initialize application stats service
            InitializeApplicationStatsService();

            // Verify the bundled 7za fallback executable is available
            CheckSevenZipFallbackAvailability();

            // Subscribe to global exception handlers
            SetupGlobalExceptionHandling();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("App", ex, "Error during application initialization");
            throw;
        }
    }

    /// <summary>
    /// Checks if the application is running from a temporary directory or directly from a compressed archive.
    /// If so, shows a message and exits to prevent issues with missing files.
    /// </summary>
    private static void CheckIfRunningFromTempDirectory()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var tempPath = Path.GetTempPath();

        var isRunningFromTemp = baseDirectory.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
        var isRunningFromZip = Path.GetFileName(baseDirectory.TrimEnd(Path.DirectorySeparatorChar))
            .Contains(".zip", StringComparison.OrdinalIgnoreCase);

        if (isRunningFromTemp || isRunningFromZip)
        {
            MessageBox.Show(
                "ROM Validator cannot run from a temporary directory or directly from a compressed archive.\n\n" +
                @"Please extract the application to a permanent folder (e.g., C:\Program Files\ROM Validator) " +
                "and run it from there.",
                "ROM Validator - Invalid Location",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Logs whether the bundled 7za fallback executable is available. The fallback is
    /// used for archive operations SharpCompress cannot perform (e.g. creating 7z
    /// archives or reading archives with unsupported compression methods).
    /// </summary>
    private static void CheckSevenZipFallbackAvailability()
    {
        try
        {
            if (!SevenZipProcess.IsAvailable)
            {
                LoggerService.LogInfo("SevenZip",
                    "The 7za fallback executable was not found; 7z archive creation will be unavailable.");
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("SevenZip", $"Could not verify 7za availability: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes a minimal debug logger before the services are created, so any
    /// initialization failure is captured even if the full logger setup fails.
    /// The bootstrap logger is replaced (and disposed) by <see cref="InitializeLogging"/>.
    /// </summary>
    private static void InitializeBootstrapLogging()
    {
        try
        {
            var bootstrapLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} [{Component}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();

            Log.Logger = bootstrapLogger;
            LoggerService.Initialize(bootstrapLogger);
        }
        catch
        {
            // LoggerService keeps its silent no-op logger; the full setup below may still succeed.
        }
    }

    /// <summary>
    /// Initializes the GitHub version checker used for the startup update check.
    /// </summary>
    private void InitializeVersionChecker()
    {
        try
        {
            _versionChecker = new GitHubVersionChecker("purelogiccode", "RomValidator");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("App", ex, "Failed to initialize GitHubVersionChecker");
        }
    }

    /// <summary>
    /// Initializes the bug report service with the API configuration.
    /// </summary>
    private void InitializeBugReportService()
    {
        try
        {
            const string apiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
            const string apiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
            const string applicationName = "ROM Validator";
            _bugReportService = new BugReportService(apiUrl, apiKey, applicationName);
        }
        catch (Exception ex)
        {
            // If we can't create the bug report service, log the failure
            LoggerService.LogException("App", ex, "Failed to initialize BugReportService");
        }
    }

    /// <summary>
    /// Initializes Serilog logging with Debug, File, and BugReport sinks.
    /// Warning and higher events are forwarded to the bug report API via the custom sink.
    /// </summary>
    private void InitializeLogging()
    {
        try
        {
            // Logs go to the per-user AppData folder so they stay writable for
            // installed apps and are easy to find next to the screenshots.
            var logFilePath = Path.Combine(AppPaths.LogsDirectory, "RomValidator.log");

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Debug(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} [{Component}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    logFilePath,
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} [{Component}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture,
                    shared: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7);

            // Forward Warning+ events to the bug report API through the custom sink
            if (_bugReportService != null)
            {
                _bugReportSink = new BugReportSink(_bugReportService);
                loggerConfig = loggerConfig.WriteTo.Sink(_bugReportSink, Serilog.Events.LogEventLevel.Warning);
            }

            var logger = loggerConfig.CreateLogger();
            var previousLogger = Log.Logger;
            Log.Logger = logger;
            LoggerService.Initialize(logger);
            (previousLogger as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            // Fall back to a debug-only logger so logging (and the global exception
            // handlers) keep working even if the file/sink setup failed.
            try
            {
                var fallbackLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Debug()
                    .CreateLogger();
                var previousLogger = Log.Logger;
                Log.Logger = fallbackLogger;
                LoggerService.Initialize(fallbackLogger);
                (previousLogger as IDisposable)?.Dispose();
                LoggerService.LogException("App", ex, "Failed to initialize Serilog logging - using debug fallback");
            }
            catch (Exception fallbackEx)
            {
                // Last resort: LoggerService stays on its silent no-op logger.
                Debug.WriteLine(
                    $"Failed to initialize fallback logging: {fallbackEx.Message} (original: {ex.Message})");
            }
        }
    }

    /// <summary>
    /// Initializes the application stats service to track application usage.
    /// </summary>
    private void InitializeApplicationStatsService()
    {
        try
        {
            const string statsBaseUrl = "https://www.purelogiccode.com/ApplicationStats";
            const string apiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
            const string statsApplicationId = "RomValidator";
            _applicationStatsService = new ApplicationStatsService(statsBaseUrl, apiKey, statsApplicationId);
        }
        catch (Exception ex)
        {
            // If we can't create the stats service, log the failure
            LoggerService.LogException("App", ex, "Failed to initialize ApplicationStatsService");
        }
    }

    /// <summary>
    /// Sets up global exception handlers for both UI and non-UI thread exceptions,
    /// and registers the application-wide F8 screenshot shortcut.
    /// </summary>
    private void SetupGlobalExceptionHandling()
    {
        // Handle exceptions from the UI thread (Dispatcher)
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Handle exceptions from non-UI threads (AppDomain)
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Handle F8 (screenshot of the active window) for every window in the application
        EventManager.RegisterClassHandler(typeof(Window), Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(OnGlobalPreviewKeyDown));
    }

    /// <summary>
    /// Handles the F8 shortcut on any window: captures a screenshot of the active window.
    /// </summary>
    private static void OnGlobalPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F8)
        {
            return;
        }

        e.Handled = true;

        try
        {
            var filePath = ScreenshotService.CaptureActiveWindowScreenshot();
            if (filePath != null && Current?.MainWindow is MainWindow mainWindow)
            {
                _ = mainWindow.UpdateStatusBarMessageAsync($"Screenshot saved: {filePath}");
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("Screenshot", ex, "Error capturing screenshot");
        }
    }

    /// <summary>
    /// Records application usage statistics on startup.
    /// </summary>
    private void RecordApplicationUsage()
    {
        try
        {
            if (_applicationStatsService != null)
            {
                // Record usage asynchronously without blocking startup
                _ = RecordUsageSafeAsync();
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("App", ex, "Failed to record application usage");
        }
    }

    /// <summary>
    /// Records application usage without allowing failures to escape into the startup path.
    /// </summary>
    private async Task RecordUsageSafeAsync()
    {
        try
        {
            if (_applicationStatsService != null)
            {
                await _applicationStatsService.RecordUsageAsync();
            }
        }
        catch (Exception ex)
        {
            // Stats failures are environmental; keep them out of bug reports.
            LoggerService.LogInfo("Startup", $"Stats recording failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedules the startup update check. StartupUri windows are created after
    /// OnStartup returns, so the check runs once the dispatcher is idle and the
    /// main window is available.
    /// </summary>
    private void ScheduleUpdateCheck()
    {
        _ = Dispatcher.InvokeAsync(
            async () => await CheckForUpdatesSafeAsync(),
            DispatcherPriority.ApplicationIdle);
    }

    /// <summary>
    /// Checks GitHub for a newer release and prompts the user to open the release page.
    /// The check is cancelled automatically when the application exits.
    /// </summary>
    private async Task CheckForUpdatesSafeAsync()
    {
        try
        {
            var cancellationToken = _globalCancellationTokenSource?.Token ?? CancellationToken.None;

            if (_versionChecker == null)
            {
                return;
            }

            var (isNewVersionAvailable, releaseUrl, latestVersionTag) =
                await _versionChecker.CheckForNewVersionAsync(cancellationToken);

            if (!isNewVersionAvailable || releaseUrl == null || latestVersionTag == null)
            {
                return;
            }

            // The main window may have been closed while the check was in flight.
            if (Current?.MainWindow is not MainWindow { IsLoaded: true } mainWindow)
            {
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            mainWindow.UpdateStatusBarMessage($"New version {latestVersionTag} available!");

            var result = MessageBox.Show(
                mainWindow,
                $"A new version ({latestVersionTag}) of ROM Validator is available!\n\n" +
                $"Your current version: {currentVersion}\n\n" +
                "Would you like to go to the release page to download it?",
                "New Version Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
            }
        }
        catch (OperationCanceledException)
        {
            // Application is shutting down - nothing to do
        }
        catch (Exception ex)
        {
            LoggerService.LogException("UpdateCheck", ex, "Error during startup update check");
        }
    }

    /// <summary>
    /// Handles unhandled exceptions from the UI thread (Dispatcher).
    /// </summary>
    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true; // Prevent application from crashing

        try
        {
            var ex = e.Exception;

            // Log the exception. LoggerService routes Warning+ events through Serilog's
            // BugReportSink, which forwards the full details to the bug report API.
            LoggerService.LogException("GlobalDispatcherException", ex, "Unhandled exception in UI/Dispatcher thread");

            // Show user-friendly error message
            ShowFatalErrorDialog(ex, "An error occurred in the application. The error has been reported.");
        }
        catch (Exception handlerEx)
        {
            // If the exception handler itself fails, try to log it
            LoggerService.LogException("GlobalDispatcherException", handlerEx, "Exception in global handler");
        }
    }

    /// <summary>
    /// Handles unhandled exceptions from non-UI threads (AppDomain).
    /// </summary>
    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
            {
                var isTerminating = e.IsTerminating ? "Application will terminate" : "Application continuing";

                // Log the exception (forwarded to the bug report API via the Serilog sink)
                LoggerService.LogException("GlobalAppDomainException", ex,
                    $"Unhandled exception in non-UI thread. {isTerminating}");

                // If the application is terminating, show a fatal error dialog
                if (e.IsTerminating)
                {
                    ShowFatalErrorDialog(ex,
                        "A fatal error occurred in the application. The application will now close.");
                }
            }
        }
        catch (Exception handlerEx)
        {
            // If the exception handler itself fails, try to log it
            LoggerService.LogException("GlobalAppDomainException", handlerEx, "Exception in global handler");
        }
    }

    /// <summary>
    /// Handles unobserved task exceptions (TaskScheduler).
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            var ex = e.Exception;

            // Log the exception (forwarded to the bug report API via the Serilog sink)
            LoggerService.LogException("GlobalTaskException", ex, "Unobserved task exception from TaskScheduler");

            // Mark as observed to prevent application crash
            e.SetObserved();
        }
        catch (Exception handlerEx)
        {
            // If the exception handler itself fails, try to log it
            LoggerService.LogException("GlobalTaskException", handlerEx, "Exception in task exception handler");
        }
    }

    /// <summary>
    /// Shows a user-friendly fatal error dialog.
    /// </summary>
    private static void ShowFatalErrorDialog(Exception ex, string message)
    {
        try
        {
            var dialogMessage =
                $"{message}\n\nError: {ex.Message}\n\nType: {ex.GetType().Name}\n\nThe error details have been sent for analysis.";

            MessageBox.Show(
                dialogMessage,
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception dialogEx)
        {
            // If we can't show the dialog, just log - we've already tried to report the bug
            LoggerService.LogDebug("App", $"Could not show fatal error dialog: {dialogEx.Message}");
        }
    }

    /// <summary>
    /// Gets the global BugReportService instance for use throughout the application.
    /// </summary>
    /// <returns>The shared bug report service, or null when initialization failed.</returns>
    public BugReportService? GetBugReportService()
    {
        return _bugReportService;
    }

    /// <summary>
    /// Override OnStartup to record application usage and check for updates.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RecordApplicationUsage();
        ScheduleUpdateCheck();
    }

    /// <summary>
    /// Override OnExit to clean up the BugReportService and other resources.
    /// </summary>
    /// <param name="e">The exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            // Flush and close Serilog FIRST, while BugReportService is still alive, so
            // pending log entries and in-flight bug-report sends are not cut off.
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error closing Serilog logger: {ex.Message}");
        }

        try
        {
            _bugReportSink?.Dispose();
            _bugReportSink = null;
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error disposing BugReportSink: {ex.Message}");
        }

        try
        {
            _bugReportService?.Dispose();
            _bugReportService = null;
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error disposing BugReportService: {ex.Message}");
        }

        try
        {
            _applicationStatsService?.Dispose();
            _applicationStatsService = null;
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error disposing ApplicationStatsService: {ex.Message}");
        }

        try
        {
            _versionChecker?.Dispose();
            _versionChecker = null;
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error disposing GitHubVersionChecker: {ex.Message}");
        }

        try
        {
            _globalCancellationTokenSource?.Dispose();
            _globalCancellationTokenSource = null;
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error disposing global cancellation token source: {ex.Message}");
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Cancels all ongoing operations and initiates application shutdown.
    /// </summary>
    public static void CancelAllOperations()
    {
        try
        {
            _globalCancellationTokenSource?.Cancel();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("App", ex, "Error cancelling ongoing operations");
        }
    }
}
