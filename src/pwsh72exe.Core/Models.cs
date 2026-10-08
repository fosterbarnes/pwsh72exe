namespace pwsh72exe.Core;

public enum PowerShellVersion
{
    WindowsPowerShell,
    PowerShell7
}

public enum OutputKind
{
    PortableExe,
    Shortcut
}

public sealed record PackageOptions(
    string ScriptPath,
    string OutputPath,
    PowerShellVersion Version = PowerShellVersion.PowerShell7,
    string? IconPath = null,
    OutputKind Kind = OutputKind.PortableExe,
    bool NoProfile = false,
    bool NonInteractive = false,
    string ExecutionPolicy = "Bypass",
    bool HideHostWindow = false,
    bool RunAsAdministrator = false);
