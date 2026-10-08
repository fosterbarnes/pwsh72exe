using System.Diagnostics;
using System.Text;

namespace pwsh72exe.Core;

// Generated EXEs run their script from %TEMP%, so $PSScriptRoot/$PSCommandPath would point there.
// This rewrites those variables (using PowerShell's own parser) to env vars the EXE sets to its own
// folder and path. Single-quoted strings are untouched because the parser does not see variables there.
public static class ScriptPathRewriter
{
    public const string RootVariable = "PWSH72EXE_ROOT";
    public const string PathVariable = "PWSH72EXE_PATH";

    private const string _REWRITE_SCRIPT = """
        param([string]$Path, [string]$Out)
        $ErrorActionPreference = 'Stop'
        $text = [IO.File]::ReadAllText($Path)
        $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($text, [ref]$null, [ref]$errors)
        if ($errors) { throw "Script has parse errors: $($errors[0].Message) (line $($errors[0].Extent.StartLineNumber))" }
        $map = @{ PSScriptRoot = '${env:PWSH72EXE_ROOT}'; PSCommandPath = '${env:PWSH72EXE_PATH}' }
        $hits = $ast.FindAll({
            param($node)
            $node -is [System.Management.Automation.Language.VariableExpressionAst] -and
                $node.VariablePath.IsUnqualified -and $map.ContainsKey($node.VariablePath.UserPath)
        }, $true) | Sort-Object { $_.Extent.StartOffset } -Descending
        foreach ($hit in $hits) {
            $text = $text.Remove($hit.Extent.StartOffset, $hit.Extent.EndOffset - $hit.Extent.StartOffset).Insert($hit.Extent.StartOffset, $map[$hit.VariablePath.UserPath])
        }
        [IO.File]::WriteAllText($Out, $text, [Text.UTF8Encoding]::new($true))
        """;

    public static async Task<string> RewriteAsync(string script, PowerShellVersion version, string workDirectory,
        CancellationToken cancellationToken = default)
    {
        var rewriterPath = Path.Combine(workDirectory, "rewrite.ps1");
        var inputPath = Path.Combine(workDirectory, "input.ps1");
        var outputPath = Path.Combine(workDirectory, "output.ps1");
        await File.WriteAllTextAsync(rewriterPath, _REWRITE_SCRIPT, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(inputPath, script, Encoding.UTF8, cancellationToken);

        var info = new ProcessStartInfo
        {
            FileName = PowerShellCommand.HostFileName(version),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in (string[])["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
            "-File", rewriterPath, "-Path", inputPath, "-Out", outputPath])
            info.ArgumentList.Add(argument);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("PowerShell could not be started to prepare the script.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Preparing the script failed:\n{await output}\n{await error}");
        return await File.ReadAllTextAsync(outputPath, cancellationToken);
    }
}
