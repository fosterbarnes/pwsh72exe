using pwsh72exe.Core;

if (args.Length > 0 && args[0] is "help" or "--help" or "-h")
{
    Console.WriteLine("pwsh72exe.Cli create -script <path> [-output <folder>] [-mode shortcut|exe] [-pwsh 5.1|7] [-icon <path>] [-execution-policy <name>] [-window hidden|console] [-noprofile [true|false]] [-noninteractive [true|false]] [-hidden [true|false]] [-admin [true|false]]");
    Console.WriteLine("pwsh72exe.Cli run -script <path> [-pwsh 5.1|7] [-execution-policy <name>] [-window hidden|console] [-noprofile [true|false]] [-noninteractive [true|false]] [-hidden [true|false]] [-admin [true|false]]");
    Console.WriteLine($"pwsh72exe.Cli <script.ps1> [the same create flags]; defaults come from {AppConfig.FileName} beside the CLI executable.");
    return;
}

try
{
    var config = AppConfig.Load(AppContext.BaseDirectory);
    if (args.Length == 0)
    {
        Console.WriteLine($"Config: {Path.Combine(AppContext.BaseDirectory, AppConfig.FileName)}");
        return;
    }

    var command = args[0];
    var values = ArgumentParser.Parse(args[1..]);
    var isBareScript = command.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);
    var options = ArgumentParser.ToOptions(values, config.ToPackageOptions(), isBareScript ? command : null);
    if (command.Equals("run", StringComparison.OrdinalIgnoreCase))
    {
        Environment.ExitCode = await new ScriptRunner().RunAsync(options);
        return;
    }
    if (!isBareScript && !command.Equals("create", StringComparison.OrdinalIgnoreCase))
    {
        Fail("The command must be 'create', 'run', or a .ps1 path.");
        return;
    }
    Console.WriteLine($"Created: {await new Packager().CreateAsync(options)}");
}
catch (Exception ex)
{
    Fail(ex.Message);
}

static void Fail(string message)
{
    Console.Error.WriteLine($"Error: {message}");
    Environment.ExitCode = 1;
}
