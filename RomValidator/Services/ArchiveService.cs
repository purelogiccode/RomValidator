using System.IO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;
using SharpCompress.Writers.Zip;

namespace RomValidator.Services;

/// <summary>
/// Archive helpers built on SharpCompress. Reading supports zip, 7z, rar and other
/// common formats; archive creation supports zip and 7z. When SharpCompress cannot
/// create an archive, callers fall back to <see cref="SevenZipProcess"/>.
/// </summary>
public static class ArchiveService
{
    /// <summary>
    /// Gets the non-directory entries (key and uncompressed size) of an archive.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <returns>The archive entries with their internal keys and sizes.</returns>
    public static IReadOnlyList<(string Key, long Size)> GetFileEntries(string archivePath)
    {
        ArgumentNullException.ThrowIfNull(archivePath);

        using var archive = ArchiveFactory.OpenArchive(archivePath);
        return archive.Entries
            .Where(static entry => !entry.IsDirectory)
            .Select(static entry => (entry.Key ?? string.Empty, entry.Size))
            .ToList();
    }

    /// <summary>
    /// Calculates the total uncompressed size of all files in an archive.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <returns>The total uncompressed size in bytes.</returns>
    public static long GetTotalUncompressedSize(string archivePath)
    {
        return GetFileEntries(archivePath).Sum(static entry => entry.Size);
    }

    /// <summary>
    /// Extracts all files from an archive into the destination directory, preserving
    /// the internal folder structure.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="destinationDirectory">Directory to extract the files into.</param>
    public static void ExtractToDirectory(string archivePath, string destinationDirectory)
    {
        ArgumentNullException.ThrowIfNull(archivePath);
        ArgumentNullException.ThrowIfNull(destinationDirectory);

        Directory.CreateDirectory(destinationDirectory);

        using var archive = ArchiveFactory.OpenArchive(archivePath);
        archive.WriteToDirectory(destinationDirectory, new ExtractionOptions
        {
            ExtractFullPath = true,
            Overwrite = true
        });
    }

    /// <summary>
    /// Creates a zip archive from the specified files. Entry names are taken from the
    /// provided mapping and may differ from the file names on disk.
    /// </summary>
    /// <param name="entries">The files to include with their desired entry names.</param>
    /// <param name="outputPath">The zip file to create.</param>
    public static void CreateZipArchive(IReadOnlyList<(string FilePath, string EntryName)> entries, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(outputPath);

        using var archive = ZipArchive.CreateArchive();
        foreach (var (filePath, entryName) in entries)
        {
            archive.AddEntry(entryName, filePath);
        }

        archive.SaveTo(outputPath, new ZipWriterOptions(CompressionType.Deflate));
    }

    /// <summary>
    /// Creates a 7z archive from the specified files. Entry names are taken from the
    /// provided mapping and may differ from the file names on disk.
    /// </summary>
    /// <param name="entries">The files to include with their desired entry names.</param>
    /// <param name="outputPath">The 7z file to create.</param>
    public static void Create7ZArchive(IReadOnlyList<(string FilePath, string EntryName)> entries, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(outputPath);

        using var stream = File.Create(outputPath);
        using var writer = WriterFactory.OpenWriter(stream, ArchiveType.SevenZip,
            new SevenZipWriterOptions(CompressionType.LZMA2) { CompressHeader = true });

        foreach (var (filePath, entryName) in entries)
        {
            using var source = File.OpenRead(filePath);
            writer.Write(entryName, source, File.GetLastWriteTime(filePath));
        }
    }
}
