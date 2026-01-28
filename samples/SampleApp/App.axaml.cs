namespace SampleApp;

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using ActiproSoftware.UI.Avalonia.Themes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Svg;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using ViewModels;

public class App : Application
{
    // Theme name constants to avoid unnecessary allocations when resolving MacOS automatic theme
    internal const string MacClassicThemeName = "MacClassic";
    internal const string LiquidGlassThemeName = "LiquidGlass";
    private static readonly object themeLock = new();
    private static bool isSettingTheme;

    private readonly Styles themeStylesContainer = new();
    private Styles? devExpressStyles;
    private bool devToolsAttached;
    private Styles? fluentStyles;
    private Styles? linuxYaruStyles;
    private Styles? simpleStyles;

    /// <summary>
    ///   Returns true if the currently applied theme is LiquidGlass (either explicitly or via auto-detection).
    /// </summary>
    public static bool IsLiquidGlassTheme =>
        EffectiveCurrentThemeName == LiquidGlassThemeName;

    public static Theme? CurrentTheme { get; set; }

    /// <summary>
    ///   Returns the effective theme name (short), resolving MacOS Automatic to Classic or LiquidGlass as appropriate.
    ///   Use this for code/logic comparisons.
    ///   This is updated whenever the theme changes (in SetTheme()).
    /// </summary>
    public static string EffectiveCurrentThemeName { get; private set; } = "";


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
            DirectoryInfo? bin = debug != null ? Directory.GetParent(debug.FullName) : null;
            if (debug?.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase) == true &&
                bin?.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) == true)
            {
                DirectoryInfo? projDir = Directory.GetParent(bin.FullName);
                string appAxamlPath = Path.Join(projDir!.FullName, "App.axaml");
                // The headless instances during test runs don't have App.axaml, so testing here to suppress warnings 
                if (File.Exists(appAxamlPath))
                {
                    doc.Load(appAxamlPath);
                    XmlElement? styles = doc["Application"]?["Application.Styles"];

                    return styles?.OfType<XmlElement>()
                        .Select(this.ThemeFromXmlElement)
                        .FirstOrDefault(theme => theme is not null);
                }
            }
        }
        catch (DirectoryNotFoundException e)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] DirectoryNotFoundException in DetectDesignTheme: {e}");
#endif
        }
        catch (IOException e)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] IOException in DetectDesignTheme: {e}");
#endif
        }
        catch (XmlException e)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] XmlException in DetectDesignTheme: {e}");
#endif
        }
        catch (UnauthorizedAccessException e)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] UnauthorizedAccessException in DetectDesignTheme: {e}");
