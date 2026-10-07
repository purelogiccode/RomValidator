using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SharpCompress.Archives;
using RomValidator.Models;

namespace RomValidator.Services;

/// <summary>
/// Provides static methods for calculating hash values (CRC32, MD5, SHA1, SHA256) for files and archives.
/// Supports both regular files and archive formats (ZIP, 7Z, RAR) using SharpCompress,
/// with the 7za command-line tool as a fallback for archives SharpCompress cannot read.
/// </summary>
public static partial class HashCalculator
{
    // Archive file extensions supported - shared regex pattern
    private static readonly Regex SArchiveExtensionRegex = MyRegex();
    private const int BufferSize = 65536; // 64KB buffer for file operations
    private const int InitialRetryDelayMs = 100; // Initial delay for retry attempts in milliseconds
    private const int ErrorCrc = unchecked((int)0x80070017); // Win32 ERROR_CRC (23): unreadable data / bad sectors

    /// <summary>
    /// Calculates hash values (CRC32, MD5, SHA1, SHA256) for a file or archive.
    /// Supports both regular files and archive formats (ZIP, 7Z, RAR).
    /// </summary>
    /// <param name="filePath">The path to the file or archive to process.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A list of <see cref="GameFile"/> objects containing hash results for each file.</returns>
    public static async Task<List<GameFile>> CalculateHashesAsync(string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CalculateHashesCoreAsync(filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LoggerService.LogException("HashCalculator", ex, $"Unexpected error calculating hashes for '{filePath}'");
            throw;
        }
    }

    /// <summary>
    /// Core hash calculation implementation shared by the public entry point.
    /// </summary>
    /// <param name="filePath">The path to the file or archive to process.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A list of <see cref="GameFile"/> objects containing hash results for each file.</returns>
    private static async Task<List<GameFile>> CalculateHashesCoreAsync(string filePath,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        var gameFiles = new List<GameFile>();

        // Skip cloud-only placeholders (e.g. OneDrive files that are not fully available locally).
        // FileInfo.Attributes returns -1 (all flags set, including ReparsePoint) for missing
        // files, so require the file to actually exist before treating it as a placeholder.
        if (fileInfo.Exists && fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            LoggerService.LogInfo("HashCalculator", $"Skipping cloud-only placeholder file: '{fileInfo.Name}'");
            gameFiles.Add(new GameFile
            {
                FileName = fileInfo.Name,
                GameName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileSize = GetFileSizeSafe(fileInfo),
                ErrorMessage =
                    "This file is a cloud-only placeholder and is not fully available locally. Please ensure the file is downloaded to your device.",
                IsUserError = true,
                Crc32 = "ERROR",
                Md5 = "ERROR",
                Sha1 = "ERROR",
                Sha256 = "ERROR"
            });
            return gameFiles;
        }

        if (IsArchiveFile(fileInfo.Name))
        {
            return await CalculateArchiveHashesAsync(fileInfo, filePath, cancellationToken).ConfigureAwait(false);
        }

        // Create algorithm instances once per file
        using var crc32 = new Crc32Algorithm();
        using var md5 = MD5.Create();
        using var sha1 = SHA1.Create();
        using var sha256 = SHA256.Create();

        // Process as regular file
        var gameFile = await ProcessFileAsync(
            filePath, fileInfo,
            crc32, md5, sha1, sha256,
            cancellationToken).ConfigureAwait(false);

        if (gameFile != null)
        {
            gameFiles.Add(gameFile);
        }

        return gameFiles;
    }

