using System.Runtime.Versioning;

namespace pwsh72exe.Core;

public sealed class ShortcutPackager
{
    [SupportedOSPlatform("windows")]
    public string Create(PackageOptions options)
    {
        if (!File.Exists(options.ScriptPath)) throw new FileNotFoundException("The PowerShell script was not found.", options.ScriptPath);
        if (options.IconPath is not null && !File.Exists(options.IconPath)) throw new FileNotFoundException("The icon was not found.", options.IconPath);

        var outputDirectory = Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(outputDirectory);
        var destination = Path.Combine(outputDirectory, PowerShellCommand.OutputName(options.ScriptPath) + ".lnk");
        var scriptPath = Path.GetFullPath(options.ScriptPath);

        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell is not available.");
        dynamic shell = Activator.CreateInstance(type) ?? throw new InvalidOperationException("WScript.Shell could not be created.");
        dynamic shortcut = shell.CreateShortcut(destination);
        shortcut.TargetPath = PowerShellCommand.HostFileName(options.Version);
        shortcut.Arguments = PowerShellCommand.ShortcutArguments(options, scriptPath);
        shortcut.WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? outputDirectory;
        shortcut.WindowStyle = options.HideHostWindow ? 7 : 1;
        if (options.IconPath is not null) shortcut.IconLocation = Path.GetFullPath(options.IconPath);
        shortcut.Save();
        return destination;
    }
}
