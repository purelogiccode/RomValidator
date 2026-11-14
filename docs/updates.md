---
title: Updates
parent: User Guide
nav_order: 6
description: "How the automatic GitHub update check works."
---

# Updates

ROM Validator checks the [GitHub releases](https://github.com/purelogiccode/RomValidator/releases)
page for a newer version every time it starts.

## How it works

1. After the main window is shown, the application queries the latest GitHub release.
2. The release tag is compared with the running version. Tags look like `release_3.0.0`
   (older `v2.8.0` tags are also understood).
3. When a newer version is available:
   - the status bar shows the new version,
   - a dialog asks whether you want to open the release page.
4. If you choose **Yes**, your default browser opens the release page where you can download the
   bundle for your platform: the Windows `.zip`, the Linux `.tar.gz`, or the macOS `.dmg`.

If the check fails because there is no network connection, the application logs it locally and
continues without interrupting your work. The check is cancelled automatically when you close
the application.

## Updating manually

1. Download the bundle for your platform and architecture from the
   [latest release](https://github.com/purelogiccode/RomValidator/releases/latest).
2. Close ROM Validator.
3. Replace the old installation:
   - **Windows** - extract the zip over your existing installation folder, replacing the old
     files.
   - **Linux** - extract the archive over your existing installation folder.
   - **macOS** - open the `.dmg` and replace `ROM Validator.app` in your Applications folder.
4. Start ROM Validator again.

Your settings are not stored in the installation folder, so nothing is lost. Logs and fallback
screenshots remain in the per-user data folder (`%LOCALAPPDATA%\ROM Validator` on Windows,
`~/Library/Application Support/ROM Validator` on macOS, `~/.local/share/ROM Validator` on Linux).

## Related pages

- [Getting Started](getting-started)
- [Release Notes](release-notes)
