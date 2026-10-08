# SCRIPTS.md - PowerShell script guide for agents

Base guide for how repo scripts are written. Copy into a new repo and trim or extend for that stack. General file and code style lives in `src/.md/STYLE.md`. The canonical version and run implementations are shown inline below.

Related: `AGENTS.md` (general agent rules) and `STYLE.md` (shared style). No em dashes anywhere in scripts or this file. Use `-` or `:`.

Agents do not run project `.scripts/` build/release/run operators for the user by default. An explicit request for execution or a debug agent mode may run them when that execution is part of the goal.

Simple one-off pwsh (rename, move, copy, list) is fine when it is cheaper than Rewrite/Delete, but every shell/pwsh command still needs the user's approval unless they waived that for the current session. See `AGENTS.md` General Guidelines.

---

# Location and layout

| Path | Role |
|------|------|
| `.scripts/` | All project automation lives here (repo root relative). No nested `mac/` / `win/` script trees unless the project already has them. |
| `.scripts/scriptHelper.ps1` | Shared paths, version, branding, helpers. Dot-sourced by every other script. Sets location to `$repoRoot` on load. Not a user entry point. |
| `.installer/` | Inno Setup (or other) installer sources; scripts write/read `Output/` here. |
| `publish/` | Staged release binaries / zips / packages (when the project uses publish output). |
| `.res/` | Icons, installer images, OS packaging templates (when needed). |
| Version SoT | Project-specific path (see Version below). Scripts read it; do not invent a second source. |
| `buildNotes.txt` | User-owned release notes. `pushRelease.ps1` reads it when non-empty. Agents never edit it unless asked. |

One OS-aware script tree. Branch with `$IsWindows` / `$IsMacOS` / `$IsLinux` inside helpers, not separate folders.

---

# Naming

PowerShell function names use camelCase in this template.

## Operator / orchestration names

Only `.run.ps1` uses a leading dot prefix. Other script files use ordinary `camelCase` names.

| Name | Every project? | Does |
|------|----------------|------|
| `.run.ps1` | Yes | Dev launch loop. Build or `dotnet run` / start exe, then `q` quit, `r` or UpArrow restart. Forwards leftover args to the app. |
| `pushRelease.ps1` | Yes (if GitHub releases) | Replace and push `v<version>`, then publish the GitHub release; `-DryRun` validates without publishing. |
| `push.ps1` | If a compact repository push helper is used | Resolve the message, stage, commit, and push to the current branch; `-DryRun` prints all commands without changing the worktree or remote. |
| `agentPush.ps1` | If the user lets agents push | `-Context` prints status, new `buildNotes.md` lines, and recent subjects. Default mode requires a changed, well-formed `buildNotes.txt`, then runs `prePush.ps1` and `push.ps1` in-process under the pipeline context and prints a log tail only on failure. Agents run it only when the user explicitly asks them to push. |
| `prePush.ps1` | Yes | Wipes `publish/`, then calls `buildAll` for the full local build, installer, publish staging, and README pipeline. It does not create commits, tags, or releases. |

## Plain names = pipeline steps (called by operators or run alone)

| Name | Every project? | Does |
|------|----------------|------|
| `scriptHelper.ps1` | Yes | Shared state + functions only. |
| `build.ps1` | Yes | Build / publish the main app for the host (and arch flags when supported). |
| `buildUpdater.ps1` | If the repo has an updater | Build updater next to the app (or into the bundle). Skip cleanly on platforms where updater is unsupported. |
| `buildInstaller.ps1` | If installers/packages exist | Inno / codesign+seal / `.deb` / `.rpm` / zip packaging for the host OS. |
| `updateReadme.ps1` | If README has download badges / Quick Reference | Rewrite release download URLs from Version + known asset names. Alternate name some repos use: `updateReleaseLink.ps1`. |
| `newVersion.ps1` | Strongly preferred | Bump Version SoT (`+` / `++` / `+++` with or without a leading dash, and `-` / `--` / `---` to decrement, never below 0). Parse `$args` so plus-flags work unquoted. No args defaults to patch bump (`-+++`). May refresh `buildNotes.txt` line 1 when it is already a `v...` title. |

## Project-only scripts

Keep names descriptive and plain unless they are operator scripts. Do not invent one-off names for jobs that already have a standard name above.

The baseline may include `prePush.ps1` as the single local validation orchestrator. It should run the repository's documented checks and stop on the first failure; it must not create commits, tags, or releases.

---

# Shared process (every project)

Ideal flow. Stack details differ (.NET publish vs CMake vs bash/Actions); names and order stay the same.

