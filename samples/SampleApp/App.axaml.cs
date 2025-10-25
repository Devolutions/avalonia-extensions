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
        App app = (App)Current!;
        Theme? previousTheme = CurrentTheme;
        CurrentTheme = theme;

        bool reopenWindow = previousTheme != null && previousTheme.Name != theme.Name;

        Styles? styles = theme switch
        {
            LinuxYaruTheme => app.linuxYaruStyles,
            DevExpressTheme => app.devExpressStyles,
            MacOsTheme => app.macOsStyles,
            _ => null,
        };

        // IMPORTANT: Capture the selected tab index BEFORE changing styles!
        // Changing styles can reset the TabControl's SelectedIndex in the old window
        int selectedTabIndex = reopenWindow
            && app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow oldMainWindow }
            ? oldMainWindow.FindControl<TabControl>("MainTabControl")?.SelectedIndex ?? 0
            : 0;

        // Now change the styles
        app.themeStylesContainer.Clear();
        app.themeStylesContainer.AddRange(styles!);

        if (reopenWindow && app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            RecreateMainWindow(desktopLifetime, selectedTabIndex);
        }
    }

    private static void RecreateMainWindow(IClassicDesktopStyleApplicationLifetime lifetime, int selectedTabIndex)
    {
        object? dataContext = lifetime.MainWindow?.DataContext;
        Window? oldWindow = lifetime.MainWindow;

        MainWindow newWindow = new() { DataContext = dataContext };
        lifetime.MainWindow = newWindow;
        newWindow.Show();

        RestoreTabSelectionWhenReady(newWindow, selectedTabIndex);
        oldWindow?.Close();
    }

    private static void RestoreTabSelectionWhenReady(MainWindow window, int selectedTabIndex)
    {
        EventHandler? layoutHandler = null;
        layoutHandler = (sender, e) =>
        {
            TabControl? tabControl = window.FindControl<TabControl>("MainTabControl");
            if (tabControl?.ContainerFromIndex(0) is null) return;

            window.LayoutUpdated -= layoutHandler;

            // Clear all IsSelected properties from TabItems
            for (int i = 0; i < tabControl.Items.Count; i++)
            {
                if (tabControl.ContainerFromIndex(i) is TabItem tabItem)
                {
                    tabItem.IsSelected = false;
                }
            }

            // Set IsSelected on the target TabItem
            if (tabControl.ContainerFromIndex(selectedTabIndex) is TabItem targetTabItem)
            {
                targetTabItem.IsSelected = true;
            }
            else
            {
                tabControl.SelectedIndex = selectedTabIndex;
            }
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