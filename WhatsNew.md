Release 3.0.0
2026-11-13

- User interface ported to Avalonia 12.1.6 with the Fluent theme and a custom
  professional dark palette
- ROM Validator now runs on Windows, Linux and macOS (x64 and arm64) and targets
  .NET 11 (net11.0)
- Release bundles are self-contained and no longer require a separate .NET
  runtime:
  - release_<version>_win-x64.zip and release_<version>_win-arm64.zip
  - release_<version>_linux-x64.tar.gz and release_<version>_linux-arm64.tar.gz
  - release_<version>_osx-x64.dmg and release_<version>_osx-arm64.dmg
    (each dmg contains ROM Validator.app)
- Windows bundles keep the bundled 7-Zip fallback (7za.exe / 7za_arm64.exe);
  Linux and macOS use SharpCompress and fall back to a system-installed 7zz, 7z
  or 7za when present
- Native file and folder pickers and a themed in-app message dialog replace the
  previous platform dialogs
- Logs and fallback screenshots use platform-specific per-user data folders
  (%LOCALAPPDATA% on Windows, ~/Library/Application Support on macOS, the XDG
  data directory on Linux)
- CI builds and tests on Windows, Linux and macOS and runs jb inspectcode with
  zero findings

Release 2.9.2
2026-10-07

- Added a Donate button to the header (next to About) that opens the donation
  page at https://www.purelogiccode.com/donate in the default browser
- Version bumped to 2.9.2; the Windows bundles remain single-file executables
  named release_<version>_win-x64.zip and release_<version>_win-arm64.zip

Release 2.9.1
2026-10-07

- Fixed a race in the bug-report sink test that could make the release verification
  job fail intermittently
- The release workflow now checks out the full repository history before publishing,
  so GitHub can generate the release notes automatically
- Version bumped to 2.9.1; application behavior is unchanged from 2.9.0 and the
  Windows bundles remain single-file executables named
  release_<version>_win-x64.zip and release_<version>_win-arm64.zip

Release 2.9.0
2026-10-07

- Archive engine migrated from SharpSevenZip to SharpCompress, with the bundled
  7-Zip 26.03 executables (7za.exe / 7za_arm64.exe) as a fallback for archives
  SharpCompress cannot read or create
- RAR archives are converted to ZIP when internal filenames must be renamed
  (RAR creation is not supported); existing files are never overwritten during
  the conversion
- Internal archive renames now target the file whose hash matches the DAT entry
  instead of the first file in the archive, and folder structure is preserved
  when repacking
- Files locked by another process no longer generate bug reports; they are
  logged locally and handled with retries
- UTF-16/UTF-32 encoded XML DAT files are accepted again (binary detection now
  respects byte order marks)
- Disk-full errors while moving files stop the validation queue cleanly
- Release bundles are now single-file executables (RomValidator.exe) and include
  LICENSE.txt, ReadMe.md and WhatsNew.md alongside the bundled 7-Zip binaries
- Bundle names follow the update-checker convention: release_<version>_win-x64.zip
  and release_<version>_win-arm64.zip
- Documentation published to GitHub Pages and mirrored to the GitHub Wiki, with
  side navigation on both
- Continuous integration (build + tests on every push) and an automated release
  workflow (tag release_* or v* to publish the bundles)
