using System.Diagnostics;

namespace pwsh72exe.Core;

public sealed class ScriptRunner
{
    public async Task<int> RunAsync(PackageOptions options, IEnumerable<string>? extraArgs = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(options.ScriptPath)) throw new FileNotFoundException("The PowerShell script was not found.", options.ScriptPath);
        using var process = Process.Start(PowerShellCommand.StartInfo(options, options.ScriptPath, extraArgs));
        if (process is null) throw new InvalidOperationException("PowerShell could not be started.");
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
