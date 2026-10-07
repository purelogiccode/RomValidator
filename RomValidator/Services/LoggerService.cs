using System.Diagnostics;
using Serilog;
using Serilog.Events;

namespace RomValidator.Services;

/// <summary>
/// Facade over Serilog that preserves the existing call-site API while routing
/// Warning and higher events to the bug report API through a custom sink.
/// All logging in the application goes through this class.
/// </summary>
public static class LoggerService
{
    private static ILogger _logger = Serilog.Core.Logger.None;

    /// <summary>
    /// Initializes the logger facade with the configured Serilog logger.
    /// </summary>
    /// <param name="logger">The Serilog logger that receives all log events.</param>
    public static void Initialize(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs an error message for the specified component.
    /// </summary>
    /// <param name="component">The component or area that produced the error.</param>
    /// <param name="message">The error message.</param>
    public static void LogError(string component, string message)
    {
        Write(LogEventLevel.Error, component, message, null);
    }

    /// <summary>
    /// Logs an exception with optional context for the specified component.
    /// </summary>
    /// <param name="component">The component or area that produced the exception.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="context">Optional context describing where the exception occurred.</param>
    public static void LogException(string component, Exception exception, string? context = null)
    {
        var fullContext = context != null ? $"{component} - {context}" : component;
        Write(LogEventLevel.Error, fullContext, exception.Message, exception);
    }

    /// <summary>
    /// Logs a warning message for the specified component.
    /// Warnings are forwarded to the bug report API.
    /// </summary>
    /// <param name="component">The component or area that produced the warning.</param>
    /// <param name="message">The warning message.</param>
    public static void LogWarning(string component, string message)
    {
        Write(LogEventLevel.Warning, component, message, null);
    }

    /// <summary>
    /// Logs an informational message for the specified component.
    /// </summary>
    /// <param name="component">The component or area that produced the message.</param>
    /// <param name="message">The informational message.</param>
    public static void LogInfo(string component, string message)
    {
        Write(LogEventLevel.Information, component, message, null);
    }

    /// <summary>
    /// Logs a debug message for the specified component.
    /// Debug logging is only compiled into DEBUG builds.
    /// </summary>
    /// <param name="component">The component or area that produced the message.</param>
    /// <param name="message">The debug message.</param>
    [Conditional("DEBUG")]
    public static void LogDebug(string component, string message)
    {
        Write(LogEventLevel.Debug, component, message, null);
    }

    /// <summary>
    /// Writes the event to the underlying Serilog logger. Never throws so logging
    /// failures cannot crash the application.
    /// </summary>
    /// <param name="level">The severity level of the event.</param>
    /// <param name="component">The component attached to the event.</param>
    /// <param name="message">The rendered message.</param>
    /// <param name="exception">The optional exception.</param>
    private static void Write(LogEventLevel level, string component, string message, Exception? exception)
    {
        try
        {
            var componentLogger = _logger.ForContext("Component", component);
            componentLogger.Write(level, exception, message);
        }
        catch
        {
            // Logging must never throw. Fall back to the debug output so the message
            // is not lost entirely while developing.
            Debug.WriteLine($"[{level}] [{component}] {message}");
        }
    }
}
