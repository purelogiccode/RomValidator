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
| Windows 10/11 | The application is WPF and builds on Windows |
| .NET 10 SDK | Pinned by [`global.json`](https://github.com/purelogiccode/RomValidator/blob/master/global.json) |
| Git | To clone the repository |

The repository contains the standalone 7-Zip 26.03 fallback executables (`7za.exe`,
`7za_arm64.exe`), so no extra download is required for a build.

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
helpers.

## Run the application

```powershell
dotnet run --project RomValidator/RomValidator.csproj
```

## Publish

The release workflow publishes framework-dependent builds for both supported architectures:

```powershell
dotnet publish RomValidator/RomValidator.csproj `
  --configuration Release `
  --runtime win-x64 `
  --output publish/win-x64 `
  --no-self-contained

dotnet publish RomValidator/RomValidator.csproj `
  --configuration Release `
  --runtime win-arm64 `
  --output publish/win-arm64 `
  --no-self-contained
```

To produce a self-contained build instead (no .NET runtime required on the target machine), add
`--self-contained true` and optionally `-p:PublishSingleFile=false`.

The publish output includes:

- `RomValidator.exe` and the WPF dependencies,
- `SharpCompress.dll`,
- `7za.exe` and `7za_arm64.exe`,
- `RomValidator.xml` (XML documentation).

## Project layout

```
CSharp_RomValidator.sln
├── RomValidator/               WPF application
│   ├── Interfaces/             service abstractions
│   ├── Models/                 data models (No-Intro XML, bug report, GitHub release)
│   ├── Pages/                  Validate ROMs and Generate DAT pages
│   ├── Services/               hashing, archives, logging, reporting, paths
│   ├── MainWindow.xaml         shell with header, frame and status bar
│   └── RomValidator.csproj
├── RomValidator.Tests/         xUnit test project
└── docs/                       this documentation site
```

## Code style

The build runs the Meziantou and Roslynator analyzers and fails on warnings. Keep the build at
zero warnings and add tests for behavior changes. XML documentation is required for public
members (`GenerateDocumentationFile` is enabled).

## Related pages

- [Architecture](architecture)
- [CI/CD](ci-cd)
- [Contributing](contributing)
