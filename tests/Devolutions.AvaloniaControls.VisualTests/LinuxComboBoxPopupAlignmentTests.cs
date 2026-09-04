namespace Devolutions.AvaloniaControls.VisualTests;

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Behaviors;
using Devolutions.AvaloniaTheme.Linux;
using Xunit;

/// <summary>
///   Checks the outcome the Linux drop-down is trying to achieve: the selected row lands on the
///   closed ComboBox, so the visible text does not jump when the list opens.
/// </summary>
/// <remarks>
///   <para>
///     The Linux theme uses the same <c>SelectedIndexToPopupOffsetConverter</c> estimate as the
///     macOS one, and shares its limitation: the estimate is only correct while the whole list fits
///     in the popup. Once the list scrolls, the ComboBox scrolls the selected row into view and the
///     row is no longer at <c>SelectedIndex * ItemHeight</c>.
///   </para>
///   <para>
///     These mirror <see cref="MacOsComboBoxPopupAlignmentTests" />, minus the parts that are
///     specific to macOS: there is no Liquid Glass/classic geometry split to cover, and the Linux
///     resources do not alias accent colours with <c>StaticResource</c>, so no application-wide
///     accent fallback is needed.
///   </para>
///   <para>
///     Every test here works by <em>perturbing an open drop-down</em> and asserting the correction
///     loop closes the gap again. Simply opening a drop-down and checking the row proves nothing:
///     under the headless overlay popup host the offset binding and Avalonia's own
///     <c>ScrollIntoView</c> already land the row on the ComboBox at every index, so such a test
///     passes with the behaviour disabled - and even with the arithmetic estimate deliberately
///     broken. Each test below was mutation-tested to confirm it fails when the mechanism it covers
///     is removed.
///   </para>
/// </remarks>
[Collection("VisualTests")]
public class LinuxComboBoxPopupAlignmentTests
{
    /// <summary>Rounding across two coordinate spaces; a fraction of a row is invisible.</summary>
    private const double Tolerance = 2.0;

    private static Window ShowLinux(ThemeVariant variant)
    {
        var theme = new DevolutionsLinuxYaruTheme();
        theme.BeginInit();
        theme.EndInit();
        // Tall enough that a scrolling drop-down is not additionally clamped by the window.
        var window = new Window { RequestedThemeVariant = variant, Width = 400, Height = 700 };
        window.Styles.Add(theme);
        return window;
    }

    /// <summary>
    ///   Vertical distance between the centre of the closed ComboBox and the centre of the selected
    ///   row, in screen space. Zero means "the text did not move".
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

    /// <summary>
    ///   The drop-down must not be allowed to flip to the other side of the ComboBox.
    /// </summary>
    /// <remarks>
    ///   A template assertion rather than a positional one: the flip is performed by the platform
    ///   compositor, which the headless overlay popup host does not model, so the misplacement it
    ///   causes cannot be reproduced in-process. See the comment on the template for why the default
    ///   <c>All</c> - which includes <c>FlipY</c> - is wrong for a drop-down that is deliberately
    ///   offset upwards.
    /// </remarks>
    [AvaloniaFact]
    public void Drop_down_is_not_allowed_to_flip_to_the_other_side()
    {
        Window window = ShowLinux(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 20, selectedIndex: 10, maxDropDownHeight: 200);
            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());

            Assert.False(
                popup.PlacementConstraintAdjustment.HasFlag(PopupPositionerConstraintAdjustment.FlipY),
                "FlipY would send the drop-down to the far side of the ComboBox at the bottom of the screen.");

