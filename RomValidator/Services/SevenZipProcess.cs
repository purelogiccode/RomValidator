using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace RomValidator.Services;

/// <summary>
/// Fallback archive helper that invokes the standalone 7-Zip command-line tool
/// (7za.exe or 7za_arm64.exe) for operations SharpCompress cannot perform, such as
/// creating 7z archives or reading archives with unsupported compression methods.
/// </summary>
public static class SevenZipProcess
{
    private const string X64ExecutableName = "7za.exe";
    private const string Arm64ExecutableName = "7za_arm64.exe";

    /// <summary>
    /// Gets the 7za executable matching the current process architecture, or null
    /// when no executable is present next to the application.
    /// </summary>
    /// <returns>The full path to the 7za executable, or null when it is missing.</returns>
    public static string? GetExecutablePath()
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var isArm64 = RuntimeInformation.OSArchitecture == Architecture.Arm64;

            var preferred = Path.Combine(baseDirectory, isArm64 ? Arm64ExecutableName : X64ExecutableName);
            if (File.Exists(preferred))
            {
                return preferred;
            }

            var alternate = Path.Combine(baseDirectory, isArm64 ? X64ExecutableName : Arm64ExecutableName);
            return File.Exists(alternate) ? alternate : null;
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("SevenZipProcess", $"Could not resolve 7za executable: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether a 7za fallback executable is available.
    /// </summary>
    public static bool IsAvailable => GetExecutablePath() != null;

    /// <summary>
    /// Extracts an archive to the destination directory using 7za.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="destinationDirectory">Directory to extract the files into.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>True when extraction succeeded.</returns>
    public static async Task<bool> ExtractArchiveAsync(string archivePath, string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var executable = GetExecutablePath();
        if (executable == null)
        {
            LoggerService.LogInfo("SevenZipProcess",
                "7za fallback executable was not found; archive extraction skipped.");
            return false;
        }

        Directory.CreateDirectory(destinationDirectory);
        var arguments = $"x -y -bd -o\"{destinationDirectory}\" \"{archivePath}\"";
        var exitCode = await RunAsync(executable, arguments, null, cancellationToken).ConfigureAwait(false);
        return exitCode == 0;
    }

    /// <summary>
    /// Creates an archive containing every file in the staging directory (entry
    /// names are the file names at the root of the staging directory) using 7za.
    /// </summary>
    /// <param name="stagingDirectory">Directory containing the files to archive.</param>
    /// <param name="outputArchivePath">The archive file to create.</param>
    /// <param name="sevenZip">True to create a 7z archive, false to create a zip archive.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>True when the archive was created.</returns>
    public static async Task<bool> CreateArchiveAsync(string stagingDirectory, string outputArchivePath,
        bool sevenZip, CancellationToken cancellationToken)
    {
        var executable = GetExecutablePath();
        if (executable == null)
        {
            LoggerService.LogInfo("SevenZipProcess",
                "7za fallback executable was not found; archive creation skipped.");
            return false;
        }

        var archiveType = sevenZip ? "7z" : "zip";
        var arguments = $"a -t{archiveType} -mx=5 -y -bd \"{outputArchivePath}\" *";
        var exitCode = await RunAsync(executable, arguments, stagingDirectory, cancellationToken).ConfigureAwait(false);
        return exitCode == 0;
    }

    /// <summary>
    /// Gets the total uncompressed size of an archive using 7za (fallback for
    /// archives SharpCompress cannot read).
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <returns>The total size in bytes, or null when it cannot be determined.</returns>
    public static long? GetUncompressedSize(string archivePath)
    {
        var executable = GetExecutablePath();
        if (executable == null)
        {
            return null;
        }

        try
        {
            var output = Run(executable, $"l -slt -y \"{archivePath}\"", null);
            return output == null ? null : ParseUncompressedSize(output);
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("SevenZipProcess", $"Could not list archive '{archivePath}' with 7za: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses the total uncompressed size from 7za "l -slt" output.
    /// </summary>
    /// <param name="listingOutput">The raw standard output of "7za l -slt".</param>
    /// <returns>The summed uncompressed size of all non-directory entries.</returns>
    internal static long ParseUncompressedSize(string listingOutput)
    {
        ArgumentNullException.ThrowIfNull(listingOutput);

        long total = 0;
        var isDirectoryEntry = false;
        var size = 0L;
        var hasSize = false;

        foreach (var rawLine in listingOutput.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                // End of an entry block: flush the pending size
                if (hasSize && !isDirectoryEntry)
                {
                    total += size;
                }

                isDirectoryEntry = false;
                size = 0;
                hasSize = false;
                continue;
            }

            if (line.StartsWith("Attributes = ", StringComparison.Ordinal))
            {
                isDirectoryEntry = line.Contains('D', StringComparison.Ordinal);
            }
            else if (line.StartsWith("Size = ", StringComparison.Ordinal) &&
                     long.TryParse(line.AsSpan("Size = ".Length), NumberStyles.Integer, CultureInfo.InvariantCulture,
                         out var parsed))
            {
                size = parsed;
                hasSize = true;
            }
        }

        if (hasSize && !isDirectoryEntry)
        {
            total += size;
        }

        return total;
    }

    private static async Task<int> RunAsync(string executable, string arguments, string? workingDirectory,
        CancellationToken cancellationToken)
    {
        using var process = StartProcess(executable, arguments, workingDirectory,
            out var standardOutputTask, out var standardErrorTask);
        if (process == null)
        {
            return -1;
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            LoggerService.LogInfo("SevenZipProcess",
                $"7za exited with code {process.ExitCode}: {standardError} {standardOutput}");
        }

        return process.ExitCode;
    }

    private static string? Run(string executable, string arguments, string? workingDirectory)
    {
        using var process = StartProcess(executable, arguments, workingDirectory,
            out var standardOutputTask, out _);
        if (process == null)
        {
            return null;
        }

        process.WaitForExit();
        return process.ExitCode == 0 ? standardOutputTask.GetAwaiter().GetResult() : null;
    }

    private static Process? StartProcess(string executable, string arguments, string? workingDirectory,
        out Task<string> standardOutputTask, out Task<string> standardErrorTask)
    {
        var startInfo = new ProcessStartInfo(executable, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        var process = Process.Start(startInfo);
        if (process == null)
        {
            standardOutputTask = Task.FromResult(string.Empty);
            standardErrorTask = Task.FromResult(string.Empty);
            return null;
        }

        standardOutputTask = process.StandardOutput.ReadToEndAsync();
        standardErrorTask = process.StandardError.ReadToEndAsync();
        return process;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogDebug("SevenZipProcess", $"Could not terminate 7za process: {ex.Message}");
        }
    }
}
