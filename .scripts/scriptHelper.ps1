#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$projectName = 'pwsh72exe'
$cliExeName = 'pwsh72exe.Cli.exe'
$guiExeName = 'pwsh72exe.Gui.exe'
$solution = "$repoRoot\pwsh72exe.sln"
$cliProject = "$repoRoot\pwsh72exe.Cli\pwsh72exe.Cli.csproj"
$guiProject = "$repoRoot\pwsh72exe\pwsh72exe.csproj"
$dotnetFramework = 'net10.0'
$dotnetFrameworkGui = 'net10.0-windows'
$versionFolder = "$repoRoot\.version"
$version = "$versionFolder\version"
$versionBuild = "$versionFolder\versionBuild"
$versionTag = "$versionFolder\versionTag"
$buildNotes = "$repoRoot\buildNotes.txt"
$readme = "$repoRoot\README.md"
$publishFolder = "$repoRoot\publish"
$appPublisher = 'fosterbarnes'
$appURL = "https://github.com/$appPublisher/$projectName"
$ghRepo = "$appPublisher/$projectName"
$versionContents = ([IO.File]::ReadAllText($version)).Trim()
$versionTagContents = if (Test-Path -LiteralPath $versionTag) { ([IO.File]::ReadAllText($versionTag)).Trim() } else { '' }
$tag = if ($versionTagContents) { $versionTagContents } else { "v$versionContents" }
$buildTargets = @(
    @{ Architecture = 'x64'; RuntimeIdentifier = 'win-x64'; BinFolder = "$publishFolder\build\x64"; CliExePath = "$publishFolder\build\x64\$cliExeName"; GuiExePath = "$publishFolder\build\x64\$guiExeName" }
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
    param([int]$Seconds = 5)
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
    if ($Seconds -lt 0) { $Seconds = 0 }
    if ($Seconds -gt 0) {
        $pad = "closing after $Seconds seconds..."
        foreach ($n in $Seconds..1) {
            writeClearedLine -Text "closing after $n seconds..." -PadWidth $pad.Length -NoNewline
            Start-Sleep -Seconds 1
        }
        writeClearedLine -Text 'closing...' -PadWidth $pad.Length
    }
    try {
        if ($env:SCRIPT_OWN_PANE -and $env:WEZTERM_PANE -and $weztermExe) { & $weztermExe @('cli', 'kill-pane', '--pane-id', $env:WEZTERM_PANE) }
    } catch { }
    [Environment]::Exit(0)
}

function buildAssetName {
    param([Parameter(Mandatory)][string]$Architecture)
    "${projectName}_v${versionContents}_windows-${Architecture}.zip"
}

function buildAll {
    param([string]$Architecture)
    $params = @{}; if ($Architecture) { $params.Architecture = $Architecture }
    runNativeCommand -FilePath "$PSScriptRoot\build.ps1" -ArgumentList $params -Name 'build.ps1'
    New-Item -ItemType Directory -Path $publishFolder -Force | Out-Null
    foreach ($target in (getBuildTargets $Architecture)) {
        Compress-Archive -Path "$($target.BinFolder)\*" -DestinationPath "$publishFolder\$(buildAssetName -Architecture $target.Architecture)" -Force
    }
    runNativeCommand -FilePath "$PSScriptRoot\updateReadme.ps1" -ArgumentList @{} -Name 'updateReadme.ps1'
}

Set-Location -LiteralPath $repoRoot
