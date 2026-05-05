namespace Devolutions.AvaloniaTheme.MacOS;

using System.ComponentModel;
using Avalonia;
using Avalonia.Styling;
using Internal;

/// <summary>
/// Includes Devolutions's MacOs theme in an application.
/// </summary>
public class DevolutionsMacOsTheme : Styles, ISupportInitialize
{
    private readonly IServiceProvider? sp;

    static DevolutionsMacOsTheme()
    {
        AvaloniaControls.Initialization.Initialize();
    }

    /// <summary> 
    /// Initializes a new instance of the <see cref="DevolutionsMacOsTheme"/> class.
    ///
    /// Global Styles will also be loaded by default, unless `GlobalStyles`
    /// is set to false (`<DevolutionsMacOsTheme GlobalStyles="False" />`)
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public DevolutionsMacOsTheme(IServiceProvider? sp = null)
    {
        this.sp = sp;
    }

    /// <summary>
    /// Control if global styles should be loaded
    /// </summary>
    public bool GlobalStyles { get; set; } = true;

    public void BeginInit() { }

    public void EndInit()
    {
        this.Add(this.GlobalStyles
            ? new MacOsThemeWithGlobalStyles(this.sp)
            : new MacOsTheme(this.sp));
    }

    /// <summary>
    ///   Removes the runtime wallpaper-tint resource overrides and disables the tint hook.
    ///   Call this when switching away from the MacOS LiquidGlass theme so that other themes
    ///   are not affected by leftover app-level resource overrides.
    ///   The hook re-enables automatically next time LiquidGlass is loaded.
    /// </summary>
    public static void ClearWallpaperTintResources(Application app) =>
        WallpaperTintApplier.Clear(app);

    /// <summary>
    ///   Log produced during the most recent wallpaper sampling attempt.
    ///   Intended for diagnostic display in dev/test tooling only.
    /// </summary>
    public static string WallpaperSamplingDiagnostics => WallpaperTintApplier.LastDiagnosticLog;
}