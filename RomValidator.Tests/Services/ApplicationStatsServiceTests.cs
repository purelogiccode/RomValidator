using System.Net;
using System.Text.Json;
using RomValidator.Services;
using RomValidator.Tests.Helpers;
using Xunit;

namespace RomValidator.Tests.Services;

public class ApplicationStatsServiceTests
{
    private const string BaseUrl = "https://example.invalid/ApplicationStats";
    private const string ApiKey = "stats-api-key";
    private const string ApplicationId = "rom-validator";

    [Fact]
    public async Task RecordUsageAsyncReturnsTrueOnSuccess()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"message":"Stats recorded successfully"}"""));
        using var service = new ApplicationStatsService(BaseUrl, ApiKey, ApplicationId, handler);

        var result = await service.RecordUsageAsync();

        Assert.True(result);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RecordUsageAsyncSendsBearerTokenAndPayload()
    {
        string? authorization = null;
        string? body = null;
        var handler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
        {
            authorization = request.Headers.Authorization?.ToString();
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return FakeHttpMessageHandler.JsonResponse("""{"message":"ok"}""");
        });
        using var service = new ApplicationStatsService(BaseUrl, ApiKey, ApplicationId, handler);

        await service.RecordUsageAsync();

        Assert.Equal($"Bearer {ApiKey}", authorization);
        Assert.NotNull(body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(ApplicationId, document.RootElement.GetProperty("applicationId").GetString());
        Assert.True(document.RootElement.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task RecordUsageAsyncReturnsFalseOnRateLimitWithoutLoggingError()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"error":"Rate limit exceeded"}""",
                HttpStatusCode.TooManyRequests));
        using var service = new ApplicationStatsService(BaseUrl, ApiKey, ApplicationId, handler);

        var result = await service.RecordUsageAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RecordUsageAsyncReturnsFalseOnServerError()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"error":"boom"}""", HttpStatusCode.InternalServerError));
        using var service = new ApplicationStatsService(BaseUrl, ApiKey, ApplicationId, handler);

        var result = await service.RecordUsageAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RecordUsageAsyncReturnsFalseWhenHandlerThrows()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("No network"));
        using var service = new ApplicationStatsService(BaseUrl, ApiKey, ApplicationId, handler);

        var result = await service.RecordUsageAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RecordUsageAsyncOnlyCallsApiOncePerInstance()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"message":"ok"}"""));
        using var service = new ApplicationStatsService(BaseUrl, ApiKey, ApplicationId, handler);

        var first = await service.RecordUsageAsync();
        var second = await service.RecordUsageAsync();

        Assert.True(first);
        Assert.True(second);
        Assert.Equal(1, handler.RequestCount);
    }
}
