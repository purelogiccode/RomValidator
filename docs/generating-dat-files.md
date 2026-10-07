---
title: Generating DAT Files
parent: User Guide
nav_order: 2
description: "Hash a folder of ROMs and export a No-Intro compliant DAT file."
---

# Generating DAT Files

The **Generate DAT** tab creates a new No-Intro compliant DAT file from a folder of ROMs. Every
file is hashed with CRC32, MD5, SHA-1 and SHA-256, and duplicates are detected automatically.

## Step by step

1. Click **Select Folder** and choose the folder that contains your ROMs.
2. Optionally fill in the DAT header fields:

   | Field | Description |
   |:------|:------------|
   | Name | Name written to the DAT file header |
   | Description | Description written to the DAT file header |
   | Author | Author written to the DAT file header |

3. Click **Start Hashing**. The progress bar and the file counter show the current state, and the
   results grid is filled in as files are processed.
4. Click **Stop** at any time to cancel; results collected so far are kept.
5. Click **Export DAT** to save the generated `.dat` file. The suggested file name contains the
   sanitized DAT name and a timestamp.

Use **Reset** to clear all results and start over.

## Results grid

Each processed file is listed with its file name, size and the four hashes:

| Column | Meaning |
|:-------|:--------|
| Filename | Name of the hashed file |
| Size | File size in bytes |
| CRC32 | CRC-32 checksum |
| MD5 | MD5 hash |
| SHA1 | SHA-1 hash |
| SHA256 | SHA-256 hash |

Hover a hash cell to see the error message when a file could not be hashed.

## Duplicate detection

Files that share the same SHA-256 hash but have different file names are grouped as duplicates.
When duplicates are found, a [Duplicate Files](duplicate-detection) window lists every group so
you can clean up the input folder before exporting the DAT file.

## Export

The exported file is a No-Intro XML datafile containing one `game` entry per hashed file with
its size and hashes. If the file was read from an archive, the archive name is preserved in the
DAT so the entry maps back to the container.

> **Tip**
>
> Remove duplicate files before exporting. Duplicate entries with the same hashes but different
> names make the DAT harder to use for validation.

## Related pages

- [Duplicate Detection](duplicate-detection)
- [Archive Support](archive-support)
- [Supported Formats](supported-formats)