```
newVersion.ps1          # optional; user owns version bumps unless they ask you to run the script
    |
prePush.ps1
    |-- wipe publish/
    |-- build.ps1
    |-- buildUpdater.ps1      # skip if no updater / unsupported OS
    |-- buildInstaller.ps1
    |-- (stage artifacts to publish/ or temp zip paths)
    |-- updateReadme.ps1     # or updateReleaseLink.ps1
    |
pushRelease.ps1         # tag and publish; assumes prePush already ran; reads buildNotes.txt when non-empty
push.ps1                # optional commit-and-push helper
.run.ps1                # daily dev; not part of release chain
```

Responsibilities:
1. **build** - compile the product the user ships.
2. **buildUpdater** - same version/arch story as the app when an updater exists.
3. **buildInstaller** - turn publish output into installable/portable artifacts.
4. **updateReadme** - keep README download links honest vs Version + tag.
5. **pushRelease** - tag and publish; assumes `prePush.ps1` already ran and `publish/` is populated. Replace and push the release tag, then publish the GitHub release; fail when no assets exist.
6. **run** - interactive loop only; do not bake release packaging into `.run.ps1`.

## Push And Release Helpers

These are plain-name helpers by explicit convention. `pushRelease.ps1` is the
single release operator for this template.

### `push.ps1`

Use the smallest safe commit-and-push flow:

1. A positional commit message wins (subject only). Otherwise read `buildNotes.txt` only when it changed since the last commit (or is untracked): line 1 is the commit subject, the next line must be blank, and the rest is the commit body. An unchanged file still holds the previous push's message, so it is ignored and the script prompts for a subject (`-DryRun` throws instead).
2. Use `git branch --show-current` to determine the current branch; fail on detached HEAD.
3. Run `git add -A`, `git commit -m <subject>` (plus a second `-m` for the body when present), and `git push origin <branch>`.
4. Accept `-f` as an alias for `-Force`; add `--force` only to the final push
   when explicitly requested.
5. Check the exit code after every Git command and stop before later operations
   when one fails.
6. After a successful push, open `$appURL` in the default browser (`openUrl`).
   `-DryRun` prints that URL and does not open a browser.

Never force-push by default. Do not hide branch selection, commit creation, or
force behavior inside another operator.

`-DryRun` still reads and validates the commit subject/body, resolves the
current branch, and prints the add, commit, and push commands. It skips all Git
mutations, including staging and commit creation.

### `pushRelease.ps1`

Assumes `prePush.ps1` already ran and `publish/` is populated. Does not build.

1. Read the version tag from the project's existing version source of truth.
   Do not create a second version file.
2. Fail when `publish/` contains no assets.
3. Force-replace the local and remote tag, push it, and call `gh release create`
   with all files directly under `publish/`.
4. Parse `buildNotes.txt` like `push.ps1`: line 1 = GitHub release `--title` (or `$tag` when line 1 is empty), blank line 2, lines 3+ = `--notes` (`--generate-notes` when body is empty).
5. Check the exit code after every native command. Do not run this helper during
   ordinary validation unless the user explicitly authorizes tag or release
   publication.
6. After a successful `gh release create`, open `$appURL/releases/tag/$tag` in
   the default browser (`openUrl`). `-DryRun` prints that URL and does not open
   a browser.

`-DryRun` still validates assets and release notes and prints the tag and
`gh release create` commands. It skips tag creation, tag pushing, and GitHub
release publication.

---

# Version

Document the exact path in the project's `AGENTS.md` / Project Guidelines.

| Style | Shape |
|-------|-------|
| Single file, 1+ lines | e.g. `Version` (semver + optional tag/build lines) or `VERSION.txt` (one line) |
| Folder | e.g. `.version/version` (+ optional `versionTag`, `versionBuild`) |

Rules:
- Default starting semver for a new repo SoT is `0.0.1` (line 1 of `.version\version` or equivalent); do not seed from another project's current version.
- User appends version numbers; agents do not bump unless asked.
- `newVersion.ps1` is the only script that should rewrite the SoT for bumps.
- `setVerBuild` (or equivalent) may write the "last built platform" field used by the app/updater; that is not a semver bump.
- Tag for GitHub is usually `v$versionContents` unless an override tag file/line exists.

---

# Code style

## Boilerplate (every entry script)

```powershell
#requires -Version 7.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
# ... work that throws on failure ...
closeOut          # default 5s; successful standalone scripts use closeOut 0
```

`scriptHelper.ps1` already `Set-Location -LiteralPath $repoRoot` on load. Every entry script still repeats that line so git, npm, and other relative-cwd tools never run from the caller's directory (for example the user's home folder) if a script is launched from a shortcut or another folder. Scripts that do not source the helper (rare) must `Set-Location` to the repo parent of `.scripts` themselves.

