using System.Diagnostics;
using System.Text;

namespace pwsh72exe.Core;

public sealed class PowerExePackager
{
    public async Task<string> CreateAsync(PackageOptions options, CancellationToken cancellationToken = default)
    {
        Validate(options);

        var outputDirectory = Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(outputDirectory);

        var safeName = PowerShellCommand.OutputName(options.ScriptPath);
        var projectDirectory = Path.Combine(Path.GetTempPath(), "pwsh72exe", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        try
        {
            var script = await ScriptPathRewriter.RewriteAsync(
                await File.ReadAllTextAsync(options.ScriptPath, cancellationToken), options.Version, projectDirectory, cancellationToken);
            var scriptBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
            var projectPath = Path.Combine(projectDirectory, $"{safeName}.csproj");
            var programPath = Path.Combine(projectDirectory, "Program.cs");
            var iconFileName = options.IconPath is null ? null : $"{safeName}.ico";

            if (options.IconPath is not null)
                File.Copy(options.IconPath, Path.Combine(projectDirectory, iconFileName!), true);

            await File.WriteAllTextAsync(projectPath, CreateProjectFile(safeName, iconFileName, !options.HideHostWindow, options.RunAsAdministrator), cancellationToken);
            if (options.RunAsAdministrator)
                await File.WriteAllTextAsync(Path.Combine(projectDirectory, "app.manifest"), AdministratorManifest, cancellationToken);
            await File.WriteAllTextAsync(programPath, CreateProgramFile(scriptBase64, options), cancellationToken);

            var publishDirectory = Path.Combine(projectDirectory, "publish");
            var arguments = $"publish \"{projectPath}\" -c Release -r win-x64 --self-contained false -o \"{publishDirectory}\" /p:PublishSingleFile=true /nologo";
            await RunDotnetAsync(arguments, projectDirectory, cancellationToken);

            var generatedExe = Path.Combine(publishDirectory, $"{safeName}.exe");
            var destination = Path.Combine(outputDirectory, $"{safeName}.exe");
            File.Copy(generatedExe, destination, true);
            return destination;
        }
        finally
        {
            try { Directory.Delete(projectDirectory, true); } catch { /* best effort cleanup */ }
        }
    }

    private static void Validate(PackageOptions options)
    {
        if (!File.Exists(options.ScriptPath)) throw new FileNotFoundException("The PowerShell script was not found.", options.ScriptPath);
        if (options.IconPath is not null && !File.Exists(options.IconPath)) throw new FileNotFoundException("The icon was not found.", options.IconPath);
        if (options.IconPath is not null && !Path.GetExtension(options.IconPath).Equals(".ico", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The icon must be an .ico file.", nameof(options));
    }

    private const string AdministratorManifest = """
        <?xml version="1.0" encoding="utf-8"?>
        <assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
          <assemblyIdentity version="1.0.0.0" name="pwsh72exe.Generated" />
          <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
            <security>
              <requestedPrivileges>
                <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
              </requestedPrivileges>
            </security>
          </trustInfo>
        </assembly>
        """;

    private static string CreateProjectFile(string name, string? iconFileName, bool showConsoleWindow, bool runAsAdministrator)
    {
        var icon = iconFileName is null ? "" : $"\n  <ItemGroup><Content Include=\"{iconFileName}\" /></ItemGroup>";
        var applicationIcon = iconFileName is null ? "" : $"\n    <ApplicationIcon>{iconFileName}</ApplicationIcon>";
        var manifest = runAsAdministrator ? "\n    <ApplicationManifest>app.manifest</ApplicationManifest>" : "";
        return string.Join(Environment.NewLine,
            "<Project Sdk=\"Microsoft.NET.Sdk\">", "  <PropertyGroup>", $"    <OutputType>{(showConsoleWindow ? "Exe" : "WinExe")}</OutputType>",
            "    <TargetFramework>net10.0</TargetFramework>", "    <ImplicitUsings>enable</ImplicitUsings>",
            "    <Nullable>enable</Nullable>", $"    <AssemblyName>{name}</AssemblyName>{applicationIcon}{manifest}",
            "  </PropertyGroup>" + icon, "</Project>");
    }

    private static string CreateProgramFile(string scriptBase64, PackageOptions options)
    {
        var host = PowerShellCommand.CSharpLiteral(PowerShellCommand.HostFileName(options.Version));
        var prefixLiteral = "new string[] { " +
            string.Join(", ", PowerShellCommand.HostFlags(options).Select(PowerShellCommand.CSharpLiteral)) + " }";
        var hidden = options.HideHostWindow ? "true" : "false";
        return """
using System.Diagnostics;
using System.Text;

var script = Encoding.UTF8.GetString(Convert.FromBase64String("__SCRIPT__"));
var scriptPath = Path.Combine(Path.GetTempPath(), "pwsh72exe", Guid.NewGuid().ToString("N") + ".ps1");
Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
await File.WriteAllTextAsync(scriptPath, script, Encoding.UTF8);

try
{
    var info = new ProcessStartInfo
    {
        FileName = __HOST__,
        UseShellExecute = false,
        CreateNoWindow = __HIDDEN__,
        WindowStyle = __HIDDEN__ ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
    };
    info.Environment["__ROOT_VAR__"] = Path.GetDirectoryName(Environment.ProcessPath!)!;
    info.Environment["__PATH_VAR__"] = Environment.ProcessPath!;
    foreach (var argument in __PREFIX__) info.ArgumentList.Add(argument);
    info.ArgumentList.Add("-File");
    info.ArgumentList.Add(scriptPath);
    foreach (var argument in args) info.ArgumentList.Add(argument);
    using var process = Process.Start(info);
    if (process is null) throw new InvalidOperationException("PowerShell could not be started.");
    await process.WaitForExitAsync();
    Environment.ExitCode = process.ExitCode;
}
finally
{
    try { File.Delete(scriptPath); } catch { }
}
"""
            .Replace("__SCRIPT__", scriptBase64)
            .Replace("__HOST__", host)
            .Replace("__PREFIX__", prefixLiteral)
            .Replace("__HIDDEN__", hidden)
            .Replace("__ROOT_VAR__", ScriptPathRewriter.RootVariable)
            .Replace("__PATH_VAR__", ScriptPathRewriter.PathVariable);
    }

    private static async Task RunDotnetAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo { FileName = "dotnet", Arguments = arguments, WorkingDirectory = workingDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
        if (process is null) throw new InvalidOperationException("The dotnet SDK could not be started.");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new InvalidOperationException($"dotnet publish failed:\n{output}\n{error}");
    }
}
