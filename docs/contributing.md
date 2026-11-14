---
title: Contributing
nav_order: 6
description: "How to report bugs, request features and submit changes to ROM Validator."
---

# Contributing

Thanks for your interest in improving ROM Validator.

## Reporting bugs

1. Reproduce the problem so it is captured in the log.
2. Open a [GitHub issue](https://github.com/purelogiccode/RomValidator/issues) and include:
   - what you did,
   - what you expected to happen,
   - what actually happened,
   - the application version (see the About window),
   - the relevant log file from the `Logs` folder of the per-user data directory
     (`%LOCALAPPDATA%\ROM Validator\Logs` on Windows,
     `~/Library/Application Support/ROM Validator/Logs` on macOS,
     `~/.local/share/ROM Validator/Logs` on Linux),
   - a screenshot (press **F8**) when it helps,
   - your operating system and architecture.

Warnings, errors and fatal events are forwarded automatically to the developer, but an issue
with context is still the best way to get a fix.

## Requesting features

Open an issue and describe the use case, not only the solution. Include examples of ROM
collections or DAT files when relevant.

## Submitting changes

1. Fork the repository and create a topic branch.
2. Follow the existing code style:
   - the build must stay at zero warnings (Meziantou and Roslynator analyzers are enabled),
   - public types and members require XML documentation,
   - keep methods small and prefer existing helpers (`FileSystemHelper`, `AppPaths`,
     `ArchiveService`, `TempDirectoryHelper`).
3. Add or update tests in `RomValidator.Tests` for behavior changes.
4. Run the full test suite:

   ```powershell
   dotnet test CSharp_RomValidator.sln --configuration Release --nologo
   ```

5. Run the JetBrains inspection over the whole solution and fix every finding:

   ```powershell
   jb inspectcode CSharp_RomValidator.sln `
     --output="$env:TEMP\romvalidator-inspectcode.sarif" `
     --format=Sarif `
     --severity=WARNING `
     --verbosity=WARN
   ```

6. Update the documentation in `docs/` when user-visible behavior changes.
7. Open a pull request that describes the change and references the related issue.

The solution builds on Windows, Linux and macOS, and CI runs the build, the test suite and the
inspection on those platforms. Keep UI code portable: use Avalonia XAML and controls, the Avalonia
storage provider for file and folder pickers, `Dispatcher.UIThread` for UI updates, and
`Process.Start` with `UseShellExecute = true` for shell integration. Do not add WPF, WinForms or
Windows-only APIs.

## Documentation

The documentation site is built from the `docs/` folder with Jekyll (just-the-docs theme) and
mirrored to the repository wiki. Add front matter to every page:

```yaml
---
title: Page Title
parent: User Guide   # optional, groups the page in the navigation
nav_order: 1
---
```

Use relative links without the `.md` extension (for example `[FAQ](faq)`) so the links work on
GitHub Pages, in the wiki and in the repository.

## License

By contributing you agree that your contributions are licensed under the GPLv3, the license of
this project.
