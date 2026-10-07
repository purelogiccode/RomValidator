using System.Runtime.InteropServices;
using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class FileSystemHelperTests
{
    [Fact]
    public void IsDiskFullErrorReturnsTrueForDiskFullHResult()
    {
        var exception = Marshal.GetExceptionForHR(unchecked((int)0x80070070));

        Assert.NotNull(exception);
        Assert.True(FileSystemHelper.IsDiskFullError(exception));
    }

    [Theory]
    [InlineData("There is not enough space on the disk.")]
    [InlineData("Disk full")]
    [InlineData("ERROR_DISK_FULL")]
    public void IsDiskFullErrorReturnsTrueForKnownMessages(string message)
    {
        Assert.True(FileSystemHelper.IsDiskFullError(new IOException(message)));
    }

    [Fact]
    public void IsDiskFullErrorReturnsFalseForOtherErrors()
    {
        Assert.False(FileSystemHelper.IsDiskFullError(new IOException("Access denied")));
    }

    [Fact]
    public void IsDiskFullErrorThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => FileSystemHelper.IsDiskFullError(null!));
    }
}
