using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class AppPathsTests
{
    [Fact]
    public void AppDataDirectoryEndsWithApplicationName()
    {
        Assert.EndsWith(AppPaths.ApplicationName, AppPaths.AppDataDirectory, StringComparison.Ordinal);
    }

    [Fact]
    public void LogsDirectoryIsInsideAppDataDirectory()
    {
        var expected = Path.Combine(AppPaths.AppDataDirectory, "Logs");

        Assert.Equal(expected, AppPaths.LogsDirectory);
    }

    [Fact]
    public void FallbackScreenshotsDirectoryIsInsideAppDataDirectory()
    {
        var expected = Path.Combine(AppPaths.AppDataDirectory, "Screenshot");

        Assert.Equal(expected, AppPaths.FallbackScreenshotsDirectory);
    }

    [Fact]
    public void PrimaryScreenshotsDirectoryIsInsideApplicationFolder()
    {
        var expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshot");

        Assert.Equal(expected, AppPaths.ScreenshotsDirectory);
    }

    [Fact]
    public void EnsureCreatedCreatesLogsAndFallbackScreenshotFolders()
    {
        // Note: intentionally does not delete the folders afterwards because they
        // may contain real application logs on a developer machine.
        AppPaths.EnsureCreated();

        Assert.True(Directory.Exists(AppPaths.LogsDirectory));
        Assert.True(Directory.Exists(AppPaths.FallbackScreenshotsDirectory));
    }
}
