using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using SampleApp.DemoPages;
using Xunit;

namespace Devolutions.AvaloniaControls.Tests;

[Collection("StylesTest")]
public class WinUiListBoxTests
{
    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void Items_use_ListView_metrics_and_state_brushes(string variant, bool mica)
    {
        var first = new ListBoxItem { Content = "First" };
        var second = new ListBoxItem { Content = new TextBlock { Text = "Second" } };
        var list = new ListBox { Items = { first, second }, Margin = new Thickness(20) };
        var window = CreateWindow(list, variant, mica);
        try
        {
            Assert.Equal(new Thickness(1), list.BorderThickness);
            AssertBrush(window, list.BorderBrush, "ControlStrongStrokeColorDefaultBrush");
            var borderBrush = Assert.IsType<SolidColorBrush>(list.BorderBrush);
            Assert.Equal(Color.Parse(variant == "Light" ? "#72000000" : "#8BFFFFFF"), borderBrush.Color);
            Assert.Equal(1d, borderBrush.Opacity);
            var outline = Assert.Single(list.GetVisualDescendants().OfType<Border>(),
                b => b.TemplatedParent == list && b.BorderThickness == new Thickness(1));
            Assert.Same(list.BorderBrush, outline.BorderBrush);
            var plate = Part(first, "Backplate");
            var indicator = Part(first, "SelectionIndicator");
            Assert.Equal(40, first.Bounds.Height);
            Assert.Equal(new Thickness(16, 0, 12, 0), first.Padding);
            Assert.Equal(HorizontalAlignment.Stretch, first.HorizontalContentAlignment);
            Assert.Equal(new Thickness(4, 2), plate.Margin);
            Assert.Equal(new CornerRadius(4), plate.CornerRadius);
            Assert.Equal(3, indicator.Width);
            Assert.False(indicator.IsVisible);

            var point = Center(first, window);
            window.MouseMove(point);
            AssertBrush(window, plate.Background, "SubtleFillColorSecondaryBrush");
            window.MouseDown(point, MouseButton.Left);
            first.IsSelected = false;
            AssertBrush(window, plate.Background, "SubtleFillColorTertiaryBrush");
            first.IsSelected = true;
            AssertBrush(window, plate.Background, "SubtleFillColorSecondaryBrush");
            window.MouseUp(point, MouseButton.Left);
            Assert.True(first.IsSelected);
            AssertBrush(window, plate.Background, "SubtleFillColorTertiaryBrush");
            Assert.True(indicator.IsVisible);
            AssertBrush(window, indicator.Background, "AccentFillColorDefaultBrush");
            window.UpdateLayout();
            Assert.Equal(16, indicator.Bounds.Height);

            window.MouseDown(point, MouseButton.Left);
            window.UpdateLayout();
            AssertBrush(window, plate.Background, "SubtleFillColorSecondaryBrush");
            Assert.Equal(10, indicator.Bounds.Height);
            window.MouseMove(new Point(1, 1));
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            window.UpdateLayout();
            AssertBrush(window, plate.Background, "SubtleFillColorSecondaryBrush");
            Assert.Equal(16, indicator.Bounds.Height);

            second.Focus();
            first.Focus(NavigationMethod.Tab);
            Assert.True(Part(first, "FocusOuter").IsVisible);
            Assert.True(Part(first, "FocusInner").IsVisible);
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Assert.True(second.IsSelected);
            Assert.False(indicator.IsVisible);

            second.IsEnabled = false;
            var content = Assert.Single(second.GetVisualDescendants().OfType<ContentPresenter>(),
                p => p.Name == "PART_ContentPresenter");
            AssertBrush(window, content.Foreground, "TextFillColorDisabledBrush");
            AssertBrush(window, Part(second, "Backplate").Background, "SubtleFillColorSecondaryBrush");
            AssertBrush(window, Part(second, "SelectionIndicator").Background, "AccentFillColorDisabledBrush");

            window.RequestedThemeVariant = variant == "Light" ? ThemeVariant.Dark : ThemeVariant.Light;
            AssertBrush(window, outline.BorderBrush, "ControlStrongStrokeColorDefaultBrush");
            AssertBrush(window, content.Foreground, "TextFillColorDisabledBrush");
            AssertBrush(window, Part(second, "SelectionIndicator").Background, "AccentFillColorDisabledBrush");

            first.IsSelected = true;
            first.Height = 80;
            window.UpdateLayout();
            Assert.Equal(40, indicator.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void WrapPanels_form_multiple_columns_and_keep_selection_visuals(bool vertical, bool customTemplate)
    {
        var list = new ListBox
        {
            SelectionMode = SelectionMode.Multiple,
            ItemsSource = Enumerable.Range(1, 12).Select(i => $"Item {i}").ToArray(),
            ItemsPanel = WrapTemplate(vertical),
            Height = 105,
            VerticalAlignment = VerticalAlignment.Top
        };
        if (customTemplate)
            list.ItemTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock
            {
                Text = text, MinWidth = 69, TextAlignment = TextAlignment.Center
            });
        var window = CreateWindow(list, "Light", false);
        try
        {
            var items = list.GetRealizedContainers().Cast<ListBoxItem>().ToArray();
            Assert.Equal(12, items.Length);
            Assert.True(items.Select(i => i.TranslatePoint(default, list)!.Value.X).Distinct().Count() > 1);
            Assert.True(items.Select(i => i.TranslatePoint(default, list)!.Value.Y).Distinct().Count() > 1);
            var viewer = Viewer(list);
            Assert.Equal(vertical ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled, viewer.HorizontalScrollBarVisibility);
            Assert.Equal(vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto, viewer.VerticalScrollBarVisibility);
            if (vertical)
            {
                Assert.True(viewer.Extent.Width > viewer.Viewport.Width);
                Assert.True(viewer.Extent.Height <= viewer.Viewport.Height);
            }

            foreach (var item in items.Take(2))
            {
                var point = Center(item, window);
                window.MouseMove(point);
                AssertBrush(window, Part(item, "Backplate").Background, "SubtleFillColorSecondaryBrush");
                window.MouseDown(point, MouseButton.Left, RawInputModifiers.Control);
                window.MouseUp(point, MouseButton.Left, RawInputModifiers.Control);
                Assert.True(item.IsSelected);
                Assert.True(Part(item, "SelectionIndicator").IsVisible);
                AssertBrush(window, Part(item, "Backplate").Background, "SubtleFillColorTertiaryBrush");
            }
            Assert.Equal(2, list.SelectedItems!.Count);
            window.MouseMove(new Point(399, 199));
            AssertBrush(window, Part(items[0], "Backplate").Background, "SubtleFillColorSecondaryBrush");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Panel_replacement_updates_scroll_defaults_and_explicit_settings_win()
    {
        var list = new ListBox { ItemsSource = Enumerable.Range(1, 20).ToArray() };
        var window = CreateWindow(list, "Light", false);
        try
        {
            Assert.Equal(ScrollBarVisibility.Disabled, Viewer(list).HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Auto, Viewer(list).VerticalScrollBarVisibility);
            Assert.IsType<VirtualizingStackPanel>(list.ItemsPanelRoot);
            list.ItemsPanel = WrapTemplate(true);
            window.UpdateLayout();
            Assert.Equal(ScrollBarVisibility.Auto, Viewer(list).HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, Viewer(list).VerticalScrollBarVisibility);
            list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
            list.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Visible);
            list.ItemsPanel = WrapTemplate(false);
            window.UpdateLayout();
            Assert.Equal(ScrollBarVisibility.Hidden, Viewer(list).HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Visible, Viewer(list).VerticalScrollBarVisibility);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("WinUiNavItemTheme")]
    [InlineData("WinUiSegmentItemTheme")]
    public void Named_item_variants_keep_focus_and_disabled_selection(string key)
    {
        var item = new ListBoxItem { Content = "Selected", IsSelected = true };
        var list = new ListBox { Items = { item }, Margin = new Thickness(20) };
        var window = CreateWindow(list, "Light", false, key);
        try
        {
            list.SelectedIndex = 0;
            window.UpdateLayout();
            Assert.True(item.IsSelected);
            var indicator = Part(item, "SelectionIndicator");
            Assert.True(indicator.IsVisible);
            bool segmented = key == "WinUiSegmentItemTheme";
            Assert.Equal(segmented ? 2 : 16, indicator.Bounds.Height);
            Assert.Equal(segmented ? VerticalAlignment.Bottom : VerticalAlignment.Center, indicator.VerticalAlignment);
            Assert.Equal(segmented ? item.Bounds.Width - 10 : 3, indicator.Bounds.Width);
            list.Focus();
            item.Focus(NavigationMethod.Tab);
            Assert.True(Part(item, "FocusOuter").IsVisible);
            window.MouseMove(Center(item, window));
            window.MouseDown(Center(item, window), MouseButton.Left);
            window.UpdateLayout();
            Assert.Equal(segmented ? 2 : 10, indicator.Bounds.Height);
            window.MouseUp(Center(item, window), MouseButton.Left);
            item.IsEnabled = false;
            AssertBrush(window, indicator.Background, "AccentFillColorDisabledBrush");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SampleApp_vertical_wrap_examples_form_columns()
    {
        var page = new ListBoxDemo();
        var window = CreateWindow(page, "Light", false);
        try
        {
            foreach (var name in new[] { "animals6", "animals7" })
            {
                var list = page.FindControl<ListBox>(name)!;
                var items = list.GetRealizedContainers().Cast<ListBoxItem>().ToArray();
                Assert.True(items.Length > 2);
                Assert.True(items.Select(i => i.TranslatePoint(default, list)!.Value.X).Distinct().Count() > 1,
                    $"{name} must wrap into multiple columns");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static FuncTemplate<Panel?> WrapTemplate(bool vertical) =>
        new(() => new WrapPanel
        {
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal,
            ItemWidth = vertical ? double.NaN : 99
        });

    private static Window CreateWindow(Control content, string variant, bool mica, object? itemThemeKey = null)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        try
        {
            theme.BeginInit();
            theme.EndInit();
        }
        finally
        {
            Windows11MicaDetector.SetTestOverride(null);
        }
        var window = new Window
        {
            Content = content, Width = 400, Height = content is ListBoxDemo ? 800 : 200,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(ListBox), out var listTheme));
        Assert.True(window.TryFindResource(itemThemeKey ?? typeof(ListBoxItem), out var itemTheme));
        foreach (var list in content.GetLogicalDescendants().Prepend(content).OfType<ListBox>())
        {
            list.Theme = Assert.IsType<ControlTheme>(listTheme);
            list.ItemContainerTheme = Assert.IsType<ControlTheme>(itemTheme);
        }
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static Border Part(ListBoxItem item, string name) =>
        Assert.Single(item.GetVisualDescendants().OfType<Border>(), b => b.Name == name);

    private static ScrollViewer Viewer(ListBox list) =>
        Assert.Single(list.GetVisualDescendants().OfType<ScrollViewer>(), v => v.Name == "PART_ScrollViewer");

    private static Point Center(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void AssertBrush(Window window, object? actual, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var expected), $"Missing {key}");
        Assert.Same(expected, actual);
    }
}
