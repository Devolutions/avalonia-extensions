using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

/// <summary>
/// Guards the Windows 11 Mica overlay mechanism for the WinUI theme.
///
/// The base ThemeResources are flattened into the theme's own ThemeDictionaries via
/// MergeResourceInclude (ThemeRoot.axaml), and an owner's own ThemeDictionaries take
/// priority over its MergedDictionaries. The conditional Mica overlay must therefore be
/// merged ABOVE the base theme (in DevolutionsWinUiTheme) so it actually overrides.
/// These tests assert the overlay wins for both the GlobalStyles=false path (used by the
/// SampleApp) and the GlobalStyles=true path (used by simple consumers).
/// </summary>
public class WinUiMicaProbe
{
    private static Color Resolve(bool globalStyles, bool? micaOverride, ThemeVariant variant)
    {
        Windows11MicaDetector.SetTestOverride(micaOverride);

        var theme = new DevolutionsWinUiTheme { GlobalStyles = globalStyles };
        theme.BeginInit();
        theme.EndInit();

        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();

        Assert.True(window.TryFindResource("SettingsCardBackground", variant, out var value),
            $"SettingsCardBackground not found (globalStyles={globalStyles}, mica={micaOverride}, variant={variant})");
        var color = ((ISolidColorBrush)value!).Color;

        window.Close();
        Windows11MicaDetector.SetTestOverride(null);
        return color;
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mica_overlay_swaps_settingscard_light(bool globalStyles)
    {
        var classic = Resolve(globalStyles, false, ThemeVariant.Light);
        var mica = Resolve(globalStyles, true, ThemeVariant.Light);

        Assert.Equal(Color.Parse("#FBFBFB"), classic);
        Assert.Equal(Color.Parse("#80FFFFFF"), mica);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mica_overlay_swaps_settingscard_dark(bool globalStyles)
    {
        var classic = Resolve(globalStyles, false, ThemeVariant.Dark);
        var mica = Resolve(globalStyles, true, ThemeVariant.Dark);

        Assert.Equal(Color.Parse("#2D2D2D"), classic);
        Assert.Equal(Color.Parse("#662D2D2D"), mica);
    }

    [AvaloniaFact]
    public void Mica_provides_translucent_sampleapp_background_and_wallpaper_colours()
    {
        Windows11MicaDetector.SetTestOverride(true);
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();

            var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
            window.Styles.Add(theme);
            window.Show();

            Assert.True(window.TryFindResource("SampleAppBackground", ThemeVariant.Light, out var bg));
            Assert.True(((ISolidColorBrush)bg!).Opacity < 1.0, "Mica SampleAppBackground should be translucent so the wallpaper preview reads through.");

            Assert.True(window.TryFindResource("PreviewCustomWallpaperLight", out var light));
            Assert.Equal(Colors.White, ((ISolidColorBrush)light!).Color);
            Assert.True(window.TryFindResource("PreviewCustomWallpaperDark", out var dark));
            Assert.Equal(Colors.Black, ((ISolidColorBrush)dark!).Color);

            window.Close();
        }
        finally
        {
            Windows11MicaDetector.SetTestOverride(null);
        }
    }
}
