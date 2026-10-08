using System.IO;
using System.Text.Json;
using System.Windows;

namespace pwsh72exe.Helpers;

internal static class WindowLocationStore
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "pwsh72exe",
        "window-location.json");

    public static void Restore(Window window)
    {
        try
        {
            if (!File.Exists(_path))
                return;

            var location = JsonSerializer.Deserialize<SavedLocation>(File.ReadAllText(_path));
            if (location is null || !IsVisible(location, window))
                return;

            window.Left = location.Left;
            window.Top = location.Top;
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static void Save(Window window)
    {
        if (window.WindowState != WindowState.Normal)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var location = new SavedLocation(window.Left, window.Top);
            File.WriteAllText(_path, JsonSerializer.Serialize(location));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool IsVisible(SavedLocation location, Window window)
    {
        if (!double.IsFinite(location.Left) || !double.IsFinite(location.Top))
            return false;

        var right = location.Left + window.Width;
        var bottom = location.Top + window.Height;
        return location.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
            && right > SystemParameters.VirtualScreenLeft
            && location.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight
            && bottom > SystemParameters.VirtualScreenTop;
    }

    private sealed record SavedLocation(double Left, double Top);
}
