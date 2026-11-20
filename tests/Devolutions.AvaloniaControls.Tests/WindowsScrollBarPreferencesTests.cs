using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.DevExpress;
using Devolutions.AvaloniaTheme.Internal;
using Devolutions.AvaloniaTheme.WinUI;

namespace Devolutions.AvaloniaControls.Tests;

[Collection("StylesTest")]
public class WindowsScrollBarPreferencesTests
{
    [Theory]
    [InlineData("-255", 1)]
    [InlineData("-480", 32.0 / 17)]
    [InlineData("-120", 8.0 / 17)]
    [InlineData("-180", 12.0 / 17)]
    [InlineData("-240", 16.0 / 17)]
    [InlineData("32", 32.0 / 17)]
    [InlineData(null, 1)]
    [InlineData("invalid", 1)]
    [InlineData("0", 1)]
    [InlineData("-2147483648", 1)]
    public void Converts_registry_metrics_relative_to_default(string? value, double expected) =>
        Assert.Equal(expected, WindowsScrollBarPreferences.GetScale(value, "ScrollWidth"), 10);

    [AvaloniaTheory]
    [InlineData(false, false, "-255", "-255")]
    [InlineData(false, true, "-480", "-255")]
    [InlineData(false, false, "-120", "-480")]
    [InlineData(true, false, "-255", "-255")]
    [InlineData(true, true, "-480", "-255")]
    [InlineData(true, false, "-120", "-480")]
    [InlineData(true, false, "-180", "-240")]
    [InlineData(false, false, "-180", "-240")]
    [InlineData(true, false, "invalid", "-480")]
    public void Themes_scale_visible_thumbs_and_hit_areas_independently(
        bool winUi, bool globalStyles, string width, string height)
    {
        Styles theme;
        if (winUi)
        {
            var winUiTheme = new DevolutionsWinUiTheme { GlobalStyles = globalStyles };
            winUiTheme.EndInit();
            theme = winUiTheme;
        }
        else
        {
            var devExpressTheme = new DevolutionsDevExpressTheme { GlobalStyles = globalStyles };
            devExpressTheme.EndInit();
            theme = devExpressTheme;
        }

        WindowsScrollBarPreferences.Apply(theme.Resources, winUi ? 12 : 17, winUi ? 8 : 7,
            name => name == "ScrollWidth" ? width : height, insetThumb: winUi);

        foreach (Orientation orientation in new[] { Orientation.Vertical, Orientation.Horizontal })
        {
            foreach (bool autoHide in new[] { false, true })
            {
                double scale = WindowsScrollBarPreferences.GetScale(
                    orientation == Orientation.Vertical ? width : height, "test");
                var bar = new ScrollBar
                {
                    Orientation = orientation,
                    AllowAutoHide = autoHide,
                    Maximum = 100,
                    ViewportSize = 20,
                    Width = orientation == Orientation.Horizontal ? 200 : double.NaN,
                    Height = orientation == Orientation.Vertical ? 200 : double.NaN,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                var window = new Window { Width = 300, Height = 300, Content = bar, UseLayoutRounding = false };
                window.Styles.Add(theme);
                Assert.True(window.TryFindResource(typeof(ScrollBar), out var barTheme));
                bar.Theme = Assert.IsType<ControlTheme>(barTheme);
                window.Show();
                try
                {
                    window.UpdateLayout();
                    var thumb = Assert.Single(bar.GetVisualDescendants().OfType<Thumb>());
                    var visual = Assert.Single(thumb.GetVisualDescendants().OfType<Border>(),
                        border => border.Name == "ThumbVisual");
                    double area = orientation == Orientation.Vertical ? bar.Bounds.Width : bar.Bounds.Height;
                    double visible = orientation == Orientation.Vertical ? visual.Bounds.Width : visual.Bounds.Height;
                    double hitArea = orientation == Orientation.Vertical ? thumb.Bounds.Width : thumb.Bounds.Height;
                    Assert.Equal((winUi ? 12 : 17) * scale, area, 6);
                    Assert.Equal((winUi ? (autoHide ? 2 : 6) : 7) * scale, visible, 6);
                    Assert.Equal((winUi ? (autoHide ? 8 : 12) : 17) * scale, hitArea, 6);
                    if (!autoHide)
                    {
                        var start = thumb.TranslatePoint(
                            new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
                        var end = start + (orientation == Orientation.Vertical ? new Vector(0, 30) : new Vector(30, 0));
                        window.MouseDown(start, MouseButton.Left);
                        window.MouseMove(end, RawInputModifiers.LeftMouseButton);
                        window.MouseUp(end, MouseButton.Left);
                        Assert.True(bar.Value > 0, "The resized thumb must remain draggable.");
                    }
                }
                finally
                {
                    window.Close();
                    window.Styles.Remove(theme);
                }
            }
        }
    }
}
