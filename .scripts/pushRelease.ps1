#requires -Version 7.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
runNativeCommand git @('tag', '-f', $tag) 'git tag'
runNativeCommand git @('push', 'origin', "refs/tags/$tag", '--force') 'git push tag'
$assets = @(Get-ChildItem -LiteralPath $publishFolder -File)
if (-not $assets) { throw "No release assets found in $publishFolder" }
$assetArgs = @($assets | ForEach-Object FullName)

$releaseTitle = $tag
$releaseNotes = ''
if (Test-Path -LiteralPath $buildNotes) {
    $lines = @([IO.File]::ReadAllLines($buildNotes))
    if ($lines.Count -gt 0 -and $lines[0].Trim()) {
        $releaseTitle = $lines[0].Trim()
        if ($lines.Count -ge 2) {
            if ($lines[1].Trim()) {
                throw 'buildNotes.txt must have one blank line after the first line, then the commit description.'
            }
            if ($lines.Count -gt 2) {
                $releaseNotes = ($lines[2..($lines.Count - 1)] -join "`n").Trim()
            }
        }
    }
}

$releaseArgs = @('release', 'create', $tag, '--title', $releaseTitle, '--repo', $ghRepo)
if ($releaseNotes) { $releaseArgs += @('--notes', $releaseNotes) } else { $releaseArgs += '--generate-notes' }
runNativeCommand gh ($releaseArgs + $assetArgs) 'gh release create'
closeOut 3
