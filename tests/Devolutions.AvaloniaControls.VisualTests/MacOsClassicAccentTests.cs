namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using Xunit;

/// <summary>
///   Guards that the classic MacOS theme's accent-derived surfaces follow the system accent when it
///   changes <em>after</em> the theme has loaded.
/// </summary>
/// <remarks>
///   <para>
///     The platform accent arrives late: a theme dictionary can be materialised before Avalonia has
///     the real value, in which case it sees the fallback accent (#0078d7). Anything that captured a
///     <c>Color</c> at that moment is frozen on the fallback for the life of the process.
///   </para>
///   <para>
///     That is what a <c>&lt;StaticResource&gt;</c> alias to a platform colour does - it resolves
///     once and stores a <c>Color</c>, which is a struct, so there is nothing left to update. An
///     alias to a <em>brush</em> whose own <c>Color</c> is a <c>DynamicResource</c> is fine, because
///     the captured brush stays live. These tests pin the distinction: they force the accent after
///     <c>Show()</c> and assert the derived brushes moved with it.
///   </para>
/// </remarks>
[Collection("VisualTests")]
public class MacOsClassicAccentTests
{
    // Deliberately nothing like the fallback accent (#0078d7 / Light1 #269fff / Dark1 #0063b1), so a
    // frozen value cannot be mistaken for a followed one.
    private static readonly Color Accent = Color.Parse("#ff0000");
    private static readonly Color AccentLight1 = Color.Parse("#ff5555");
    private static readonly Color AccentDark1 = Color.Parse("#aa0000");

    private static Window ShowClassicThenForceAccent(ThemeVariant variant)
    {
        MacOSVersionDetector.SetTestOverride(false);
        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The whole point: the accent lands only now, after the dictionaries are up. Setting it
        // before Show() would let a StaticResource alias capture the right value and hide the bug.
        window.Resources["SystemAccentColor"] = Accent;
        window.Resources["SystemAccentColorLight1"] = AccentLight1;
        window.Resources["SystemAccentColorDark1"] = AccentDark1;
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    /// <summary>Reads a key as a colour, whether it resolves to a brush or a bare colour.</summary>
    private static Color ColorOf(Window w, string key, ThemeVariant variant)
    {
        Assert.True(w.TryFindResource(key, variant, out object? value), $"'{key}' should resolve.");
        return value switch
        {
            ISolidColorBrush brush => brush.Color,
            Color color => color,
            _ => throw new Xunit.Sdk.XunitException(
                $"'{key}' resolved to {value?.GetType().Name ?? "<null>"}, expected a solid brush or a colour."),
        };
    }

    private static (Color Top, Color Bottom) StopsOf(Window w, string key, ThemeVariant variant)
    {
        Assert.True(w.TryFindResource(key, variant, out object? value), $"'{key}' should resolve.");
        IGradientBrush brush = Assert.IsAssignableFrom<IGradientBrush>(value);
        Assert.Equal(2, brush.GradientStops.Count);
        return (brush.GradientStops[0].Color, brush.GradientStops[1].Color);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Menu_hover_follows_a_late_accent_change(string variantName)
    {
        // The known-good control: this brush reads {DynamicResource SystemAccentColor} directly, so
        // it has always followed. If this one fails, the harness is wrong, not the theme.
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowClassicThenForceAccent(variant);
        try
        {
            Assert.Equal(Accent, ColorOf(w, "ControlBackgroundAccentMidBrush", variant));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Accent_raised_gradient_follows_a_late_accent_change(string variantName)
    {
        // Reaches accent buttons, checkbox/radio fills, the ComboBox button, drop-down row hover,
        // DropDownButton and ListBoxItem selection - most of the classic accent surface.
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowClassicThenForceAccent(variant);
        try
        {
            (Color top, Color bottom) = StopsOf(w, "ControlBackgroundAccentRaisedBrush", variant);
            if (variant == ThemeVariant.Dark)
            {
                Assert.Equal(Accent, top);
                Assert.Equal(AccentDark1, bottom);
            }
            else
            {
                Assert.Equal(AccentLight1, top);
                Assert.Equal(Accent, bottom);
            }
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Accent_recessed_gradient_follows_a_late_accent_change(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowClassicThenForceAccent(variant);
        try
        {
            (Color top, Color bottom) = StopsOf(w, "ButtonBackgroundAccentRecessedBrush", variant);
            if (variant == ThemeVariant.Dark)
            {
                Assert.Equal(AccentLight1, top);
                Assert.Equal(Accent, bottom);
            }
            else
            {
                Assert.Equal(Accent, top);
                Assert.Equal(AccentDark1, bottom);
            }
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Accent_borders_follow_a_late_accent_change(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowClassicThenForceAccent(variant);
        try
        {
            // Dark takes the lighter step so the border reads against a dark surface.
            Color expected = variant == ThemeVariant.Dark ? AccentLight1 : Accent;
            Assert.Equal(expected, ColorOf(w, "ControlBorderAccentBrush", variant));
            Assert.Equal(expected, ColorOf(w, "ButtonBorderAccentBrush", variant));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void LiquidGlass_still_overrides_the_ComboBox_accent_tokens(string variantName)
    {
        // These three tokens used to sit at the classic dictionary's root and now sit in its
        // ThemeDictionaries, so that they can vary by variant. LiquidGlass overrides all three and is
        // merged after classic, so it must still win - if the move had cost it that, drop-down rows
        // would hover with the classic gradient under LiquidGlass instead of AccentHighlight.
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        MacOSVersionDetector.SetTestOverride(true);
        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            Assert.True(window.TryFindResource("AccentHighlight", variant, out object? accentHighlight));
            Assert.True(window.TryFindResource("ComboBoxItemPointerOverBackgroundBrush", variant, out object? hover));
            Assert.Same(accentHighlight, hover);

            Assert.True(window.TryFindResource("ComboBoxButtonBackgroundBrush", variant, out object? button));
            Assert.IsNotAssignableFrom<IGradientBrush>(button);
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
    public void Checked_pressed_fill_follows_a_late_accent_change(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowClassicThenForceAccent(variant);
        try
        {
            Assert.Equal(Accent, ColorOf(w, "CheckBoxCheckedPressedBackgroundBrush", variant));

            // The radio button takes the same fill. The light dictionary used to spell this key
            // "RadioButtonPressedCheckedBackgroundBrush" while every consumer says
            // "CheckedPressed", so in light it resolved nothing at all and the pressed-checked
            // radio kept its unpressed fill.
            Assert.Equal(Accent, ColorOf(w, "RadioButtonCheckedPressedBackgroundBrush", variant));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }
}