#endif
        }

        return null;
    }

    private Theme? ThemeFromXmlElement(XmlElement? elem)
    {
        if (elem is null) return null;

        // Use LocalName to ignore namespace prefix (e.g., "local:MacOsClassicThemeStyle" -> "MacOsClassicThemeStyle")
        return elem.LocalName switch
        {
            "DevolutionsMacOsTheme" => new MacOsTheme(),
            "MacOsClassicThemeStyle" => new MacOsClassicTheme(),
            "MacOsLiquidGlassThemeStyle" => new MacOsLiquidGlassTheme(),
            "DevolutionsLinuxYaruTheme" => new LinuxYaruTheme(),
            "DevolutionsDevExpressTheme" => new DevExpressTheme(),
            "FluentThemePlusAddOns" => new FluentTheme(),
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

    /// <summary>
    ///   Creates fresh MacOS theme styles with current override settings.
    ///   This is necessary because theme resources are loaded at initialization time,
    ///   so we need new instances when switching between MacOS sub-themes.
    /// </summary>
    private Styles CreateMacOsStyles()
    {
        Styles styles = new();

        // Recreate the same structure as in App.axaml's MacOsStyles resource
        // Order is important: base theme must be added before global styles
        styles.Add(new ModernTheme
        {
            AreNativeControlThemesEnabled = false,
        });

        // Create and manually initialize the theme
        // ISupportInitialize requires calling BeginInit() and EndInit()
        DevolutionsMacOsTheme macOsTheme = new();
        macOsTheme.BeginInit();
        // GlobalStyles defaults to true, so it will load global styles
        macOsTheme.EndInit();
        styles.Add(macOsTheme);

        // Add app-specific styles
        styles.Add(new SampleAppStyles());

        return styles;
    }

    public static void SetTheme(Theme theme)
    {
        lock (themeLock)
        {
            // Prevent recursive calls during window initialization
            if (isSettingTheme) return;

            // Early exit if we're already on this theme (prevents unnecessary style churn)
            if (CurrentTheme?.Name == theme.Name) return;

            isSettingTheme = true;
        }

        try
        {
            App app = (App)Current!;
            Theme? previousTheme = CurrentTheme;
            CurrentTheme = theme;

            bool reopenWindow = previousTheme != null && previousTheme.Name != theme.Name;

            Styles? styles;

            // MacOS themes require special handling to support sub-theme switching
            if (theme is MacOsTheme macOsTheme)
            {
                // Set override FIRST so IsLiquidGlassSupported() returns the correct value
                MacOSVersionDetector.SetTestOverride(macOsTheme.OsVersionOverride);

                // Create fresh styles with the new override active
                // This is necessary because theme resources are loaded at initialization time
                styles = app.CreateMacOsStyles();
            }
            else
            {
                // Non-MacOS themes use cached styles (from App.axaml Resources)
                styles = theme switch
                {
                    LinuxYaruTheme => app.linuxYaruStyles,
                    DevExpressTheme => app.devExpressStyles,
                    FluentTheme => app.fluentStyles,
                    SimpleTheme => app.simpleStyles,
                    _ => null,
                };
            }

            // Update cached effective theme name
            // This avoids repeated OS version checks when XAML bindings access the property
            if (theme is MacOsTheme)
            {
                EffectiveCurrentThemeName = MacOSVersionDetector.IsLiquidGlassSupported()
                    ? LiquidGlassThemeName
                    : MacClassicThemeName;
            }
            else
            {
                EffectiveCurrentThemeName = theme?.Name ?? "";
            }

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

                if (styles == null)
                {
                    throw new InvalidOperationException($"Unable to load styles for theme: {theme?.Name ?? "Unknown"}");
                }

                app.themeStylesContainer.Clear();
                app.themeStylesContainer.AddRange(styles);

                RecreateMainWindow(desktopLifetime, oldWindow, selectedTabIndex);
            }
            else
            {
                // Just change styles without window recreation
                if (styles == null)
                {
                    throw new InvalidOperationException($"Unable to load styles for theme: {theme?.Name ?? "Unknown"}");
                }

                app.themeStylesContainer.Clear();
                app.themeStylesContainer.AddRange(styles);
            }
        }
        finally
        {
            lock (themeLock)
            {
                isSettingTheme = false;
            }
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
        Dispatcher.UIThread.Post(
            () => window.SuppressThemeChangeEvents(false),
            DispatcherPriority.Background);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        this.FixCommunityToolkitMvvmDataValidation();

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

    private void FixCommunityToolkitMvvmDataValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
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
    public abstract string DisplayName { get; }

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is Theme other && this.Equals(other));

    protected bool Equals(Theme other) =>
        this.Name == other.Name;

    public override int GetHashCode() =>
        this.Name.GetHashCode();
}

public class LinuxYaruTheme : Theme
{
    public override string Name => "Linux";
    public override string DisplayName => "Linux - Yaru";
}

public class DevExpressTheme : Theme
{
    public override string Name => "DevExpress";
    public override string DisplayName => "Windows - DevExpress";
}

public class MacOsTheme : Theme
{
    public override string Name => "MacOS";
    public override string DisplayName => "MacOS (automatic)";

    /// <summary>
    ///   OS version override to apply before loading theme resources.
    ///   null = use actual OS detection (default behavior)
    /// </summary>
    public virtual bool? OsVersionOverride => null;
}

public class MacOsClassicTheme : MacOsTheme
{
    public override string Name => App.MacClassicThemeName;
    public override string DisplayName => "MacOS - classic";

    /// <summary>
    ///   Force classic theme by simulating OS version &lt;= 26
    /// </summary>
    public override bool? OsVersionOverride => false;
}

public class MacOsLiquidGlassTheme : MacOsTheme
{
    public override string Name => App.LiquidGlassThemeName;
    public override string DisplayName => "MacOS - LiquidGlass";

    /// <summary>
    ///   Force LiquidGlass theme by simulating OS version &gt;= 26
    /// </summary>
    public override bool? OsVersionOverride => true;
}

public class FluentTheme : Theme
{
    public override string Name => "Fluent";
    public override string DisplayName => "Avalonia Fluent";
}

public class SimpleTheme : Theme
{
    public override string Name => "Simple";
    public override string DisplayName => "Avalonia Simple";
}

/// <summary>
/// XAML-compatible wrapper for MacOS Classic theme.
/// Used in Application.Styles to force Classic theme on startup.
/// </summary>
public class MacOsClassicThemeStyle : Styles, ISupportInitialize
{
    public void BeginInit() { }

    public void EndInit()
    {
        // At runtime, this wrapper doesn't actually load styles - it's just a marker.
        // The actual theme is loaded by App.Initialize() after detecting this element.
        // However, in Design Mode (Previewer), we must explicitly load the theme here
        // because App.Initialize() is not called.
        if (Design.IsDesignMode)
        {
            MacOSVersionDetector.SetTestOverride(false);
            var theme = new DevolutionsMacOsTheme();
            theme.BeginInit();
            theme.EndInit();
            this.Add(theme);
        }
    }
}

/// <summary>
/// XAML-compatible wrapper for MacOS LiquidGlass theme.
/// Used in Application.Styles to force LiquidGlass theme on startup.
/// </summary>
public class MacOsLiquidGlassThemeStyle : Styles, ISupportInitialize
{
    public void BeginInit() { }

    public void EndInit()
    {
        // At runtime, this wrapper doesn't actually load styles - it's just a marker.
        // The actual theme is loaded by App.Initialize() after detecting this element.
        // However, in Design Mode (Previewer), we must explicitly load the theme here
        // because App.Initialize() is not called.
        if (Design.IsDesignMode)
        {
            MacOSVersionDetector.SetTestOverride(true);
            var theme = new DevolutionsMacOsTheme();
            theme.BeginInit();
            theme.EndInit();
            this.Add(theme);
        }
    }
}

public class FluentThemePlusAddOns : Styles { }