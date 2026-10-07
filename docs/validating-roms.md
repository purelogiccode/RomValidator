---
title: Validating ROMs
parent: User Guide
nav_order: 1
description: "Validate a ROM folder against a No-Intro DAT file, rename matches, and organize results."
---

# Validating ROMs

The **Validate ROMs** tab compares every file in a folder against a No-Intro XML DAT file and
organizes the results. Validation combines filename matching with CRC32, MD5, SHA-1 and SHA-256
hash checks.

## Before you start

- A folder that contains the files you want to validate.
- A No-Intro XML DAT file (`.dat` or `.xml`). Only No-Intro XML is supported; ClrMamePro,
  MAME, binary and zip files are detected and rejected with a clear message.
- Enough free space if you enable moving files and your ROMs are inside archives
  (archives are extracted to a temporary directory for hashing).

## Step by step

1. **ROMs Folder to Scan** - click **Browse...** and select the folder. Subfolders are not
   scanned recursively; hidden, system and reparse-point (cloud placeholder) files are skipped.
2. **DAT File** - click **Browse...** and select the No-Intro XML DAT file. The DAT information
   panel shows the name, description, version, author, homepage, URL and the number of ROM
   entries as soon as the file is loaded.
3. Choose the file handling options (see below).
4. Click **Start Validation**.

The live log shows one line per file with the result, and the statistics panel updates as the run
progresses. Click **Cancel** at any time to stop the run safely.

## File handling options

| Option | Default | Effect |
|:-------|:--------|:-------|
| Move successful items to `_success` folder | On | Valid ROMs are moved to `<folder>\_success` |
| Move failed/unknown items to `_fail` folder | On | Invalid or unknown files are moved to `<folder>\_fail` |
| Permanently delete failed/unknown files | Off | Deletes invalid/unknown files. **Irreversible** - a confirmation dialog is shown before the run starts |
| Automatically rename files when hash matches but filename differs | On | Renames a file to the DAT name when its hash matches, including files inside archives |

> **Warning**
>
> *Move failed/unknown* and *Permanently delete failed/unknown* are mutually exclusive.
> Deletion never happens unless you explicitly enable it and confirm the dialog.

## How a file is validated

1. The file name is looked up in the DAT.
2. If the name is not found and renaming is enabled, the file is hashed and looked up by
   CRC32, MD5, SHA-1 and SHA-256.
3. If a hash match is found, the file is optionally renamed to the DAT name. When the file is an
   archive, the file inside the archive is renamed too (RAR archives are repacked as ZIP).
4. The final hashes are compared against the DAT entry. A file is successful when the size and
   all available hashes match.

If a rename target already exists, the source file is treated as a duplicate and moved to the
`_duplicate` folder at the root of the scanned folder.

## Result folders

| Folder | Meaning |
|:-------|:--------|
| `_success` | The file matched the DAT entry |
| `_fail` | The file did not match, or the archive could not be read |
| `_duplicate` | The file matched, but a file with the target name already existed |

Folders are created only when the corresponding option is enabled (or when a duplicate is found).

## Statistics

| Field | Meaning |
|:------|:--------|
| Total Files | Number of files discovered in the selected folder |
| Success | Files that matched the DAT |
| Failed | Files that did not match, including corrupt archives |
| Unknown | Files that are not present in the DAT |
| Renamed | Files renamed to match the DAT |
| Deleted | Files permanently deleted |
| Duplicates | Files moved to `_duplicate` |
| Processing Time | Elapsed time for the run |

## Notes and edge cases

- **Locked files**: files that are in use are retried automatically. If the file stays locked,
  a dialog lets you close the other program and retry.
- **Cloud-only placeholders** (for example OneDrive files that are not downloaded) are reported
  as user errors instead of being hashed.
- **Corrupt or unsupported archives** are reported per archive or per entry and do not stop the
  run. See [Archive Support](archive-support).
- **Disk full**: archive extraction checks for free space before it starts and stops the queue
  when no drive has enough room.
- **Cancelling** stops the queue after the current file and keeps everything already processed.

## Related pages

- [Archive Support](archive-support)
- [Supported Formats](supported-formats)
- [Troubleshooting](troubleshooting)
