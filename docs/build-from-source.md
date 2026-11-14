---
title: Build from Source
parent: Reference
nav_order: 3
description: "Prerequisites, build, test and publish commands for ROM Validator."
---

# Build from Source

## Prerequisites

| Requirement | Notes |
|:------------|:------|
| Windows, Linux or macOS | The application is built with Avalonia and builds on all three platforms |
| .NET 11 SDK | Pinned by [`global.json`](https://github.com/purelogiccode/RomValidator/blob/master/global.json) |
| Git | To clone the repository |
| JetBrains ReSharper command-line tools | Required for the `jb inspectcode` check that CI enforces |

The repository contains the standalone 7-Zip 26.03 fallback executables (`7za.exe`,
`7za_arm64.exe`), so no extra download is required to build the Windows bundles.

## Clone and build

```powershell
git clone https://github.com/purelogiccode/RomValidator.git
cd RomValidator
dotnet build CSharp_RomValidator.sln --configuration Release
```

## Run the tests

```powershell
dotnet test CSharp_RomValidator.sln --configuration Release --nologo
```

The test project (`RomValidator.Tests`) uses xUnit and coverlet. Tests cover models, services,
hash calculation, archive handling, HTTP-based services (with a fake handler) and file-system
helpers. The suite runs on Windows, Linux and macOS.

## Run the application

```powershell
dotnet run --project RomValidator/RomValidator.csproj
```

## Publish

The release workflow publishes self-contained builds for every supported runtime identifier:

```powershell
dotnet publish RomValidator/RomValidator.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output publish/win-x64 `
  -p:PublishSingleFile=false
```

Use the same command with another runtime identifier to target Linux or macOS, for example
`linux-x64`, `linux-arm64`, `osx-x64` or `osx-arm64`. The release workflow packages the publish
folders as follows:

| Runtime identifier | Release bundle |
|:-------------------|:---------------|
| `win-x64`, `win-arm64` | `release_<version>_<rid>.zip` |
| `linux-x64`, `linux-arm64` | `release_<version>_<rid>.tar.gz` |
| `osx-x64`, `osx-arm64` | `release_<version>_<rid>.dmg` (contains `ROM Validator.app`) |

Self-contained bundles do not require the .NET runtime on the target machine. To produce a
framework-dependent build instead, use `--self-contained false`.

The publish output includes:

- `RomValidator.exe` (Windows) or `RomValidator` (Linux/macOS) and the .NET runtime,
- `Avalonia` and `SharpCompress` assemblies,
- `7za.exe` and `7za_arm64.exe` (Windows bundles),
- `RomValidator.xml` (XML documentation).

## Project layout

```
CSharp_RomValidator.sln
├── RomValidator/               Avalonia application
│   ├── App.axaml               Fluent theme and custom dark palette
│   ├── Program.cs              Avalonia entry point
│   ├── MainWindow.axaml        shell with header, page host and status bar
│   ├── Pages/                  Validate ROMs and Generate DAT views (.axaml)
│   ├── Views/                  themed message dialog
│   ├── Interfaces/             service abstractions
│   ├── Models/                 data models (No-Intro XML, bug report, GitHub release)
│   ├── Services/               hashing, archives, logging, reporting, paths
│   └── RomValidator.csproj
├── RomValidator.Tests/         xUnit test project
└── docs/                       this documentation site
```

## Code style

The build runs the Meziantou and Roslynator analyzers and fails on warnings. Keep the build at
zero warnings and add tests for behavior changes. XML documentation is required for public
members (`GenerateDocumentationFile` is enabled). Before committing, run `jb inspectcode` over the
solution; CI fails when it reports any finding. See [Contributing](contributing) for the full
checklist.

## Related pages

- [Architecture](architecture)
- [CI/CD](ci-cd)
- [Contributing](contributing)
