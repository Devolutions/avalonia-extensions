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
    private static bool isSettingTheme;

    private readonly Styles themeStylesContainer = new();
    private Styles? devExpressStyles;
    private Styles? linuxYaruStyles;
    private Styles? macOsStyles;
    private Styles? fluentStyles;
    private Styles? simpleStyles;
    private bool devToolsAttached;

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
        this.fluentStyles = this.Resources["FluentStyles"] as Styles;
        this.simpleStyles = this.Resources["SimpleStyles"] as Styles;

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
        if (isSettingTheme) return;

        // Early exit if we're already on this theme (prevents unnecessary style churn)
        if (CurrentTheme?.Name == theme.Name) return;

        isSettingTheme = true;
        try
        {
            App app = (App)Current!;
            Theme? previousTheme = CurrentTheme;
            CurrentTheme = theme;

            bool reopenWindow = previousTheme != null && previousTheme.Name != theme.Name;

            Styles? styles = theme switch
            {
                LinuxYaruTheme => app.linuxYaruStyles,
                DevExpressTheme => app.devExpressStyles,
                MacOsTheme => app.macOsStyles,
                FluentTheme => app.fluentStyles,
                SimpleTheme => app.simpleStyles,
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

                // Suppress theme changes on old window to prevent its ComboBox binding from
                // triggering SetTheme when we update CurrentTheme
                if (oldWindow is MainWindow oldMain)
                {
                    oldMain.SuppressThemeChangeEvents(true);
                }

                oldWindow?.Hide();

                app.themeStylesContainer.Clear();
                app.themeStylesContainer.AddRange(styles!);

                RecreateMainWindow(desktopLifetime, oldWindow, selectedTabIndex);
            }
            else
            {
                // Just change styles without window recreation
                app.themeStylesContainer.Clear();
                app.themeStylesContainer.AddRange(styles!);
            }
        }
        finally
        {
            isSettingTheme = false;
        }
    }

    private static void RecreateMainWindow(IClassicDesktopStyleApplicationLifetime lifetime, Window? oldWindow, int selectedTabIndex)
    {
        object? dataContext = oldWindow?.DataContext;

        MainWindow newWindow = new() { DataContext = dataContext };

        // Suppress theme change events during window initialization to prevent
        // SelectionChanged from firing multiple times as bindings initialize
        newWindow.SuppressThemeChangeEvents(true);

        lifetime.MainWindow = newWindow;
        newWindow.Show();

        RestoreTabSelectionWhenReady(newWindow, selectedTabIndex);

        // Re-enable theme changes after window is fully initialized
        EnableThemeChangesWhenReady(newWindow);

        oldWindow?.Close();
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

    private static void EnableThemeChangesWhenReady(MainWindow window)
    {
        // Use Dispatcher to re-enable theme changes after the window initialization completes
        // This happens quickly enough to not block user clicks, but late enough that
        // binding-triggered SelectionChanged events during construction are suppressed
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => window.SuppressThemeChangeEvents(false),
            Avalonia.Threading.DispatcherPriority.Background);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = new() { DataContext = new MainWindowViewModel() };

            // Suppress theme changes during initial window setup
            mainWindow.SuppressThemeChangeEvents(true);
            desktop.MainWindow = mainWindow;

            // Re-enable after initialization completes
            EnableThemeChangesWhenReady(mainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void AttachDevToolsOnce()
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

public class FluentTheme : Theme
{
    public override string Name => "Fluent";
}

public class SimpleTheme : Theme
{
    public override string Name => "Simple";
}