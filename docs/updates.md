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
2. The release tag is compared with the running version.
3. When a newer version is available:
   - the status bar shows the new version,
   - a dialog asks whether you want to open the release page.
4. If you choose **Yes**, your default browser opens the release page where you can download the
   new zip.

If the check fails because there is no network connection, the application logs it locally and
continues without interrupting your work. The check is cancelled automatically when you close
the application.

## Updating manually

1. Download the zip for your architecture from the
   [latest release](https://github.com/purelogiccode/RomValidator/releases/latest).
2. Close ROM Validator.
3. Extract the zip over your existing installation folder, replacing the old files.
4. Start ROM Validator again.

Your settings are not stored in the installation folder, so nothing is lost. Logs and fallback
screenshots remain in `%LOCALAPPDATA%\ROM Validator`.

## Related pages

- [Getting Started](getting-started)
- [Release Notes](release-notes)
