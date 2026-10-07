using System.Net;
using RomValidator.Services;
using RomValidator.Tests.Helpers;
using Xunit;

namespace RomValidator.Tests.Services;

public class BugReportServiceTests
{
    private const string ApiUrl = "https://example.invalid/bugreport";
    private const string ApiKey = "test-api-key";
    private const string ApplicationName = "ROM Validator";

    [Fact]
    public async Task SendBugReportAsyncReturnsTrueOnSuccessfulResponse()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"message":"Bug report received","id":42}"""));
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, handler);

        var result = await service.SendBugReportAsync("Test context");

        Assert.True(result);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task SendBugReportAsyncSendsApiKeyHeader()
    {
        string? apiKeyHeader = null;
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            apiKeyHeader = request.Headers.TryGetValues("X-API-KEY", out var values) ? string.Join("", values) : null;
            return Task.FromResult(FakeHttpMessageHandler.JsonResponse("""{"message":"ok","id":1}"""));
        });
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, handler);

        await service.SendBugReportAsync("Test context");

        Assert.Equal(ApiKey, apiKeyHeader);
    }

    [Fact]
    public async Task SendBugReportAsyncReturnsFalseOnErrorStatus()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"error":"Invalid API key"}""", HttpStatusCode.Unauthorized));
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, handler);

        var result = await service.SendBugReportAsync("Test context");

        Assert.False(result);
    }

    [Fact]
    public async Task SendBugReportAsyncReturnsFalseWhenHandlerThrows()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("No network"));
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, handler);

        var result = await service.SendBugReportAsync("Test context");

        Assert.False(result);
    }

    [Fact]
    public void BuildBugReportMessageContainsAllRequiredSections()
    {
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, new FakeHttpMessageHandler(
            () => FakeBugReportResponse()));

        var message = service.BuildBugReportMessage("Something failed", null, null);

        Assert.Contains("=== Environment Details ===", message, StringComparison.Ordinal);
        Assert.Contains("Date:", message, StringComparison.Ordinal);
        Assert.Contains("Application Name: ROM Validator", message, StringComparison.Ordinal);
        Assert.Contains("Application Version:", message, StringComparison.Ordinal);
        Assert.Contains("OS Version:", message, StringComparison.Ordinal);
        Assert.Contains("Architecture:", message, StringComparison.Ordinal);
        Assert.Contains("Bitness:", message, StringComparison.Ordinal);
        Assert.Contains("Processor Count:", message, StringComparison.Ordinal);
        Assert.Contains("Base Directory:", message, StringComparison.Ordinal);
        Assert.Contains("Temp Path:", message, StringComparison.Ordinal);
        Assert.Contains("=== Error Details ===", message, StringComparison.Ordinal);
        Assert.Contains("Error message: Something failed", message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBugReportMessageContainsExceptionDetails()
    {
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, new FakeHttpMessageHandler(
            () => FakeBugReportResponse()));

        Exception exception;
        try
        {
            throw new InvalidOperationException("Boom");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var message = service.BuildBugReportMessage("Failure context", exception, "extra info");

        Assert.Contains("=== Exception Details ===", message, StringComparison.Ordinal);
        Assert.Contains("Type: System.InvalidOperationException", message, StringComparison.Ordinal);
        Assert.Contains("Message: Boom", message, StringComparison.Ordinal);
        Assert.Contains("Source:", message, StringComparison.Ordinal);
        Assert.Contains("StackTrace:", message, StringComparison.Ordinal);
        Assert.Contains("Additional Info: extra info", message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBugReportMessageIncludesInnerExceptions()
    {
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, new FakeHttpMessageHandler(
            () => FakeBugReportResponse()));

        var inner = new FormatException("inner problem");
        var outer = new InvalidOperationException("outer problem", inner);

        var message = service.BuildBugReportMessage("Failure context", outer, null);

        Assert.Contains("--- Inner Exception #1 ---", message, StringComparison.Ordinal);
        Assert.Contains("Message: inner problem", message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBugReportMessageTruncatesToApiLimit()
    {
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, new FakeHttpMessageHandler(
            () => FakeBugReportResponse()));

        var hugeContext = new string('x', 20000);
        var message = service.BuildBugReportMessage(hugeContext, null, null);

        Assert.EndsWith("[MESSAGE TRUNCATED DUE TO LENGTH LIMITS]", message, StringComparison.Ordinal);
        Assert.True(message.Length <= 4000, $"Message must not exceed the API limit of 4000 chars, was {message.Length}");
    }

    [Fact]
    public void BuildBugReportMessageIncludesPlatformSpecificVersionLabel()
    {
        using var service = new BugReportService(ApiUrl, ApiKey, ApplicationName, new FakeHttpMessageHandler(
            () => FakeBugReportResponse()));

        var message = service.BuildBugReportMessage("context", null, null);

        var hasOsLabel = message.Contains("Windows Version:", StringComparison.Ordinal) ||
                         message.Contains("Linux Version:", StringComparison.Ordinal) ||
                         message.Contains("MacOSX Version:", StringComparison.Ordinal);
        Assert.True(hasOsLabel, "Bug report must include a platform-specific OS version label.");
    }

    private static HttpResponseMessage FakeBugReportResponse()
    {
        return FakeHttpMessageHandler.JsonResponse("""{"message":"ok","id":1}""");
    }
}
