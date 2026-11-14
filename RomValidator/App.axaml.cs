using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RomValidator.Services;
using RomValidator.Views;
using Serilog;

namespace RomValidator;

/// <summary>
/// Application class with global exception handling to ensure all bugs are reported.
/// </summary>
public class App : Application
{
    private BugReportService? _bugReportService;
    private ApplicationStatsService? _applicationStatsService;
    private GitHubVersionChecker? _versionChecker;
    private BugReportSink? _bugReportSink;
    private static CancellationTokenSource? _globalCancellationTokenSource;

    /// <summary>Gets the shared bug report service, or null when initialization failed.</summary>
    public BugReportService? GetBugReportService() => _bugReportService;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Bootstrap logging first so failures during service initialization are
        // captured even before the full Serilog configuration is in place.
        InitializeBootstrapLogging();

        try
        {
            AppPaths.EnsureCreated();
            _globalCancellationTokenSource = new CancellationTokenSource();
            InitializeBugReportService();
            InitializeVersionChecker();
            InitializeLogging();
            InitializeApplicationStatsService();
            CheckSevenZipFallbackAvailability();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("App", ex, "Error during application initialization");
        }
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            SetupGlobalExceptionHandling();

            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) => ShutdownServices();

            if (CheckIfRunningFromTempDirectory())
            {
                Dispatcher.UIThread.Post(() => _ = ShowInvalidLocationAsync(desktop, mainWindow));
            }

            RecordApplicationUsage();
            ScheduleUpdateCheck(mainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Checks if the application is running from a temporary directory or directly from a
    /// compressed archive. This is primarily a Windows Explorer behavior guard.
    /// </summary>
    private static bool CheckIfRunningFromTempDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var tempPath = Path.GetTempPath();

        var isRunningFromTemp = baseDirectory.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
        var isRunningFromZip = Path.GetFileName(baseDirectory.TrimEnd(Path.DirectorySeparatorChar))
            .Contains(".zip", StringComparison.OrdinalIgnoreCase);

        return isRunningFromTemp || isRunningFromZip;
    }

    /// <summary>
    /// Logs whether a 7-Zip fallback executable is available. The fallback is used for
    /// archive operations SharpCompress cannot perform.
    /// </summary>
    private static void CheckSevenZipFallbackAvailability()
    {
        try
        {
            if (!SevenZipProcess.IsAvailable)
            {
                LoggerService.LogInfo("SevenZip",
                    "No 7-Zip fallback executable was found; archive creation falls back to SharpCompress only.");
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("SevenZip", $"Could not verify 7-Zip availability: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes a minimal debug logger before the services are created, so any
    /// initialization failure is captured even if the full logger setup fails.
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
            // LoggerService keeps its silent no-op logger.
        }
    }

    /// <summary>Initializes the GitHub version checker used for the startup update check.</summary>
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

    /// <summary>Initializes the bug report service with the API configuration.</summary>
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
            LoggerService.LogException("App", ex, "Failed to initialize BugReportService");
        }
    }

    /// <summary>Initializes Serilog logging with Debug, File, and BugReport sinks.</summary>
    private void InitializeLogging()
    {
        try
        {
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
                Debug.WriteLine(
                    $"Failed to initialize fallback logging: {fallbackEx.Message} (original: {ex.Message})");
            }
        }
    }

    /// <summary>Initializes the application stats service to track application usage.</summary>
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
            LoggerService.LogException("App", ex, "Failed to initialize ApplicationStatsService");
        }
    }

    /// <summary>Sets up global exception handlers for UI and non-UI thread exceptions.</summary>
    private static void SetupGlobalExceptionHandling()
    {
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>Records application usage statistics on startup.</summary>
    private void RecordApplicationUsage()
    {
        try
        {
            if (_applicationStatsService != null)
            {
                _ = RecordUsageSafeAsync();
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("App", ex, "Failed to record application usage");
        }
    }

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
            LoggerService.LogInfo("Startup", $"Stats recording failed: {ex.Message}");
        }
    }

    /// <summary>Schedules the startup update check after the main window is available.</summary>
    private static void ScheduleUpdateCheck(MainWindow mainWindow)
    {
        _ = Dispatcher.UIThread.InvokeAsync(
            async () => await CheckForUpdatesSafeAsync(mainWindow),
            DispatcherPriority.ApplicationIdle);
    }

    private static async Task CheckForUpdatesSafeAsync(MainWindow mainWindow)
    {
        try
        {
            if (Application.Current is not App app || app._versionChecker is null)
            {
                return;
            }

            var cancellationToken = _globalCancellationTokenSource?.Token ?? CancellationToken.None;
            var (isNewVersionAvailable, releaseUrl, latestVersionTag) =
                await app._versionChecker.CheckForNewVersionAsync(cancellationToken);

            if (!isNewVersionAvailable || releaseUrl == null || latestVersionTag == null ||
                !mainWindow.IsLoaded)
            {
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            mainWindow.UpdateStatusBarMessage($"New version {latestVersionTag} available!");

            var result = await DialogService.ShowAsync(
                mainWindow,
                $"A new version ({latestVersionTag}) of ROM Validator is available!\n\n" +
                $"Your current version: {currentVersion}\n\n" +
                "Would you like to go to the release page to download it?",
                "New Version Available",
                DialogButtons.YesNo,
                DialogIcon.Info);

            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
            }
        }
        catch (OperationCanceledException)
        {
            // Application is shutting down.
        }
        catch (Exception ex)
        {
            LoggerService.LogException("UpdateCheck", ex, "Error during startup update check");
        }
    }

    private static void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        try
        {
            LoggerService.LogException("GlobalDispatcherException", e.Exception,
                "Unhandled exception in UI/Dispatcher thread");
            ShowFatalErrorDialog(e.Exception,
                "An error occurred in the application. The error has been reported.");
        }
        catch (Exception handlerEx)
        {
            LoggerService.LogException("GlobalDispatcherException", handlerEx, "Exception in global handler");
        }
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
            {
                var isTerminating = e.IsTerminating ? "Application will terminate" : "Application continuing";
                LoggerService.LogException("GlobalAppDomainException", ex,
                    $"Unhandled exception in non-UI thread. {isTerminating}");

                if (e.IsTerminating)
                {
                    ShowFatalErrorDialog(ex,
                        "A fatal error occurred in the application. The application will now close.");
                }
            }
        }
        catch (Exception handlerEx)
        {
            LoggerService.LogException("GlobalAppDomainException", handlerEx, "Exception in global handler");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            LoggerService.LogException("GlobalTaskException", e.Exception,
                "Unobserved task exception from TaskScheduler");
            e.SetObserved();
        }
        catch (Exception handlerEx)
        {
            LoggerService.LogException("GlobalTaskException", handlerEx, "Exception in task exception handler");
        }
    }

    private static async Task ShowInvalidLocationAsync(IClassicDesktopStyleApplicationLifetime desktop,
        Window mainWindow)
    {
        await DialogService.ShowAsync(mainWindow,
            "ROM Validator cannot run from a temporary directory or directly from a compressed archive.\n\n" +
            "Please extract the application to a permanent folder and run it from there.",
            "ROM Validator - Invalid Location",
            DialogButtons.Ok,
            DialogIcon.Warning);
        desktop.Shutdown(1);
    }

    private static void ShowFatalErrorDialog(Exception ex, string message)
    {
        Dispatcher.UIThread.Post(() => _ = ShowFatalErrorDialogAsync(ex, message));
    }

    private static async Task ShowFatalErrorDialogAsync(Exception ex, string message)
    {
        try
        {
            var owner = GetMainWindow();
            if (owner is null)
            {
                return;
            }

            var dialogMessage =
                $"{message}\n\nError: {ex.Message}\n\nType: {ex.GetType().Name}\n\nThe error details have been sent for analysis.";

            await DialogService.ShowAsync(owner, dialogMessage, "Application Error",
                DialogButtons.Ok, DialogIcon.Error);
        }
        catch (Exception dialogEx)
        {
            LoggerService.LogDebug("App", $"Could not show fatal error dialog: {dialogEx.Message}");
        }
    }

    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    private void ShutdownServices()
    {
        try
        {
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            LoggerService.LogWarning("App", $"Error closing Serilog logger: {ex.Message}");
        }

        _bugReportSink?.Dispose();
        _bugReportSink = null;
        _bugReportService?.Dispose();
        _bugReportService = null;
        _applicationStatsService?.Dispose();
        _applicationStatsService = null;
        _versionChecker?.Dispose();
        _versionChecker = null;
        _globalCancellationTokenSource?.Dispose();
        _globalCancellationTokenSource = null;
    }

    /// <summary>Cancels all ongoing operations and initiates application shutdown.</summary>
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
