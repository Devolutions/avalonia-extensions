namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Converters;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using Xunit;

/// <summary>
///   Pins how the LiquidGlass selection colour is derived from the system accent.
/// </summary>
/// <remarks>
///   <para>
///     There is no formula in macOS to copy. Apple ships hand-picked per-accent constants -
///     <c>AppleHighlightColor</c> stores explicit RGB per accent while <c>AppleAccentColor</c> is
///     only an index - and exposes variants through <c>NSColor</c> APIs that Avalonia cannot reach.
///     Measuring Finder against four accents ruled out constant Oklch offsets, constant HSL offsets,
///     compositing over any single surface in sRGB or linear light, fixed-alpha Oklab blending,
///     gamut limiting, and Display P3 blending. So this is a deliberate approximation.
///   </para>
///   <para>
///     Two things are therefore pinned: the exact output for the default accent, because the
///     standalone pack has to carry it as a literal, and the distance from the recorded native
///     colours, because that is the actual intent and a tightening of the model should not be free
///     to drift away from Finder.
///   </para>
///   <para>
///     The derivation is <see cref="OklchAdjustmentConverter" />: hue preserved, lightness shifted,
///     chroma capped. Removing the brush opacity, not any constant, is what fixed the washed-out
///     look; the cap is what keeps vivid accents - red, pink - from overshooting the other way.
///   </para>
/// </remarks>
[Collection("VisualTests")]
public class MacOsAccentSelectionTests
{
    /// <summary>The accent macOS reports for its default "Blue", read from Avalonia per appearance.</summary>
    private const string DefaultAccent = "#007AFF";

    // What the derivation produces for the default accent. MenuResources_LiquidGlass pins these same
    // two literals, because the pack cannot run a converter.
    private const string DefaultSelectionLight = "#3884ED";
    private const string DefaultSelectionDark = "#005BC4";

    /// <summary>
    ///   Accent as resolved by Avalonia per appearance, against Finder's <em>drop-down</em> selection
    ///   measured on screen for the same appearance. Captured 2026-09-02.
    /// </summary>
    /// <remarks>
    ///   The dark figures are the softer evidence: Finder's dark menus are translucent, so a measured
    ///   dark target carries some of whatever sat behind the menu. Purple dark is the least
    ///   trustworthy row and is the worst case in the tolerance test below.
    /// </remarks>
    public static readonly IEnumerable<object[]> NativeMeasurements = new[]
    {
        new object[] { "purple", "#953D96", "#AA44AA", "#A550A7", "#A3259F" },
        new object[] { "red", "#E0383E", "#DC5050", "#FF5257", "#CD3D3D" },
        new object[] { "green", "#62BA46", "#59B44F", "#62BA46", "#3B9430" },
        new object[] { "blue", DefaultAccent, "#4F8AF9", DefaultAccent, "#2A52C2" },
    };

    // Fitted mean deviation is 0.04 light / 0.04 dark, worst case 0.077 (purple dark, whose
    // measurement is the least trustworthy). The bound sits just above that worst case: tight enough
    // that a real regression trips it, loose enough that it is not pinning measurement noise.
    private const double MaxDeviation = 0.08;

    // Must stay in step with the two converter declarations in ThemeResources_LiquidGlass.axaml.
    private static OklchAdjustmentConverter Converter(bool dark) =>
        dark
            ? new OklchAdjustmentConverter { LightnessAdjustment = -0.110, ChromaCap = 0.180 }
            : new OklchAdjustmentConverter { LightnessAdjustment = 0.020, ChromaCap = 0.175 };

    private static Color Derive(string accent, bool dark) =>
        Assert.IsType<Color>(Converter(dark).Convert(Color.Parse(accent), typeof(Color), null, null!));

    /// <summary>Oklab distance - a perceptual difference, so the tolerance means something.</summary>
    private static double Deviation(Color a, Color b)
    {
        (double L1, double A1, double B1) = ToOklab(a);
        (double L2, double A2, double B2) = ToOklab(b);
        return Math.Sqrt(((L1 - L2) * (L1 - L2)) + ((A1 - A2) * (A1 - A2)) + ((B1 - B2) * (B1 - B2)));
    }

