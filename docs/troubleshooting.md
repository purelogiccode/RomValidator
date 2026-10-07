---
title: Troubleshooting
parent: Reference
nav_order: 2
description: "Solutions for common ROM Validator problems."
---

# Troubleshooting

## The application will not start and shows "Invalid Location"

ROM Validator refuses to run from a temporary folder or directly from a zip archive. Extract the
release zip to a permanent folder (for example `C:\Program Files\ROM Validator`) and run
`RomValidator.exe` from there.

## "The .NET Desktop Runtime is required"

The release zip is framework-dependent. Install the .NET 10 Desktop Runtime from
[dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) and start the
application again.

## A DAT file is rejected

Only No-Intro XML DAT files are supported. ClrMamePro, MAME, binary and zip files are rejected
on purpose. Download a No-Intro XML DAT from [no-intro.org](https://no-intro.org/).

## Validation reports files as "unknown"

- The file is not present in the selected DAT file.
- The file name does not match and the hash does not match either (a different dump, a modified
  file, or a bad rip).
- The file is inside an archive whose internal name differs and renaming is disabled.

## Files are reported as "failed" but look correct

- Check the log line for the mismatch reason (size, CRC32, MD5, SHA-1 or SHA-256).
- A corrupt or partially downloaded archive is reported as failed; re-download it.
- Cloud-only placeholder files are reported as user errors; download them locally first.

## "File is locked" dialog

Another program (antivirus scanner, cloud sync client, emulator) is holding the file. Close the
other program and click **Retry**. The application retries automatically before showing the
dialog.

## A RAR archive became a ZIP file

RAR creation is not supported, so an archive that needed an internal rename was repacked as zip.
This is expected behavior. See [Archive Support](archive-support).

## Archive processing stops with a disk space message

Repacking needs temporary free space (roughly twice the uncompressed size plus twice the archive
size). Free space on one of your drives or disable renaming inside archives, then run again.

## Screenshots are not in the application folder

When the application folder is read-only (for example under `C:\Program Files`), screenshots go
to `%LOCALAPPDATA%\ROM Validator\Screenshot`. Click **App Data** to open that folder.

## Finding logs

Logs are stored in `%LOCALAPPDATA%\ROM Validator\Logs` and retained for seven days. Include the
relevant log file when you
[open an issue](https://github.com/purelogiccode/RomValidator/issues).

## Reporting a bug

1. Reproduce the problem once so it is captured in the log.
2. Open an issue and describe what you did, what you expected, and what happened.
3. Attach the log file and, if useful, an F8 screenshot.

Warnings, errors and fatal events are also forwarded automatically to the developer, but a
GitHub issue with context is always welcome.

## Related pages

- [Screenshots and Application Data](screenshots-and-data)
- [Archive Support](archive-support)
- [Supported Formats](supported-formats)
