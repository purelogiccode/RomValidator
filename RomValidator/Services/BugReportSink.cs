using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace RomValidator.Services;

/// <summary>
/// A Serilog sink that forwards Error and Fatal log events to the bug report API.
/// Each event is submitted via BugReportService with full environment and exception details.
/// Protected against recursion so a failure inside the sink does not trigger another bug report.
/// </summary>
internal sealed class BugReportSink : ILogEventSink, IDisposable
{
    private readonly BugReportService _bugReportService;
    private int _isSending; // 1 = a bug report send is currently in flight

    public BugReportSink(BugReportService bugReportService)
    {
        _bugReportService = bugReportService;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error)
            return;

        var component = "Serilog";
        if (logEvent.Properties.TryGetValue("Component", out var componentValue) &&
            componentValue is ScalarValue { Value: string compStr })
        {
            component = compStr;
        }

        // Never forward failures originating from the bug reporting pipeline itself,
        // otherwise a failing report would recursively generate more reports.
        // Prefix match covers both "BugReportService" and composite contexts such as
        // "BugReportService - <context>".
        if (component.StartsWith("BugReportService", StringComparison.Ordinal) ||
            component.StartsWith("BugReportSink", StringComparison.Ordinal))
        {
            return;
        }

        // Allow only ONE in-flight bug report at a time (atomic check-and-claim).
        // The gate is released when the send completes, so bursts of errors cannot
        // flood the API with concurrent requests. Anything logged while a send is
        // pending is dropped - this is also a second recursion guard.
        if (Interlocked.Exchange(ref _isSending, 1) != 0)
            return;

        try
        {
            var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

            Exception? exception = null;
            string? additionalInfo = null;
            if (logEvent.Exception != null)
            {
                exception = logEvent.Exception;
            }
            else if (logEvent.Properties.TryGetValue("ErrorMessage", out var errorMsgValue) &&
                     errorMsgValue is ScalarValue { Value: string errStr })
            {
                additionalInfo = errStr;
            }

            var sendTask = _bugReportService.SendBugReportAsync(
                message,
                exception,
                additionalInfo,
                CancellationToken.None);

            // Release the gate when the send completes or fails. SendBugReportAsync
            // never faults (it catches everything internally), so the continuation is
            // guaranteed to run.
            _ = sendTask.ContinueWith(
                _ => Interlocked.Exchange(ref _isSending, 0),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        catch
        {
            // If the send could not even be started, release the gate immediately.
            Interlocked.Exchange(ref _isSending, 0);
        }
    }

    public void Dispose()
    {
    }
}