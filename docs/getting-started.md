---
title: Getting Started
nav_order: 2
description: "Install ROM Validator, run it for the first time, and validate your first folder."
---

# Getting Started

This page walks you through installing ROM Validator and completing your first validation.

## Requirements

| Component | Requirement |
|:----------|:------------|
| Operating system | Windows 10/11, Linux, or macOS 11 or later |
| Architecture | x64 or ARM64 |
| Runtime | None - the release bundles are self-contained |

> On Windows, the application refuses to run from a temporary folder or directly from a zip
> archive. Always extract the release bundle to a permanent folder before launching it.

## Install

1. Open the [latest release](https://github.com/purelogiccode/RomValidator/releases/latest).
2. Download the bundle that matches your platform and architecture:
   - `release_<version>_win-x64.zip` or `release_<version>_win-arm64.zip` for Windows
   - `release_<version>_linux-x64.tar.gz` or `release_<version>_linux-arm64.tar.gz` for Linux
   - `release_<version>_osx-x64.dmg` or `release_<version>_osx-arm64.dmg` for macOS
3. Install it:
   - **Windows** - extract the zip to a permanent folder, for example
     `C:\Program Files\ROM Validator` or `D:\Tools\RomValidator`, and run `RomValidator.exe`.
   - **Linux** - extract the archive to a permanent folder, make the executable startable if
     needed (`chmod +x RomValidator`), and run `./RomValidator`.
   - **macOS** - open the `.dmg` and drag `ROM Validator.app` to your Applications folder, then
     launch it. The app is ad-hoc signed, so macOS may ask you to confirm the first launch.

Every bundle is self-contained, so no separate .NET runtime has to be installed. Windows bundles
also include the 7-Zip fallback executables (`7za.exe` / `7za_arm64.exe`), and every bundle
contains `LICENSE.txt`, `ReadMe.md` and `WhatsNew.md`. On Linux and macOS the archive fallback
uses a system-installed `7zz`, `7z` or `7za` when one is available; otherwise SharpCompress is used
on its own.

## First run

When the application starts you will see:

- the **Validate ROMs** and **Generate DAT** tabs in the header,
- the **App Data**, **Donate**, **About** and **Exit** buttons on the right,
- a status bar at the bottom that reports what the application is doing.

The application checks GitHub for a newer release on every launch. If an update is available you
are asked whether you want to open the release page. See [Updates](updates).

## Quick start: validate a folder

1. Open the **Validate ROMs** tab.
2. Click **Browse...** next to **ROMs Folder to Scan** and select the folder that contains your
   ROM files.
3. Click **Browse...** next to **DAT File** and select a No-Intro XML DAT file
   (`.dat` or `.xml`). DAT files can be downloaded from [no-intro.org](https://no-intro.org/)
   or with the **Download Dat Files** button.
4. Keep the default options:
   - *Move successful items to `_success` folder*
   - *Move failed/unknown items to `_fail` folder*
   - *Automatically rename files when hash matches but filename differs*
5. Click **Start Validation** and watch the live log and statistics.

When the run finishes, the results are organized in subfolders of the folder you selected:

| Folder | Contents |
|:-------|:---------|
| `_success` | Files that matched the DAT file |
| `_fail` | Files that did not match or are unknown |
| `_duplicate` | Files that matched but whose target name already existed |

See [Validating ROMs](validating-roms) for the full workflow.

## Quick start: generate a DAT file

1. Open the **Generate DAT** tab.
2. Click **Select Folder** and choose the folder that contains your ROMs.
3. Optionally fill in **Name**, **Description** and **Author** for the DAT header.
4. Click **Start Hashing**, then **Export DAT** and choose where to save the `.dat` file.

See [Generating DAT Files](generating-dat-files) for details.

## Where your data lives

| Data | Windows | macOS | Linux |
|:-----|:--------|:------|:------|
| Logs | `%LOCALAPPDATA%\ROM Validator\Logs` | `~/Library/Application Support/ROM Validator/Logs` | `~/.local/share/ROM Validator/Logs` (XDG data directory when set) |
| Fallback screenshots | `%LOCALAPPDATA%\ROM Validator\Screenshot` | `~/Library/Application Support/ROM Validator/Screenshot` | `~/.local/share/ROM Validator/Screenshot` |
| Screenshots (primary) | `Screenshot` next to `RomValidator.exe` | `Screenshot` next to the app | `Screenshot` next to `RomValidator` |

Click **App Data** in the header to open the per-user folder in your file manager. See
[Screenshots and Application Data](screenshots-and-data).

## Next steps

- [Validating ROMs](validating-roms)
- [Generating DAT Files](generating-dat-files)
- [Archive Support](archive-support)
- [Troubleshooting](troubleshooting)
