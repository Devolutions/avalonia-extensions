using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Xunit;

namespace Devolutions.AvaloniaControls.Tests;

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
[Collection("StylesTest")]
public class WinUiMicaProbe
{
    private static Color Resolve(bool globalStyles, bool? micaOverride, ThemeVariant variant,
        string resourceKey = "SettingsCardBackground") =>
        ResolveBrush(globalStyles, micaOverride, variant, resourceKey).Color;

    private static (Color Color, double Opacity) ResolveBrush(
        bool globalStyles,
        bool? micaOverride,
        ThemeVariant variant,
        string resourceKey)
    {
        Windows11MicaDetector.SetTestOverride(micaOverride);

        var theme = new DevolutionsWinUiTheme { GlobalStyles = globalStyles };
        theme.BeginInit();
        theme.EndInit();

        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();

        try
        {
            Assert.True(window.TryFindResource(resourceKey, variant, out var value),
                $"{resourceKey} not found (globalStyles={globalStyles}, mica={micaOverride}, variant={variant})");
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(value);
            return (brush.Color, brush.Opacity);
        }
        finally
        {
            window.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mica_overlay_translucifies_combobox_surface(bool globalStyles)
    {
        var classicLight = ResolveBrush(globalStyles, false, ThemeVariant.Light, "ComboBoxDropDownBackground");
        var micaLight = ResolveBrush(globalStyles, true, ThemeVariant.Light, "ComboBoxDropDownBackground");
        var classicDark = ResolveBrush(globalStyles, false, ThemeVariant.Dark, "ComboBoxDropDownBackground");
        var micaDark = ResolveBrush(globalStyles, true, ThemeVariant.Dark, "ComboBoxDropDownBackground");

        Assert.Equal((Color.Parse("#FFF9F9F9"), 1.0), classicLight);
        Assert.Equal((Color.Parse("#FFFCFCFC"), 0.85), micaLight);
        Assert.Equal((Color.Parse("#FF2C2C2C"), 1.0), classicDark);
        Assert.Equal((Color.Parse("#FF2C2C2C"), 0.96), micaDark);
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

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mica_overlay_translucifies_menuflyout_surface(bool globalStyles)
    {
        var classicLight = Resolve(globalStyles, false, ThemeVariant.Light, "MenuFlyoutPresenterBackground");
        var micaLight = Resolve(globalStyles, true, ThemeVariant.Light, "MenuFlyoutPresenterBackground");
        var micaLightBorder = Resolve(globalStyles, true, ThemeVariant.Light, "MenuFlyoutPresenterBorderBrush");
        var classicDark = Resolve(globalStyles, false, ThemeVariant.Dark, "MenuFlyoutPresenterBackground");
        var micaDark = Resolve(globalStyles, true, ThemeVariant.Dark, "MenuFlyoutPresenterBackground");
        var micaDarkBorder = Resolve(globalStyles, true, ThemeVariant.Dark, "MenuFlyoutPresenterBorderBrush");

        Assert.Equal(Color.Parse("#FFF9F9F9"), classicLight);
        Assert.Equal(Color.Parse("#D9F9F9F9"), micaLight);
        Assert.Equal(Color.Parse("#14000000"), micaLightBorder);
        Assert.Equal(Color.Parse("#FF2D2D2D"), classicDark);
        Assert.Equal(Color.Parse("#CC2D2D2D"), micaDark);
        Assert.Equal(Color.Parse("#26FFFFFF"), micaDarkBorder);
    }

    [AvaloniaFact]
    public void Mica_provides_translucent_sampleapp_background_and_wallpaper_colours()
    {
        Windows11MicaDetector.SetTestOverride(true);
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(theme);
        window.Show();

        try
        {
            Assert.True(window.TryFindResource("SampleAppBackground", ThemeVariant.Light, out var bg));
            Assert.True(((ISolidColorBrush)bg!).Opacity < 1.0, "Mica SampleAppBackground should be translucent so the wallpaper preview reads through.");

            Assert.True(window.TryFindResource("PreviewCustomWallpaperLight", out var light));
            Assert.Equal(Colors.White, ((ISolidColorBrush)light!).Color);
            Assert.True(window.TryFindResource("PreviewCustomWallpaperDark", out var dark));
            Assert.Equal(Colors.Black, ((ISolidColorBrush)dark!).Color);
        }
        finally
        {
            window.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }
}
