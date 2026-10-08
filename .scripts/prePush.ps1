#requires -Version 7.0
param([Alias('h')][switch]$Help, [Parameter(ValueFromRemainingArguments = $true)][string[]]$BuildArgs)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'prePush.ps1 [-x86|-x64|-arm64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$architecture = getArchitecture (@($BuildArgs) + @($args))
if ($architecture -eq 'help') { Write-Host 'prePush.ps1 [-x86|-x64|-arm64]'; return }
$previousPipeline = $env:BASE_BUILD_PIPELINE
try {
    $env:BASE_BUILD_PIPELINE = '1'
    buildAll $architecture
} finally {
    $env:BASE_BUILD_PIPELINE = $previousPipeline
}
Write-Host 'Pre-push build and packaging passed. No commit or push was performed.'
closeOut 0
