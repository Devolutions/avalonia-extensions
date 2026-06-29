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
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Svg;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using ViewModels;

public class App : Application
{
    private readonly record struct WindowPlacement(PixelPoint Position, double Width, double Height, WindowState State);

    // Theme name constants to avoid unnecessary allocations when resolving MacOS automatic theme
    internal const string MacClassicThemeName = "MacClassic";
    internal const string LiquidGlassThemeName = "LiquidGlass";

    // WinUI theme name constants. All three WinUI variants share the same logical
    // identity ("WinUI") for control applicability/visibility gating; the variant names
    // are only used to distinguish dropdown entries and force the Mica override.
    internal const string WinUiThemeName = "WinUI";
    internal const string WinUiClassicThemeName = "WinUiClassic";
    internal const string WinUiMicaThemeName = "WinUiMica";
    private static readonly Lock ThemeLock = new();
    private static bool isSettingTheme;

    private readonly Styles themeStylesContainer = [];
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

    /// <summary>
    ///   Returns true if the currently applied theme is WinUI with the Windows 11 Mica surface treatment active.
    ///   Updated whenever the theme changes (in SetTheme()).
    /// </summary>
    public static bool IsWinUiMicaTheme { get; private set; }

    /// <summary>
    ///   Returns true when the current theme renders translucent surfaces over a backdrop, so the SampleApp
    ///   wallpaper-preview affordance is relevant. Covers macOS LiquidGlass and Windows 11 WinUI Mica.
    /// </summary>
    public static bool IsWallpaperPreviewTheme => IsLiquidGlassTheme || IsWinUiMicaTheme;

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
            "DevolutionsWinUiTheme" => new WinUiTheme(),
            "FluentThemePlusAddOns" => new FluentTheme(),
            "StyleInclude" => elem.GetAttribute("Source") switch
            {
                "avares://Devolutions.AvaloniaTheme.MacOS/MacOSTheme.axaml" => new MacOsTheme(),
                "avares://Devolutions.AvaloniaTheme.Linux/LinuxTheme.axaml" => new LinuxYaruTheme(),
                "avares://Devolutions.AvaloniaTheme.DevExpress/DevExpressTheme.axaml" => new DevExpressTheme(),
                "avares://Devolutions.AvaloniaTheme.WinUI/WinUITheme.axaml" => new WinUiTheme(),
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

    /// <summary>
    ///   Creates fresh WinUI theme styles with the current Mica override active.
    ///   This mirrors App.axaml's WinUiStyles resource, but is rebuilt per switch so the
    ///   classic/Win11 override re-triggers the conditional Mica resource merge in the theme.
    /// </summary>
    private Styles CreateWinUiStyles()
    {
        Styles styles = new();

        // Order matches App.axaml's WinUiStyles resource.
        styles.Add(new ModernTheme
        {
            AreNativeControlThemesEnabled = false,
        });

        DevolutionsWinUiTheme winUiTheme = new() { GlobalStyles = false };
        winUiTheme.BeginInit();
        winUiTheme.EndInit();
        styles.Add(winUiTheme);

        styles.Add(new DevolutionsWinUiThemeGlobalStyles());

        styles.Add(new SampleAppStyles());

        return styles;
    }

    public static void SetTheme(Theme theme)
    {
        lock (ThemeLock)
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
            bool shouldApplyWallpaperTint = false;

            Styles? styles;

            // Default: only the dedicated WinUI-Mica branch below turns this on.
            IsWinUiMicaTheme = false;

            // MacOS themes require special handling to support sub-theme switching
            if (theme is MacOsTheme macOsTheme)
            {
                // Set override FIRST so IsLiquidGlassSupported() returns the correct value
                MacOSVersionDetector.SetTestOverride(macOsTheme.OsVersionOverride);
                shouldApplyWallpaperTint = MacOSVersionDetector.IsLiquidGlassSupported();

                // Create fresh styles with the new override active
                // This is necessary because theme resources are loaded at initialization time
                styles = app.CreateMacOsStyles();
            }
            // WinUI themes likewise need fresh styles so the classic/Win11-Mica override
            // re-triggers the conditional Mica resource merge inside the theme loader.
            else if (theme is WinUiTheme winUiTheme)
            {
                Windows11MicaDetector.SetTestOverride(winUiTheme.Windows11Override);
                IsWinUiMicaTheme = Windows11MicaDetector.IsMicaSupported();
                styles = app.CreateWinUiStyles();
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

            if (!shouldApplyWallpaperTint)
            {
                ClearWallpaperTintOverrides(app);
            }

            // Update cached effective theme name
            // This avoids repeated OS version checks when XAML bindings access the property
            if (theme is MacOsTheme)
            {
                EffectiveCurrentThemeName = MacOSVersionDetector.IsLiquidGlassSupported()
                    ? LiquidGlassThemeName
                    : MacClassicThemeName;
            }
            else if (theme is WinUiTheme)
            {
                // All WinUI variants (automatic / classic / Win11) share the same logical
                // identity for applicability gating; Mica only swaps surface translucency.
                EffectiveCurrentThemeName = WinUiThemeName;
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
                WindowPlacement? windowPlacement = CaptureWindowPlacement(oldWindow);
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

                RecreateMainWindow(desktopLifetime, oldWindow, selectedTabIndex, windowPlacement);
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
            lock (ThemeLock)
            {
                isSettingTheme = false;
            }
        }
    }

    private static void ClearWallpaperTintOverrides(Application app)
    {
        DevolutionsMacOsTheme.ClearWallpaperTintResources(app);
    }

    private static WindowPlacement? CaptureWindowPlacement(Window? window)
    {
        if (window == null)
        {
            return null;
        }

        Rect bounds = window.Bounds;
        return new WindowPlacement(window.Position, bounds.Width, bounds.Height, window.WindowState);
    }

    private static void ApplyWindowPlacement(MainWindow window, WindowPlacement? placement)
    {
        if (placement is null)
        {
            return;
        }

        WindowPlacement p = placement.Value;

        // Manual startup location prevents centering on the primary display when recreating the window.
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Position = p.Position;

        if (p.Width > 0 && p.Height > 0)
        {
            window.Width = p.Width;
            window.Height = p.Height;
        }

        // Don't carry a minimized state across recreation.
        window.WindowState = p.State == WindowState.Minimized ? WindowState.Normal : p.State;
    }

    private static void RecreateMainWindow(
        IClassicDesktopStyleApplicationLifetime lifetime,
        Window? oldWindow,
        int selectedTabIndex,
        WindowPlacement? windowPlacement)
    {
        object? dataContext = oldWindow?.DataContext;

        MainWindow newWindow = new() { DataContext = dataContext };
        ApplyWindowPlacement(newWindow, windowPlacement);

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

public class WinUiTheme : Theme
{
    public override string Name => App.WinUiThemeName;
    public override string DisplayName => "Windows - WinUI (automatic)";

    /// <summary>
    ///   Mica override to apply before loading theme resources.
    ///   null = use actual OS detection (default behavior)
    /// </summary>
    public virtual bool? Windows11Override => null;
}

public class WinUiClassicTheme : WinUiTheme
{
    public override string Name => App.WinUiClassicThemeName;
    public override string DisplayName => "Windows - WinUI classic";

    /// <summary>
    ///   Force the classic (solid, Windows 10 era) surfaces by disabling Mica.
    /// </summary>
    public override bool? Windows11Override => false;
}

public class WinUiMicaTheme : WinUiTheme
{
    public override string Name => App.WinUiMicaThemeName;
    public override string DisplayName => "Windows - WinUI (Win11 Mica)";

    /// <summary>
    ///   Force the Windows 11 Mica translucent surfaces.
    /// </summary>
    public override bool? Windows11Override => true;
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