#requires -Version 7.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot
if (-not (Test-Path -LiteralPath $readme)) { throw "README not found: $readme" }
$content = [IO.File]::ReadAllText($readme)
$start = '<!-- Quick Reference -->'
$end = '<!-- End Quick Reference -->'
$startIndex = $content.IndexOf($start, [StringComparison]::Ordinal)
$endIndex = $content.IndexOf($end, [StringComparison]::Ordinal)
if ($startIndex -lt 0 -or $endIndex -lt $startIndex) { throw 'README Quick Reference markers are missing.' }
$lines = @(
    $start
    '<table border="0">'
    '<tbody>'
    '<tr>'
    "<td valign=`"top`"><a href=`"$appURL/releases/download/$tag/$(buildAssetName -Kind Installer -Architecture x64)`"><img src=`"https://raw.githubusercontent.com/fosterbarnes/res/main/btn/x64Installer.svg`" width=`"180`" height=`"auto`" alt=`"Download Windows x64 installer`"/></a></td>"
    "<td valign=`"top`"><a href=`"$appURL/releases/download/$tag/$(buildAssetName -Kind Portable -Architecture x64)`"><img src=`"https://raw.githubusercontent.com/fosterbarnes/res/main/btn/x64Portable.svg`" width=`"180`" height=`"auto`" alt=`"Download Windows x64 portable ZIP`"/></a></td>"
    '</tr>'
    '</tbody>'
    '</table>'
    ''
    $end
)
$prefix = $content.Substring(0, $startIndex) ; $suffix = $content.Substring($endIndex + $end.Length)
writeFileNoBom -LiteralPath $readme -Content ($prefix + ($lines -join "`n") + $suffix)
closeOut 3
