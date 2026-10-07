---
title: Release Notes
nav_order: 7
description: "Release history for ROM Validator. Full details on GitHub Releases."
---

# Release Notes

The full release history, with commit-level detail and downloadable zips, is available on the
[GitHub Releases](https://github.com/purelogiccode/RomValidator/releases) page.

## 2.8.0

### Highlights

- **Archive support** for `.zip`, `.7z` and `.rar` using SharpCompress, with the bundled 7-Zip
  26.03 standalone executables (`7za.exe`, `7za_arm64.exe`) as a fallback for reading and
  creating archives.
- **DAT generation** from a folder of ROMs with CRC32, MD5, SHA-1 and SHA-256, duplicate
  detection and a No-Intro compliant XML export.
- **Validation** against No-Intro XML DAT files with automatic renaming when the hash matches a
  different file name, including files inside archives.
- **Safe result organization**: `_success`, `_fail` and `_duplicate` folders; deletion is opt-in
  and confirmed.
- **Automatic update checks** against GitHub releases.
- **Diagnostics**: rolling log files, F8 screenshots, and automatic bug reports for Warning,
  Error and Fatal events.
- **Fluent dark theme** built on WPF-UI.

### Notes

- The release zip is framework-dependent and requires the .NET 10 Desktop Runtime.
- Only No-Intro XML DAT files are accepted; ClrMamePro, MAME and binary formats are rejected with
  a clear message.

## Earlier releases

See [GitHub Releases](https://github.com/purelogiccode/RomValidator/releases) for release notes
of earlier versions.

## Related pages

- [Updates](updates)
- [Getting Started](getting-started)
