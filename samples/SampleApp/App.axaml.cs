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
            Theme? theme = this.DetectDesignTheme();

            if (OperatingSystem.IsWindows())
            {
                theme ??= new DevExpressTheme();
            }
            else if (OperatingSystem.IsMacOS())
            {
                theme ??= new MacOsTheme();
            }
            else if (OperatingSystem.IsLinux())
            {
                theme ??= new LinuxYaruTheme();
            }
            else
            {
                theme ??= new MacOsTheme();
            }

            SetTheme(theme);
        }
    }

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
                foreach (object? obj in styles!)
                {
                    Theme? theme = this.ThemeFromXmlElement(obj as XmlElement);
                    if (theme is not null) return theme;
                }
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
        int selectedTabIndex = 0; // Default to first tab
        if (reopenWindow && app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            Window? oldWindow = lifetime.MainWindow;
            if (oldWindow is MainWindow oldMainWindow)
            {
                TabControl? oldTabControl = oldMainWindow.FindControl<TabControl>("MainTabControl");
                if (oldTabControl != null)
                {
                    selectedTabIndex = oldTabControl.SelectedIndex;
                    Console.WriteLine($"[Theme Switch] Captured tab index: {selectedTabIndex} from old window (BEFORE style change)");
                }
                else
                {
                    Console.WriteLine($"[Theme Switch] WARNING: Could not find MainTabControl in old window");
                }
            }
        }

        // Now change the styles
        app.themeStylesContainer.Clear();
        app.themeStylesContainer.AddRange(styles!);

        if (reopenWindow)
        {
            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                Window? oldWindow = desktopLifetime.MainWindow;
                object? dataContext = oldWindow?.DataContext;

                MainWindow newWindow = new() { DataContext = dataContext };
                desktopLifetime.MainWindow = newWindow;

                newWindow.Show();

                // Wait for the window to fully load and containers to be created
                // Using LayoutUpdated to ensure the visual tree is ready
                EventHandler? layoutHandler = null;
                layoutHandler = (sender, e) =>
                {
                    TabControl? tabControl = newWindow.FindControl<TabControl>("MainTabControl");
                    if (tabControl != null)
                    {
                        // Check if containers are ready
                        if (tabControl.ContainerFromIndex(0) != null)
                        {
                            Console.WriteLine($"[Theme Switch] LayoutUpdated: Containers are ready! Current SelectedIndex={tabControl.SelectedIndex}, target={selectedTabIndex}");

                            // Unsubscribe from the event to prevent repeated execution
                            newWindow.LayoutUpdated -= layoutHandler;

                            // Clear all IsSelected
                            for (int i = 0; i < tabControl.Items.Count; i++)
                            {
                                if (tabControl.ContainerFromIndex(i) is TabItem tabItem)
                                {
                                    tabItem.IsSelected = false;
                                }
                            }

                            // Set the correct one
                            if (tabControl.ContainerFromIndex(selectedTabIndex) is TabItem targetTabItem)
                            {
                                targetTabItem.IsSelected = true;
                                Console.WriteLine($"[Theme Switch] Successfully set IsSelected=true on TabItem {selectedTabIndex}");
                            }
                            else
                            {
                                Console.WriteLine($"[Theme Switch] WARNING: Still couldn't get container {selectedTabIndex}, using SelectedIndex");
                                tabControl.SelectedIndex = selectedTabIndex;
                            }
                        }
                    }
                };

                newWindow.LayoutUpdated += layoutHandler;

                oldWindow?.Close();
            }
        }
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

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return this.Equals((Theme)obj);
    }

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