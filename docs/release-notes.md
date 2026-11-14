---
title: Release Notes
nav_order: 7
description: "Release history for ROM Validator. Full details on GitHub Releases."
---

# Release Notes

The full release history, with commit-level detail and downloadable bundles, is available on the
[GitHub Releases](https://github.com/purelogiccode/RomValidator/releases) page.

## 3.0.0

### Highlights

- **Avalonia port**: the user interface was ported to Avalonia 12.1.6 with the Fluent theme and a
  custom professional dark palette.
- **Cross-platform desktop**: ROM Validator now runs on Windows, Linux and macOS on x64 and ARM64,
  and targets .NET 11 (`net11.0`).
- **Self-contained bundles**: Windows `.zip`, Linux `.tar.gz` and macOS `.dmg` bundles include the
  runtime, so no separate .NET installation is required. macOS bundles contain `ROM Validator.app`.
- **Archive fallback per platform**: Windows bundles keep `7za.exe` / `7za_arm64.exe`; Linux and
  macOS use a system-installed `7zz`, `7z` or `7za` when one is present.
- **Themed dialogs and native pickers**: message dialogs are rendered by the application, and file
  and folder selection uses the Avalonia storage provider.
- **Activity log on the right**: the validation screen uses a two-column layout with a full-height,
  resizable live log panel next to the validation controls and DAT information.
- **Fewer false bug reports**: corrupt archive entries are surfaced as per-file user errors instead
  of being forwarded to the bug-report API.
- **Validation flow fixes**: the completion summary is no longer shown when validation never
  started, and the F8 screenshot shortcut works on all application windows.
- **Platform-aware data folders**: logs and fallback screenshots use the per-user data directory
  of each operating system.
- **CI on three platforms**: builds and tests run on Windows, Linux and macOS, and `jb inspectcode`
  must report zero findings.

### Notes

- Primary screenshots still go to a `Screenshot` folder next to the executable when it is writable.
- The temporary-folder/zip launch check remains a Windows-specific guard.

## 2.9.2

### Highlights

- **Donate button**: a new **Donate** button in the header (next to **About**) opens the
  [donation page](https://www.purelogiccode.com/donate) in your default browser.

### Notes

- Windows bundles from this era; superseded by the cross-platform 3.0.0 bundles.

## 2.9.1

### Highlights

- **Reliable release verification**: fixed a race in the bug-report sink test that could make the
  release verification job fail intermittently.
- **Generated release notes**: the publish job now checks out the full repository history before
  creating the GitHub release, so `gh release create --generate-notes` works reliably.

### Notes

- Maintenance release; the application behavior is unchanged from 2.9.0.

## 2.9.0

### Highlights

- **Single-file executables**: each bundle contains one `RomValidator.exe`.
- **Complete bundles**: the zips now include `LICENSE.txt`, `ReadMe.md` and `WhatsNew.md`
  alongside the bundled 7-Zip fallback executables.
- **Update-checker compatible names**: `release_<version>_win-x64.zip` and
  `release_<version>_win-arm64.zip`, matching the tag convention parsed by the in-app update
  check.
- **Safer archive repacking**: internal renames target the file whose hash matches the DAT entry
  (not just the first file), folder structure is preserved, and converting a `.rar` to `.zip`
  never overwrites an existing zip.
- **Fewer false bug reports**: files locked by another process are logged locally instead of
  being reported as bugs.
- **UTF-16/UTF-32 XML DAT files** are accepted again, and disk-full errors while moving files
  stop the validation queue cleanly.
- **Documentation** published to GitHub Pages and mirrored to the GitHub Wiki, both with side
  navigation, plus CI and an automated release workflow.

### Notes

- RAR archives are converted to ZIP when their internal filenames must be renamed.

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
- **Fluent-styled dark theme**.

### Notes

- Only No-Intro XML DAT files are accepted; ClrMamePro, MAME and binary formats are rejected with
  a clear message.

## Earlier releases

See [GitHub Releases](https://github.com/purelogiccode/RomValidator/releases) for release notes
of earlier versions.

## Related pages

- [Updates](updates)
- [Getting Started](getting-started)
