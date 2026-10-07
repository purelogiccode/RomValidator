---
title: Duplicate Detection
parent: User Guide
nav_order: 3
description: "Understand the duplicate report shown during DAT generation."
---

# Duplicate Detection

During [DAT generation](generating-dat-files) every file is hashed. When two or more files share
the same SHA-256 hash but have different file names, they are the same ROM data stored twice.
ROM Validator groups them and shows the **Duplicate Files Detected** window.

## What the window shows

| Column | Meaning |
|:-------|:--------|
| SHA256 Hash | The hash shared by every file in the group |
| Filenames | Comma-separated list of the file names in the group |

The header shows the total number of duplicate groups. A warning banner reminds you that the
files contain identical data under different names.

## Why duplicates matter

- The generated DAT file would contain multiple entries with identical hashes and different
  names, which makes validation ambiguous.
- Duplicate files waste disk space and can hide an incomplete or incorrectly named collection.

## What to do

1. Review each group and decide which file name is correct.
2. Delete or move the extra copies in File Explorer.
3. Re-run **Start Hashing** so the results reflect the cleaned folder.
4. Export the DAT file.

> **Note**
>
> Duplicate detection is based on SHA-256. Files that are similar but not byte-identical are not
> reported as duplicates.

## Related pages

- [Generating DAT Files](generating-dat-files)
- [Validating ROMs](validating-roms)
