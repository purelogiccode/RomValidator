using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RomValidator.Models;
using RomValidator.Models.NoIntro;
using RomValidator.Services;
using RomValidator.Views;

namespace RomValidator.Pages;

/// <summary>
/// Page that hashes all files in a folder and exports the results as a
/// No-Intro compliant DAT file, reporting duplicate ROMs along the way.
/// </summary>
public partial class GenerateDatPage : UserControl, IDisposable
{
    // Maximum time a regex match is allowed to run before timing out
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(2);

    // Compiled regex for sanitizing filenames (Issue C fix)
    private static readonly Regex InvalidFileNameCharsRegex =
        new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled, RegexMatchTimeout);

    private const int MaxUiDisplayItems = 500; // Limit UI list size to prevent lag (Issue 8 fix)

    private readonly MainWindow _mainWindow;
    private CancellationTokenSource? _cts;
    private readonly Lock _ctsLock = new();
    private readonly ObservableCollection<GameFile> _fileDataCollection = [];
    private readonly List<GameFile> _processedFilesList = [];
    private int _processedFileCount;
    private readonly Lock _operationLock = new();

    // Batching for UI updates (Issue A fix)
    private readonly List<GameFile> _uiUpdateBuffer = [];
    private DispatcherTimer? _uiUpdateTimer;

    private int _discoveredFilesCount;
    private int _archiveExpansionCount;

    /// <summary>Initializes a new instance for the XAML designer.</summary>
    public GenerateDatPage()
    {
        _mainWindow = null!;
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the GenerateDatPage class.
    /// </summary>
    /// <param name="mainWindow">The main window instance for status updates.</param>
    public GenerateDatPage(MainWindow mainWindow)
    {
        try
        {
            _mainWindow = mainWindow;
            InitializeComponent();

            SelectFolderButton.Click += async (_, _) => await SelectFolderAsync();
            StartButton.Click += async (_, _) => await StartHashingAsync();
            StopButton.Click += (_, _) => StopHashing();
            ResetButton.Click += (_, _) => ResetFromUi();
            ExportDatButton.Click += async (_, _) => await ExportDatAsync();

            HashListView.ItemsSource = _fileDataCollection;
            UpdateFileCountText(0);

            // Initialize UI update timer for batching (Issue A fix)
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _uiUpdateTimer.Tick += UIUpdateTimer_Tick;

            _mainWindow.UpdateStatusBarMessage("Ready to generate DAT file.");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error initializing GenerateDatPage");
            throw;
        }
    }

    private async Task SelectFolderAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
            {
                return;
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select folder to hash",
                AllowMultiple = false
            });

            var folderPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            ResetPage();
            FolderTextBox.Text = folderPath;
            NameTextBox.Text = new DirectoryInfo(folderPath).Name;
            StartButton.IsEnabled = true;
            _mainWindow.UpdateStatusBarMessage($"Folder selected: {folderPath}");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error selecting folder");
        }
    }

    private async Task StartHashingAsync()
    {
        CancellationTokenSource? operationCts = null; // Capture variable for this operation

        try
        {
            if (string.IsNullOrEmpty(FolderTextBox.Text) || !Directory.Exists(FolderTextBox.Text))
            {
                await DialogService.ShowAsync(_mainWindow, "Please select a valid folder first.",
                    "Invalid Folder", DialogButtons.Ok, DialogIcon.Warning);
                return;
            }

            ClearAll();

            // Cancel previous operation and create new CancellationTokenSource
            lock (_ctsLock)
            {
                _cts?.Cancel(); // Signal cancellation to any running operation
                _cts = new CancellationTokenSource();
                operationCts = _cts; // Capture the instance for this operation
            }

            // Batch UI updates instead of invoking for each file (Issue A fix)
            var progress = new Progress<GameFile>(update =>
            {
                lock (_uiUpdateBuffer)
                {
                    _uiUpdateBuffer.Add(update);
                }

                // Start timer if not already running
                if (_uiUpdateTimer is { IsEnabled: false })
                {
                    _uiUpdateTimer.Start();
                }
            });

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            ExportDatButton.IsEnabled = false;
            // Disable folder selection and reset while hashing: ResetPage() cancels and
            // disposes the running operation's CTS, which would leave the stale operation
            // popping a spurious "cancelled" dialog after a new folder was picked.
            SelectFolderButton.IsEnabled = false;
            ResetButton.IsEnabled = false;
            await _mainWindow.UpdateStatusBarMessageAsync("Hashing in progress...", operationCts.Token);

            // Use the captured CTS instance
            await HashFilesAsync(FolderTextBox.Text, progress, operationCts.Token);

            // Sync progress bar to actual count so it reaches 100%
            HashProgressBar.Maximum = _processedFileCount;
            HashProgressBar.Value = _processedFileCount;
            ProgressText.Text = $"{_processedFileCount} / {_processedFileCount}";

            if (!operationCts.IsCancellationRequested)
            {
                await DialogService.ShowAsync(_mainWindow,
                    $"Hashing complete! {_processedFileCount} files processed.", "Complete",
                    DialogButtons.Ok, DialogIcon.Info);
                lock (_operationLock)
                {
                    ExportDatButton.IsEnabled = _processedFilesList.Count > 0;
                }

                await _mainWindow.UpdateStatusBarMessageAsync(
                    $"Hashing complete. {_processedFileCount} files processed.", operationCts.Token);

                // Automatically trigger save dialog after hash calculation
                lock (_operationLock)
                {
                    if (_processedFilesList.Count > 0)
                    {
                        Dispatcher.UIThread.Post(() => _ = ExportDatAsync());
                    }
                }
            }
            else
            {
                await DialogService.ShowAsync(_mainWindow, "Operation was cancelled", "Cancelled",
                    DialogButtons.Ok, DialogIcon.Info);
                ExportDatButton.IsEnabled = false;
                await _mainWindow.UpdateStatusBarMessageAsync("Hashing cancelled.", operationCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            await DialogService.ShowAsync(_mainWindow, "Operation was cancelled", "Cancelled",
                DialogButtons.Ok, DialogIcon.Info);
            ExportDatButton.IsEnabled = false;
            if (operationCts != null)
                await _mainWindow.UpdateStatusBarMessageAsync("Hashing cancelled.", operationCts.Token);
        }
        catch (Exception ex)
        {
            _ = _mainWindow.BugReportService.SendBugReportAsync(
                $"Error during hashing operation for folder: {FolderTextBox.Text}", ex);
            await DialogService.ShowAsync(_mainWindow, $"An error occurred: {ex.Message}", "Error",
                DialogButtons.Ok, DialogIcon.Error);
            if (operationCts != null)
                await _mainWindow.UpdateStatusBarMessageAsync("Error during hashing.", operationCts.Token);
        }
        finally
        {
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            SelectFolderButton.IsEnabled = true;
            ResetButton.IsEnabled = true;

            // Dispose only this operation's CTS instance
            lock (_ctsLock)
            {
                // Only clear field if it's still pointing to our instance
                if (_cts == operationCts)
                {
                    _cts = null;
                }
            }

            operationCts?.Dispose();
        }
    }

    // Timer tick handler for batch UI updates (Issue A fix)
    private void UIUpdateTimer_Tick(object? sender, EventArgs e)
    {
        lock (_uiUpdateBuffer)
        {
            if (_uiUpdateBuffer.Count > 0)
            {
                var totalProcessed = _processedFilesList.Count;

                foreach (var item in _uiUpdateBuffer)
                {
                    _fileDataCollection.Add(item);
                    if (_fileDataCollection.Count > MaxUiDisplayItems)
                        _fileDataCollection.RemoveAt(0);
                }

                UpdateFileCountText(totalProcessed);
                if (HashProgressBar.Maximum > 0)
                {
                    HashProgressBar.Value = totalProcessed;
                    ProgressText.Text = $"{totalProcessed} / {(int)HashProgressBar.Maximum}";
                }

                _uiUpdateBuffer.Clear();
            }

            _uiUpdateTimer?.Stop();
        }
    }

    private void StopHashing()
    {
        try
        {
            lock (_ctsLock)
            {
                _cts?.Cancel();
            }

            StopButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error stopping operation");
        }
    }

    private async Task HashFilesAsync(string folderPath, IProgress<GameFile> progress,
        CancellationToken cancellationToken)
    {
        var totalRomCount = 0;
        _discoveredFilesCount = 0;
        _archiveExpansionCount = 0;
        var lastUiUpdate = DateTime.UtcNow;
        const int uiUpdateIntervalMs = 100; // Throttle UI updates to every 100ms (Issue 5 fix)

        // Track duplicates during processing
        var hashToFilenames = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var duplicateGroups = 0;

        // Use EnumerationOptions to ignore inaccessible folders and files (Issue 4 fix)
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false, // Changed to false for top directory only
            AttributesToSkip =
                FileAttributes.Hidden | FileAttributes.System |
                FileAttributes.ReparsePoint // Skip cloud-only placeholders (e.g. OneDrive)
        };

        // Start a background task to count files so we can estimate progress bar Max
        // The actual Maximum will be set atomically in the main loop (Issue 2 & 3 fix)
        _ = Task.Run(
            () => CountFilesInBackground(folderPath, enumerationOptions, ref _discoveredFilesCount, cancellationToken),
            cancellationToken);

        // Stream the files using EnumerationOptions to skip inaccessible items (Issue 4 fix)
        var fileEnumerable = Directory.EnumerateFiles(folderPath, "*", enumerationOptions);

        // Sequential processing
        foreach (var filePath in fileEnumerable)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var gameFiles =
                await HashCalculator.CalculateHashesAsync(filePath, cancellationToken);
            var romsFromFile = gameFiles.Count;

            // Read the discovered count from the background task (do NOT increment here —
            // CountFilesInBackground already handles that)
            var currentDiscovered = _discoveredFilesCount;

            // Track how many extra items come from archives (files inside archives minus the archive file itself)
            if (romsFromFile > 1)
            {
                Interlocked.Add(ref _archiveExpansionCount, romsFromFile - 1);
            }

            var currentExpansion = _archiveExpansionCount; // Read is atomic for int

            // Throttle UI updates to prevent flooding the UI thread (Issue 5 fix)
            // Only dispatch to UI thread every 100ms to avoid queuing thousands of operations
            var now = DateTime.UtcNow;
            if ((now - lastUiUpdate).TotalMilliseconds >= uiUpdateIntervalMs)
            {
                lastUiUpdate = now;
                var newMaximum = currentDiscovered + currentExpansion;
                await Dispatcher.UIThread.InvokeAsync(() => HashProgressBar.Maximum = newMaximum);
            }

            foreach (var gameFile in gameFiles)
            {
                if (gameFile.ErrorMessage != null)
                {
                    // Log error to bug report service or UI.
                    // Skip errors that are caused by the user's file or environment
                    // (corrupt/unsupported archives, cloud-only placeholders, locked files)
                    // as these are not application bugs.
                    if (!gameFile.IsUserError &&
                        !string.Equals(gameFile.ErrorMessage, "File is locked or access denied after retries",
                            StringComparison.Ordinal))
                    {
                        _ = _mainWindow.BugReportService.SendBugReportAsync(
                            $"Error hashing file {filePath}: {gameFile.ErrorMessage}", null, null, cancellationToken);
                    }
                }
                else if (!string.IsNullOrEmpty(gameFile.Sha256))
                {
                    // Check for duplicates (same hash, different filename)
                    lock (hashToFilenames)
                    {
                        if (!hashToFilenames.TryGetValue(gameFile.Sha256, out var filenames))
                        {
                            filenames = new List<string>();
                            hashToFilenames[gameFile.Sha256] = filenames;
                        }

                        // Only add if not already in list (case-insensitive)
                        if (!filenames.Any(f =>
                                string.Equals(f, gameFile.FileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            filenames.Add(gameFile.FileName);

                            // If we have more than one filename for this hash, log a warning
                            if (filenames.Count == 2) // First time we detect a duplicate for this hash
                            {
                                duplicateGroups++;
                            }

                            if (filenames.Count > 1)
                            {
                                // Duplicate ROMs are user data, not application bugs; keep
                                // them at Information level so no bug report is sent.
                                LoggerService.LogInfo("DAT Generation",
                                    $"Duplicate ROM detected: Hash {gameFile.Sha256} has multiple filenames: {string.Join(", ", filenames)}");
                            }
                        }
                    }
                }

                lock (_operationLock)
                {
                    _processedFilesList.Add(gameFile);
                }

                progress.Report(gameFile);
                Interlocked.Increment(ref totalRomCount);
            }
        }

        _processedFileCount = totalRomCount;

        // Show duplicate warning after hashing completes
        if (duplicateGroups > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // Filter to only include hashes with multiple filenames
                var duplicateHashToFilenames = hashToFilenames
                    .Where(static kvp => kvp.Value.Count > 1)
                    .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

                var duplicateWindow = new DuplicateFilesWindow();
                duplicateWindow.SetDuplicateData(duplicateHashToFilenames, "Duplicate ROMs Detected");
                await duplicateWindow.ShowDialog(_mainWindow);
            });
        }
    }

    private static string SanitizeFileName(string name)
    {
        return InvalidFileNameCharsRegex.Replace(name, "_");
    }

    private async Task ExportDatAsync()
    {
        try
        {
            bool hasFiles;
            lock (_operationLock)
            {
                hasFiles = _processedFilesList.Count > 0;
            }

            if (!hasFiles)
            {
                await DialogService.ShowAsync(_mainWindow, "No files have been processed to export.",
                    "No Data", DialogButtons.Ok, DialogIcon.Warning);
                return;
            }

            var sanitizedName = SanitizeFileName(NameTextBox.Text ?? string.Empty);
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = "Exported-DAT";
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
            {
                return;
            }

            var saveFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save DAT File",
                SuggestedFileName = $"{sanitizedName} ({DateTime.Now:yyyyMMdd-HHmmss}).dat",
                DefaultExtension = "dat",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType("DAT file") { Patterns = ["*.dat"] },
                    FilePickerFileTypes.All
                ]
            });

            var savePath = saveFile?.TryGetLocalPath();
            if (string.IsNullOrEmpty(savePath)) return;

            try
            {
                var dataFile = new Datafile
                {
                    Header = new Header
                    {
                        Name = NameTextBox.Text ?? string.Empty,
                        Description = DescriptionTextBox.Text ?? string.Empty,
                        Author = AuthorTextBox.Text ?? string.Empty,
                        Version = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                    }
                };

                List<GameFile> filesToExport;
                lock (_operationLock)
                {
                    filesToExport = new List<GameFile>(_processedFilesList);
                }

                // Detect filename collisions: same filename but different hashes
                var filenameCollisions = filesToExport
                    .Where(static file => file.ErrorMessage == null)
                    .GroupBy(static file => file.FileName, StringComparer.OrdinalIgnoreCase)
                    .Where(static group =>
                        group.Select(static f => f.Sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                    .ToList();

                if (filenameCollisions.Count > 0)
                {
                    var collisionMessage = new StringBuilder();
                    collisionMessage.AppendLine("WARNING: Filename collisions detected!");
                    collisionMessage.AppendLine(
                        "The following ROM filenames have multiple different ROMs (different hashes):");
                    collisionMessage.AppendLine();
                    foreach (var collision in filenameCollisions)
                    {
                        collisionMessage.AppendLine(CultureInfo.InvariantCulture, $"  '{collision.Key}':");
                        foreach (var file in collision)
                        {
                            collisionMessage.AppendLine(CultureInfo.InvariantCulture,
                                $"    - From: {file.ArchiveFileName ?? "(loose file)"}");
                            collisionMessage.AppendLine(CultureInfo.InvariantCulture, $"      SHA256: {file.Sha256}");
                        }

                        collisionMessage.AppendLine();
                    }

                    collisionMessage.AppendLine(
                        "These files will overwrite each other in the DAT. Consider renaming them.");

                    _mainWindow.UpdateStatusBarMessage(
                        $"Warning: {filenameCollisions.Count} filename collision(s) detected!");
                    await DialogService.ShowAsync(_mainWindow, collisionMessage.ToString(),
                        "Filename Collisions Detected", DialogButtons.Ok, DialogIcon.Warning);
                }

                // Note: Duplicate ROM notifications are already shown after hash calculation completes
                // (see HashFilesAsync method lines 323-341), so we don't show them again here

                // Group files by GameName to create proper No-Intro DAT format
                dataFile.Games.AddRange(filesToExport
                    .Where(static file => file.ErrorMessage == null)
                    .GroupBy(static file => file.GameName, StringComparer.OrdinalIgnoreCase)
                    .Select(static group => new Game
                    {
                        Name = group.Key,
                        Description = group.Key,
                        Roms = group.Select(static file => new Rom
                        {
                            Name = file.FileName,
                            Size = file.FileSize,
                            Crc = file.Crc32,
                            Md5 = file.Md5,
                            Sha1 = file.Sha1,
                            Sha256 = file.Sha256
                        }).ToList()
                    }));

                var serializer = new XmlSerializer(typeof(Datafile));
                var settings = new XmlWriterSettings
                    { Indent = true, IndentChars = "\t", Encoding = new UTF8Encoding(false), Async = true };

                _mainWindow.UpdateStatusBarMessage("Serializing and saving DAT file...");
                await using var writer = XmlWriter.Create(savePath, settings);

                // Serialize with proper No-Intro namespaces
                await Task.Run(() =>
                {
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    serializer.Serialize(writer, dataFile, namespaces);
                }); // Offload sync serialization (Issue 12 fix)

                var result = await DialogService.ShowAsync(_mainWindow,
                    "DAT file exported successfully! Would you like to open it?",
                    "Export Complete", DialogButtons.YesNo, DialogIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                            { FileName = savePath, UseShellExecute = true });
                    }
                    catch (Exception openEx) when (openEx is System.ComponentModel.Win32Exception or InvalidOperationException)
                    {
                        // The export itself succeeded, but the operating system has no
                        // application associated with .dat files.
                        _mainWindow.UpdateStatusBarMessage(
                            "DAT exported, but no app is associated with .dat files.");
                        await DialogService.ShowAsync(_mainWindow,
                            "The DAT file was exported successfully, but the operating system could not open it automatically.\n\n" +
                            "This usually means no application is associated with .dat files.\n\n" +
                            $"The file was saved to:\n{savePath}",
                            "Export Complete", DialogButtons.Ok, DialogIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _mainWindow.BugReportService.SendBugReportAsync("Error exporting DAT file.", ex);
                await DialogService.ShowAsync(_mainWindow, $"Error exporting file: {ex.Message}", "Export Error",
                    DialogButtons.Ok, DialogIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _ = _mainWindow.BugReportService.SendBugReportAsync("Error exporting DAT file.", ex);
            await DialogService.ShowAsync(_mainWindow, $"Error exporting file: {ex.Message}", "Export Error",
                DialogButtons.Ok, DialogIcon.Error);
        }
    }

    private void UpdateFileCountText(int count)
    {
        FileCountText.Text = count == 0 ? "No files processed" : $"{count} files processed";
    }

    private void ResetFromUi()
    {
        try
        {
            ResetPage();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error resetting page");
        }
    }

    /// <summary>
    /// Resets the page to its initial state, clearing all data and stopping ongoing operations.
    /// </summary>
    private void ResetPage()
    {
        try
        {
            lock (_ctsLock)
            {
                _uiUpdateTimer?.Stop();
                lock (_uiUpdateBuffer)
                {
                    _uiUpdateBuffer.Clear();
                }

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }

            ClearAll();
            FolderTextBox.Text = string.Empty;
            NameTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            AuthorTextBox.Text = string.Empty;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            ExportDatButton.IsEnabled = false;
            _mainWindow.UpdateStatusBarMessage("Ready to generate DAT file.");
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error during ResetPage");
        }
    }

    private void ClearAll()
    {
        _fileDataCollection.Clear();
        lock (_operationLock)
        {
            _processedFilesList.Clear();
        }

        HashProgressBar.Value = 0;
        HashProgressBar.Maximum = 0; // Reset Maximum to prevent visual issues on subsequent runs (Issue 3 fix)
        ProgressText.Text = "";
        _processedFileCount = 0;
        UpdateFileCountText(0);
        ExportDatButton.IsEnabled = false;
    }

    private static void CountFilesInBackground(string folderPath, EnumerationOptions options, ref int counter,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var _ in Directory.EnumerateFiles(folderPath, "*", options))
            {
                if (cancellationToken.IsCancellationRequested) break;

                Interlocked.Increment(ref counter);
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error counting files in background");
        }
    }

    /// <summary>
    /// Disposes of resources used by the GenerateDatPage.
    /// Stops timers and cancels ongoing operations.
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Stop and dispose timer properly
            if (_uiUpdateTimer != null)
            {
                _uiUpdateTimer.Stop();
                _uiUpdateTimer.Tick -= UIUpdateTimer_Tick;
                _uiUpdateTimer = null;
            }

            // Clear UI buffer to release references
            lock (_uiUpdateBuffer)
            {
                _uiUpdateBuffer.Clear();
            }

            // Cancel and dispose CTS with error handling
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogException("GenerateDatPage", ex, "Error during Dispose");
        }

        GC.SuppressFinalize(this);
    }
}