`closeOut` is required shared behavior, not optional boilerplate. Imports and trimmed copies must preserve its full implementation and call it for terminal success and handled failure paths. Successful standalone scripts use `closeOut 0`; handled failures use `closeOut -KeepOpen`, which preserves the console and returns control so the original error can remain visible. Nested `buildAll` steps suppress their own success closeout through the pipeline context, and `prePush.ps1` calls it once after the full pipeline succeeds. Help paths return without closeout.

Help-first scripts may parse `-Help` before dot-sourcing if sourcing is heavy; otherwise source first then branch. Help prints then `return` (not `exit 0` and not `closeOut`) so a `-NoExit` window stays open.

Launch GUI/shortcuts with `wezterm start --cwd <root> -- pwsh -NoProfile -NoExit -File <script>` when `wezterm.exe` is on PATH so the operator gets its own WezTerm pane; otherwise `pwsh -NoProfile -NoExit -File <script>`. GUI wezterm start and openSidebar send-text set `$env:SCRIPT_OWN_PANE=1`. `closeOut` takes optional `$Seconds` (default 5; standalone success uses 0) and `-KeepOpen` for handled failures. It counts down when `$Seconds -gt 0`, including direct PowerShell invocation, except for nested steps running under the shared `buildAll` pipeline context. Success then kills a WezTerm pane and exits only for an owned launcher context; `-KeepOpen` returns without killing or exiting so the original error remains visible. Typed `pwsh -File` in an existing pane must not kill-pane. A failed or missing wezterm call is ignored so Exit still runs. Nested `& other.ps1` and an already-open prompt no-op. Do not call `closeOut` on help, inside `buildAll`, or while loading `scriptHelper.ps1`.

## Paths

Prefer string concat (house style):

```powershell
$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$scripts = "$repoRoot\.scripts"
$versionFolder = "$repoRoot\.version"
$version = "$versionFolder\version"
$buildNotes = "$repoRoot\buildNotes.txt"
$readme = "$repoRoot\README.md"
```

Cross-platform .NET repos may use `Join-RepoPath` / `Resolve-CanonicalRepoRoot` when shared-folder aliases require it. For Windows-first repos, prefer the `$repoRoot\...` form above.

Always `-LiteralPath` for Test-Path / Get-Content / Remove-Item when paths come from variables.

## Errors

```powershell
& dotnet publish $csproj -c Release -o $outDir
if ($LASTEXITCODE) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

& "$PSScriptRoot\buildUpdater.ps1" -Architecture $Architecture
if ($LASTEXITCODE) { throw "buildUpdater failed (exit $LASTEXITCODE)" }
```

Throw on failure. No silent catch-and-continue unless the project already documents a skip (e.g. updater on Linux). Leave the window open on failure: do not call `closeOut`.

## File writes

UTF-8 **no BOM** for Version / buildNotes / README rewrites:

```powershell
$noBom = New-Object System.Text.UTF8Encoding $false

function writeFileNoBom {
    param(
        [Parameter(Mandatory)][string]$LiteralPath,
        [Parameter(Mandatory)][string]$Content
    )
    [System.IO.File]::WriteAllText($LiteralPath, $Content, $noBom)
}
```

## Comments and structure

- Minimal comments; human-sounding when needed.
- Prefer `switch` / loops over long nested `if`.
- No fallbacks and no debug spew unless asked.
- Keep functions in `scriptHelper.ps1` when two or more scripts need them.
- Keep a script thin: parse args, call helpers, invoke the next tool.

## Params and flags

```powershell
param(
    [Alias('h')][switch]$Help,
    [string]$Architecture,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppLaunchArgs
)
```

- Support `-h` / `--help` on operator scripts.
- Architecture flags: accept both PowerShell switches and leftover tokens like `--x64` / `-arm64` when `.run.ps1` / `prePush.ps1` already do.
- `newVersion.ps1`: do **not** use `param()` for `-+` / `-++` / `-+++`. Parse `$args` with regex so pwsh does not eat `+`.
- `newVersion.ps1`: no args defaults to patch bump (same as `-+++`).
- `newVersion.ps1`: plus flags accept an optional leading dash (`+` == `-+`). Minus flags `-` / `--` / `---` decrement (major/minor/patch) and mirror the bump reset (zero lower parts), clamped at 0 so a decrement on an already-0 component is a no-op.
- `newVersion.ps1`: do **not** put a bare `|` pipeline inside `-or` without parentheses. `|` grabs the entire left side as input, so `$x.Count -ne 3 -or $x | Where-Object { ... }` becomes `($x.Count -ne 3 -or $x) | Where-Object { ... }`; the non-empty `$x` array makes the `-or` `$true`, and `Where-Object` passes that `$true` through (it fails `-match ^\d+$`). Wrap the pipeline in parens or use `foreach`.

