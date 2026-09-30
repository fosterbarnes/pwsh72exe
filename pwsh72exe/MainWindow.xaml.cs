using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Microsoft.Win32;

using pwsh72exe.Core;
using pwsh72exe.Helpers;

namespace pwsh72exe;

public partial class MainWindow : Window
{
    private static readonly TimeSpan _tabTransitionDuration = TimeSpan.FromMilliseconds(180);

    // The config sits beside the executables so the GUI and CLI share one set of defaults.
    private readonly string _installPath = AppContext.BaseDirectory.TrimEnd(
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar);
    private readonly FrameworkElement[] _pages;
    private FrameworkElement? _activePage;
    private int _pageTransition;

    public MainWindow()
    {
        InitializeComponent();
        _pages = [BuildPage, AboutPage];
        InitializeAboutPage();
        LoadConfig();
        WindowLocationStore.Restore(this);
        SourceInitialized += (_, _) => WindowsTitleBarTheme.ApplyImmersiveDarkMode(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTabForegrounds(false);
        UpdateTabRowLayout(false);
        ShowModePage(false);
    }

    private void InitializeAboutPage()
    {
        AboutTitleText.Text = $"pwsh72exe ({GetPlatformLabel()})";
        AboutVersionText.Text = $"v{ReadVersion()}";
        AboutInstallPathText.Text = _installPath;
        AboutConfigPathText.Text = Path.Combine(_installPath, AppConfig.FileName);
    }

    private static string ReadVersion()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Version");
        return File.ReadLines(path).First(line => !string.IsNullOrWhiteSpace(line)).Trim().TrimStart('v', 'V');
    }

