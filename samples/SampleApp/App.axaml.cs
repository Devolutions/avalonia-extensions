namespace SampleApp;

using System;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Svg;
using Avalonia.VisualTree;
using ViewModels;

public class App : Application
{
    private readonly Styles themeStylesContainer = new();
    private Styles? devExpressStyles;
    private Styles? linuxYaruStyles;
    private Styles? macOsStyles;
    private bool devToolsAttached;
    private static bool isSettingTheme;
    public static Theme? CurrentTheme { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (!Design.IsDesignMode)
        {
            this.Styles.Clear();
            this.Styles.Add(this.themeStylesContainer);
        }

        this.linuxYaruStyles = this.Resources["LinuxYaruStyles"] as Styles;
        this.devExpressStyles = this.Resources["DevExpressStyles"] as Styles;
        this.macOsStyles = this.Resources["MacOsStyles"] as Styles;

        GC.KeepAlive(typeof(Svg).Assembly);
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);

        if (!Design.IsDesignMode)
        {
            Theme? theme = this.DetectDesignTheme() ?? GetDefaultThemeForPlatform();
            SetTheme(theme);
        }
    }

    private static Theme GetDefaultThemeForPlatform() => OperatingSystem.IsWindows() ? new DevExpressTheme()
        : OperatingSystem.IsMacOS() ? new MacOsTheme()
        : OperatingSystem.IsLinux() ? new LinuxYaruTheme()
        : new MacOsTheme();

    private Theme? DetectDesignTheme()
    {
        try
        {
            XmlDocument doc = new();
            string dir = Directory.GetCurrentDirectory();
            DirectoryInfo? debug = Directory.GetParent(dir);
            DirectoryInfo? bin = Directory.GetParent(debug!.FullName);
            if (debug.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase) && bin!.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo? projDir = Directory.GetParent(bin.FullName);
                doc.Load(Path.Join(projDir!.FullName, "App.axaml"));
                XmlElement? styles = doc["Application"]!["Application.Styles"];

                return styles?.OfType<XmlElement>()
                    .Select(this.ThemeFromXmlElement)
                    .FirstOrDefault(theme => theme is not null);
            }
        }
        catch (Exception)
        { /* ignore */
        }

        return null;
    }

    private Theme? ThemeFromXmlElement(XmlElement? elem)
    {
        if (elem is null) return null;

        return elem.Name switch
        {
            "DevolutionsMacOsTheme" => new MacOsTheme(),
            "DevolutionsLinuxYaruTheme" => new LinuxYaruTheme(),
            "DevolutionsDevExpressTheme" => new DevExpressTheme(),
            "StyleInclude" => elem.GetAttribute("Source") switch
            {
                "avares://Devolutions.AvaloniaTheme.MacOS/MacOSTheme.axaml" => new MacOsTheme(),
                "avares://Devolutions.AvaloniaTheme.Linux/LinuxTheme.axaml" => new LinuxYaruTheme(),
                "avares://Devolutions.AvaloniaTheme.DevExpress/DevExpressTheme.axaml" => new DevExpressTheme(),
                _ => null,
            },
            _ => null,
        };
    }

    public static void SetTheme(Theme theme)
    {
        // Prevent recursive calls during window initialization
        if (isSettingTheme)
        {
            Console.WriteLine($"[Theme Switch] BLOCKED recursive call to {theme.Name}");
            return;
        }

        isSettingTheme = true;
        try
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            App app = (App)Current!;
            Theme? previousTheme = CurrentTheme;
            CurrentTheme = theme;

            bool reopenWindow = previousTheme != null && previousTheme.Name != theme.Name;

            Console.WriteLine($"[Theme Switch] Starting switch to {theme.Name}");

        Styles? styles = theme switch
        {
            LinuxYaruTheme => app.linuxYaruStyles,
            DevExpressTheme => app.devExpressStyles,
            MacOsTheme => app.macOsStyles,
            _ => null,
        };

        if (reopenWindow && app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            // IMPORTANT: Capture state and hide old window BEFORE changing styles
            // to prevent expensive re-rendering of a window we're about to discard
            Window? oldWindow = desktopLifetime.MainWindow;
            int selectedTabIndex = oldWindow is MainWindow oldMainWindow
                ? oldMainWindow.FindControl<TabControl>("MainTabControl")?.SelectedIndex ?? 0
                : 0;

            // Diagnostic: Check for resource/handler accumulation
            Console.WriteLine($"[Theme Switch] App.Styles.Count: {app.Styles.Count}");
            Console.WriteLine($"[Theme Switch] themeStylesContainer.Count: {app.themeStylesContainer.Count}");

            oldWindow?.Hide();  // Hide immediately to prevent re-rendering during style change

            // Now change the styles (old window won't waste time re-rendering)
            app.themeStylesContainer.Clear();
            app.themeStylesContainer.AddRange(styles!);

            Console.WriteLine($"[Theme Switch] After clear/add - themeStylesContainer.Count: {app.themeStylesContainer.Count}");
            Console.WriteLine($"[Theme Switch] Time before RecreateMainWindow: {sw.ElapsedMilliseconds}ms");

            RecreateMainWindow(desktopLifetime, oldWindow, selectedTabIndex);

            Console.WriteLine($"[Theme Switch] TOTAL TIME: {sw.ElapsedMilliseconds}ms");
        }
        else
        {
            // Just change styles without window recreation
            app.themeStylesContainer.Clear();
            app.themeStylesContainer.AddRange(styles!);
            Console.WriteLine($"[Theme Switch] TOTAL TIME (no reopen): {sw.ElapsedMilliseconds}ms");
        }
        }
        finally
        {
            isSettingTheme = false;
        }
    }

    private static void RecreateMainWindow(IClassicDesktopStyleApplicationLifetime lifetime, Window? oldWindow, int selectedTabIndex)
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        object? dataContext = oldWindow?.DataContext;

        Console.WriteLine($"[RecreateWindow] Creating new MainWindow... ({sw.ElapsedMilliseconds}ms)");
        MainWindow newWindow = new() { DataContext = dataContext };
        Console.WriteLine($"[RecreateWindow] MainWindow created ({sw.ElapsedMilliseconds}ms)");

        lifetime.MainWindow = newWindow;

        Console.WriteLine($"[RecreateWindow] Showing new window... ({sw.ElapsedMilliseconds}ms)");
        newWindow.Show();
        Console.WriteLine($"[RecreateWindow] New window shown ({sw.ElapsedMilliseconds}ms)");

        RestoreTabSelectionWhenReady(newWindow, selectedTabIndex);

        Console.WriteLine($"[RecreateWindow] Closing old window... ({sw.ElapsedMilliseconds}ms)");
        oldWindow?.Close();
        Console.WriteLine($"[RecreateWindow] Old window closed ({sw.ElapsedMilliseconds}ms)");

        // Diagnostic: Force garbage collection and report memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Console.WriteLine($"[Theme Switch] GC after close - Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");
        Console.WriteLine($"[Theme Switch] Total memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
        Console.WriteLine($"[RecreateWindow] TOTAL: {sw.ElapsedMilliseconds}ms");
    }

    private static void RestoreTabSelectionWhenReady(MainWindow window, int selectedTabIndex)
    {
        EventHandler? layoutHandler = null;
        layoutHandler = (sender, e) =>
        {
            // CRITICAL: Unsubscribe immediately to prevent handler accumulation
            window.LayoutUpdated -= layoutHandler;

            TabControl? tabControl = window.FindControl<TabControl>("MainTabControl");
            if (tabControl?.ContainerFromIndex(0) is null) return;

            // Clear all IsSelected properties from TabItems to remove XAML hardcoded values
            foreach (TabItem tabItem in tabControl.Items.OfType<TabItem>())
            {
                tabItem.IsSelected = false;
            }

            // Now SelectedIndex works correctly without XAML interference
            tabControl.SelectedIndex = selectedTabIndex;
        };

        window.LayoutUpdated += layoutHandler;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void AttacheDevToolsOnce()
    {
        if (this.devToolsAttached)
        {
            return;
        }

#if DEBUG
        this.AttachDeveloperTools();
#endif
        this.devToolsAttached = true;
    }
}

public abstract class Theme
{
    public abstract string Name { get; }

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is Theme other && this.Equals(other));

    protected bool Equals(Theme other) =>
        this.Name == other.Name;

    public override int GetHashCode() =>
        this.Name.GetHashCode();
}

public class LinuxYaruTheme : Theme
{
    public override string Name => "Linux - Yaru";
}

public class DevExpressTheme : Theme
{
    public override string Name => "Windows - DevExpress";
}

public class MacOsTheme : Theme
{
    public override string Name => "MacOS";
}