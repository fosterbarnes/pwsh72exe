#requires -Version 7.0
param([Alias('h')][switch]$Help, [switch]$Cli, [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppLaunchArgs)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host '.run.ps1 [-Cli] [-x86|-x64|-arm64] [-- app args...]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
$architectureArgs = @($AppLaunchArgs | Where-Object { "$_" -match '(?i)^(--x86|-x86|--86|-86|--x64|-x64|--64|-64|--arm64|-arm64|--arm|-arm|--help|-h)$' })
$architecture = getArchitecture $architectureArgs
if ($architecture -eq 'help') { Write-Host '.run.ps1 [-Cli] [-x86|-x64|-arm64] [-- app args...]'; return }
$target = getBuildTargets ($architecture ?? 'x64')
$forward = @($AppLaunchArgs | Where-Object { "$_" -notmatch '(?i)^(--x86|-x86|--86|-86|--x64|-x64|--64|-64|--arm64|-arm64|--arm|-arm)$' })
$project = if ($Cli) { $cliProject } else { $guiProject }
$framework = if ($Cli) { $dotnetFramework } else { $dotnetFrameworkGui }
while ($true) {
    setVerBuild $target.Architecture
    $dotnetArgs = @('run', '--project', $project, '--framework', $framework, '-c', 'Release', "-p:Platform=$($target.Architecture)")
    if ($forward) { $dotnetArgs += '--'; $dotnetArgs += $forward }
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList $dotnetArgs -WorkingDirectory $repoRoot -NoNewWindow -PassThru
    Write-Host "=== running $projectName... === `nq = quit `nr , up arrow = restart"
    $action = $null
    while (-not $proc.HasExited) {
        Start-Sleep -Milliseconds 50
        try { if (-not [Console]::KeyAvailable) { continue } } catch { continue }
        $key = [Console]::ReadKey($true)
        if ($key.Key -eq [ConsoleKey]::R -or $key.Key -eq [ConsoleKey]::UpArrow) { $action = 'restart'; break }
        if ($key.Key -eq [ConsoleKey]::Q) { $action = 'quit'; break }
    }
    if ($action) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    if ($action -ne 'restart') { break }
}; closeOut 0
