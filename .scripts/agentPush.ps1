#requires -Version 7.0
# Non-interactive push for agents, run only when the user explicitly asks them to push: -Context prints what the agent needs to write
# buildNotes.txt; the default mode validates it, runs prePush.ps1 and push.ps1 quietly, then reports.
param([Alias('h')][switch]$Help, [switch]$Context)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'agentPush.ps1 [-Context]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
& git diff --quiet HEAD -- $buildNotes 2>$null
$notesChanged = $LASTEXITCODE -ne 0 -or -not (& git ls-files -- $buildNotes)

if ($Context) {
    Write-Host "branch: $(git branch --show-current)  tag: $tag"
    Write-Host '--- status'
    git status --short 2>$null
    Write-Host '--- new buildNotes.md lines'
    git diff -U0 HEAD -- buildNotes.md 2>$null | Where-Object { $_ -match '^\+(?!\+\+)' } | ForEach-Object { $_.Substring(1) }
    Write-Host '--- last 10 subjects'
    git log -10 --format=%s
    Write-Host '--- last commit message (style sample)'
    git log -1 --format=%B
    if ($notesChanged) { Write-Host 'WARN: buildNotes.txt already changed since HEAD; overwrite it.' }
    return
}

# Fail on a bad message before the slow checks run.
if (-not $notesChanged) { throw 'buildNotes.txt is unchanged since the last commit; write the new message first.' }
$lines = @([IO.File]::ReadAllLines($buildNotes))
if ($lines.Count -lt 3 -or -not $lines[0].Trim() -or $lines[1].Trim() -or -not ($lines[2..($lines.Count - 1)] -join '').Trim()) {
    throw 'buildNotes.txt needs line 1, one blank line, then a non-empty body.'
}

# Run each script in-process as one pipeline so closeOut stays a no-op; print the tail only on failure.
$previousPipeline = $env:BASE_BUILD_PIPELINE
try {
    $env:BASE_BUILD_PIPELINE = '1'
    foreach ($step in 'prePush', 'push') {
        $log = [Collections.Generic.List[string]]::new()
        try { & "$PSScriptRoot\$step.ps1" *>&1 | ForEach-Object { $log.Add("$_") } }
        catch {
            $log | Select-Object -Last 40
            throw "$step.ps1 failed: $_"
        }
        Write-Host "$step.ps1 passed."
    }
} finally {
    $env:BASE_BUILD_PIPELINE = $previousPipeline
}

Set-Location -LiteralPath $repoRoot
Write-Host "--- pushed $(git log -1 --format=%h) to origin/$(git branch --show-current)"
git show -s --format=%B HEAD
git show --name-status --format= HEAD
