using System.Diagnostics;
using System.Text;

namespace pwsh72exe.Core;

public static class PowerShellCommand
{
    public static string HostFileName(PowerShellVersion version) =>
        version == PowerShellVersion.WindowsPowerShell ? "powershell.exe" : "pwsh.exe";

    public static string OutputName(string scriptPath)
    {
        var name = Path.GetFileNameWithoutExtension(scriptPath);
        if (string.IsNullOrWhiteSpace(name)) return "PowerShellApp";
        var invalid = Path.GetInvalidFileNameChars();
        var value = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(value) ? "PowerShellApp" : value;
    }

    // Host flags that precede -File; shared by direct runs, shortcuts, and generated EXEs.
    public static IReadOnlyList<string> HostFlags(PackageOptions options)
    {
        var flags = new List<string>();
        if (options.NoProfile) flags.Add("-NoProfile");
        if (options.NonInteractive) flags.Add("-NonInteractive");
        if (!string.IsNullOrWhiteSpace(options.ExecutionPolicy))
        {
            flags.Add("-ExecutionPolicy");
            flags.Add(options.ExecutionPolicy);
        }
        return flags;
    }

    public static IReadOnlyList<string> Arguments(PackageOptions options, string scriptPath, IEnumerable<string>? extraArgs = null) =>
        [.. HostFlags(options), "-File", Path.GetFullPath(scriptPath), .. extraArgs ?? []];

    // A .lnk stores one argument string; -WindowStyle Hidden is needed because the shortcut
    // window style can only minimize the console.
    public static string ShortcutArguments(PackageOptions options, string scriptPath)
    {
        IEnumerable<string> parts = Arguments(options, scriptPath);
        if (options.HideHostWindow) parts = parts.Prepend("Hidden").Prepend("-WindowStyle");
        return string.Join(" ", parts.Select(Quote));
    }

    public static ProcessStartInfo StartInfo(PackageOptions options, string scriptPath, IEnumerable<string>? extraArgs = null)
    {
        var info = new ProcessStartInfo
        {
            FileName = HostFileName(options.Version),
            UseShellExecute = options.RunAsAdministrator,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(scriptPath)) ?? Environment.CurrentDirectory
        };
        foreach (var argument in Arguments(options, scriptPath, extraArgs)) info.ArgumentList.Add(argument);
        if (options.RunAsAdministrator) info.Verb = "runas";
        if (options.HideHostWindow)
        {
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
        }
        return info;
    }

    public static string Quote(string value)
    {
        if (value.Length > 0 && value.All(c => !char.IsWhiteSpace(c) && c != '"')) return value;
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    public static string CSharpLiteral(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c.ToString()
            });
        }
        builder.Append('"');
        return builder.ToString();
    }
}
