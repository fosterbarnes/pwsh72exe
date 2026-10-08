namespace pwsh72exe.Core;

public sealed class Packager
{
    public Task<string> CreateAsync(PackageOptions options, CancellationToken cancellationToken = default)
    {
        if (options.RunAsAdministrator && options.Kind == OutputKind.Shortcut)
            throw new ArgumentException("Administrator elevation is supported for portable EXE output only.", nameof(options));
        if (options.Kind != OutputKind.Shortcut) return new PowerExePackager().CreateAsync(options, cancellationToken);
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Shortcuts require Windows.");
        return Task.FromResult(new ShortcutPackager().Create(options));
    }
}
