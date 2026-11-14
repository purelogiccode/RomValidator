using System.Buffers.Binary;
using System.IO.Compression;
using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class HashCalculatorArchiveTests
{
    [Fact]
    public async Task CalculateHashesAsyncZipReturnsEntryHashes()
    {
        using var scope = new TempDirectoryScope();
        var zipPath = Path.Combine(scope.Path, "roms.zip");
        var content = "123456789"u8.ToArray();
        CreateZip(zipPath, ("game.nes", content));

        var results = await HashCalculator.CalculateHashesAsync(zipPath, CancellationToken.None);

        Assert.Single(results);
        var gameFile = results[0];
        Assert.Equal("game.nes", gameFile.FileName);
        Assert.Equal("roms.zip", gameFile.ArchiveFileName);
        Assert.Equal(9, gameFile.FileSize);
        Assert.Equal("cbf43926", gameFile.Crc32);
        Assert.Equal("25f9e794323b453885f5181f1b624d0b", gameFile.Md5);
        Assert.Equal("f7c3bc1d808e04732adf679965ccc34ca7ae3441", gameFile.Sha1);
        Assert.Equal("15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225", gameFile.Sha256);
        Assert.Empty(gameFile.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task CalculateHashesAsyncZipPreservesNestedEntryKeys()
    {
        using var scope = new TempDirectoryScope();
        var zipPath = Path.Combine(scope.Path, "roms.zip");
        CreateZip(zipPath, ("folder/game.nes", "abc"u8.ToArray()));

        var results = await HashCalculator.CalculateHashesAsync(zipPath, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("folder/game.nes", results[0].FileName);
    }

    [Fact]
    public async Task CalculateHashesAsyncSevenZipReturnsEntryHashes()
    {
        using var scope = new TempDirectoryScope();
        var sourcePath = Path.Combine(scope.Path, "game.nes");
        var content = "123456789"u8.ToArray();
        await File.WriteAllBytesAsync(sourcePath, content);
        var archivePath = Path.Combine(scope.Path, "roms.7z");
        ArchiveService.Create7ZArchive([(sourcePath, "game.nes")], archivePath);

        var results = await HashCalculator.CalculateHashesAsync(archivePath, CancellationToken.None);

        Assert.Single(results);
        var gameFile = results[0];
        Assert.Equal("game.nes", gameFile.FileName);
        Assert.Equal("roms.7z", gameFile.ArchiveFileName);
        Assert.Equal(9, gameFile.FileSize);
        Assert.Equal("cbf43926", gameFile.Crc32);
        Assert.Equal("15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225", gameFile.Sha256);
        Assert.Empty(gameFile.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task CalculateHashesAsyncCorruptArchiveReturnsUserErrorEntry()
    {
        using var scope = new TempDirectoryScope();
        var archivePath = Path.Combine(scope.Path, "corrupt.zip");
        await File.WriteAllTextAsync(archivePath, "this is not a valid archive");

        var results = await HashCalculator.CalculateHashesAsync(archivePath, CancellationToken.None);

        Assert.Single(results);
        Assert.True(results[0].IsUserError);
        Assert.Equal("ERROR", results[0].Crc32);
    }

    [Fact]
    public async Task CalculateHashesAsyncCorruptZipEntryReturnsUserErrorEntry()
    {
        using var scope = new TempDirectoryScope();
        var zipPath = Path.Combine(scope.Path, "corrupt-entry.zip");
        var content = new byte[4096];
        for (var i = 0; i < content.Length; i++)
        {
            content[i] = (byte)(i % 251);
        }

        CreateZip(zipPath, ("game.nes", content));
        CorruptFirstEntryPayload(zipPath);

        var results = await HashCalculator.CalculateHashesAsync(zipPath, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("game.nes", results[0].FileName);
        Assert.True(results[0].IsUserError);
        Assert.Equal("ERROR", results[0].Crc32);
        Assert.Contains("corrupted", results[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Overwrites the first byte of the first entry's deflate payload with a reserved
    /// block header, making the compressed data invalid while keeping the zip readable.
    /// </summary>
    private static void CorruptFirstEntryPayload(string zipPath)
    {
        var bytes = File.ReadAllBytes(zipPath);
        Assert.Equal(0x04034b50u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        var compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(18));
        var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26));
        var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28));
        var dataOffset = 30 + nameLength + extraLength;
        Assert.True(compressedSize > 0);

        // 0xFF sets BFINAL=1 and BTYPE=3 (reserved/invalid block type).
        bytes[dataOffset] = 0xFF;
        File.WriteAllBytes(zipPath, bytes);
    }

    private static void CreateZip(string zipPath, params (string Name, byte[] Content)[] entries)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using var stream = entry.Open();
            stream.Write(content);
        }
    }

    private sealed class TempDirectoryScope : IDisposable
    {
        public TempDirectoryScope()
        {
            Path = Directory.CreateTempSubdirectory("romvalidator_tests_").FullName;
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
