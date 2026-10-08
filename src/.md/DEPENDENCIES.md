# Dependencies

Windows-first WPF + .NET console tooling for **pwsh72exe**. Lists tools and packages required to build, run, test, and publish this repo.

## Development Tools

| Tool | Purpose |
|------|---------|
| .NET 10 SDK | Build CLI, GUI, Core, and tests |
| PowerShell 7 (pwsh) | Run all `.scripts` automation |
| GitHub CLI (`gh`) | `pushRelease.ps1` |
| Inno Setup 6 (`ISCC.exe`) | `buildInstaller.ps1` |
| ImageMagick (`magick`) | Generates `icon.ico` and installer wizard images when missing |
| RipGrep (`rg`) | Repo-wide search (recommended) |

Publish uses framework-dependent single-file outputs for `win-x64` with `--self-contained false`. Target machines need the matching .NET runtime to launch the published app. Generated PowerShell wrapper EXEs also require the selected PowerShell runtime on the target machine.

## .NET Projects

| Project | TFM | Role |
|---------|-----|------|
| pwsh72exe.Core | net10.0 | Shared packaging logic |
| pwsh72exe.Cli | net10.0 | Command-line wrapper builder |
| pwsh72exe.Gui | net10.0-windows | WPF UI |
| pwsh72exe.Tests | net10.0 | xUnit tests |

## Test Packages

| Package | Version |
|---------|---------|
| Microsoft.NET.Test.Sdk | 17.11.1 |
| xunit | 2.9.2 |
| xunit.runner.visualstudio | 2.8.2 |

## Version And Release

- Version source of truth: `.version\version` (line 1 semver).
- Release assets: `publish\pwsh72exe_v<version>_windows-x64.zip` (CLI + GUI EXEs) and `publish\pwsh72exe_v<version>_windows-x64.exe` (installer).
- User release notes: `buildNotes.txt` at repo root, local and not committed (agents do not edit unless asked).

## Supported Platforms

Windows x64 only for published artifacts (`win-x64` RID).
