# pwsh72exe

Pwsh script .exe wrapper. Windows only. C# .NET 10 WPF

<!-- Quick Reference -->
<table border="0">
<tbody>
<tr>
<td valign="top"><a href="https://github.com/fosterbarnes/pwsh72exe/releases/download/v0.0.2/pwsh72exe_v0.0.2_windows-x64.exe"><img src="https://raw.githubusercontent.com/fosterbarnes/res/main/btn/x64Installer.svg" width="180" height="auto" alt="Windows x64 installer"/></a></td>
</tr>
<tr>
<td valign="top"><a href="https://github.com/fosterbarnes/pwsh72exe/releases/download/v0.0.2/pwsh72exe_v0.0.2_windows-x64.zip"><img src="https://raw.githubusercontent.com/fosterbarnes/res/main/btn/x64Portable.svg" width="180" height="auto" alt="Windows x64 portable ZIP"/></a></td>
</tr>
</tbody>
</table>
<!-- End Quick Reference -->


<img src="./.res/scr/1.png" width="600" style="margin: 20px; padding: 20px;">

## Examples

- [`hello.ps1`](src/examples/hello.ps1): a minimal script to wrap into an EXE.

## Requirements

- .NET 10 SDK
- PowerShell 7
- Inno Setup 6 for installers
- GitHub CLI for release scripts
- ImageMagick (`magick`) for installer icon helpers

## Versioning

- Semantic version: `.version/version`
- Optional release tag override: `.version/versionTag`
- Last build platform: `.version/versionBuild`
- User release notes: `buildNotes.txt` (local, not committed)
- Agent history: `buildNotes.md` (local, not committed)

## Agent Docs

Built on [base](https://github.com/fosterbarnes/base).

- [AGENTS.md](AGENTS.md): agent rules and project guidelines
- [STYLE.md](src/.md/STYLE.md): file, code, docs, and design style
- [SCRIPTS.md](src/.md/SCRIPTS.md): PowerShell script guide
- [THEMING.md](src/.md/THEMING.md): colors and theming
- [IMPORT.md](src/.md/IMPORT.md): sync shared guidance from `base`
- [DEPENDENCIES.md](src/.md/DEPENDENCIES.md): tools and packages

