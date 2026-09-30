using pwsh72exe.Core;
using Xunit;

namespace pwsh72exe.Tests;

public sealed class PackageOptionsTests
{
    private static readonly PackageOptions Defaults = new("", ".");

    [Fact]
    public void DefaultsToPowerShell7AndExe()
    {
        var options = new PackageOptions("script.ps1", ".");
        Assert.Equal(PowerShellVersion.PowerShell7, options.Version);
        Assert.Equal(OutputKind.PortableExe, options.Kind);
        Assert.False(options.HideHostWindow);
        Assert.Equal("Bypass", options.ExecutionPolicy);
    }

    [Fact]
    public void ArgumentsIncludeSelectedFlags()
    {
        var options = new PackageOptions("script.ps1", ".", NoProfile: true, NonInteractive: true, ExecutionPolicy: "RemoteSigned");
        var argv = PowerShellCommand.Arguments(options, @"C:\tmp\hello.ps1");
        Assert.Equal(["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "RemoteSigned", "-File", @"C:\tmp\hello.ps1"], argv);
    }

    [Fact]
    public void StartInfoKeepsPathsAndArgumentsIntact()
    {
        var info = PowerShellCommand.StartInfo(new PackageOptions("a.ps1", "."), @"C:\a b\x.ps1", [@"C:\dir\", "say \"hi\""]);
        Assert.Equal("", info.Arguments);
        Assert.Equal(["-ExecutionPolicy", "Bypass", "-File", @"C:\a b\x.ps1", @"C:\dir\", "say \"hi\""], info.ArgumentList);
    }

    [Fact]
    public void HideHostDoesNotChangeArguments()
    {
        var shown = PowerShellCommand.Arguments(new PackageOptions("a.ps1", "."), "a.ps1");
        var hidden = PowerShellCommand.Arguments(new PackageOptions("a.ps1", ".", HideHostWindow: true), "a.ps1");
        Assert.Equal(shown, hidden);
        Assert.True(PowerShellCommand.StartInfo(new PackageOptions("a.ps1", ".", HideHostWindow: true), "a.ps1").CreateNoWindow);
    }

    [Fact]
    public void ShortcutArgumentsHideWindowOnlyWhenHidden()
    {
        Assert.StartsWith("-WindowStyle Hidden ", PowerShellCommand.ShortcutArguments(new PackageOptions("a.ps1", ".", HideHostWindow: true), @"C:\a.ps1"));
        Assert.DoesNotContain("-WindowStyle", PowerShellCommand.ShortcutArguments(new PackageOptions("a.ps1", "."), @"C:\a.ps1"));
        Assert.Contains("-File \"C:\\a b\\x.ps1\"", PowerShellCommand.ShortcutArguments(new PackageOptions("a.ps1", "."), @"C:\a b\x.ps1"));
    }

    [Theory]
    [InlineData(PowerShellVersion.PowerShell7)]
    [InlineData(PowerShellVersion.WindowsPowerShell)]
    public async Task RewriterReplacesScriptPathVariablesOnly(PowerShellVersion version)
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            const string script = """
                param([string]$Name = "é")
                function Get-Here { $PSScriptRoot }
                "$PSScriptRoot\data.txt"
                '$PSScriptRoot stays'
                $PSCommandPath
                $env:PSScriptRoot
                """;

            var result = await ScriptPathRewriter.RewriteAsync(script, version, directory.FullName);

            Assert.Equal("""
                param([string]$Name = "é")
                function Get-Here { ${env:PWSH72EXE_ROOT} }
                "${env:PWSH72EXE_ROOT}\data.txt"
                '$PSScriptRoot stays'
                ${env:PWSH72EXE_PATH}
                $env:PSScriptRoot
                """, result);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task RewriterRejectsScriptsWithParseErrors()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ScriptPathRewriter.RewriteAsync("function {", PowerShellVersion.PowerShell7, directory.FullName));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void AdministratorRunRequestsElevation()
    {
        var info = PowerShellCommand.StartInfo(new PackageOptions("a.ps1", ".", RunAsAdministrator: true), "a.ps1");

        Assert.True(info.UseShellExecute);
        Assert.Equal("runas", info.Verb);
    }

    [Fact]
    public void OutputNameUsesScriptBase()
    {
        Assert.Equal("hello", PowerShellCommand.OutputName(@"C:\tmp\hello.ps1"));
    }

    [Fact]
    public void ParserAcceptsSwitchesAndMode()
    {
        var values = ArgumentParser.Parse(["--script", "a.ps1", "--mode", "shortcut", "--noprofile", "--pwsh", "5.1", "--window", "hidden"]);
        var options = ArgumentParser.ToOptions(values, Defaults);
        Assert.Equal(OutputKind.Shortcut, options.Kind);
        Assert.Equal(PowerShellVersion.WindowsPowerShell, options.Version);
        Assert.True(options.NoProfile);
        Assert.True(options.HideHostWindow);
        Assert.Equal("a.ps1", options.ScriptPath);
    }

    [Fact]
    public void ConfigCreatesDefaultsBesideExecutableDirectory()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var config = AppConfig.Load(directory.FullName);

            Assert.Equal(AppConfig.FileName, Path.GetFileName(Directory.GetFiles(directory.FullName).Single()));
            Assert.Equal(AppConfig.Defaults.OutputPath, config.OutputPath);
            var configText = File.ReadAllText(Path.Combine(directory.FullName, AppConfig.FileName));
            Assert.Contains("OutputPath=", configText);
            Assert.Contains("NoProfile=false", configText);
            Assert.Contains("Hidden=false", configText);
            Assert.Contains("NonInteractive=false", configText);
            Assert.Contains("RunAsAdministrator=false", configText);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void ConfigRoundTripsPackageOptions()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var options = new PackageOptions("", @"C:\out dir", PowerShellVersion.WindowsPowerShell, @"C:\i.ico",
                OutputKind.Shortcut, true, true, "RemoteSigned", true, true);

            AppConfig.From(options).Save(directory.FullName);

            Assert.Equal(options, AppConfig.Load(directory.FullName).ToPackageOptions());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void ExplicitFlagsOverrideConfigDefaults()
    {
        var defaults = new PackageOptions(
            "",
            @"C:\configured",
            PowerShellVersion.WindowsPowerShell,
            @"C:\configured.ico",
            OutputKind.Shortcut,
            NoProfile: true,
            NonInteractive: true,
            ExecutionPolicy: "RemoteSigned",
            HideHostWindow: true);
        var values = ArgumentParser.Parse(["--output", @"C:\explicit", "--mode", "exe", "--pwsh", "7", "--window", "console"]);

        var options = ArgumentParser.ToOptions(values, defaults, @"C:\script.ps1");

        Assert.Equal(@"C:\explicit", options.OutputPath);
        Assert.Equal(OutputKind.PortableExe, options.Kind);
        Assert.Equal(PowerShellVersion.PowerShell7, options.Version);
        Assert.Equal(@"C:\configured.ico", options.IconPath);
        Assert.True(options.NoProfile);
        Assert.True(options.NonInteractive);
        Assert.Equal("RemoteSigned", options.ExecutionPolicy);
        Assert.False(options.HideHostWindow);
    }

    [Fact]
    public void CreateWithoutOutputUsesConfiguredOutput()
    {
        var defaults = new AppConfig(@"C:\configured").ToPackageOptions();

        var options = ArgumentParser.ToOptions(ArgumentParser.Parse(["-script", "a.ps1"]), defaults);

        Assert.Equal(@"C:\configured", options.OutputPath);
        Assert.Equal("a.ps1", options.ScriptPath);
    }

    [Fact]
    public void ConfigReadsWindowsPathsWithoutEscaping()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, AppConfig.FileName),
                "OutputPath=C:\\Users\\Foster\\Desktop\nIconPath=C:\\Users\\Foster\\icon.ico\nHidden=true\n");

            var config = AppConfig.Load(directory.FullName);

            Assert.Equal(@"C:\Users\Foster\Desktop", config.OutputPath);
            Assert.Equal(@"C:\Users\Foster\icon.ico", config.IconPath);
            Assert.True(config.Hidden);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void BooleanFlagsCanOverrideConfigValues()
    {
        var values = ArgumentParser.Parse(["--noprofile", "false", "--noninteractive", "false", "--hidden", "false", "--admin"]);
        var defaults = new PackageOptions("", ".", NoProfile: true, NonInteractive: true, HideHostWindow: true);

        var options = ArgumentParser.ToOptions(values, defaults, "script.ps1");

        Assert.False(options.NoProfile);
        Assert.False(options.NonInteractive);
        Assert.False(options.HideHostWindow);
        Assert.True(options.RunAsAdministrator);
    }

    [Fact]
    public void SingleDashFlagsAreAccepted()
    {
        var values = ArgumentParser.Parse(["-script", "script.ps1", "-mode", "shortcut", "-noprofile", "false"]);

        var options = ArgumentParser.ToOptions(values, Defaults);

        Assert.Equal("script.ps1", options.ScriptPath);
        Assert.Equal(OutputKind.Shortcut, options.Kind);
        Assert.False(options.NoProfile);
    }
}
