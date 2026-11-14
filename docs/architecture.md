---
title: Architecture
parent: Reference
nav_order: 4
description: "Project structure, services, threading model and the logging pipeline."
---

# Architecture

ROM Validator is a .NET 11 desktop application built with Avalonia 12.1.6 that runs on Windows,
Linux and macOS. The code is organized into a small UI layer, a service layer, and plain data
models.

## High-level structure

```
Program (Avalonia entry point - Program.cs)
└── App (composition root, Fluent theme and dark palette resources)
    ├── MainWindow
    │   ├── ValidatePage        (validation workflow, UserControl)
    │   ├── GenerateDatPage     (DAT generation workflow, UserControl)
    │   ├── AboutWindow
    │   ├── DuplicateFilesWindow
    │   └── Views/MessageDialog themed dialogs via DialogService
    └── Services
        ├── HashCalculator      hashing of files and archives
        ├── ArchiveService      SharpCompress read/extract/create helpers
        ├── SevenZipProcess     7-Zip fallback (bundled or system)
        ├── TempDirectoryHelper temp folders, disk space, cleanup retries
        ├── LoggerService       Serilog facade
        ├── BugReportService    bug report API client
        ├── BugReportSink       Serilog sink that forwards Warning+
        ├── ApplicationStatsService  anonymous usage stats
        ├── GitHubVersionChecker     update check
        ├── ScreenshotService   F8 screenshots
        ├── WindowThemeService  dark native title bar on Windows
        ├── AppPaths            application data folders
        └── FileSystemHelper    shared error classification
```

## Composition root

`Program.Main` builds the Avalonia `AppBuilder` (`UsePlatformDetect`, Inter font) and starts the
classic desktop lifetime. `App` loads the XAML resources, creates the long-lived services
(`BugReportService`, `GitHubVersionChecker`, `ApplicationStatsService`), configures Serilog,
registers global exception handlers and the application-wide F8 shortcut, and disposes everything
on exit. `MainWindow` receives the services through the `App` instance so that only one HTTP client
per service exists. Native file and folder pickers use the Avalonia `StorageProvider`; message
dialogs are shown through `DialogService` and rendered by the themed `MessageDialog` window.

The validation screen uses a two-column layout: validation controls and DAT metadata on the left,
and a full-height live activity log on the right. A `GridSplitter` lets the user resize the log
panel, while statistics and progress remain full-width below both columns.

## Service layer

| Service | Responsibility |
|:--------|:---------------|
| `HashCalculator` | Computes CRC32/MD5/SHA-1/SHA-256 for files and archive entries. Streams data, retries locked files, detects disk-full and CRC errors |
| `ArchiveService` | SharpCompress wrappers: list entries, total size, extract, create zip and 7z |
| `SevenZipProcess` | Runs a 7-Zip fallback executable: the bundled `7za.exe` / `7za_arm64.exe` on Windows, or a system `7zz` / `7z` / `7za` on Linux and macOS |
| `TempDirectoryHelper` | Creates temp folders, tracks them, deletes with retries and background retry timer, estimates archive sizes |
| `LoggerService` | Facade over Serilog; never throws; used by every component |
| `BugReportService` | Builds and sends bug reports (environment/error/exception sections) |
| `BugReportSink` | Serilog sink forwarding Warning+ events; filters its own components and allows one in-flight report |
| `GitHubVersionChecker` | Queries the latest GitHub release and compares versions |
| `ScreenshotService` | Captures the active Avalonia window to PNG |
| `WindowThemeService` | Applies the dark native title bar on Windows; no-op on Linux and macOS |
| `AppPaths` | Resolves the platform-specific data, logs and screenshot folders |

## Models

- `Models/NoIntro` - `Datafile`, `Header`, `Game`, `Rom`: the No-Intro XML datafile model used
  for reading and writing DAT files.
- `Models/GameFile` - hashing result for one file or archive entry.
- `Models/DuplicateGroup` - a group of files sharing a hash.
- `Models/GitHubRelease`, `Models/GitHubAsset` - GitHub API contract.
- `Models/BugReportPayload` - bug report API contract.

## Interfaces

`Interfaces/` contains the abstractions used for dependency injection and testing:
`IBugReportService`, `IApplicationStatsService` and `IGitHubVersionChecker`.

## Threading

- File validation is intentionally **sequential** to avoid race conditions when moving files to
  the shared `_success` / `_fail` / `_duplicate` folders.
- Hashing of a single file/entry is asynchronous and streams data with a 64 KB buffer.
- UI updates are marshalled to the Avalonia dispatcher (`Dispatcher.UIThread`). Progress and
  status updates are fire-and-forget with logging on failure.
- Cancellation uses `CancellationTokenSource` per operation, plus a global token source that is
  cancelled when the application exits.

## Platform support

- One target framework (`net11.0`) and six runtime identifiers: `win-x64`, `win-arm64`,
  `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.
- The Fluent theme, the custom dark palette and all windows are defined in Avalonia XAML
  (`.axaml`), so the same UI runs on every platform.
- File and folder pickers use the Avalonia storage provider; buttons that open web pages or
  folders use `Process.Start` with `UseShellExecute = true`.
- The dark native title bar and the temporary-folder/zip check are Windows-specific refinements.
  Linux and macOS rely on the window manager and the platform's normal launch behavior.
- Per-user data folders are resolved by `AppPaths`:

| Platform | Data folder |
|:---------|:------------|
| Windows | `%LOCALAPPDATA%\ROM Validator` |
| macOS | `~/Library/Application Support/ROM Validator` |
| Linux | `~/.local/share/ROM Validator` (XDG data directory when set) |

## Logging pipeline

```
LoggerService ──► Serilog
                    ├── Debug sink   (IDE output)
                    ├── File sink    (<per-user data folder>/Logs, 7 days)
                    └── BugReportSink (Warning+, one report in flight)
```

Environmental conditions (network failures, corrupt user files, locked files, cloud
placeholders) are logged at Information level so they are not forwarded as bug reports. Warnings
and errors indicate genuine application anomalies.

## Error handling strategy

- Every public entry point wraps work in try/catch and logs through `LoggerService`.
- User-environment failures (corrupt files, access denied, disk full, cloud placeholders) are
  surfaced as per-file results with an explanatory message instead of aborting the run.
- Global handlers capture unhandled dispatcher, AppDomain and task exceptions, log them and show
  a friendly themed dialog.

## Archive stack

1. **SharpCompress 0.50.9** is the primary reader/writer for zip and 7z, and the reader for rar.
2. A **7-Zip fallback** is used when SharpCompress cannot read or create an archive:
   - Windows bundles the architecture-matched `7za.exe` / `7za_arm64.exe` (7-Zip 26.03),
   - Linux and macOS look for `7zz`, `7z` or `7za` next to the application and on the `PATH`.
3. RAR archives are converted to zip when an internal rename is required, because RAR creation is
   not supported.
4. When no fallback executable is available, archive creation falls back to SharpCompress only and
   the application continues to work for zip archives and for reading all supported formats.

## Related pages

- [Build from Source](build-from-source)
- [CI/CD](ci-cd)
- [Screenshots and Application Data](screenshots-and-data)
