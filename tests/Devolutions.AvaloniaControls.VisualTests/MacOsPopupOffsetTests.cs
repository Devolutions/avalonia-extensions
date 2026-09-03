namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using Xunit;

/// <summary>
///   Checks the popup offset a control actually computes, not just the resources it is built from.
/// </summary>
/// <remarks>
///   <para>
///     <c>InitialFirstItemDistance</c> and <c>PopupTrimHeight</c> are constants that duplicate
///     geometry defined elsewhere - the first must equal <c>ComboBoxShadowMargin.Bottom +
///     PopupPadding.Top</c>, the second <c>PopupMargin</c>'s vertical total. Nothing recomputes
///     them, so changing the geometry silently mis-positions every ComboBox popup.
///   </para>
///   <para>
///     These tests therefore derive what the offset should be from the *source* geometry, open a
///     real popup, and compare. That covers the whole chain - geometry, constant, and the control
///     that consumes it - rather than asserting a constant against itself.
///   </para>
///   <para>
///     Note both constants live in merged dictionaries where the LiquidGlass copy is merged last,
///     so <c>StaticResource</c> resolves them correctly; the <c>DynamicResource</c> the templates
///     now use is belt-and-braces. That is unlike
///     <c>MacOsMenuSelectionCornerRadius</c>, whose classic default is an own entry and whose
///     LiquidGlass value arrives through the runtime alias dictionary - there
///     <c>StaticResource</c> did resolve the wrong one.
///   </para>
/// </remarks>
[Collection("VisualTests")]
public class MacOsPopupOffsetTests
{
    private const int SelectedIndex = 3;

    private static Window ShowLiquidGlass(ThemeVariant variant)
    {
        MacOSVersionDetector.SetTestOverride(true);
        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        var window = new Window { RequestedThemeVariant = variant, Width = 400, Height = 300 };
        window.Styles.Add(theme);
        return window;
    }

    private static Avalonia.Thickness Thickness(Window w, string key, ThemeVariant variant)
    {
        Assert.True(w.TryFindResource(key, variant, out object? value), $"'{key}' should resolve.");
        return Assert.IsType<Avalonia.Thickness>(value);
    }

    private static int Int(Window w, string key, ThemeVariant variant)
    {
        Assert.True(w.TryFindResource(key, variant, out object? value), $"'{key}' should resolve.");
        return Convert.ToInt32(value);
    }

    /// <summary>
    ///   Opens the drop-down and returns the offset the template's binding produced, sampled at
    ///   <see cref="Popup.Opened" />.
    /// </summary>
    /// <remarks>
    ///   <see cref="Devolutions.AvaloniaControls.Behaviors.ComboBoxPopupAlignmentBehavior" /> refines
    ///   this value afterwards, on the popup's first layout pass, by measuring where the selected row
    ///   actually landed - see <see cref="MacOsComboBoxPopupAlignmentTests" /> for that outcome.
    ///   These tests are about the estimate the constants feed in, so they sample before that runs.
    /// </remarks>
    private static double OpenAndCaptureBoundOffset(Window window, ComboBox combo)
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
        double captured = double.NaN;
        popup.Opened += (_, _) => captured = popup.VerticalOffset;

        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.False(double.IsNaN(captured), "The popup should have opened.");
        return captured;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Combo_box_popup_offset_follows_the_resolved_constants(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window window = ShowLiquidGlass(variant);
        var combo = new ComboBox
        {
            ItemsSource = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToList(),
            SelectedIndex = SelectedIndex,
            // Large enough that the offset is not clamped, so this measures the offset itself.
            MaxDropDownHeight = 400,
        };
        window.Content = combo;

        try
        {
            double actualOffset = OpenAndCaptureBoundOffset(window, combo);

            // Derived from the geometry the constant is supposed to track, not from the constant.
            Avalonia.Thickness shadow = Thickness(window, "ComboBoxShadowMargin", variant);
            Avalonia.Thickness padding = Thickness(window, "PopupPadding", variant);
            int rowHeight = Int(window, "ComboBoxItemHeight", variant);
            double expected = ((SelectedIndex + 1) * -rowHeight) - (shadow.Bottom + padding.Top);

            Assert.Equal(expected, actualOffset);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   The clamp path, which is what <c>PopupTrimHeight</c> drives: with a short
    ///   MaxDropDownHeight the offset must stop at the popup's usable height instead of shifting
    ///   the selected row all the way up.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Combo_box_popup_offset_clamps_using_the_resolved_trim(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window window = ShowLiquidGlass(variant);
        const double maxDropDownHeight = 60;
        var combo = new ComboBox
        {
            ItemsSource = Enumerable.Range(1, 40).Select(i => $"Item {i}").ToList(),
            SelectedIndex = 30,
            MaxDropDownHeight = maxDropDownHeight,
        };
        window.Content = combo;

        try
        {
            double actualOffset = OpenAndCaptureBoundOffset(window, combo);

            // PopupTrimHeight is meant to be PopupMargin's vertical total; derive it from there.
            Avalonia.Thickness margin = Thickness(window, "PopupMargin", variant);
            Assert.Equal(-(maxDropDownHeight - (margin.Top + margin.Bottom)), actualOffset);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }
}
