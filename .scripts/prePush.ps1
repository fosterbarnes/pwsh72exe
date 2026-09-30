#requires -Version 7.0
param([Alias('h')][switch]$Help, [Parameter(ValueFromRemainingArguments = $true)][string[]]$BuildArgs)
$ErrorActionPreference = 'Stop'
trap { Write-Error -ErrorRecord $_; exit 1 }
if ($Help) { Write-Host 'prePush.ps1 [-x86|-x64|-arm64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$architecture = getArchitecture (@($BuildArgs) + @($args))
if ($architecture -eq 'help') { Write-Host 'prePush.ps1 [-x86|-x64|-arm64]'; return }
buildAll $architecture
Write-Host 'Pre-push build and packaging passed. No commit or push was performed.'
closeOut 3
