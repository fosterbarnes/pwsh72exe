namespace pwsh72exe.Core;

// Shared GUI/CLI defaults stored as key=value lines in pwsh72exe.config.ini beside the executables.
public sealed record AppConfig(
    string OutputPath,
    string Mode = "exe",
    string PowerShell = "7",
    string? IconPath = null,
    bool NoProfile = false,
    bool Hidden = false,
    bool NonInteractive = false,
    string ExecutionPolicy = "Bypass",
    bool RunAsAdministrator = false)
{
    public static string FileName => "pwsh72exe.config.ini";

    public static AppConfig Defaults => new(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

    public static AppConfig Load(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
        {
            var defaults = Defaults;
            defaults.Save(directory);
            return defaults;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var content = line.Trim();
            if (content.Length == 0 || content.StartsWith(';') || content.StartsWith('#')) continue;
            var separator = content.IndexOf('=');
            if (separator <= 0) throw new InvalidDataException($"Invalid config entry: {line}");
            values[content[..separator].Trim()] = content[(separator + 1)..].Trim();
        }

        return new AppConfig(
            Required(values, "OutputPath", path),
            values.GetValueOrDefault("Mode", "exe"),
            values.GetValueOrDefault("PowerShell", "7"),
            NullIfEmpty(values.GetValueOrDefault("IconPath")),
            ParseBool(values, "NoProfile", false, path),
            ParseBool(values, "Hidden", false, path),
            ParseBool(values, "NonInteractive", false, path),
            values.GetValueOrDefault("ExecutionPolicy", "Bypass"),
            ParseBool(values, "RunAsAdministrator", false, path));
    }

    public void Save(string directory) =>
        File.WriteAllText(Path.Combine(directory, FileName), Serialize());

    public PackageOptions ToPackageOptions() => new(
        "",
        OutputPath,
        ArgumentParser.ParsePowerShellVersion(PowerShell),
        IconPath,
        ArgumentParser.ParseOutputKind(Mode),
        NoProfile,
        NonInteractive,
        ExecutionPolicy,
        Hidden,
        RunAsAdministrator);

    public static AppConfig From(PackageOptions options) => new(
        options.OutputPath,
        options.Kind == OutputKind.Shortcut ? "shortcut" : "exe",
        options.Version == PowerShellVersion.WindowsPowerShell ? "5.1" : "7",
        options.IconPath,
        options.NoProfile,
        options.HideHostWindow,
        options.NonInteractive,
        options.ExecutionPolicy,
        options.RunAsAdministrator);

    private string Serialize() => string.Join(Environment.NewLine,
        $"OutputPath={OutputPath}",
        $"Mode={Mode}",
        $"PowerShell={PowerShell}",
        $"IconPath={IconPath ?? string.Empty}",
        $"NoProfile={Bool(NoProfile)}",
        $"Hidden={Bool(Hidden)}",
        $"NonInteractive={Bool(NonInteractive)}",
        $"ExecutionPolicy={ExecutionPolicy}",
        $"RunAsAdministrator={Bool(RunAsAdministrator)}") + Environment.NewLine;

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Required(Dictionary<string, string> values, string key, string path) =>
        values.TryGetValue(key, out var value) && value.Length > 0
            ? value
            : throw new InvalidDataException($"Missing config value '{key}': {path}");

    private static bool ParseBool(Dictionary<string, string> values, string key, bool fallback, string path) =>
        !values.TryGetValue(key, out var value) ? fallback :
        bool.TryParse(value, out var result) ? result :
        throw new InvalidDataException($"Config value '{key}' must be true or false: {path}");

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
