#requires -Version 7.0
param([Alias('f')][switch]$Force, [Parameter(Position = 0)][string]$Message)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$subject = $Message
$body = ''
if (Test-Path -LiteralPath $buildNotes) {
    $lines = @([IO.File]::ReadAllLines($buildNotes))
    if ($lines.Count -gt 0 -and $lines[0].Trim()) {
        $subject = $lines[0].Trim()
        if ($lines.Count -ge 2) {
            if ($lines[1].Trim()) {
                throw 'buildNotes.txt must have one blank line after the first line, then the commit description.'
            }
            if ($lines.Count -gt 2) {
                $body = ($lines[2..($lines.Count - 1)] -join "`n").Trim()
            }
        }
    }
}
if (-not $subject) { throw 'Provide a commit message or add a non-empty first line to buildNotes.txt.' }
$branch = ((& git branch --show-current) | Out-String).Trim()
if ($LASTEXITCODE) { throw 'Could not determine the current branch.' }
if (-not $branch) { throw 'Detached HEAD; refusing to push.' }
runNativeCommand git @('add', '-A') 'git add'
$commitArgs = @('commit', '-m', $subject)
if ($body) { $commitArgs += @('-m', $body) }
runNativeCommand git $commitArgs 'git commit'
$pushArgs = @('push', 'origin', $branch); if ($Force) { $pushArgs += '--force' }
runNativeCommand git $pushArgs 'git push'
closeOut 3
