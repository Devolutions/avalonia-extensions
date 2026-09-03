namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Behaviors;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using Xunit;

/// <summary>
///   Checks the outcome the macOS drop-down is actually trying to achieve: the selected row lands
///   on the closed ComboBox, so the visible text does not jump when the list opens.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="MacOsPopupOffsetTests" /> pins the arithmetic estimate the template's
///     <c>VerticalOffset</c> binding produces. That estimate is only correct while the entire list
///     fits in the popup; once the list scrolls, the ComboBox scrolls the selected row into view and
///     the row is no longer at <c>SelectedIndex * ItemHeight</c>.
///   </para>
///   <para>
///     These tests therefore assert the observable result rather than the intermediate offset, which
///     is what lets the correction pass use whichever lever - popup offset or scroll position - is
///     still available.
///   </para>
/// </remarks>
[Collection("VisualTests")]
public class MacOsComboBoxPopupAlignmentTests
{
    /// <summary>Rounding across two coordinate spaces; a fraction of a row is invisible.</summary>
    private const double Tolerance = 2.0;

    /// <summary>Accent colour keys the classic resources alias at load time.</summary>
    private static readonly (string Key, string Colour)[] AccentFallback =
    [
        ("SystemAccentColor", "#007aff"),
        ("SystemAccentColorLight1", "#3395ff"),
        ("SystemAccentColorLight2", "#66b0ff"),
        ("SystemAccentColorLight3", "#99caff"),
        ("SystemAccentColorDark1", "#0062cc"),
        ("SystemAccentColorDark2", "#004999"),
        ("SystemAccentColorDark3", "#003166"),
    ];

    /// <summary>
    ///   Shows a window using one of the two macOS geometries. They differ in row height and popup
    ///   trim, so the alignment cases are worth running against both.
    /// </summary>
    /// <remarks>
    ///   The classic resources alias the accent colours with <c>StaticResource</c>, which resolves as
    ///   the theme loads, so the accent has to already be in scope - putting it on the window is too
    ///   late. It therefore goes on the <see cref="Application" />, which the headless test host
    ///   shares with every other test, so <see cref="RemoveAccentFallback" /> must take it back off
    ///   again rather than leaving the suite order-dependent.
    /// </remarks>
    private static Window ShowMacOs(ThemeVariant variant, bool liquidGlass = true)
    {
        MacOSVersionDetector.SetTestOverride(liquidGlass);

        if (Application.Current is { } app)
        {
            foreach ((string key, string colour) in AccentFallback)
            {
                app.Resources[key] = Color.Parse(colour);
            }
        }

        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        // Tall enough that a scrolling drop-down is not additionally clamped by the window.
        var window = new Window { RequestedThemeVariant = variant, Width = 400, Height = 700 };
        window.Styles.Add(theme);
        return window;
    }

    /// <summary>Undoes <see cref="ShowMacOs" />'s application-wide accent fallback.</summary>
    private static void RemoveAccentFallback()
    {
        if (Application.Current is not { } app) return;

        foreach ((string key, _) in AccentFallback)
        {
            app.Resources.Remove(key);
        }
    }

    private static Window ShowLiquidGlass(ThemeVariant variant) => ShowMacOs(variant);

    /// <summary>
    ///   The accent fallback lives on the application the headless host shares with every other
    ///   test, so it must not outlive these tests.
    /// </summary>
    [AvaloniaFact]
    public void Accent_fallback_does_not_outlive_the_test()
    {
        Window window = ShowMacOs(ThemeVariant.Light, liquidGlass: false);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Close();
        MacOSVersionDetector.SetTestOverride(null);
        RemoveAccentFallback();

        foreach ((string key, _) in AccentFallback)
        {
            Assert.False(Application.Current!.Resources.ContainsKey(key), $"'{key}' should not be left behind.");
        }
    }

    /// <summary>
    ///   Vertical distance between the centre of the closed ComboBox and the centre of the
    ///   selected row, in screen space. Zero means "the text did not move".
    /// </summary>
    private static double SelectedRowOffsetFromComboBox(ComboBox combo)
    {
        Control container = Assert.IsAssignableFrom<Control>(combo.ContainerFromIndex(combo.SelectedIndex));
        Assert.True(container.IsAttachedToVisualTree(), "The selected row should be realized when the popup is open.");

        double comboCentre = combo.PointToScreen(new Point(0, combo.Bounds.Height / 2)).Y;
        double rowCentre = container.PointToScreen(new Point(0, container.Bounds.Height / 2)).Y;
        return rowCentre - comboCentre;
    }

