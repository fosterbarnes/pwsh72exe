using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace pwsh72exe.Helpers;

public static class WindowsTitleBarTheme
{
    private const int _DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int _DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20 = 19;

    public static void ApplyImmersiveDarkMode(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero)
            return;

        var enabled = 1;
        _ = DwmSetWindowAttribute(hwnd, _DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, _DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int attribute,
        ref int pvAttribute,
        int cbAttribute);
}
