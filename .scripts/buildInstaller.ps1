#requires -Version 7.0
param([Alias('h')][switch]$Help, [string]$Architecture)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'buildInstaller.ps1 [-x64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Write-Host "--- building $projectName installer... ---"
Set-Location -LiteralPath $repoRoot
$targetArchitecture = getArchitecture @($Architecture)
$iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue)?.Source
if (-not $iscc) { $iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' }
if (-not (Test-Path -LiteralPath $iscc)) { throw "Inno Setup compiler not found: $iscc" }
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
    foreach ($exePath in $target.CliExePath, $target.GuiExePath) {
        if (-not (Test-Path -LiteralPath $exePath)) { throw "Missing publish output: $exePath" }
    }
    runNativeCommand $iscc (@($isccDefines) + $target.InstallerScript) "ISCC $($target.Architecture)"
}; closeOut 3
