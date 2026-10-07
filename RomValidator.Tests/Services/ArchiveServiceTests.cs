using System.IO.Compression;
using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class ArchiveServiceTests
{
    [Fact]
    public void GetFileEntriesListsNonDirectoryEntries()
    {
        using var scope = new TempDirectoryScope();
        var zipPath = Path.Combine(scope.Path, "test.zip");
        CreateZip(zipPath, ("file1.txt", "hello"u8.ToArray()), ("folder/file2.bin", new byte[100]));

        var entries = ArchiveService.GetFileEntries(zipPath);

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => string.Equals(entry.Key, "file1.txt", StringComparison.Ordinal) && entry.Size == 5);
        Assert.Contains(entries,
            entry => string.Equals(entry.Key, "folder/file2.bin", StringComparison.Ordinal) && entry.Size == 100);
    }

    [Fact]
    public void GetTotalUncompressedSizeSumsAllEntries()
    {
        using var scope = new TempDirectoryScope();
        var zipPath = Path.Combine(scope.Path, "test.zip");
        CreateZip(zipPath, ("a.bin", new byte[10]), ("b.bin", new byte[32]));

        var total = ArchiveService.GetTotalUncompressedSize(zipPath);

        Assert.Equal(42, total);
    }

    [Fact]
    public void ExtractToDirectoryExtractsNestedEntries()
    {
        using var scope = new TempDirectoryScope();
        var zipPath = Path.Combine(scope.Path, "test.zip");
        CreateZip(zipPath, ("file1.txt", "hello"u8.ToArray()), ("folder/file2.bin", new byte[7]));
        var extractPath = Path.Combine(scope.Path, "extracted");

        ArchiveService.ExtractToDirectory(zipPath, extractPath);

        Assert.Equal("hello", File.ReadAllText(Path.Combine(extractPath, "file1.txt")));
        Assert.Equal(7, new FileInfo(Path.Combine(extractPath, "folder", "file2.bin")).Length);
    }

    [Fact]
    public void CreateZipArchiveUsesProvidedEntryNames()
    {
        using var scope = new TempDirectoryScope();
        var sourcePath = Path.Combine(scope.Path, "source.bin");
        File.WriteAllText(sourcePath, "content");
        var zipPath = Path.Combine(scope.Path, "output.zip");

        ArchiveService.CreateZipArchive([(sourcePath, "renamed.bin")], zipPath);

        var entries = ArchiveService.GetFileEntries(zipPath);
        Assert.Single(entries);
        Assert.Equal("renamed.bin", entries[0].Key);

        var extractPath = Path.Combine(scope.Path, "extracted");
        ArchiveService.ExtractToDirectory(zipPath, extractPath);
        Assert.Equal("content", File.ReadAllText(Path.Combine(extractPath, "renamed.bin")));
    }

    [Fact]
    public void Create7ZArchiveRoundTripsWithProvidedEntryNames()
    {
        using var scope = new TempDirectoryScope();
        var sourcePath = Path.Combine(scope.Path, "source.bin");
        var content = new byte[256];
        new Random(7).NextBytes(content);
        File.WriteAllBytes(sourcePath, content);
        var archivePath = Path.Combine(scope.Path, "output.7z");

        ArchiveService.Create7ZArchive([(sourcePath, "renamed.bin")], archivePath);

        var entries = ArchiveService.GetFileEntries(archivePath);
        Assert.Single(entries);
        Assert.Equal("renamed.bin", entries[0].Key);
        Assert.Equal(content.Length, entries[0].Size);

        var extractPath = Path.Combine(scope.Path, "extracted");
        ArchiveService.ExtractToDirectory(archivePath, extractPath);
        Assert.Equal(content, File.ReadAllBytes(Path.Combine(extractPath, "renamed.bin")));
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