    private static (double L, double A, double B) ToOklab(Color color)
    {
        static double Lin(byte c)
        {
            double v = c / 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        double r = Lin(color.R), g = Lin(color.G), b = Lin(color.B);
        double l = Math.Cbrt((0.4122214708 * r) + (0.5363325363 * g) + (0.0514459929 * b));
        double m = Math.Cbrt((0.2119034982 * r) + (0.6806995451 * g) + (0.1073969566 * b));
        double s = Math.Cbrt((0.0883024619 * r) + (0.2817188376 * g) + (0.6299787005 * b));
        return ((0.2104542553 * l) + (0.7936177850 * m) - (0.0040720468 * s),
                (1.9779984951 * l) - (2.4285922050 * m) + (0.4505937099 * s),
                (0.0259040371 * l) + (0.7827717662 * m) - (0.8086757660 * s));
    }

    [Theory]
    [InlineData(false, DefaultSelectionLight)]
    [InlineData(true, DefaultSelectionDark)]
    public void Default_accent_derives_the_literal_the_pack_pins(bool dark, string expected)
    {
        // If this changes, MenuResources_LiquidGlass must be re-pinned in the same commit or
        // Pack_menu_tokens_match_the_full_theme will fail.
        Assert.Equal(Color.Parse(expected), Derive(DefaultAccent, dark));
    }

    [Theory]
    [MemberData(nameof(NativeMeasurements))]
    public void Derived_selection_stays_close_to_the_measured_native_colour(
        string name, string accentLight, string nativeLight, string accentDark, string nativeDark)
    {
        double light = Deviation(Derive(accentLight, dark: false), Color.Parse(nativeLight));
        double dark = Deviation(Derive(accentDark, dark: true), Color.Parse(nativeDark));

        Assert.True(light <= MaxDeviation, $"{name} light: dE {light:F4} exceeds {MaxDeviation}");
        Assert.True(dark <= MaxDeviation, $"{name} dark: dE {dark:F4} exceeds {MaxDeviation}");
    }

    [Theory]
    [InlineData("#8E8E93")]
    [InlineData("#98989D")]
    public void A_grey_accent_stays_grey(string graphite)
    {
        // The cap is a ceiling, so a near-grey accent passes through at its own chroma. Only a
        // chroma *offset* could tint it - which is what the previous values did, giving graphite a
        // violet cast.
        foreach (bool dark in new[] { false, true })
        {
            Color derived = Derive(graphite, dark);
            int spread = Math.Max(derived.R, Math.Max(derived.G, derived.B))
                         - Math.Min(derived.R, Math.Min(derived.G, derived.B));
            Assert.True(spread <= 8, $"graphite {graphite} ({(dark ? "dark" : "light")}) gained a hue: {derived}");
        }
    }

    [AvaloniaTheory]
    [InlineData("Light", "#953D96", "#9B439C")]
    [InlineData("Dark", "#A550A7", "#822E84")]
    public void Standalone_pack_follows_a_custom_accent(string variantName, string accent, string expected)
    {
        // RDM consumes the standalone pack, so its menus have to follow the user's accent. The
        // parity tests cannot catch a regression here: they pin SystemAccentColor to the default
        // accent on both sides, so a hard-coded literal would satisfy them. This uses a NON-default
        // accent, which only a live derivation can track.
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        MacOSVersionDetector.SetTestOverride(true);

        var page = new UserControl();
        var window = new Window { Content = page, RequestedThemeVariant = variant };
        window.Resources["SystemAccentColor"] = Color.Parse(accent);
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        page.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            Assert.True(page.TryFindResource("MacOsMenuItemPointerOverBackgroundBrush", variant, out object? value),
                "The pack should resolve its menu selection brush.");
            ISolidColorBrush brush = Assert.IsAssignableFrom<ISolidColorBrush>(value);
            Assert.Equal(Color.Parse(expected), brush.Color);
            Assert.Equal(1.0, brush.Opacity, 3);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Menu_and_drop_down_hover_are_the_same_brush(string variantName)
    {
        // Harmonized deliberately: both surfaces hover on one colour, fitted to Finder's drop-down
        // selection. Finder itself uses two slightly different colours - its menu selection is
        // lighter - and fitting that lighter one is what made drop-down rows read washed out.
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        MacOSVersionDetector.SetTestOverride(true);
        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        var window = new Window { RequestedThemeVariant = variant };
        window.Resources["SystemAccentColor"] = Color.Parse(DefaultAccent);
        window.Styles.Add(theme);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            Assert.True(window.TryFindResource("MenuItemPointerOverBackgroundBrush", variant, out object? menu));
            Assert.True(window.TryFindResource("ComboBoxItemPointerOverBackgroundBrush", variant, out object? row));
            Assert.Same(menu, row);

            ISolidColorBrush brush = Assert.IsAssignableFrom<ISolidColorBrush>(menu);
            Assert.Equal(1.0, brush.Opacity, 3);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light", DefaultSelectionLight)]
    [InlineData("Dark", DefaultSelectionDark)]
    public void AccentHighlight_resolves_opaque_to_the_derived_colour(string variantName, string expected)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        MacOSVersionDetector.SetTestOverride(true);
        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        var window = new Window { RequestedThemeVariant = variant };
        window.Resources["SystemAccentColor"] = Color.Parse(DefaultAccent);
        window.Styles.Add(theme);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            Assert.True(window.TryFindResource("AccentHighlight", variant, out object? value));
            ISolidColorBrush brush = Assert.IsAssignableFrom<ISolidColorBrush>(value);

            // Opaque: the light target is unreachable through opacity over the popup surface at all,
            // so the brush colour has to be the rendered colour.
            Assert.Equal(1.0, brush.Opacity, 3);
            Assert.Equal(Color.Parse(expected), brush.Color);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }
}
