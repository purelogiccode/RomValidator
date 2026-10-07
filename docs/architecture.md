---
title: Architecture
parent: Reference
nav_order: 4
description: "Project structure, services, threading model and the logging pipeline."
---

# Architecture

ROM Validator is a .NET 10 WPF application. The code is organized into a small UI layer, a
service layer, and plain data models.

## High-level structure

```
App (composition root)
├── MainWindow
│   ├── ValidatePage        (validation workflow)
│   ├── GenerateDatPage     (DAT generation workflow)
│   ├── AboutWindow
│   └── DuplicateFilesWindow
└── Services
    ├── HashCalculator      hashing of files and archives
    ├── ArchiveService      SharpCompress read/extract/create helpers
    ├── SevenZipProcess     bundled 7za fallback
    ├── TempDirectoryHelper temp folders, disk space, cleanup retries
    ├── LoggerService       Serilog facade
    ├── BugReportService    bug report API client
    ├── BugReportSink       Serilog sink that forwards Warning+
    ├── ApplicationStatsService  anonymous usage stats
    ├── GitHubVersionChecker     update check
    ├── ScreenshotService   F8 screenshots
    ├── AppPaths            application data folders
    └── FileSystemHelper    shared error classification
```

## Composition root

`App` creates the long-lived services (`BugReportService`, `GitHubVersionChecker`,
`ApplicationStatsService`), configures Serilog, registers global exception handlers and the
application-wide F8 shortcut, and disposes everything on exit. `MainWindow` receives the
services through the `App` instance so that only one HTTP client per service exists.

## Service layer

| Service | Responsibility |
|:--------|:---------------|
| `HashCalculator` | Computes CRC32/MD5/SHA-1/SHA-256 for files and archive entries. Streams data, retries locked files, detects disk-full and CRC errors |
| `ArchiveService` | SharpCompress wrappers: list entries, total size, extract, create zip and 7z |
| `SevenZipProcess` | Runs the bundled `7za.exe` / `7za_arm64.exe` as a fallback for extraction and creation |
| `TempDirectoryHelper` | Creates temp folders, tracks them, deletes with retries and background retry timer, estimates archive sizes |
| `LoggerService` | Facade over Serilog; never throws; used by every component |
| `BugReportService` | Builds and sends bug reports (environment/error/exception sections) |
| `BugReportSink` | Serilog sink forwarding Warning+ events; filters its own components and allows one in-flight report |
| `GitHubVersionChecker` | Queries the latest GitHub release and compares versions |
| `ScreenshotService` | Captures the active window to PNG |
| `AppPaths` | Resolves the AppData, logs and screenshot folders |

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
- UI updates are marshalled to the dispatcher. Progress and status updates are fire-and-forget
  with logging on failure.
- Cancellation uses `CancellationTokenSource` per operation, plus a global token source that is
  cancelled when the application exits.

## Logging pipeline

```
LoggerService ──► Serilog
                    ├── Debug sink   (Visual Studio output)
                    ├── File sink    (%LOCALAPPDATA%\ROM Validator\Logs, 7 days)
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
  a friendly dialog.

## Archive stack

1. **SharpCompress 0.50.4** is the primary reader/writer for zip and 7z, and the reader for rar.
2. **7za 26.03** (bundled, architecture-matched) is the fallback when SharpCompress cannot read
   or create an archive.
3. RAR archives are converted to zip when an internal rename is required, because RAR creation is
   not supported.

## Related pages

- [Build from Source](build-from-source)
- [CI/CD](ci-cd)
- [Screenshots and Application Data](screenshots-and-data)
