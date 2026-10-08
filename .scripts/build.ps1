#requires -Version 7.0
param([Alias('h')][switch]$Help, [string]$Architecture)
$ErrorActionPreference = 'Stop'
if ($Help) { Write-Host 'build.ps1 [-x86|-x64|-arm64]'; return }
. "$PSScriptRoot\scriptHelper.ps1"
Write-Host "=== building $projectName... ==="
Set-Location -LiteralPath $repoRoot
checkVerBuild $Architecture
$targetArchitecture = getArchitecture @($Architecture)
$targets = getBuildTargets $targetArchitecture
runNativeCommand dotnet @('restore', $solution) 'dotnet restore'
foreach ($target in $targets) {
    deleteDir $target.BinFolder
    New-Item -ItemType Directory -Path $target.BinFolder -Force | Out-Null
    $temp = "$env:TEMP\pwsh72exe-publish-$([guid]::NewGuid().ToString('N'))"
    $cliOutput = "$temp\cli"
    $guiOutput = "$temp\gui"
    try {
        $publishArguments = @('-c', 'Release', '-r', $target.RuntimeIdentifier, '--self-contained', 'false', '-p:PublishSingleFile=true', '-p:DebugType=None')
        runNativeCommand dotnet (@('publish', $cliProject) + $publishArguments + @('-o', $cliOutput)) "dotnet publish CLI $($target.Architecture)"
        runNativeCommand dotnet (@('publish', $guiProject) + $publishArguments + @('-o', $guiOutput)) "dotnet publish GUI $($target.Architecture)"
        Copy-Item -LiteralPath "$cliOutput\$cliExeName" -Destination $target.CliExePath -Force
        Copy-Item -LiteralPath "$guiOutput\$guiExeName" -Destination $target.GuiExePath -Force
        Copy-Item -LiteralPath $version -Destination "$($target.BinFolder)\Version" -Force
        publishCleanup $target.BinFolder
    }
    finally {
        if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
    }
}
closeOut 0
