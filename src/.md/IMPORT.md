# IMPORT.md - Sync `base` guidance into pwsh72exe

Agent playbook for pulling shared rules, docs, and script improvements from [base](https://github.com/fosterbarnes/base) into pwsh72exe. pwsh72exe already follows base's layout; this is a merge, not a fresh import. Do not replace pwsh72exe application code.

No em dashes. Use `-` or `:`. Do not run `prePush.ps1`, `pushRelease.ps1`, builds, or version bumps as part of a sync. Do not edit `buildNotes.txt`.

## 1. Locate `base`

1. If the user has a local clone of `base` that contains `AGENTS.md`, `src\.md\`, and `.scripts\`, use that tree.
2. Otherwise, clone `https://github.com/fosterbarnes/base` into a temp directory and use that clone as the source.
3. Never treat pwsh72exe as the source.

## 2. What to merge

Both repos use the same layout: `AGENTS.md` at the root, shared docs in `src\.md\`, scripts in `.scripts\`.

| base file | pwsh72exe handling |
|-----------|--------------------|
| `AGENTS.md` | The General Guidelines, buildNotes.md, and buildNotes.txt sections must match base exactly; copy them over. Keep pwsh72exe's header, Project Guidelines, WPF stack section, and Agent Notes. |
| `src\.md\STYLE.md` | Replace with base's copy. |
| `src\.md\SCRIPTS.md` | Replace with base's copy, then keep the `pwsh72exe configuration` section at the end. |
| `src\.md\THEMING.md` | Replace with base's copy, then restore the `pwsh72exe` palette section in place of base's. |
| `src\.md\DEPENDENCIES.md` | Keep pwsh72exe's project and test package tables. Merge only shared tool rows. |
| `.scripts\*.ps1` | Shared operators (`push.ps1`, `pushRelease.ps1`, `agentPush.ps1`, `newVersion.ps1`, `updateReadme.ps1`) must match base exactly; copy them over. Merge `scriptHelper.ps1`, `build.ps1`, `buildInstaller.ps1`, `.run.ps1`, and `prePush.ps1` by hand, keeping pwsh72exe's project values. |

## 3. Do not copy

- base's sample app, updater, `base.sln`, or csproj files.
- `buildUpdater.ps1`: pwsh72exe has no updater.
- Avalonia packages, AXAML, or Avalonia stack rules.
- base's `README.md` or its `.version\` values.
- `buildNotes.md` / `buildNotes.txt`: local to each clone.

## 4. pwsh72exe rules to preserve

- `scriptHelper.ps1`: `$projectName = 'pwsh72exe'`, `$srcRoot`, `$cliProject` (`src\pwsh72exe.Cli\pwsh72exe.Cli.csproj`) and `$guiProject` (`src\pwsh72exe\pwsh72exe.csproj`) instead of a single `$csproj`, pwsh72exe's GitHub URL and icon paths.
- `build.ps1`: publishes the CLI and GUI separately, framework-dependent (`--self-contained false`), staged under `publish\build\x64`.
- `$buildTargets`: x64 only. Buttons are `x64Installer.svg` and `x64Portable.svg` from [fosterbarnes/res/btn](https://github.com/fosterbarnes/res/tree/main/btn).
- `.run.ps1`: keeps the `-Cli` option that launches the CLI and forwards remaining arguments.
- Keep the complete `closeOut` implementation: `closeOut 0` after successful standalone scripts, `closeOut -KeepOpen` before rethrowing handled failures, no closeout on help paths.
- `agentPush.ps1`: agents run it only when the user explicitly asks them to push.

## 5. Verify

- `AGENTS.md` still has only the WPF stack section and pwsh72exe's Project Guidelines.
- No base sample, updater, or Avalonia references were introduced.
- `scriptHelper.ps1` paths, names, and `$buildTargets` still match pwsh72exe.
- No personal paths, no em dashes in files you edited.
- If the user keeps a local `buildNotes.md`, add one session entry after a successful sync.