    private static ComboBox OpenComboBox(Window window, int itemCount, int selectedIndex, double maxDropDownHeight)
    {
        var combo = new ComboBox
        {
            ItemsSource = Enumerable.Range(1, itemCount).Select(i => $"Item {i}").ToList(),
            SelectedIndex = selectedIndex,
            MaxDropDownHeight = maxDropDownHeight,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        window.Content = combo;
        window.Show();
        Dispatcher.UIThread.RunJobs();

        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();
        return combo;
    }

    /// <summary>The case that already worked: the whole list fits, no scrolling involved.</summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selected_row_sits_on_the_combo_box_when_the_list_fits(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window window = ShowLiquidGlass(variant);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 6, selectedIndex: 3, maxDropDownHeight: 400);
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   The regression this work targets. With a selection in the middle of a long, scrolling list
    ///   the popup cannot be shifted far enough on its own, so the list has to be scrolled to make
    ///   up the difference.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Selected_row_sits_on_the_combo_box_when_the_list_scrolls(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window window = ShowLiquidGlass(variant);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 200, selectedIndex: 120, maxDropDownHeight: 200);
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   The same cases against the classic geometry, whose row height and popup trim differ from
    ///   LiquidGlass. The alignment is measured rather than derived from those constants, so it
    ///   should hold either way - this is what proves that.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(120)]
    [InlineData(199)]
    public void Selected_row_sits_on_the_combo_box_with_the_classic_geometry(int selectedIndex)
    {
        Window window = ShowMacOs(ThemeVariant.Light, liquidGlass: false);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 200, selectedIndex: selectedIndex, maxDropDownHeight: 200);
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   The ends of a long list are the case where the popup has to do the work: the list is
    ///   already scrolled as far as it goes, so the alignment can only come from moving the popup.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(199)]
    public void Selected_row_sits_on_the_combo_box_at_the_ends_of_a_long_list(int selectedIndex)
    {
        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 200, selectedIndex: selectedIndex, maxDropDownHeight: 200);
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   Guards the mechanism rather than the outcome: for a mid-list selection the alignment must
    ///   come from scrolling, since the popup alone cannot reach that far into the list. Without
    ///   that second lever the first item would be showing instead.
    /// </summary>
    [AvaloniaFact]
    public void Long_list_is_scrolled_to_bring_the_selection_into_view()
    {
        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 200, selectedIndex: 120, maxDropDownHeight: 200);

            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
            ScrollViewer scroller = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().First();

            Assert.True(scroller.Offset.Y > 0, "A mid-list selection should have scrolled the drop-down.");
            Assert.True(
                scroller.Offset.Y < scroller.Extent.Height - scroller.Viewport.Height,
                "A mid-list selection should not have run the drop-down to the end of the list.");
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   Re-opening must align as well as the first open did. Corrections used to accumulate on top
    ///   of the offset the template's binding produced, so the second open of a far-down selection
    ///   started from an already-corrected offset and landed a row or more out.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(998)]
    [InlineData(120)]
    [InlineData(0)]
    public void Selected_row_still_sits_on_the_combo_box_after_reopening(int selectedIndex)
    {
        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: selectedIndex, maxDropDownHeight: 200);
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);

            for (int reopen = 0; reopen < 3; reopen++)
            {
                combo.IsDropDownOpen = false;
                Dispatcher.UIThread.RunJobs();

                combo.IsDropDownOpen = true;
                Dispatcher.UIThread.RunJobs();

                Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
            }
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   Closing must hand the offset back to the template's binding. A leftover correction is what
    ///   let a later open place the drop-down far away from its ComboBox.
    /// </summary>
    [AvaloniaFact]
    public void Closing_the_drop_down_restores_the_offset_the_binding_produced()
    {
        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 800, maxDropDownHeight: 200);

            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());

            combo.IsDropDownOpen = false;
            Dispatcher.UIThread.RunJobs();

            double afterFirstClose = popup.VerticalOffset;

            for (int reopen = 0; reopen < 3; reopen++)
            {
                combo.IsDropDownOpen = true;
                Dispatcher.UIThread.RunJobs();
                combo.IsDropDownOpen = false;
                Dispatcher.UIThread.RunJobs();

                // Every open/close cycle must land back on the same estimate rather than drifting by
                // the correction that cycle applied.
                Assert.Equal(afterFirstClose, popup.VerticalOffset, 3);
            }
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   The everyday interaction: the user opens the list and picks a different item. The offset
    ///   left behind must be the estimate for the item they picked, not the one that was selected
    ///   when the drop-down opened - otherwise the next open starts from a stale position and is
    ///   visibly dragged into place.
    /// </summary>
    [AvaloniaFact]
    public void Choosing_a_different_item_leaves_the_offset_for_the_new_selection()
    {
        double expected = BindingOffsetFor(selectedIndex: 500);

        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 0, maxDropDownHeight: 200);
            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());

            // Pick a far-away item from inside the open drop-down, as a user would.
            combo.SelectedIndex = 500;
            Dispatcher.UIThread.RunJobs();

            combo.IsDropDownOpen = false;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(expected, popup.VerticalOffset, 3);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   Switching the behavior off while the drop-down is open must hand the offset back too, or
    ///   the correction it had already applied would outlive it with nothing left to clear it.
    /// </summary>
    [AvaloniaFact]
    public void Disabling_the_behavior_while_open_hands_the_offset_back()
    {
        double expected = BindingOffsetFor(selectedIndex: 500);

        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 500, maxDropDownHeight: 200);
            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());

            ComboBoxPopupAlignmentBehavior.SetEnable(combo, false);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(expected, popup.VerticalOffset, 3);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   Reopening before a queued pass has run must not let that pass measure the new drop-down. It
    ///   was queued against the previous opening, so it would run before the new popup has been
    ///   positioned - the stale-coordinate reading the deferred timing exists to avoid.
    /// </summary>
    [AvaloniaFact]
    public void A_pass_queued_for_a_previous_opening_does_not_disturb_the_next_one()
    {
        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 700, maxDropDownHeight: 200);

            // Close and reopen without draining the dispatcher, so any pass queued for the first
            // opening is still pending when the second one starts.
            combo.IsDropDownOpen = false;
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();

            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }

    /// <summary>
    ///   The offset the template's binding produces for <paramref name="selectedIndex" />, sampled
    ///   at <see cref="Popup.Opened" /> - before any correction has been applied, and without
    ///   involving the close path at all. Reading it after a close would make this agree with a
    ///   broken close path instead of checking it.
    /// </summary>
    private static double BindingOffsetFor(int selectedIndex)
    {
        Window window = ShowLiquidGlass(ThemeVariant.Light);
        try
        {
            var combo = new ComboBox
            {
                ItemsSource = Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList(),
                SelectedIndex = selectedIndex,
                MaxDropDownHeight = 200,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            window.Content = combo;
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
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
            RemoveAccentFallback();
        }
    }
}