            Assert.True(
                popup.PlacementConstraintAdjustment.HasFlag(PopupPositionerConstraintAdjustment.SlideY),
                "Without SlideY the drop-down could be positioned off-screen instead of being nudged back on.");
        }
        finally
        {
            window.Close();
        }
    }



    /// <summary>
    ///   A drop-down knocked out of alignment while open must be corrected without waiting to be
    ///   closed and reopened.
    /// </summary>
    /// <remarks>
    ///   The real trigger is choosing a different item from the open list, whose fresh arithmetic
    ///   estimate is exactly the error this behavior corrects. That cannot be asserted positionally
    ///   here, because the headless overlay popup host leaves every index aligned on its own, so the
    ///   offset is moved directly instead - the same external write the binding performs.
    /// </remarks>
    [AvaloniaFact]
    public void Offset_moved_while_open_is_corrected_without_reopening()
    {
        Window window = ShowLinux(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 500, maxDropDownHeight: 200);
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);

            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
            popup.SetCurrentValue(Popup.VerticalOffsetProperty, popup.VerticalOffset + 100);
            Dispatcher.UIThread.RunJobs();

            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    ///   Stepping between two adjacent far-down selections in an open drop-down must re-align, even
    ///   though the offset binding produces no new value.
    /// </summary>
    /// <remarks>
    ///   The converter clamps at the effective popup height, so every sufficiently large index
    ///   yields the same offset and no property change is raised. The selection itself has to be
    ///   observed.
    /// </remarks>
    [AvaloniaFact]
    public void Stepping_between_clamped_selections_still_re_aligns()
    {
        Window window = ShowLinux(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 500, maxDropDownHeight: 200);
            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());

            // Scroll the open list away from the selected row. The behavior has already converged
            // and unsubscribed, so nothing corrects this on its own.
            ScrollViewer scroller = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().First();
            scroller.SetCurrentValue(ScrollViewer.OffsetProperty, scroller.Offset.WithY(scroller.Offset.Y + 100));
            Dispatcher.UIThread.RunJobs();

            double displaced = SelectedRowOffsetFromComboBox(combo);
            Assert.True(
                Math.Abs(displaced) > Tolerance,
                $"The row should be displaced before the selection changes, but it was off by {displaced}.");

            // Both indices produce the same clamped offset, so the binding raises no property change
            // at all - only SelectionChanged can reveal that the row to align to has moved.
            Assert.Equal(BindingOffsetFor(selectedIndex: 500), BindingOffsetFor(selectedIndex: 501), 3);

            combo.SelectedIndex = 501;
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    ///   Enabling the behavior while the drop-down is already showing must align that opening,
    ///   rather than waiting for an <see cref="Popup.Opened" /> event that has already passed.
    /// </summary>
    [AvaloniaFact]
    public void Enabling_the_behavior_on_an_open_drop_down_aligns_it()
    {
        Window window = ShowLinux(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 500, maxDropDownHeight: 200);

            ComboBoxPopupAlignmentBehavior.SetEnable(combo, false);
            Dispatcher.UIThread.RunJobs();

            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
            popup.SetCurrentValue(Popup.VerticalOffsetProperty, popup.VerticalOffset + 100);
            Dispatcher.UIThread.RunJobs();
            Assert.True(
                Math.Abs(SelectedRowOffsetFromComboBox(combo)) > Tolerance,
                "The drop-down should be misaligned while the behavior is off, or this proves nothing.");

            ComboBoxPopupAlignmentBehavior.SetEnable(combo, true);
            Dispatcher.UIThread.RunJobs();

            Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        }
        finally
        {
            window.Close();
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

        Window window = ShowLinux(ThemeVariant.Light);
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
        }
    }

    /// <summary>
    ///   The offset the template's binding produces for <paramref name="selectedIndex" />, sampled
    ///   at <see cref="Popup.Opened" /> - before any correction has been applied, and without
    ///   involving the close path at all.
    /// </summary>
    private static double BindingOffsetFor(int selectedIndex)
    {
        Window window = ShowLinux(ThemeVariant.Light);
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

            // Disable the correction pass so the estimate is observed on its own.
            ComboBoxPopupAlignmentBehavior.SetEnable(combo, false);
            Dispatcher.UIThread.RunJobs();

            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();

            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
            return popup.VerticalOffset;
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    ///   The delayed settle check exists to catch a popup whose asynchronous move landed after the
    ///   loop had already unsubscribed. It must not mistake the user's own scrolling for that: if
    ///   the list is wheeled inside the delay window, re-aligning would snap it back from under
    ///   them.
    /// </summary>
    /// <remarks>
    ///   This drives the decision directly rather than waiting for the real callback.
    ///   <c>DispatcherTimer</c> does not fire under the headless dispatcher - it runs on a virtual
    ///   clock that sleeping does not advance - so a test that scrolled and then waited would pass
    ///   whether or not the guard existed, which an earlier version of this test did.
    /// </remarks>
    [Theory]
    [InlineData(120.0, 120.0, false)]   // Untouched: a late compositor move leaves the list alone.
    [InlineData(120.0, 220.0, true)]    // Wheeled down inside the delay window.
    [InlineData(120.0, 20.0, true)]     // Wheeled up.
    [InlineData(120.0, 120.3, false)]   // Sub-tolerance jitter is not a user scroll.
    [InlineData(null, 120.0, false)]    // No scroller when scheduled: nothing to compare against.
    [InlineData(120.0, null, false)]
    public void Scrolling_within_the_settle_delay_is_recognized(double? atSchedule, double? now, bool expected) =>
        Assert.Equal(expected, ComboBoxPopupAlignmentBehavior.UserScrolledSince(atSchedule, now));

    /// <summary>
    ///   Guards the premise of the test above: the row really is displaced by a scroll, and nothing
    ///   in the behaviour puts it back on its own.
    /// </summary>
    [AvaloniaFact]
    public void Scrolling_an_aligned_drop_down_is_not_undone()
    {
        Window window = ShowLinux(ThemeVariant.Light);
        try
        {
            ComboBox combo = OpenComboBox(window, itemCount: 1000, selectedIndex: 500, maxDropDownHeight: 200);
            Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());

            ScrollViewer scroller = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().First();
            scroller.SetCurrentValue(ScrollViewer.OffsetProperty, scroller.Offset.WithY(scroller.Offset.Y + 100));
            Dispatcher.UIThread.RunJobs();

            double displaced = SelectedRowOffsetFromComboBox(combo);
            Assert.True(
                Math.Abs(displaced) > Tolerance,
                $"The row should be displaced by the scroll, but it was off by {displaced}.");

            Dispatcher.UIThread.RunJobs();
            Assert.Equal(displaced, SelectedRowOffsetFromComboBox(combo), 3);
        }
        finally
        {
            window.Close();
        }
    }
}
