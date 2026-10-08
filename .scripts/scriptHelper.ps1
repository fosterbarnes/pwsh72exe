#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$projectName = 'pwsh72exe'
$cliExeName = 'pwsh72exe.Cli.exe'
$guiExeName = 'pwsh72exe.Gui.exe'
$solution = "$repoRoot\pwsh72exe.sln"
$srcRoot = "$repoRoot\src"
$cliProject = "$srcRoot\pwsh72exe.Cli\pwsh72exe.Cli.csproj"
$guiProject = "$srcRoot\pwsh72exe\pwsh72exe.csproj"
$dotnetFramework = 'net10.0'
$dotnetFrameworkGui = 'net10.0-windows'
$versionFolder = "$repoRoot\.version"
$version = "$versionFolder\version"
$versionBuild = "$versionFolder\versionBuild"
$versionTag = "$versionFolder\versionTag"
$buildNotes = "$repoRoot\buildNotes.txt"
$readme = "$repoRoot\README.md"
$publishFolder = "$repoRoot\publish"
$installerFolder = "$repoRoot\.installer"
$installerOutput = "$installerFolder\Output"
$iconDir = "$repoRoot\.res\icon"
$iconPng = "$iconDir\icon.png"
$iconIco = "$iconDir\icon.ico"
$wizardSmallPng = "$iconDir\installer-wizard-small.png"
$wizardLargePng = "$iconDir\installer-wizard-large.png"
$licenseFile = "$repoRoot\LICENSE"
$imageSizes = @(16, 32, 48, 64, 128, 256)
$appPublisher = 'fosterbarnes'
$appURL = "https://github.com/$appPublisher/$projectName"
$ghRepo = "$appPublisher/$projectName"
$versionContents = ([IO.File]::ReadAllText($version)).Trim()
$versionTagContents = if (Test-Path -LiteralPath $versionTag) { ([IO.File]::ReadAllText($versionTag)).Trim() } else { '' }
$tag = if ($versionTagContents) { $versionTagContents } else { "v$versionContents" }
$buttonURL = 'https://raw.githubusercontent.com/fosterbarnes/res/main/btn'
$buildTargets = @(
    @{ Architecture = 'x64'; RuntimeIdentifier = 'win-x64'; BinFolder = "$publishFolder\build\x64"; CliExePath = "$publishFolder\build\x64\$cliExeName"; GuiExePath = "$publishFolder\build\x64\$guiExeName"; InstallerName = "$projectName-x64-installer.exe"; InstallerScript = "$installerFolder\$projectName.x64.installer.iss"; InstallerButton = 'x64Installer.svg'; PortableButton = 'x64Portable.svg' }
)
$noBom = New-Object System.Text.UTF8Encoding $false
$weztermExe = (Get-Command wezterm.exe -ErrorAction SilentlyContinue)?.Source

function readVerFile {
    param([string]$LiteralPath = $version)
    $lines = @(([IO.File]::ReadAllText($LiteralPath) -split '\r?\n' | ForEach-Object { $_.Trim() }))
    while ($lines.Count -lt 3) { $lines += '' }
    $lines
}

function writeFileNoBom {
    param([Parameter(Mandatory)][string]$LiteralPath, [Parameter(Mandatory)][string]$Content)
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

function checkVerBuild {
    param([string]$Architecture)
    if (Test-Path -LiteralPath $versionBuild) { return }
    setVerBuild ([string]::IsNullOrWhiteSpace($Architecture) ? 'x64' : $Architecture)
}

function getArchitecture {
    param([string[]]$FlagArgs)
    $found = @()
    foreach ($arg in @($FlagArgs)) {
        if ([string]::IsNullOrWhiteSpace("$arg")) { continue }
        switch -Regex ("$arg".Trim()) {
            '(?i)^(x86|--x86|-x86|--86|-86)$' { $found += 'x86' }
            '(?i)^(x64|--x64|-x64|--64|-64)$' { $found += 'x64' }
            '(?i)^(arm64|--arm64|-arm64|--arm|-arm)$' { $found += 'arm64' }
            '(?i)^(--help|-h)$' { return 'help' }
            default { throw "Unknown architecture flag: $arg" }
        }
    }
    $unique = @($found | Select-Object -Unique)
    if ($unique.Count -gt 1) { throw "Conflicting architecture flags: $($unique -join ', ')" }
    if ($unique.Count -eq 1) { return $unique[0] }
    $null
}

function getBuildTargets {
    param([string]$Architecture)
    if (-not $Architecture) { return $buildTargets }
    $target = @($buildTargets | Where-Object Architecture -eq $Architecture)
    if (-not $target) { throw "No build target for architecture: $Architecture" }
    return ,$target[0]
}

function deleteDir {
    param([Parameter(Mandatory)][string]$Path)
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Recurse -Force }
    if (Test-Path -LiteralPath $Path) { throw "Could not remove '$Path'." }
}

function publishCleanup {
    param([Parameter(Mandatory)][string]$BinFolder)
    Get-ChildItem -LiteralPath $BinFolder -Filter '*.pdb' -File -ErrorAction SilentlyContinue | Remove-Item -Force
}