    private static string GetPlatformLabel() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "ARM64",
        var architecture => architecture.ToString()
    };

    private void LoadConfig()
    {
        try
        {
            var options = AppConfig.Load(AppContext.BaseDirectory).ToPackageOptions();
            OutputBox.Text = options.OutputPath;
            IconBox.Text = options.IconPath ?? string.Empty;
            ModeBox.SelectedIndex = options.Kind == OutputKind.Shortcut ? 0 : 1;
            HostBox.SelectedIndex = options.Version == PowerShellVersion.WindowsPowerShell ? 0 : 1;
            PolicyBox.SelectedItem = PolicyBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(item.Content as string, options.ExecutionPolicy, StringComparison.OrdinalIgnoreCase))
                ?? PolicyBox.Items[0];
            NoProfileBox.IsChecked = options.NoProfile;
            NonInteractiveBox.IsChecked = options.NonInteractive;
            HideHostBox.IsChecked = options.HideHostWindow;
            AdminBox.IsChecked = options.RunAsAdministrator;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            OutputBox.Text = AppConfig.Defaults.OutputPath;
            SetStatus(ex.Message, "StatusErrorBrush");
        }
    }

    private void SaveConfig()
    {
        try
        {
            AppConfig.From(ReadOptions()).Save(AppContext.BaseDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus(ex.Message, "StatusErrorBrush");
        }
    }

    private void Script_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PowerShell scripts (*.ps1)|*.ps1|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true) ScriptBox.Text = dialog.FileName;
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Icon files (*.ico)|*.ico" };
        if (dialog.ShowDialog() == true) IconBox.Text = dialog.FileName;
    }

    private void Output_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select output folder" };
        if (dialog.ShowDialog() == true) OutputBox.Text = dialog.FolderName;
    }

    private async void Run_Click(object sender, RoutedEventArgs e) =>
        await RunBusy("Running...", async () => $"Exit code {await new ScriptRunner().RunAsync(ReadOptions())}");

    private async void Build_Click(object sender, RoutedEventArgs e) =>
        await RunBusy("Building...", async () =>
        {
            var result = await new Packager().CreateAsync(ReadOptions());
            SaveConfig();
            return $"Created {result}";
        });

    // Blocks a second Run/Build while one is in flight and reports the outcome in the status line.
    private async Task RunBusy(string status, Func<Task<string>> action)
    {
        RunButton.IsEnabled = false;
        BuildButton.IsEnabled = false;
        try
        {
            SetStatus(status, "SecondaryTextBrush");
            SetStatus(await action(), "StatusSuccessBrush");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, "StatusErrorBrush");
        }
        finally
        {
            RunButton.IsEnabled = true;
            BuildButton.IsEnabled = true;
        }
    }

    private PackageOptions ReadOptions() => new(
        ScriptBox.Text,
        OutputBox.Text,
        HostBox.SelectedIndex == 0 ? PowerShellVersion.WindowsPowerShell : PowerShellVersion.PowerShell7,
        string.IsNullOrWhiteSpace(IconBox.Text) ? null : IconBox.Text,
        ModeBox.SelectedIndex == 0 ? OutputKind.Shortcut : OutputKind.PortableExe,
        NoProfileBox.IsChecked == true,
        NonInteractiveBox.IsChecked == true,
        (PolicyBox.SelectedItem as ComboBoxItem)?.Content as string ?? "Bypass",
        HideHostBox.IsChecked == true,
        AdminBox.IsChecked == true);

    private void SetStatus(string text, string brushKey)
    {
        StatusText.Text = text;
        StatusText.Foreground = (Brush)FindResource(brushKey);
        StatusText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != ModeTabs)
            return;

        UpdateTabForegrounds(true);
        UpdateTabRowLayout(true);
        if (IsLoaded)
            ShowModePage(true);
    }

    private void ModeTabs_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTabRowLayout(false);
    }

    private void UpdateTabRowLayout(bool animate)
    {
        if (ModeTabs.SelectedIndex < 0 || ModeTabs.Items.Count == 0)
            return;

        var headerHost = ModeTabs.Template.FindName("HeaderHost", ModeTabs) as FrameworkElement;
        var dividerLayer = ModeTabs.Template.FindName("TabDividers", ModeTabs) as Panel;
        var indicator = ModeTabs.Template.FindName("SelectedTabIndicator", ModeTabs) as Border;
        var transform = ModeTabs.Template.FindName("SelectedTabIndicatorTransform", ModeTabs) as TranslateTransform;
        if (headerHost is null || dividerLayer is null || indicator is null || transform is null || headerHost.ActualWidth <= 0)
            return;

        var tabCount = ModeTabs.Items.Count;
        var tabWidth = headerHost.ActualWidth / tabCount;

        indicator.Width = tabWidth;
        UpdateTabDividers(dividerLayer, tabWidth, tabCount);
        if (animate)
        {
            transform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation(ModeTabs.SelectedIndex * tabWidth, _tabTransitionDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                },
                HandoffBehavior.SnapshotAndReplace);
        }
        else
        {
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            transform.X = ModeTabs.SelectedIndex * tabWidth;
        }
    }

    private static void UpdateTabDividers(Panel dividerLayer, double tabWidth, int tabCount)
    {
        dividerLayer.Children.Clear();
        var inset = (Thickness)dividerLayer.FindResource("TabDividerInset");
        var brush = (Brush)dividerLayer.FindResource("BorderSubtleBrush");
        for (var index = 1; index < tabCount; index++)
        {
            dividerLayer.Children.Add(new Border
            {
                Width = 1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(tabWidth * index, inset.Top, inset.Right, inset.Bottom),
                Background = brush,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            });
        }
    }

    private void ShowModePage(bool animate)
    {
        FrameworkElement? nextPage = (ModeTabs.SelectedItem as TabItem)?.Tag switch
        {
            "Build" => BuildPage,
            "About" => AboutPage,
            _ => null
        };
        if (nextPage is null || ReferenceEquals(nextPage, _activePage))
            return;

        var previousPage = _activePage;
        _activePage = nextPage;
        var transition = ++_pageTransition;

        nextPage.BeginAnimation(UIElement.OpacityProperty, null);
        nextPage.Visibility = Visibility.Visible;
        if (!animate || previousPage is null)
        {
            nextPage.Opacity = 1;
            foreach (var page in _pages)
                HideInactivePage(page, nextPage);
            return;
        }

        previousPage.BeginAnimation(UIElement.OpacityProperty, null);
        previousPage.Visibility = Visibility.Visible;
        previousPage.Opacity = 1;
        nextPage.Opacity = 0;

        var fadeOut = new DoubleAnimation(1, 0, _tabTransitionDuration);
        var fadeIn = new DoubleAnimation(0, 1, _tabTransitionDuration);
        fadeIn.Completed += (_, _) =>
        {
            if (transition != _pageTransition)
                return;

            previousPage.Visibility = Visibility.Collapsed;
            previousPage.Opacity = 0;
        };
        previousPage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        nextPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    private static void HideInactivePage(FrameworkElement page, FrameworkElement activePage)
    {
        if (ReferenceEquals(page, activePage))
            return;

        page.BeginAnimation(UIElement.OpacityProperty, null);
        page.Opacity = 0;
        page.Visibility = Visibility.Collapsed;
    }

    private Color ResourceColor(string key) => ((SolidColorBrush)FindResource(key)).Color;

    private void UpdateTabForegrounds(bool animate)
    {
        var selectedColor = ResourceColor("PrimaryTextBrush");
        var unselectedColor = ResourceColor("TertiaryTextBrush");
        foreach (var tab in ModeTabs.Items.OfType<TabItem>())
            SetTabForeground(tab, tab.IsSelected ? selectedColor : unselectedColor, animate);
    }

    private static void SetTabForeground(TabItem tab, Color color, bool animate)
    {
        if (tab.Foreground is not SolidColorBrush)
            return;

        var brush = Unfrozen(tab.Foreground);
        tab.Foreground = brush;
        AnimateColor(brush, color, animate);
    }

    private static SolidColorBrush Unfrozen(Brush source) =>
        source is SolidColorBrush brush && !brush.IsFrozen ? brush : ((SolidColorBrush)source).Clone();

    private static void AnimateColor(SolidColorBrush brush, Color color, bool animate)
    {
        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            animate
                ? new ColorAnimation(color, _tabTransitionDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                }
                : null);
        if (!animate)
            brush.Color = color;
    }

    private void AboutOpenInstallLocation_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = _installPath, UseShellExecute = true });
    }

    private void AboutHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
        e.Handled = true;
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveConfig();
        WindowLocationStore.Save(this);
    }
}
