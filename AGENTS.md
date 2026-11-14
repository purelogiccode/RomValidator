# AGENTS.md

Instructions for AI coding agents and contributors working on ROM Validator.

## Project Overview

ROM Validator is a Windows-only WPF desktop application that validates ROM files
against No-Intro DAT files and generates new DAT files from a ROM collection.

- **Framework:** .NET 11 (`net11.0-windows`), C# 15, nullable enabled
- **UI:** WPF with WPF-UI (Fluent theme)
- **Archives:** SharpCompress, with the bundled `7za.exe` / `7za_arm64.exe` as fallback
- **Logging / reporting:** Serilog; Warning and higher events are forwarded to the
  bug-report API
- **Tests:** xUnit in `RomValidator.Tests`
- **Docs:** Jekyll site in `docs/`, published to GitHub Pages and the wiki

## Repository Layout

| Path | Contents |
|:-----|:---------|
| `RomValidator/` | WPF application |
| `RomValidator/Pages/` | Validate ROMs and Generate DAT pages |
| `RomValidator/Services/` | Hashing, archives, DAT parsing, logging, temp cleanup, bug reporting |
| `RomValidator/Models/` | Domain models and DAT serialization types |
| `RomValidator.Tests/` | Unit and integration tests |
| `docs/` | User and developer documentation |
| `.github/workflows/` | CI, release and documentation workflows |

## Prerequisites

- .NET SDK 11 (pinned by `global.json`; use `rollForward` as configured)
- JetBrains ReSharper command-line tools for `jb inspectcode`:

  ```powershell
  dotnet tool install --global JetBrains.ReSharper.GlobalTools
  jb --version
  ```

## Build and Test

```powershell
dotnet restore CSharp_RomValidator.sln
dotnet build CSharp_RomValidator.sln --configuration Release --nologo
dotnet test CSharp_RomValidator.sln --configuration Release --no-build --nologo
```

The build must succeed with **zero warnings and zero errors**. Meziantou.Analyzer and
Roslynator.Analyzers are enabled, so new warnings are treated as defects.

## Mandatory Pre-Commit Checklist

**Always run `jb inspectcode` before a commit. Fix every warning and error it reports.**

1. Build and test the solution with the commands above.
2. Run the JetBrains inspection over the whole solution:

   ```powershell
   jb inspectcode CSharp_RomValidator.sln `
     --output "$env:TEMP\romvalidator-inspectcode.sarif" `
     --format=Sarif `
     --severity=WARNING `
     --verbosity=WARN
   ```

3. Verify that the inspection produced no findings:

   ```powershell
   $results = (Get-Content "$env:TEMP\romvalidator-inspectcode.sarif" -Raw | ConvertFrom-Json).runs[0].results
   if ($results.Count -gt 0) {
       $results | ForEach-Object {
           "{0}: {1} ({2})" -f $_.ruleId, $_.message.text,
               $_.locations[0].physicalLocation.artifactLocation.uri
       }
       throw "jb inspectcode reported $($results.Count) issue(s). Fix them before committing."
   }
   ```

4. Do not commit while any inspection warning or error remains.
5. Do not silence findings with `#pragma warning disable`, `.editorconfig` severity
   changes, or inspection suppressions unless the finding is a documented false
   positive. Prefer fixing the code.
6. Run `git diff --check` and review `git diff` before committing.

## Code Conventions

- Use file-scoped namespaces, four-space indentation, and the existing brace style.
- Keep public types and members documented with XML comments.
- Prefer existing helpers: `FileSystemHelper`, `AppPaths`, `ArchiveService`,
  `TempDirectoryHelper`, `LoggerService`.
- Use `CancellationToken` for cancellable asynchronous work and `ConfigureAwait(false)`
  in services and other non-UI code.
- Keep archive, hashing and error-handling behavior aligned with the existing user-error
  policy: corrupt/unsupported user files are reported to the user, not as application
  bugs.
- Add or update tests for every behavior change. New services and error paths need tests.
- Update `docs/` when user-visible behavior changes, including front matter for the
  Jekyll site.

## Security

- Follow [`SECURITY.md`](SECURITY.md) when reporting vulnerabilities.
- Never commit credentials, tokens, private keys, or other secrets.
- Report security vulnerabilities through GitHub private vulnerability reporting,
  not public issues.
