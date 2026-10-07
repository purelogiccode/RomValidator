---
title: Supported Formats
parent: Reference
nav_order: 1
description: "Supported DAT formats, archive formats and hash algorithms."
---

# Supported Formats

## DAT files

| Format | Supported | Notes |
|:-------|:----------|:------|
| No-Intro XML (`.dat`, `.xml`) | Yes | The only supported validation reference |
| ClrMamePro DAT | No | Detected and rejected with an explanatory message |
| MAME XML | No | Detected and rejected |
| Binary or unknown files | No | Detected and rejected |
| Zip files selected as DAT | No | Detected and rejected |

A valid No-Intro XML DAT contains a `datafile` element with `header` metadata and a list of
`game` elements, each with one or more `rom` entries (name, size, CRC, MD5, SHA-1, SHA-256).

Download DAT files from [no-intro.org](https://no-intro.org/) or use the **Download Dat Files**
button in the Validate ROMs tab.

## Archive formats

| Format | Read | Create / repack |
|:-------|:-----|:----------------|
| `.zip` | Yes | Yes (SharpCompress) |
| `.7z` | Yes | Yes (SharpCompress, fallback 7za) |
| `.rar` | Yes | Converted to zip when repacking |

See [Archive Support](archive-support) for details.

## Hash algorithms

| Algorithm | Length | Used for |
|:----------|:-------|:---------|
| CRC32 | 8 hex characters | Fast validation and legacy DAT files |
| MD5 | 32 hex characters | Validation |
| SHA-1 | 40 hex characters | Validation |
| SHA-256 | 64 hex characters | Validation and duplicate detection |

Validation compares every hash that is present in the DAT entry, together with the file size.
The generated DAT file contains all four hashes.

## File system requirements

- Windows file system (NTFS or ReFS recommended).
- Read access to the ROM folder; write access when moving, renaming, deleting or repacking.
- Temporary disk space for archive repacking.

## Related pages

- [Validating ROMs](validating-roms)
- [Generating DAT Files](generating-dat-files)
