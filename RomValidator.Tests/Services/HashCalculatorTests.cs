using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class HashCalculatorTests
{
    [Theory]
    [InlineData("game.zip", true)]
    [InlineData("game.7z", true)]
    [InlineData("game.rar", true)]
    [InlineData("GAME.ZIP", true)]
    [InlineData("Game.7Z", true)]
    [InlineData("game.rom", false)]
    [InlineData("game.nes", false)]
    [InlineData("game.smc", false)]
    [InlineData("archive.zip.txt", false)]
    [InlineData("game", false)]
    public void IsArchiveFileDetectsArchiveExtensions(string fileName, bool expected)
    {
        // Act
        var result = HashCalculator.IsArchiveFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("archive.zipp", false)]
    [InlineData("archive.zi", false)]
    [InlineData(".zip", true)]
    [InlineData("path/to/game.zip", true)]
    [InlineData(@"C:\Users\test\backup.7z", true)]
    [InlineData("archive.ZIP.PART", false)]
    [InlineData("game.nes.zip", true)]
    public void IsArchiveFileHandlesEdgeCases(string fileName, bool expected)
    {
        // Act
        var result = HashCalculator.IsArchiveFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CalculateHashesAsyncNonExistentFileReturnsErrorEntry()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.bin");

        var results = await HashCalculator.CalculateHashesAsync(missingFile, CancellationToken.None);

        Assert.Single(results);
        Assert.False(string.IsNullOrEmpty(results[0].ErrorMessage));
        Assert.Equal("ERROR", results[0].Crc32);
    }

    [Fact]
    public async Task CalculateHashesAsyncDirectoryPathReturnsErrorEntry()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hashdir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var results = await HashCalculator.CalculateHashesAsync(tempDir, CancellationToken.None);

            Assert.Single(results);
            Assert.False(string.IsNullOrEmpty(results[0].ErrorMessage));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CalculateHashesAsyncSingleByteFileMatchesKnownHashes()
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(tempFile, [0x00]);

            var results = await HashCalculator.CalculateHashesAsync(tempFile, CancellationToken.None);

            Assert.Single(results);
            Assert.Equal("d202ef8d", results[0].Crc32);
            Assert.Equal("93b885adfe0da089cdf634904fd59f71", results[0].Md5);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}