function runNativeCommand {
    param([Parameter(Mandatory)][string]$FilePath, [Parameter(Mandatory)]$ArgumentList, [Parameter(Mandatory)][string]$Name)
    & $FilePath @ArgumentList
    if ($LASTEXITCODE) { throw "$Name failed (exit $LASTEXITCODE)." }
}

function writeClearedLine {
    param([Parameter(Mandatory)][string]$Text, [Parameter(Mandatory)][int]$PadWidth, [switch]$NoNewline)
    Write-Host "`r$Text$(' ' * [Math]::Max(0, $PadWidth - $Text.Length))" -NoNewline:$NoNewline
}

function closeOut {
    param([int]$Seconds = 5, [switch]$KeepOpen)
    if ($KeepOpen) { return }
    if ($env:BASE_BUILD_PIPELINE) { return }
    if ($Seconds -lt 0) { $Seconds = 0 }
    if ($Seconds -gt 0) {
        $pad = "closing after $Seconds seconds..."
        foreach ($n in $Seconds..1) {
            writeClearedLine -Text "closing after $n seconds..." -PadWidth $pad.Length -NoNewline
            Start-Sleep -Seconds 1
        }
        writeClearedLine -Text 'closing...' -PadWidth $pad.Length
    }
    $caller = $MyInvocation.PSCommandPath
    if ([string]::IsNullOrWhiteSpace($caller)) { return }
    $argv = [Environment]::GetCommandLineArgs()
    $fileArg = $null
    for ($i = 0; $i -lt $argv.Length; $i++) {
        if ("$($argv[$i])" -match '^(?i)-File$|^(?i)-f$') {
            if ($i + 1 -lt $argv.Length) { $fileArg = $argv[$i + 1] }
            break
        }
    }
    if ([string]::IsNullOrWhiteSpace($fileArg)) { return }
    try {
        $fileFull = [IO.Path]::GetFullPath($fileArg)
        $callerFull = [IO.Path]::GetFullPath($caller)
    } catch { return }
    if (-not [string]::Equals($fileFull, $callerFull, [StringComparison]::OrdinalIgnoreCase)) { return }
    try {
        if ($env:SCRIPT_OWN_PANE -and $env:WEZTERM_PANE -and $weztermExe) { & $weztermExe @('cli', 'kill-pane', '--pane-id', $env:WEZTERM_PANE) }
    } catch { }
    [Environment]::Exit(0)
}

function convertToIco {
    param([Parameter(Mandatory)][string]$InputPath)
    $source = (Resolve-Path -LiteralPath $InputPath).Path
    $output = [IO.Path]::ChangeExtension($source, '.ico')
    $arguments = [Collections.Generic.List[string]]::new()
    $arguments.Add($source)
    foreach ($size in $imageSizes) {
        $arguments.AddRange(@('(', '-clone', '0', '-resize', "${size}x${size}", ')'))
    }
    $arguments.Add('-delete'); $arguments.Add('0'); $arguments.Add($output)
    runNativeCommand -FilePath 'magick' -ArgumentList $arguments.ToArray() -Name 'ImageMagick ICO conversion'
    $output
}

function writeWizardImages {
    param([string]$InputPath = $iconPng)
    $source = (Resolve-Path -LiteralPath $InputPath).Path
    runNativeCommand -FilePath 'magick' -ArgumentList @($source, '-resize', '256x256', $wizardSmallPng) -Name 'ImageMagick wizard small resize'
    runNativeCommand -FilePath 'magick' -ArgumentList @($source, '-background', 'black', '-gravity', 'center', '-resize', '240x459', '-extent', '240x459', $wizardLargePng) -Name 'ImageMagick wizard large resize'
}

function ensureInstallerImages {
    if (-not (Test-Path -LiteralPath $iconPng)) { throw "Missing source icon: $iconPng" }
    if (-not (Test-Path -LiteralPath $iconIco)) { convertToIco -InputPath $iconPng | Out-Null }
    if (-not (Test-Path -LiteralPath $wizardSmallPng) -or -not (Test-Path -LiteralPath $wizardLargePng)) {
        writeWizardImages -InputPath $iconPng
    }
}

function openUrl {
    param([Parameter(Mandatory)][string]$Url)
    if ([string]::IsNullOrWhiteSpace($Url)) { throw 'openUrl requires a URL.' }
    Start-Process $Url
}

function buildAssetName {
    param([Parameter(Mandatory)][ValidateSet('Installer', 'Portable')][string]$Kind, [Parameter(Mandatory)][string]$Architecture)
    $extension = if ($Kind -eq 'Installer') { 'exe' } else { 'zip' }
    "${projectName}_v${versionContents}_windows-${Architecture}.${extension}"
}

function buildAll {
    param([string]$Architecture)
    $previousPipeline = $env:BASE_BUILD_PIPELINE
    try {
        $env:BASE_BUILD_PIPELINE = '1'
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
    } finally {
        $env:BASE_BUILD_PIPELINE = $previousPipeline
    }
}

Set-Location -LiteralPath $repoRoot
