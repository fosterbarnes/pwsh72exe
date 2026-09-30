namespace pwsh72exe.Core;

public static class ArgumentParser
{
    private static readonly HashSet<string> Switches = new(StringComparer.OrdinalIgnoreCase)
    {
        "noprofile", "noninteractive", "hidden", "admin"
    };

    public static Dictionary<string, string> Parse(string[] arguments)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < arguments.Length; i++)
        {
            if (!arguments[i].StartsWith('-')) throw new ArgumentException($"Expected a flag, got {arguments[i]}.");
            var name = arguments[i].TrimStart('-');
            if (name.Length == 0) throw new ArgumentException($"Expected a flag, got {arguments[i]}.");
            if (Switches.Contains(name))
            {
                values[name] = i + 1 < arguments.Length && !arguments[i + 1].StartsWith('-')
                    ? arguments[++i]
                    : "true";
                continue;
            }
            if (i + 1 >= arguments.Length) throw new ArgumentException($"Expected a value for --{name}.");
            values[name] = arguments[++i];
        }
        return values;
    }

    // Explicit flags override the supplied defaults (normally the shared config).
    public static PackageOptions ToOptions(Dictionary<string, string> values, PackageOptions defaults,
        string? scriptOverride = null)
    {
        var version = values.TryGetValue("pwsh", out var pwsh)
            ? ParsePowerShellVersion(pwsh)
            : defaults.Version;
        var hide = values.TryGetValue("hidden", out var hidden)
            ? ParseBool(hidden, "hidden")
            : values.TryGetValue("window", out var window)
                ? window.Equals("hidden", StringComparison.OrdinalIgnoreCase)
                : defaults.HideHostWindow;
        return new PackageOptions(
            scriptOverride ?? values.GetValueOrDefault("script", defaults.ScriptPath),
            values.GetValueOrDefault("output", defaults.OutputPath),
            version,
            values.TryGetValue("icon", out var icon) ? icon : defaults.IconPath,
            values.TryGetValue("mode", out var mode) ? ParseOutputKind(mode) : defaults.Kind,
            values.TryGetValue("noprofile", out var noProfile) ? ParseBool(noProfile, "noprofile") : defaults.NoProfile,
            values.TryGetValue("noninteractive", out var nonInteractive) ? ParseBool(nonInteractive, "noninteractive") : defaults.NonInteractive,
            values.GetValueOrDefault("execution-policy", defaults.ExecutionPolicy),
            hide,
            values.TryGetValue("admin", out var admin) ? ParseBool(admin, "admin") : defaults.RunAsAdministrator);
    }

    public static PowerShellVersion ParsePowerShellVersion(string value) => value.ToLowerInvariant() switch
    {
        "5.1" or "windows" or "windows-powershell" => PowerShellVersion.WindowsPowerShell,
        "7" or "pwsh" => PowerShellVersion.PowerShell7,
        _ => throw new ArgumentException($"Unsupported PowerShell version: {value}.")
    };

    public static OutputKind ParseOutputKind(string value) => value.ToLowerInvariant() switch
    {
        "shortcut" or "lnk" => OutputKind.Shortcut,
        "exe" or "portable" => OutputKind.PortableExe,
        _ => throw new ArgumentException($"Unsupported mode: {value}.")
    };

    private static bool ParseBool(string value, string name) => bool.TryParse(value, out var result)
        ? result
        : throw new ArgumentException($"The value for --{name} must be true or false.");
}
