---
title: Archive Support
parent: User Guide
nav_order: 4
description: "Read and repack .zip, .7z and .rar archives with SharpCompress and the 7za fallback."
---

# Archive Support

ROM Validator reads ROM files directly from archives. You do not have to extract them yourself.

## Supported archive formats

| Format | Read | Internal rename | Notes |
|:-------|:-----|:----------------|:------|
| `.zip` | Yes | Yes (repacked as zip) | |
| `.7z` | Yes | Yes (repacked as 7z) | |
| `.rar` | Yes | Yes (repacked as zip) | RAR creation is not supported by any open library, so repacked RAR archives become zip files |

Only files with these extensions are treated as archives. Everything else is hashed as a plain
file.

## How archives are processed

1. The archive is opened with **SharpCompress** and every non-directory entry is hashed.
2. If SharpCompress cannot read the archive, the bundled **7za** executable is used as a
   fallback: the archive is extracted to a temporary folder and the extracted files are hashed.
3. When a file inside an archive must be renamed to match the DAT, the archive is extracted,
   the entry is renamed and the archive is repacked:
   - zip and 7z archives are repacked with SharpCompress,
   - if SharpCompress cannot create the archive, the bundled 7za is used,
   - RAR archives are converted to zip.

Large entries are streamed; the application does not load a whole ROM into memory.

## Bundled 7-Zip executables

The application ships with standalone 7-Zip 26.03 command-line tools:

| File | Architecture |
|:-----|:-------------|
| `7za.exe` | x64 |
| `7za_arm64.exe` | ARM64 |

The correct executable is selected automatically based on the operating system architecture. If
the executable is missing, archive creation falls back to SharpCompress only and the application
continues to work for zip archives and for reading all supported formats.

## Disk space

Repacking an archive requires temporary disk space:

- the application estimates the uncompressed size of the archive and requires roughly twice that
  size plus twice the archive size,
- the default temporary drive is tried first, then the drive of the archive, then any other
  ready drive,
- if no drive has enough free space, the archive is skipped and the run stops with a clear
  message instead of failing halfway.

Temporary folders are tracked and cleaned up automatically, including a background retry for
files that are briefly locked by other programs.

## Cloud-only files

Files that are placeholders for cloud storage (for example OneDrive "online-only" files) are
reported as user errors and skipped. Download the file locally before validating.

## Corrupt archives

A corrupt or unsupported archive is reported as a failed item; the run continues with the next
file. Per-entry corruption is reported for the affected entry only.

## Related pages

- [Supported Formats](supported-formats)
- [Validating ROMs](validating-roms)
- [Troubleshooting](troubleshooting)
