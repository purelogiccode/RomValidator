using RomValidator.Interfaces;
using RomValidator.Services;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace RomValidator.Tests.Services;

public class BugReportSinkTests
{
    [Fact]
    public void WarningEventsAreForwardedToBugReportApi()
    {
        var fakeService = new FakeBugReportService();
        using var sink = new BugReportSink(fakeService);

        sink.Emit(CreateLogEvent(LogEventLevel.Warning, "A warning"));

        Assert.Single(fakeService.Reports);
        Assert.Equal("A warning", fakeService.Reports[0].Context);
    }

    [Fact]
    public void ErrorEventsWithExceptionAreForwardedWithException()
    {
        var fakeService = new FakeBugReportService();
        using var sink = new BugReportSink(fakeService);
        var exception = new InvalidOperationException("Boom");

        sink.Emit(CreateLogEvent(LogEventLevel.Error, "An error", exception));

        Assert.Single(fakeService.Reports);
        Assert.Same(exception, fakeService.Reports[0].Exception);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    public void LowerLevelEventsAreNotForwarded(LogEventLevel level)
    {
        var fakeService = new FakeBugReportService();
        using var sink = new BugReportSink(fakeService);

        sink.Emit(CreateLogEvent(level, "Low level message"));

        Assert.Empty(fakeService.Reports);
    }

    [Fact]
    public void BugReportServiceComponentsAreNotForwardedToAvoidRecursion()
    {
        var fakeService = new FakeBugReportService();
        using var sink = new BugReportSink(fakeService);
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Recursive failure", component: "BugReportService");

        sink.Emit(logEvent);

        Assert.Empty(fakeService.Reports);
    }

    [Fact]
    public async Task OnlyOneReportIsSentWhileASendIsInFlight()
    {
        var fakeService = new FakeBugReportService { HoldSends = true };
        using var sink = new BugReportSink(fakeService);

        sink.Emit(CreateLogEvent(LogEventLevel.Error, "First"));
        sink.Emit(CreateLogEvent(LogEventLevel.Error, "Second"));

        Assert.Equal(1, fakeService.CallCount);

        fakeService.ReleaseSends();

        // Wait until the first send completes; the sink releases its gate on completion.
        await fakeService.WaitForSendCompletionAsync();

        sink.Emit(CreateLogEvent(LogEventLevel.Error, "Third"));

        Assert.Equal(2, fakeService.CallCount);
    }

    private static LogEvent CreateLogEvent(LogEventLevel level, string message, Exception? exception = null,
        string? component = null)
    {
        var template = new MessageTemplateParser().Parse(message);
        List<LogEventProperty> properties = [];

        if (component != null)
        {
            properties.Add(new LogEventProperty("Component", new ScalarValue(component)));
        }

        return new LogEvent(DateTimeOffset.UtcNow, level, exception, template, properties);
    }

    private sealed class FakeBugReportService : IBugReportService
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _sendCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<(string Context, Exception? Exception, string? AdditionalInfo)> Reports { get; } = [];
        public int CallCount { get; private set; }
        public bool HoldSends { get; init; }

        public Task<bool> SendBugReportAsync(string context, Exception? exception = null, string? additionalInfo = null)
        {
            return SendBugReportAsync(context, exception, additionalInfo, CancellationToken.None);
        }

        public async Task<bool> SendBugReportAsync(string context, Exception? exception, string? additionalInfo,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Reports.Add((context, exception, additionalInfo));

            try
            {
                if (HoldSends)
                {
                    await _gate.Task;
                }

                return true;
            }
            finally
            {
                _sendCompleted.TrySetResult();
            }
        }

        public void ReleaseSends()
        {
            _gate.TrySetResult();
        }

        public Task WaitForSendCompletionAsync()
        {
            return _sendCompleted.Task;
        }

        public void Dispose()
        {
        }
    }
}
