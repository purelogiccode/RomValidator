---
title: Release Notes
nav_order: 7
description: "Release history for ROM Validator. Full details on GitHub Releases."
---

# Release Notes

The full release history, with commit-level detail and downloadable zips, is available on the
[GitHub Releases](https://github.com/purelogiccode/RomValidator/releases) page.

## 2.9.1

### Highlights

- **Reliable release verification**: fixed a race in the bug-report sink test that could make the
  release verification job fail intermittently.
- **Generated release notes**: the publish job now checks out the full repository history before
  creating the GitHub release, so `gh release create --generate-notes` works reliably.

### Notes

- Maintenance release; the application behavior is unchanged from 2.9.0.
- The release zip is framework-dependent and requires the .NET 10 Desktop Runtime.

## 2.9.0

### Highlights

- **Single-file executables**: each bundle contains one `RomValidator.exe` (framework-dependent;
  the .NET 10 Desktop Runtime is still required).
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

- The release zip is framework-dependent and requires the .NET 10 Desktop Runtime.
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
