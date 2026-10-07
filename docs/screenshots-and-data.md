---
title: Screenshots and Application Data
parent: User Guide
nav_order: 5
description: "F8 screenshots, log files, the App Data folder, and what is sent in bug reports."
---

# Screenshots and Application Data

## Screenshots

Press **F8** anywhere in the application to capture the active window. Screenshots are saved as
PNG files and the status bar shows the full path of the saved file.

| Priority | Location |
|:---------|:---------|
| Primary | `Screenshot\` next to `RomValidator.exe` |
| Fallback | `%LOCALAPPDATA%\ROM Validator\Screenshot` |

The fallback is used automatically when the application folder is read-only, for example when
the application is installed under `C:\Program Files`.

## Application data folder

Click **App Data** in the header to open the per-user folder in File Explorer:

```
%LOCALAPPDATA%\ROM Validator\
├── Logs\         rolling Serilog log files (7 days retained)
└── Screenshot\   fallback screenshots
```

The folder is always writable, even for installed applications.

## Log files

ROM Validator writes structured logs with [Serilog](https://serilog.net/):

- one rolling file per day in `Logs\`, retained for seven days,
- Debug and higher events are written locally,
- **Warning, Error and Fatal** events are also forwarded to the bug report API.

Log entries include the component name, so you can search for `HashCalculator`,
`ArchiveRepack`, `ValidatePage`, `Cleanup` and similar values when investigating an issue.

## Automatic bug reports

Unhandled exceptions, logged errors and warnings are sent to the bug report API. A report
contains:

- environment details (application version, OS version, architecture, processor count,
  base directory, temp path),
- the error context and any additional information,
- the full exception chain with stack traces and HResult values.

> **Privacy**
>
> Bug reports do not include your ROM files, file contents or the names of files you validate.
> Reports are used only to diagnose application problems. Reports are rate-limited and only one
> report is sent at a time to avoid flooding the API.

## Application statistics

On startup the application records an anonymous usage event (application id and version) so the
developer can see which versions are in use. No personal data or file information is sent.

## Related pages

- [Troubleshooting](troubleshooting)
- [Updates](updates)
