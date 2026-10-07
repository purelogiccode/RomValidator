using RomValidator.Services;
using Xunit;

namespace RomValidator.Tests.Services;

public class SevenZipProcessTests
{
    [Fact]
    public void GetExecutablePathReturnsBundledExecutable()
    {
        var executablePath = SevenZipProcess.GetExecutablePath();

        Assert.NotNull(executablePath);
        Assert.True(File.Exists(executablePath));
    }

    [Fact]
    public void ParseUncompressedSizeSumsFilesAndSkipsDirectories()
    {
        const string listing = """
                              7-Zip (a) 26.03 (x64) : Copyright (c) 1999-2026 Igor Pavlov : 2026-09-03

                              Scanning the drive for archives:
                              1 file, 5000 bytes (5 KiB)

                              ----------
                              Path = C:\test.7z
                              Type = 7z
                              Physical Size = 5000
                              Headers Size = 200

                              ----------
                              Path = file1.txt
                              Size = 1000
                              Attributes = A

                              Path = nested
                              Size = 0
                              Attributes = D

                              Path = file2.txt
                              Size = 234
                              Attributes = A

                              """;

        var total = SevenZipProcess.ParseUncompressedSize(listing);

        Assert.Equal(1234, total);
    }

    [Fact]
    public void ParseUncompressedSizeReturnsZeroForEmptyOutput()
    {
        Assert.Equal(0, SevenZipProcess.ParseUncompressedSize(string.Empty));
    }

    [Fact]
    public async Task ExtractArchiveAsyncExtractsSevenZipArchive()
    {
        var executablePath = SevenZipProcess.GetExecutablePath();
        Assert.NotNull(executablePath);

        var scopePath = Directory.CreateTempSubdirectory("romvalidator_7za_tests_").FullName;
        try
        {
            var sourcePath = Path.Combine(scopePath, "source.bin");
            await File.WriteAllTextAsync(sourcePath, "fallback content");
            var archivePath = Path.Combine(scopePath, "test.7z");
            ArchiveService.Create7ZArchive([(sourcePath, "entry.bin")], archivePath);

            var extractPath = Path.Combine(scopePath, "extracted");
            var result = await SevenZipProcess.ExtractArchiveAsync(archivePath, extractPath, CancellationToken.None);

            Assert.True(result);
            Assert.Equal("fallback content", File.ReadAllText(Path.Combine(extractPath, "entry.bin")));
        }
        finally
        {
            try
            {
                if (Directory.Exists(scopePath))
                {
                    Directory.Delete(scopePath, true);
                }
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task CreateArchiveAsyncCreatesSevenZipArchiveWith7Za()
    {
        var executablePath = SevenZipProcess.GetExecutablePath();
        Assert.NotNull(executablePath);

        var scopePath = Directory.CreateTempSubdirectory("romvalidator_7za_tests_").FullName;
        try
        {
            var stagingPath = Path.Combine(scopePath, "staging");
            Directory.CreateDirectory(stagingPath);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, "entry.bin"), "created by 7za");
            var archivePath = Path.Combine(scopePath, "created.7z");

            var result = await SevenZipProcess.CreateArchiveAsync(stagingPath, archivePath, true,
                CancellationToken.None);

            Assert.True(result);
            var entries = ArchiveService.GetFileEntries(archivePath);
            Assert.Single(entries);
            Assert.Equal("entry.bin", entries[0].Key);
        }
        finally
        {
            try
            {
                if (Directory.Exists(scopePath))
                {
                    Directory.Delete(scopePath, true);
                }
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