```powershell
# newVersion.ps1 - parse $args so -+, -++, -+++ work unquoted
$flags = @(
    $args |
        ForEach-Object { "$_".Trim() } |
        Where-Object { $_.Length -gt 0 }
)
foreach ($f in $flags) {
    $next = switch -Regex ($f) {
        '^(?i)(-h|--help)$' { 'help' }
        '^-\+\+\+$' { 'patch' }
        '^-\+\+$' { 'minor' }
        '^-\+$' { 'major' }
        default { throw "Unknown argument: $f" }
    }
    # ...
}
```

---

# scriptHelper.ps1

Use the baseline below. Replace only marked project-specific paths and values.
Keep shared state and reusable functions here.

Every project helper should define at least:

```powershell
#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
# project-specific roots (app csproj dir, cmake root, etc.)
$versionFolder = "$repoRoot\.version"   # adjust to project SoT; some repos use a single file
$version = "$versionFolder\version"
$versionBuild = "$versionFolder\versionBuild"
$versionTag = "$versionFolder\versionTag"
$buildNotes = "$repoRoot\buildNotes.txt"
$scripts = "$repoRoot\.scripts"
$readme = "$repoRoot\README.md"
$versionContents = ([IO.File]::ReadAllText($version)).Trim()
# branding: $projectName, $appPublisher, $appURL, $ghRepo, icon paths, installer paths
```

Required helper implementations:

```powershell
function readVerFile {
    param([string]$LiteralPath = $version)
    $lines = @(([IO.File]::ReadAllText($LiteralPath) -split '\r?\n' | ForEach-Object { $_.Trim() }))
    while ($lines.Count -lt 3) { $lines += '' }
    $lines
}

function writeFileNoBom {
    param(
        [Parameter(Mandatory)][string]$LiteralPath,
        [Parameter(Mandatory)][string]$Content
    )
    [IO.File]::WriteAllText($LiteralPath, $Content, $noBom)
}

function writeVerFile {
    param(
        [Parameter(Mandatory)][string]$SemVer,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Tag,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Build
    )
    writeFileNoBom -LiteralPath $version -Content (($SemVer.Trim(), $Tag.Trim(), $Build.Trim()) -join "`n")
}

function setVerBuild {
    param([Parameter(Mandatory)][string]$Platform)
    writeFileNoBom -LiteralPath $versionBuild -Content ($Platform.Trim() + "`n")
}

function openUrl {
    param([Parameter(Mandatory)][string]$Url)
    if ([string]::IsNullOrWhiteSpace($Url)) { throw 'openUrl requires a URL.' }
    Start-Process $Url
}
```

Add project-specific asset, branding, Markdown, and native-tool helpers below
this baseline only when two or more scripts use them. Every native command must
be followed by an exit-code check.

Dot-source comment at the top is fine so humans remember how to load it:

```powershell
#. "$PSScriptRoot\scriptHelper.ps1"
```

---

# Version And Run Scripts

These scripts are prescribed templates. Use the code below, changing only the
marked project-specific paths, helper names, app launch flags, and process
names. Do not replace this with a shorter, more abstract, or alternate
implementation.

## `newVersion.ps1`

```powershell
#requires -Version 7.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot

function Show-Help {
    Write-Host @"
newVersion.ps1              Patch bump
newVersion.ps1 -+ / +       Major bump
newVersion.ps1 -++ / ++     Minor bump
newVersion.ps1 -+++ / +++   Patch bump
newVersion.ps1 -            Major bump down
newVersion.ps1 --           Minor bump down
newVersion.ps1 ---          Patch bump down
newVersion.ps1 -tag         Set the optional release tag
"@
}

