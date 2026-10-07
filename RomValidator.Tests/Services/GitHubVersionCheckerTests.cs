using System.Net;
using System.Reflection;
using RomValidator.Services;
using RomValidator.Tests.Helpers;
using Xunit;

namespace RomValidator.Tests.Services;

public class GitHubVersionCheckerTests
{
    private const string RepoOwner = "purelogiccode";
    private const string RepoName = "RomValidator";

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("release_1.2.3", "1.2.3")]
    [InlineData("V2.8.0", "2.8.0")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    public void ParseVersionTagParsesCommonTagFormats(string tag, string expectedVersion)
    {
        var version = GitHubVersionChecker.ParseVersionTag(tag);

        Assert.NotNull(version);
        Assert.Equal(expectedVersion, version.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    [InlineData("release_latest")]
    public void ParseVersionTagReturnsNullForInvalidTags(string? tag)
    {
        Assert.Null(GitHubVersionChecker.ParseVersionTag(tag));
    }

    [Fact]
    public async Task CheckForNewVersionAsyncReturnsTrueWhenNewerVersionExists()
    {
        var handler = new FakeHttpMessageHandler(() => FakeHttpMessageHandler.JsonResponse("""
            {
                "tag_name": "v99.0.0",
                "html_url": "https://github.com/purelogiccode/RomValidator/releases/tag/v99.0.0"
            }
            """));
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        var (isNewVersionAvailable, releaseUrl, latestVersionTag) = await checker.CheckForNewVersionAsync();

        Assert.True(isNewVersionAvailable);
        Assert.Equal("https://github.com/purelogiccode/RomValidator/releases/tag/v99.0.0", releaseUrl);
        Assert.Equal("v99.0.0", latestVersionTag);
    }

    [Fact]
    public async Task CheckForNewVersionAsyncReturnsFalseForOlderVersion()
    {
        var handler = new FakeHttpMessageHandler(() => FakeHttpMessageHandler.JsonResponse("""
            {
                "tag_name": "v0.0.1",
                "html_url": "https://github.com/purelogiccode/RomValidator/releases/tag/v0.0.1"
            }
            """));
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        var (isNewVersionAvailable, releaseUrl, latestVersionTag) = await checker.CheckForNewVersionAsync();

        Assert.False(isNewVersionAvailable);
        Assert.Null(releaseUrl);
        Assert.Null(latestVersionTag);
    }

    [Fact]
    public async Task CheckForNewVersionAsyncReturnsFalseWhenCurrentVersionIsTheSame()
    {
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        var handler = new FakeHttpMessageHandler(() => FakeHttpMessageHandler.JsonResponse($$"""
            {
                "tag_name": "v{{currentVersion}}",
                "html_url": "https://github.com/purelogiccode/RomValidator/releases/tag/v{{currentVersion}}"
            }
            """));
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        var (isNewVersionAvailable, _, _) = await checker.CheckForNewVersionAsync();

        Assert.False(isNewVersionAvailable);
    }

    [Fact]
    public async Task CheckForNewVersionAsyncReturnsFalseWhenReleaseInfoMissing()
    {
        var handler = new FakeHttpMessageHandler(() => FakeHttpMessageHandler.JsonResponse("""{"tag_name": null}"""));
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        var (isNewVersionAvailable, releaseUrl, latestVersionTag) = await checker.CheckForNewVersionAsync();

        Assert.False(isNewVersionAvailable);
        Assert.Null(releaseUrl);
        Assert.Null(latestVersionTag);
    }

    [Fact]
    public async Task CheckForNewVersionAsyncReturnsFalseOnHttpError()
    {
        var handler = new FakeHttpMessageHandler(
            () => FakeHttpMessageHandler.JsonResponse("""{"message":"Not Found"}""", HttpStatusCode.NotFound));
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        var (isNewVersionAvailable, releaseUrl, latestVersionTag) = await checker.CheckForNewVersionAsync();

        Assert.False(isNewVersionAvailable);
        Assert.Null(releaseUrl);
        Assert.Null(latestVersionTag);
    }

    [Fact]
    public async Task CheckForNewVersionAsyncReturnsFalseWhenHandlerThrows()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("No network"));
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        var (isNewVersionAvailable, releaseUrl, latestVersionTag) = await checker.CheckForNewVersionAsync();

        Assert.False(isNewVersionAvailable);
        Assert.Null(releaseUrl);
        Assert.Null(latestVersionTag);
    }

    [Fact]
    public async Task CheckForNewVersionAsyncSendsUserAgentHeader()
    {
        string? userAgent = null;
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            userAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(
                FakeHttpMessageHandler.JsonResponse("""{"tag_name":"v0.0.1","html_url":"https://example.com"}"""));
        });
        using var checker = new GitHubVersionChecker(RepoOwner, RepoName, handler);

        await checker.CheckForNewVersionAsync();

        Assert.Contains("RomValidator", userAgent, StringComparison.OrdinalIgnoreCase);
    }
}
