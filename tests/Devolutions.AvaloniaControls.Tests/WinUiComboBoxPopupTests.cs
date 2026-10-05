namespace Devolutions.AvaloniaControls.Tests;

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaControls.Behaviors;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Xunit;

[Collection("StylesTest")]
public class WinUiComboBoxPopupTests
{
    private const double Tolerance = 2.0;

    private static DevolutionsWinUiTheme CreateTheme()
    {
        Windows11MicaDetector.SetTestOverride(false);
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            return theme;
        }
        finally
        {
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    private static double SelectedRowOffsetFromComboBox(ComboBox combo)
    {
        Control container = Assert.IsAssignableFrom<Control>(combo.ContainerFromIndex(combo.SelectedIndex));
        Assert.True(container.IsAttachedToVisualTree(), "The selected row should be realized when the popup is open.");

        double comboCentre = combo.PointToScreen(new Point(0, combo.Bounds.Height / 2)).Y;
        double rowCentre = container.PointToScreen(new Point(0, container.Bounds.Height / 2)).Y;
        return rowCentre - comboCentre;
    }

    private static void Detach(Window window, ComboBox combo)
    {
        combo.IsDropDownOpen = false;
        window.Content = null;
        Dispatcher.UIThread.RunJobs();
    }

    private static void VerifyNonEditable(
        Window window,
        int itemCount,
        int selectedIndex,
        double maxDropDownHeight)
    {
        var combo = new ComboBox
        {
            ItemsSource = Enumerable.Range(1, itemCount).Select(i => $"Item {i}").ToList(),
            SelectedIndex = selectedIndex,
            MaxDropDownHeight = maxDropDownHeight,
            VerticalAlignment = VerticalAlignment.Center,
        };
        window.Content = combo;
        Dispatcher.UIThread.RunJobs();
        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(ComboBoxPopupAlignmentBehavior.GetEnable(combo));
        Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);

        Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
        popup.SetCurrentValue(Popup.VerticalOffsetProperty, popup.VerticalOffset + 100);
        Dispatcher.UIThread.RunJobs();

        Assert.InRange(SelectedRowOffsetFromComboBox(combo), -Tolerance, Tolerance);
        Detach(window, combo);
    }

    private static void VerifyEditable(
        Window window,
        VerticalAlignment verticalAlignment,
        bool opensAbove)
    {
        var combo = new ComboBox
        {
            IsEditable = true,
            ItemsSource = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList(),
            SelectedIndex = 10,
            MaxDropDownHeight = 200,
            VerticalAlignment = verticalAlignment,
        };
        window.Content = combo;
        Dispatcher.UIThread.RunJobs();
        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        Popup popup = Assert.Single(combo.GetVisualDescendants().OfType<Popup>());
        Assert.False(ComboBoxPopupAlignmentBehavior.GetEnable(combo));
        Assert.Equal(0, popup.VerticalOffset);
        Assert.Equal(PlacementMode.BottomEdgeAlignedLeft, popup.Placement);
        Assert.True(popup.PlacementConstraintAdjustment.HasFlag(PopupPositionerConstraintAdjustment.FlipY));
        Assert.Equal(opensAbove, combo.Classes.Contains(":dropdown-open-from-top"));
        Assert.Equal(!opensAbove, combo.Classes.Contains(":dropdown-open-from-bottom"));
        Detach(window, combo);
    }

    [AvaloniaFact]
    public void Popup_geometry_covers_noneditable_and_editable_paths()
    {
        var window = new Window
        {
            Width = 400,
            Height = 700,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(CreateTheme());
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();

            VerifyNonEditable(window, itemCount: 5, selectedIndex: 3, maxDropDownHeight: 400);
            VerifyNonEditable(window, itemCount: 1000, selectedIndex: 500, maxDropDownHeight: 200);

            window.Height = 300;
            Dispatcher.UIThread.RunJobs();
            VerifyEditable(window, VerticalAlignment.Top, opensAbove: false);
            VerifyEditable(window, VerticalAlignment.Bottom, opensAbove: true);
        }
        finally
        {
            window.Close();
        }
    }
}