$wantTag = $false
$bump = $null
$dir = 'up'
foreach ($flag in @($args | ForEach-Object { "$_".Trim() } | Where-Object Length)) {
    switch -Regex ($flag) {
        '(?i)^(-h|--help)$' { Show-Help; return }
        '(?i)^(-tag|--tag)$' { $wantTag = $true }
        '^\+{1,3}$' { if ($bump) { throw 'Use only one bump flag.' }; $bump = @{1 = 'major'; 2 = 'minor'; 3 = 'patch'}[$flag.Length]; $dir = 'up' }
        '^\-\+{1,3}$' { if ($bump) { throw 'Use only one bump flag.' }; $bump = @{1 = 'major'; 2 = 'minor'; 3 = 'patch'}[$flag.TrimStart('-').Length]; $dir = 'up' }
        '^\-{1,3}$' { if ($bump) { throw 'Use only one bump flag.' }; $bump = @{1 = 'major'; 2 = 'minor'; 3 = 'patch'}[$flag.Length]; $dir = 'down' }
        default { throw "Unknown argument: $flag" }
    }
}
if (-not $bump -and -not $wantTag) { $bump = 'patch'; $dir = 'up' }
$lines = readVerFile
$parts = @($lines[0] -split '\.')
if ($parts.Count -ne 3 -or ($parts | Where-Object { $_ -notmatch '^\d+$' })) { throw "Invalid semantic version: $($lines[0])" }
$major = [int]$parts[0]; $minor = [int]$parts[1]; $patch = [int]$parts[2]
if ($dir -eq 'down') {
    switch ($bump) {
        'major' { if ($major -gt 0) { $major--; $minor = 0; $patch = 0 } }
        'minor' { if ($minor -gt 0) { $minor--; $patch = 0 } }
        'patch' { if ($patch -gt 0) { $patch-- } }
    }
} else {
    switch ($bump) {
        'major' { $major++; $minor = 0; $patch = 0 }
        'minor' { $minor++; $patch = 0 }
        'patch' { $patch++ }
    }
}
$tagValue = $lines[1]
if ($wantTag) { $tagValue = Read-Host 'Release tag (leave empty to use v<version>)' }
writeVerFile -SemVer "$major.$minor.$patch" -Tag $tagValue -Build $lines[2]
if (Test-Path -LiteralPath $buildNotes) {
    $tail = @([IO.File]::ReadAllLines($buildNotes) | Select-Object -Skip 1)
    writeFileNoBom -LiteralPath $buildNotes -Content ((@("v$major.$minor.$patch release") + $tail) -join "`n")
}
Write-Host "Version -> $major.$minor.$patch"
closeOut 0
```

Substitute only project-specific paths and helper names. Keep raw `$args`,
the regex parsing, validation, version resets, and file-write flow.

## `.run.ps1`

```powershell
#requires -Version 7.0
param([Alias('h')][switch]$Help, [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppLaunchArgs)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host '.run.ps1 [-x86|-x64|-arm64] [-- app args...]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$architectureArgs = @($AppLaunchArgs | Where-Object { "$_" -match '(?i)^(--x86|-x86|--86|-86|--x64|-x64|--64|-64|--arm64|-arm64|--arm|-arm|--help|-h)$' })
$architecture = getArchitecture $architectureArgs
if ($architecture -eq 'help') { Write-Host '.run.ps1 [-x86|-x64|-arm64] [-- app args...]'; return }
$target = getBuildTargets ($architecture ?? 'x64')
$forward = @($AppLaunchArgs | Where-Object { "$_" -notmatch '(?i)^(--x86|-x86|--86|-86|--x64|-x64|--64|-64|--arm64|-arm64|--arm|-arm)$' })
try {
while ($true) {
    setVerBuild $target.Architecture
    $dotnetArgs = @('run', '--project', $csproj, '--framework', $dotnetFramework, '-c', 'Release', "-p:Platform=$($target.Architecture)")
    if ($forward) { $dotnetArgs += '--'; $dotnetArgs += $forward }
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList $dotnetArgs -WorkingDirectory $repoRoot -NoNewWindow -PassThru
    Write-Host "=== running $projectName... ===`nq = quit`nr , up arrow = restart"
    $action = $null
    while (-not $proc.HasExited) {
        Start-Sleep -Milliseconds 50
        try { if (-not [Console]::KeyAvailable) { continue } } catch { continue }
        $key = [Console]::ReadKey($true)
        if ($key.Key -eq [ConsoleKey]::R -or $key.Key -eq [ConsoleKey]::UpArrow) { $action = 'restart'; break }
        if ($key.Key -eq [ConsoleKey]::Q) { $action = 'quit'; break }
    }
    if ($action) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    if ($action -ne 'restart') {
        if (-not $action -and $proc.ExitCode -ne 0) { throw "$projectName exited with code $($proc.ExitCode)." }
        break
    }
}
catch {
    closeOut -KeepOpen
    throw
}
closeOut 0
```

Substitute only `$csproj`, `$dotnetFramework`, `$projectName`, and
`$repoRoot` (all defined in `scriptHelper.ps1`). Keep the argument
forwarding, `Start-Process`, single-keypress loop (R/UpArrow to restart,
Q to quit), nonzero app-exit check, `closeOut -KeepOpen` failure handling, and `closeOut 0` success handling.

## Canonical script review

Before accepting either script:

1. Compare it directly with the complete code examples above.
2. Confirm raw `$args`, bump/tag validation, version writes, and build-notes
   header update in `newVersion.ps1`. Confirm it ends with `closeOut 0`.
3. Confirm argument forwarding, platform parsing, `Start-Process`, single-keypress
   loop (R/UpArrow restart, Q quit), nonzero app-exit handling, `closeOut -KeepOpen`
   failure handling, and `closeOut 0` success handling in `.run.ps1`.
4. Confirm differences are limited to the explicitly permitted project-specific
   names, paths, tools, and launch flags.

# Skeleton Shared Script Templates

For the shared scripts below, use these implementations and change only the
explicit project identity, path, resource, and framework values. The helper
functions named here must exist in `scriptHelper.ps1`; do not replace them with
local copies in each script.

## `build.ps1`

```powershell
#requires -Version 7.0
param([Alias('h')][switch]$Help, [string]$Architecture)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'build.ps1 [-x86|-x64|-arm64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
checkVerBuild $Architecture
$targetArchitecture = getArchitecture @($Architecture)
$targets = getBuildTargets $targetArchitecture
runNativeCommand dotnet @('restore', $solution) 'dotnet restore'
foreach ($target in $targets) {
    deleteDir $target.BinFolder
    New-Item -ItemType Directory -Path $target.BinFolder -Force | Out-Null
    runNativeCommand dotnet @('publish', $csproj, '-c', 'Release', '-r', $target.RuntimeIdentifier, '--no-self-contained', '-p:PublishReadyToRun=true', '-o', $target.BinFolder) "dotnet publish $($target.Architecture)"
    Copy-Item -LiteralPath $version -Destination "$($target.BinFolder)\Version" -Force
    publishCleanup $target.BinFolder
}
& "$PSScriptRoot\buildUpdater.ps1" -Architecture ($targetArchitecture ?? '')
if ($LASTEXITCODE) { throw 'buildUpdater.ps1 failed.' }
closeOut 0
```

For cross-platform projects, wrap `buildUpdater` in `if (-not $IsLinux)` and
add `New-MacAppBundle` / `Set-UnixHostExecutable` calls as needed. The
Windows-only form above always calls `buildUpdater` unconditionally.

## `buildUpdater.ps1`

```powershell
#requires -Version 7.0
param([Alias('h')][switch]$Help, [string]$Architecture)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'buildUpdater.ps1 [-x86|-x64|-arm64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$targetArchitecture = getArchitecture @($Architecture)
foreach ($target in (getBuildTargets $targetArchitecture)) {
    if (-not (Test-Path -LiteralPath $target.BinFolder)) { throw "Missing app output: $($target.BinFolder). Run build.ps1 first." }
    runNativeCommand dotnet @('publish', $updaterCsproj, '-c', 'Release', '-r', $target.RuntimeIdentifier, '--no-self-contained', '-o', $target.BinFolder) "dotnet publish updater $($target.Architecture)"
}; closeOut 0
```

## `buildInstaller.ps1`

Windows-first live script. Calls `ensureInstallerImages` so missing `icon.ico` or wizard PNGs are created from `icon.png`, then compiles each arch `.iss` with `/D` for version, publisher, URL, license file, and those image paths. The license page shows `LICENSE` with "I accept" pre-selected. Existing `.res\icon` files are left in place.

```powershell
param([string]$Architecture)
#requires -Version 7.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$targetArchitecture = getArchitecture @($Architecture)
ensureInstallerImages
$isccDefines = @(
    "/DAppVersion=$versionContents"
    "/DAppPublisher=$appPublisher"
    "/DAppURL=$appURL"
    "/DSetupIconFile=$($iconIco -replace '\\','/')"
    "/DWizardImageFile=$($wizardLargePng -replace '\\','/')"
    "/DWizardSmallImageFile=$($wizardSmallPng -replace '\\','/')"
    "/DLicenseFile=$($licenseFile -replace '\\','/')"
)
if (-not $Architecture) { deleteDir $installerOutput }
New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null
foreach ($target in (getBuildTargets $targetArchitecture)) {
    if (-not (Test-Path -LiteralPath $target.ExePath)) { throw "Missing publish output: $($target.ExePath)" }
    runNativeCommand $iscc (@($isccDefines) + $target.InstallerScript) "ISCC $($target.Architecture)"
}
```

Shared wizard, tasks, icons, license, and postinstall run live in `.installer\base.installer.iss` (`LicenseFile` + `[Code]` pre-checks "I accept"). Each arch script keeps AppId, architectures, output name, and `[Files]`.

## Installer image helpers (`scriptHelper.ps1`)

`deleteDir` throws when the folder still exists after removal, so a locked publish or installer output fails loudly instead of mixing old and new files.

`convertToIco` writes a sibling multi-size ICO using `$imageSizes` (`16` through `256`). `resizePng` writes sibling `<name><size>.png` files (defaults `$imageSizes`). `writeWizardImages` overwrites `.res\icon\installer-wizard-small.png` (`256x256`) and `installer-wizard-large.png` (`240x459`, black pad). `prepareInstallerImages` runs `resizePng` with `$pngInstallerSizes` (`512, 256, 128, 64, 32`) then `writeWizardImages`. `ensureInstallerImages` creates `icon.ico` and the wizard PNGs only when they are missing. None of these rewrite the source raster. Requires ImageMagick `magick` on PATH. `buildInstaller.ps1` calls `ensureInstallerImages` so Inno always has the files it references.

One-liner from the repo root (always rewrite sized PNGs and wizard files):

```powershell
. .\.scripts\scriptHelper.ps1; prepareInstallerImages
```

Outputs under `.res\icon\`: `icon512.png`, `icon256.png`, `icon128.png`, `icon64.png`, `icon32.png`, `installer-wizard-small.png`, `installer-wizard-large.png`. `convertToIco` also writes `icon.ico` when `ensureInstallerImages` needs it.

## `buildAll` helper

```powershell
function buildAll {
    param([string]$Architecture)
    deleteDir $publishFolder
    $params = @{}; if ($Architecture) { $params.Architecture = $Architecture }
    runNativeCommand -FilePath "$PSScriptRoot\build.ps1" -ArgumentList $params -Name 'build.ps1'
    runNativeCommand -FilePath "$PSScriptRoot\buildInstaller.ps1" -ArgumentList $params -Name 'buildInstaller.ps1'
    New-Item -ItemType Directory -Path $publishFolder -Force | Out-Null
    foreach ($target in (getBuildTargets $Architecture)) {
        Compress-Archive -Path "$($target.BinFolder)\*" -DestinationPath "$publishFolder\$(buildAssetName -Kind Portable -Architecture $target.Architecture)" -Force
        Copy-Item -LiteralPath "$installerOutput\$($target.InstallerName)" -Destination "$publishFolder\$(buildAssetName -Kind Installer -Architecture $target.Architecture)" -Force
    }
    runNativeCommand -FilePath "$PSScriptRoot\updateReadme.ps1" -ArgumentList @{} -Name 'updateReadme.ps1'
}
```

## `updateReadme.ps1`

Require the `<!-- Quick Reference -->` block, construct every URL from `$tag`
and the helper asset-name functions, replace only that block and documented
download commands, write UTF-8 without BOM, and throw when the marker is
missing. Do not rewrite an entire README or invent a marker.

## `pushRelease.ps1`

Assumes `prePush.ps1` already ran. Does not call `buildAll`. Force-replaces
and pushes `$tag`, then uploads every file directly under `publish/`. Fails
when no assets exist. Parse `buildNotes.txt` like `push.ps1` (title, blank line,
body) for `--title` and `--notes`; use `--generate-notes` when the body is empty.
After a successful create, open `$appURL/releases/tag/$tag` in the default
browser. `-DryRun` only prints that URL.

## Windows run shortcut

On Windows projects, create a `run.lnk` shortcut in the repository root when
`.run.ps1` exists. Target `pwsh.exe`, pass `-NoProfile -File .run.ps1`, and set
the shortcut's **Start in** directory to the repository root so it launches the
operator from the correct working directory.

## `prePush.ps1`

```powershell
#requires -Version 7.0
param([Alias('h')][switch]$Help, [Parameter(ValueFromRemainingArguments = $true)][string[]]$BuildArgs)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'prePush.ps1 [-x86|-x64|-arm64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
$architecture = getArchitecture (@($BuildArgs) + @($args))
if ($architecture -eq 'help') { Write-Host 'prePush.ps1 [-x86|-x64|-arm64]'; return }
buildAll $architecture
Write-Host 'Pre-push build and packaging passed. No commit or push was performed.'
closeOut 0
```

Keep extras in the orchestrator or plain helper scripts, not inside `build.ps1`.

## `pushRelease.ps1`

Assumes `prePush.ps1` already ran and `publish/` is populated. Does not build.

1. Fail when `publish/` contains no assets.
2. Force-replace and push the tag.
3. Upload every file found directly under `publish/`.
4. Parse `buildNotes.txt` like `push.ps1`: line 1 = GitHub release `--title` (or `$tag` when line 1 is empty), blank line 2, lines 3+ = `--notes` (`--generate-notes` when body is empty).
5. Check every native-command exit code and require explicit authorization before publication.
6. After a successful publish, open `$appURL/releases/tag/$tag` in the default browser. Do not open a browser on `-DryRun`.

Agents never invent release notes into `buildNotes.txt`. Paste notes in chat for the user.

## `updateReadme.ps1`

1. Require a stable marker in README (common: `<!-- Quick Reference -->` … `-->`).
2. Build URLs from `$appURL/releases/download/$tag` + asset file names. The block is an HTML table: an installer row and a portable row, one button cell per `$buildTargets` entry. Button images come from `$buttonURL` ([fosterbarnes/res/btn](https://github.com/fosterbarnes/res/tree/main/btn)) using each target's `InstallerButton` / `PortableButton` file name (the arm64 installer button is `arm64.svg`).
3. Rewrite only the marked block / known badge hrefs.
4. Throw if the marker is missing. Do not invent a new README structure silently.

---

# When stacks diverge

Document stack-specific publish/installer details in the project's own `SCRIPTS.md` or `AGENTS.md` Project Guidelines. Do not force one toolchain into every repo. Keep the **names and order** above even when the tools inside `build.ps1` differ.

This template's checked-in sample is WPF + `dotnet`. Copied repos pick one row. Omit operators that have no artifact (no fake `dotnet` stubs).

| Stack | `build.ps1` / `.run.ps1` tool | Notes |
|-------|-------------------------------|--------|
| C# WPF .NET 10 (default) | `dotnet` publish/run | `net10.0-windows`. Canonical script dumps above. |
| Avalonia C# .NET 10 | `dotnet` | Set `$dotnetFramework` to the Avalonia TFM. Same operator names. |
| C# .NET, no GUI | `dotnet` | No theme/UI steps. |
| Rust | `cargo build` / `cargo run` | Default Windows MSVC target unless the crate says otherwise. |
| PowerShell only | main `.ps1` | `build.ps1` may no-op or zip scripts; `.run.ps1` launches the product script. |
| C | `cmake --build` or `msbuild` | Follow existing CMake/VS files. Default MSVC on Windows. |

---

# Do / don't

**Do**
- Put new automation under `.scripts/`
- Dot-source `scriptHelper.ps1` for paths and Version
- Reuse the standard names above when the job matches
- Check `$LASTEXITCODE` after external tools
- Use `buildNotes.txt` or a required message for `push.ps1`; never force-push unless `-f` is explicit
- Keep `pushRelease.ps1` package/version/tag handling tied to project metadata
- Clean `publish/` at the start of `buildAll` (before `build.ps1`) so old zips, installers, and `publish/build` trees cannot mix into the next `pushRelease.ps1`
- Clean temporary release artifacts in `finally`
- Keep orchestrators thin; keep shared logic in the helper
- Preserve the complete shared `closeOut` implementation; call `closeOut 0` after successful standalone entry scripts and `closeOut -KeepOpen` before rethrowing handled failures
- Match existing asset naming helpers used by README + release scripts

**Don't**
- Add a second Version file or hardcode semver in scripts
- Edit `buildNotes.txt` when asked for changelog text (chat paste only)
- Run builds/releases from the agent session
- Use `Join-Path` for simple Windows repo paths when `$repoRoot\foo` is the project style
- Create `.scripts/mac/` or duplicate OS trees without a strong existing reason
- Put release packaging into `.run.ps1`
- Silence failures or add "helpful" fallbacks
- Replace `closeOut` with a no-op or remove successful-call sites during an import or trim
- Publish or replace tags during ordinary checks without explicit user authorization

---

# New script checklist

1. Does an existing standard name already cover this job? Extend it.
2. Operator (dot prefix) vs step (plain name)?
3. Add shared paths/functions to `scriptHelper.ps1` first if reuse is likely.
4. `#requires -Version 7.0`, `$ErrorActionPreference = 'Stop'`, LiteralPath, exit-code checks.
5. Help text for anything users run directly.
6. Preserve the full `closeOut` helper; call `closeOut 0` after successful standalone completion and `closeOut -KeepOpen` before rethrowing handled failures. Do not call it on help or while dot-sourcing a helper.
7. Note the change in repo-root `buildNotes.md` (Agent + User sections).
8. If the script becomes part of the release chain, wire it into `buildAll` in `scriptHelper.ps1` or `prePush.ps1` explicitly.
9. For `push.ps1`, verify `buildNotes.txt`/required-message fallback, branch selection, normal push, explicit `-f` behavior, and opening `$appURL` after a real push (not `-DryRun`).
10. For `pushRelease.ps1`, verify package metadata, tag handling, notes fallback, asset discovery, publication authorization, and opening the tag release URL after a real publish (not `-DryRun`).

## pwsh72exe configuration

- Projects under `src/`: `pwsh72exe` (WPF GUI, `net10.0-windows`), `pwsh72exe.Cli` and `pwsh72exe.Core` (`net10.0`), `pwsh72exe.Tests` (xUnit). `scriptHelper.ps1` exposes `$cliProject` and `$guiProject` instead of a single `$csproj`.
- Publish target: `win-x64` only. `build.ps1` restores, then framework-dependent-publishes (`--self-contained false`) the CLI and GUI, staging `pwsh72exe.Cli.exe`, `pwsh72exe.Gui.exe`, and `Version` under `publish\build\x64` and removing PDB files. Target machines need the matching .NET runtime; generated wrapper EXEs also need the selected PowerShell runtime.
- `buildAll` produces one portable zip and one Inno Setup installer (`publish\pwsh72exe_v<version>_windows-x64.zip` / `.exe`), then refreshes the README.
- `.run.ps1` launches the GUI by default; `-Cli` launches the CLI and forwards remaining arguments.
- README buttons: x64 only (`x64Installer.svg`, `x64Portable.svg`), one stacked cell per row.
- No updater: `buildUpdater.ps1` is omitted.
