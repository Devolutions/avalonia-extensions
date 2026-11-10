namespace Devolutions.AvaloniaControls.Tests;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    private static void VerifyEditableVisualStates(Window window)
    {
        var combo = new ComboBox
        {
            IsEditable = true,
            IsEnabled = false,
            PlaceholderText = "Required",
        };
        window.Content = combo;
        Dispatcher.UIThread.RunJobs();

        TextBox editor = Assert.Single(combo.GetVisualDescendants().OfType<TextBox>(),
            textBox => textBox.Name == "PART_EditableTextBox");
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(editor.Background).Color);

        Path glyph = Assert.Single(combo.GetVisualDescendants().OfType<Path>(),
            path => path.Name == "DropDownGlyph");
        Assert.True(glyph.TryFindResource("ComboBoxDropDownGlyphForegroundDisabled", glyph.ActualThemeVariant, out object? disabledGlyph));
        Assert.Same(disabledGlyph, glyph.Stroke);

        combo.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(glyph.TryFindResource("ComboBoxEditableDropDownGlyphForeground", glyph.ActualThemeVariant, out object? editableGlyph));
        Assert.Same(editableGlyph, glyph.Stroke);

        var states = (IPseudoClasses)combo.Classes;
        states.Set(":error", true);
        states.Set(":focus-within", true);
        Dispatcher.UIThread.RunJobs();

        Border background = Assert.Single(combo.GetVisualDescendants().OfType<Border>(),
            border => border.Name == "Background");
        Assert.Equal(BackgroundSizing.CenterBorder, background.BackgroundSizing);
        Assert.True(window.TryFindResource("SystemControlErrorTextForegroundBrush", out object? errorBrush));
        Assert.Equal(
            Assert.IsAssignableFrom<ISolidColorBrush>(errorBrush).Color,
            Assert.IsAssignableFrom<ISolidColorBrush>(background.BorderBrush).Color);

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
            VerifyEditableVisualStates(window);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void Dark_border_renders_subtle_sides_without_double_compositing(double scale)
    {
        var combo = new ComboBox
        {
            Width = 200,
            Height = 32,
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var panel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#202020")),
            Children = { combo },
        };
        var window = new Window
        {
            Width = 240,
            Height = 52,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = panel,
        };
        window.Styles.Add(CreateTheme());
        Assert.True(window.TryFindResource(typeof(ComboBox), out var comboTheme));
        combo.Theme = Assert.IsType<ControlTheme>(comboTheme);
        window.Show();
        try
        {
            foreach (bool hovered in new[] { false, true })
            {
                ((IPseudoClasses)combo.Classes).Set(":pointerover", hovered);
                ((IPseudoClasses)combo.Classes).Set(":focus-visible", false);
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(ThemeVariant.Dark, combo.ActualThemeVariant);
                Assert.Equal(new Rect(10, 10, 200, 32), combo.Bounds);

                var size = new PixelSize((int)(240 * scale), (int)(52 * scale));
                using var rendered = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
                rendered.Render(panel);
                using var pixels = new WriteableBitmap(size, new Vector(96, 96),
                    PixelFormat.Rgba8888, AlphaFormat.Premul);
                using var buffer = pixels.Lock();
                rendered.CopyPixels(buffer);

                byte Gray(int x, int y)
                {
                    int offset = y * buffer.RowBytes + x * 4;
                    byte red = Marshal.ReadByte(buffer.Address, offset);
                    Assert.Equal(red, Marshal.ReadByte(buffer.Address, offset + 1));
                    Assert.Equal(red, Marshal.ReadByte(buffer.Address, offset + 2));
                    return red;
                }

                int centreX = (int)(110 * scale);
                int centreY = (int)(26 * scale);
                Assert.InRange(Gray(centreX, (int)(10 * scale)), 59, 61);
                Assert.InRange(Gray((int)(10 * scale), centreY), 47, 49);
                Assert.InRange(Gray((int)(210 * scale) - 1, centreY), 47, 49);
                Assert.InRange(Gray(centreX, (int)(42 * scale) - 1), 47, 49);
                if (scale == 1.5)
                {
                    // The fractional inner row blends the 48-gray stroke with the 45-gray fill.
                    Assert.InRange(Gray(centreX, (int)(42 * scale) - 2), 45, 49);
                }
            }
        }
        finally
        {
            window.Close();
        }
    }
}