    /// <summary>
    /// Calculates hashes for every file inside an archive using SharpCompress. When the
    /// archive cannot be read by SharpCompress, the 7za fallback is attempted before
    /// returning an error entry.
    /// </summary>
    /// <param name="fileInfo">Information about the archive file.</param>
    /// <param name="filePath">The path to the archive file.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>A list of <see cref="GameFile"/> objects, one per archive entry.</returns>
    private static async Task<List<GameFile>> CalculateArchiveHashesAsync(FileInfo fileInfo, string filePath,
        CancellationToken cancellationToken)
    {
        var gameFiles = new List<GameFile>();

        try
        {
            using var archive = ArchiveFactory.OpenArchive(filePath);

            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory) continue;

                cancellationToken.ThrowIfCancellationRequested();

                var entryKey = entry.Key ?? string.Empty;

                // Create algorithm instances once per archive entry
                using var crc32 = new Crc32Algorithm();
                using var md5 = MD5.Create();
                using var sha1 = SHA1.Create();
                using var sha256 = SHA256.Create();

                try
                {
                    await using var entryStream = entry.OpenEntryStream();
                    var gameFile = await ProcessStreamAsync(
                        entryStream,
                        entryKey,
                        crc32, md5, sha1, sha256,
                        cancellationToken,
                        entry.Size).ConfigureAwait(false);

                    if (gameFile != null)
                    {
                        // Track the original archive filename for proper DAT generation
                        gameFile.ArchiveFileName = fileInfo.Name;
                        gameFiles.Add(gameFile);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception entryEx) when (!FileSystemHelper.IsDiskFullError(entryEx))
                {
                    // Individual entry is corrupted - a user-environment issue, not an app bug
                    LoggerService.LogInfo("HashCalculator",
                        $"Archive entry extraction failed for '{entryKey}' in archive '{fileInfo.Name}': {entryEx.Message}");
                    gameFiles.Add(CreateArchiveErrorGameFile(entryKey, entry.Size,
                        "This file is corrupted or damaged within the archive"));
                }
            }

            return gameFiles;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // SharpCompress could not read the archive. Try the 7za fallback before failing.
            var fallbackResult = await TryCalculateArchiveHashesWithSevenZipAsync(fileInfo, filePath, ex,
                cancellationToken).ConfigureAwait(false);
            if (fallbackResult != null)
            {
                return fallbackResult;
            }

            if (FileSystemHelper.IsDiskFullError(ex))
            {
                LoggerService.LogException("HashCalculator", ex,
                    $"Archive processing failed for file '{fileInfo.Name}' - DISK FULL");
            }
            else
            {
                LoggerService.LogInfo("HashCalculator",
                    $"Could not read archive '{fileInfo.Name}': {ex.Message}");
            }

            // Return an error object for the archive itself so the UI knows extraction failed.
            // We do NOT hash the container anymore.
            return
            [
                CreateArchiveErrorGameFile(fileInfo.Name, GetFileSizeSafe(fileInfo),
                    "The archive file appears to be corrupted, incomplete, or in an unsupported format. The file may be damaged or not a valid archive.")
            ];
        }
    }

    /// <summary>
    /// Extracts an archive with 7za and calculates hashes for the extracted files.
    /// </summary>
    /// <param name="fileInfo">Information about the archive file.</param>
    /// <param name="filePath">The path to the archive file.</param>
    /// <param name="primaryException">The SharpCompress failure that triggered the fallback.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>The hash results, or null when the fallback could not read the archive.</returns>
    private static async Task<List<GameFile>?> TryCalculateArchiveHashesWithSevenZipAsync(FileInfo fileInfo,
        string filePath, Exception primaryException, CancellationToken cancellationToken)
    {
        if (!SevenZipProcess.IsAvailable || FileSystemHelper.IsDiskFullError(primaryException))
        {
            return null;
        }

        var tempDir = TempDirectoryHelper.CreateTempDirectory("romvalidator_7za");
        try
        {
            var extracted =
                await SevenZipProcess.ExtractArchiveAsync(filePath, tempDir, cancellationToken).ConfigureAwait(false);
            if (!extracted)
            {
                return null;
            }

            var extractedFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            if (extractedFiles.Length == 0)
            {
                return null;
            }

            LoggerService.LogInfo("HashCalculator",
                $"Archive '{fileInfo.Name}' was read using the 7za fallback.");

            var gameFiles = new List<GameFile>(extractedFiles.Length);
            foreach (var extractedFile in extractedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var crc32 = new Crc32Algorithm();
                using var md5 = MD5.Create();
                using var sha1 = SHA1.Create();
                using var sha256 = SHA256.Create();

                var gameFile = await ProcessFileAsync(extractedFile, new FileInfo(extractedFile),
                    crc32, md5, sha1, sha256, cancellationToken).ConfigureAwait(false);

                if (gameFile != null)
                {
                    gameFile.FileName = Path.GetRelativePath(tempDir, extractedFile).Replace('\\', '/');
                    gameFile.GameName = Path.GetFileNameWithoutExtension(gameFile.FileName);
                    gameFile.ArchiveFileName = fileInfo.Name;
                    gameFiles.Add(gameFile);
                }
            }

            return gameFiles;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception fallbackEx)
        {
            LoggerService.LogInfo("HashCalculator",
                $"7za fallback failed for archive '{fileInfo.Name}': {fallbackEx.Message}");
            return null;
        }
        finally
        {
            await TempDirectoryHelper.CleanupTempDirectoryAsync(tempDir).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates an error result for an archive or archive entry.
    /// </summary>
    /// <param name="fileName">The archive or entry name.</param>
    /// <param name="fileSize">The uncompressed size in bytes.</param>
    /// <param name="errorMessage">The user-facing error message.</param>
    /// <returns>A <see cref="GameFile"/> marked with error hashes.</returns>
    private static GameFile CreateArchiveErrorGameFile(string fileName, long fileSize, string errorMessage)
    {
        return new GameFile
        {
            FileName = fileName,
            GameName = Path.GetFileNameWithoutExtension(fileName),
            FileSize = fileSize,
            ErrorMessage = errorMessage,
            IsUserError = true,
            Crc32 = "ERROR",
            Md5 = "ERROR",
            Sha1 = "ERROR",
            Sha256 = "ERROR"
        };
    }

    private static async Task<GameFile?> ProcessFileAsync(
        string filePath,
        FileInfo fileInfo,
        Crc32Algorithm crc32, MD5 md5, SHA1 sha1, SHA256 sha256,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var fileStream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
            return await ProcessStreamAsync(
                fileStream,
                fileInfo.Name,
                crc32, md5, sha1, sha256,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex) when (IsAccessDeniedError(ex))
        {
            // A file locked by another process is a user-environment condition, not an
            // application bug: log at Information level so no bug report is sent.
            LoggerService.LogInfo("HashCalculator",
                $"File '{fileInfo.Name}' is locked or access denied: {ex.Message}");
            return new GameFile
            {
                FileName = fileInfo.Name,
                GameName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileSize = GetFileSizeSafe(fileInfo),
                ErrorMessage = "File is locked or access denied",
                IsUserError = true,
                Crc32 = "ERROR",
                Md5 = "ERROR",
                Sha1 = "ERROR",
                Sha256 = "ERROR"
            };
        }
        catch (Exception ex)
        {
            LoggerService.LogException("HashCalculator", ex, $"Error processing file '{fileInfo.Name}'");
            return new GameFile
            {
                FileName = fileInfo.Name,
                GameName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileSize = GetFileSizeSafe(fileInfo),
                ErrorMessage = ex.Message,
                Crc32 = "ERROR",
                Md5 = "ERROR",
                Sha1 = "ERROR",
                Sha256 = "ERROR"
            };
        }
    }

    /// <summary>
    /// Safely reads the file length, returning 0 when the file no longer exists or
    /// the length cannot be read (e.g. the path is a directory). This prevents the
    /// error-handling path itself from throwing.
    /// </summary>
    /// <param name="fileInfo">The file information to inspect.</param>
    /// <returns>The file size in bytes, or 0 when unavailable.</returns>
    private static long GetFileSizeSafe(FileInfo fileInfo)
    {
        try
        {
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("HashCalculator",
                $"Could not read size for '{fileInfo.Name}': {ex.Message}");
            return 0;
        }
    }

    private static async Task<GameFile?> ProcessStreamAsync(
        Stream stream,
        string fileName,
        Crc32Algorithm crc32, MD5 md5, SHA1 sha1, SHA256 sha256,
        CancellationToken cancellationToken,
        long? knownSize = null)
    {
        // Use the size reported by the archive when the stream itself cannot provide it
        long fileSize = knownSize ?? 0;
        if (knownSize == null)
        {
            try
            {
                if (stream.CanSeek)
                {
                    fileSize = stream.Length;
                }
            }
            catch (NotSupportedException)
            {
                // Length not supported, leave as 0
            }
        }

        var gameFile = new GameFile
        {
            FileName = fileName,
            GameName = Path.GetFileNameWithoutExtension(fileName),
            FileSize = fileSize
        };

        const int maxRetries = 3;
        var retryDelay = InitialRetryDelayMs;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Reinitialize algorithms for each retry attempt
                crc32.Initialize();
                md5.Initialize();
                sha1.Initialize();
                sha256.Initialize();

                // Use array instead of Span to allow usage across await boundaries
                HashAlgorithm[] algorithms = [crc32, md5, sha1, sha256];

                var buffer = new byte[BufferSize];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    foreach (var algorithm in algorithms)
                    {
                        algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }
                }

                foreach (var algorithm in algorithms)
                {
                    algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                }

                // Batch hex conversions using local function
                gameFile.Crc32 = ToHexLower(crc32.Hash);
                gameFile.Md5 = ToHexLower(md5.Hash);
                gameFile.Sha1 = ToHexLower(sha1.Hash);
                gameFile.Sha256 = ToHexLower(sha256.Hash);

                return gameFile;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException ex) when (ex.HResult == ErrorCrc)
            {
                // Win32 ERROR_CRC: the data cannot be read from the medium (bad sectors,
                // corrupted file, failing disk). Retrying will not help and this is a
                // user hardware/environment issue, not an app bug.
                LoggerService.LogInfo("HashCalculator", $"CRC read error for file '{fileName}': {ex.Message}");
                gameFile.ErrorMessage =
                    "The file cannot be read: data error (CRC). The file or the disk it is stored on may be corrupted.";
                gameFile.Crc32 = "ERROR";
                gameFile.Md5 = "ERROR";
                gameFile.Sha1 = "ERROR";
                gameFile.Sha256 = "ERROR";
                return gameFile;
            }
            catch (IOException ex) when (IsAccessDeniedError(ex))
            {
                if (attempt == maxRetries - 1)
                {
                    LoggerService.LogInfo("HashCalculator",
                        $"File '{fileName}' is locked or access denied after {maxRetries} attempts");
                    gameFile.ErrorMessage = "File is locked or access denied after retries";
                    return gameFile;
                }

                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                retryDelay *= 2; // Exponential backoff

                // Reset stream position for retry (only works for seekable streams like FileStream)
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                else
                {
                    // Archive streams don't support seeking, so we can't retry
                    LoggerService.LogInfo("HashCalculator",
                        $"File access error for '{fileName}' on a non-seekable stream");
                    gameFile.ErrorMessage = "File access error (non-seekable stream)";
                    return gameFile;
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException("HashCalculator", ex, $"Error hashing stream for file '{fileName}'");
                gameFile.ErrorMessage = ex.Message;
                gameFile.Crc32 = "ERROR";
                gameFile.Md5 = "ERROR";
                gameFile.Sha1 = "ERROR";
                gameFile.Sha256 = "ERROR";
                return gameFile;
            }
        }

        LoggerService.LogException("HashCalculator",
            new InvalidOperationException("Retry loop exceeded max attempts without returning or throwing"),
            $"Unexpected exit from retry loop while hashing '{fileName}'");
        gameFile.ErrorMessage = "Unexpected error during hash calculation";
        return gameFile;
    }

    /// <summary>
    /// Determines whether the specified file name has a supported archive extension
    /// (.zip, .7z, or .rar).
    /// </summary>
    /// <param name="fileName">The file name (or path) to test.</param>
    /// <returns>True when the file is treated as an archive.</returns>
    public static bool IsArchiveFile(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return SArchiveExtensionRegex.IsMatch(fileName);
    }

    // Local function for efficient hex conversion
    private static string ToHexLower(byte[]? hash)
    {
        return hash == null ? string.Empty : Convert.ToHexStringLower(hash);
    }

    private static bool IsAccessDeniedError(IOException ex)
    {
        // Prefer native error codes (language-independent):
        // 32 = ERROR_SHARING_VIOLATION, 33 = ERROR_LOCK_VIOLATION.
        // Falls back to message text for non-Win32 IOExceptions (e.g. from archive readers).
        if (ex.InnerException is Win32Exception { NativeErrorCode: 32 or 33 })
        {
            return true;
        }

        return ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("the process cannot access the file", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\.(zip|7z|rar)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 2000, "en-US")]
    private static partial Regex MyRegex();
}
