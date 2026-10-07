using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class ScreenshotServiceTests
{
    [Fact]
    public void CaptureWindowScreenshotThrowsForNullWindow()
    {
        Assert.Throws<ArgumentNullException>(() => ScreenshotService.CaptureWindowScreenshot(null!));
    }

    [Fact]
    public void CaptureActiveWindowScreenshotDoesNotThrowWithoutApplication()
    {
        // In the test host there is no WPF Application running, so the service
        // must fail gracefully instead of throwing.
        var exception = Record.Exception(() => ScreenshotService.CaptureActiveWindowScreenshot());

        Assert.Null(exception);
    }
}
