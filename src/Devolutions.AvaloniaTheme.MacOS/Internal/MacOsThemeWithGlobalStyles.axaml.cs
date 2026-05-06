namespace Devolutions.AvaloniaTheme.MacOS.Internal;

using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvaloniaControls.Helpers;

internal class MacOsThemeWithGlobalStyles : Styles
{
  public MacOsThemeWithGlobalStyles(IServiceProvider? sp = null)
  {
    AvaloniaXamlLoader.Load(sp, this);

    // 1) Always load the base theme
    Uri baseUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml");
    ResourceInclude baseInclude = new(baseUri) { Source = baseUri };
    this.Resources.MergedDictionaries.Add(baseInclude);

    // 2) Conditionally load LiquidGlass overrides
    if (MacOSVersionDetector.IsLiquidGlassSupported())
    {
      Uri liquidGlassUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml");
      ResourceInclude liquidGlassInclude = new(liquidGlassUri) { Source = liquidGlassUri };
      this.Resources.MergedDictionaries.Add(liquidGlassInclude);

      // Apply wallpaper-tinted TextBox background for LiquidGlass.
      // Deferred so that Application.Current is fully initialised before the hook runs.
      if (Avalonia.Application.Current is { } app)
      {
        Avalonia.Threading.Dispatcher.UIThread.Post(
          () => WallpaperTintApplier.HookAndApply(app),
          Avalonia.Threading.DispatcherPriority.Background);
      }
    }

#if ENABLE_ACCELERATE
    if (AvaloniaAccelerateHelpers.IsTreeDataGridAvailable)
    {
      Uri treeDataGridUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Controls/TreeDataGrid.axaml");
      this.Resources.MergedDictionaries.Add(new ResourceInclude(treeDataGridUri) { Source = treeDataGridUri });
    }
#endif

#if DEBUG
    Uri themePreviewerUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Design/ThemePreviewer.axaml");
    this.Resources.MergedDictionaries.Add(new ResourceInclude(themePreviewerUri) { Source = themePreviewerUri });
#endif
  }
}