using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Xunit;
using Path = Avalonia.Controls.Shapes.Path;

namespace Devolutions.AvaloniaControls.Tests;

[Collection("StylesTest")]
public class WinUiExpanderTests
{
    [AvaloniaTheory]
    [InlineData(ExpandDirection.Down)]
    [InlineData(ExpandDirection.Up)]
    [InlineData(ExpandDirection.Left)]
    [InlineData(ExpandDirection.Right)]
    public void Headers_stretch_and_chevrons_keep_their_small_size(ExpandDirection direction)
    {
        var first = new Expander { Header = "Short", ExpandDirection = direction };
        var second = new Expander { Header = "A much longer header", ExpandDirection = direction };
        var panel = new StackPanel { Children = { first, second } };
        var window = CreateWindow(panel, false, ThemeVariant.Light);
        try
        {
            Assert.Equal(HorizontalAlignment.Stretch, first.HorizontalAlignment);
            Assert.Equal(first.Bounds.Width, second.Bounds.Width);
            Assert.Equal(panel.Bounds.Width, first.Bounds.Width);

            foreach (var expander in new[] { first, second })
            {
                var header = Header(expander);
                var glyph = Assert.Single(header.GetVisualDescendants().OfType<Path>(),
                    path => path.Name == "ExpandCollapseChevron");
                bool vertical = direction is ExpandDirection.Down or ExpandDirection.Up;
                Assert.Equal(vertical ? 10 : 5, glyph.Width);
                Assert.Equal(vertical ? 5 : 10, glyph.Height);
                Assert.Equal(vertical ? 10 : 5, glyph.Data!.Bounds.Width);
                Assert.Equal(vertical ? 5 : 10, glyph.Data.Bounds.Height);
                Assert.Equal(32, Assert.Single(header.GetVisualDescendants().OfType<Border>(),
                    border => border.Name == "ExpandCollapseChevronBorder").Width);
                if (vertical)
                    Assert.Equal(expander.Bounds.Width, header.Bounds.Width);
            }

            first.HorizontalAlignment = HorizontalAlignment.Left;
            window.UpdateLayout();
            Assert.True(first.Bounds.Width < second.Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(ExpandDirection.Down, false, false, "Light")]
    [InlineData(ExpandDirection.Up, false, false, "Dark")]
    [InlineData(ExpandDirection.Down, true, true, "Dark")]
    [InlineData(ExpandDirection.Up, true, true, "Light")]
    public async Task Content_slides_in_the_expand_direction_and_replays_on_reopen(
        ExpandDirection direction, bool borderless, bool mica, string variant)
    {
        var expander = new Expander
        {
            Header = "Animated",
            ExpandDirection = direction,
            Content = new Border { Height = 120 }
        };
        if (borderless)
            expander.Classes.Add("borderless");

        var window = CreateWindow(expander, mica, variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark);
        try
        {
            var clip = ContentBorder(expander, "ExpanderContentClip");
            var content = ContentBorder(expander, "ExpanderContent");
            var translation = Assert.IsType<TranslateTransform>(content.RenderTransform);
            Assert.True(clip.ClipToBounds);
            Assert.False(clip.IsVisible);

            for (int iteration = 0; iteration < 2; iteration++)
            {
                Header(expander).IsChecked = true;
                window.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                Assert.True(expander.IsExpanded);
                Assert.True(clip.IsVisible);
                Assert.True(content.Bounds.Height >= 120);
                Assert.True(direction == ExpandDirection.Down ? translation.Y < 0 : translation.Y > 0,
                    $"Expected a {direction} entrance, got Y={translation.Y}.");
                double start = Math.Abs(translation.Y);
                Assert.InRange(start, content.Bounds.Height * 0.5, content.Bounds.Height);

                await Task.Delay(400);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(0, translation.Y, 3);

                Header(expander).IsChecked = false;
                Assert.False(expander.IsExpanded);
                Assert.False(clip.IsVisible);
                Assert.Equal(0, translation.Y);
            }

            expander.IsExpanded = true;
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            expander.IsExpanded = false;
            expander.IsExpanded = true;
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.True(direction == ExpandDirection.Down ? translation.Y < 0 : translation.Y > 0);
            await Task.Delay(400);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, translation.Y, 3);

            if (borderless)
            {
                Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(content.Background).Color);
                Assert.Equal(direction == ExpandDirection.Down
                    ? new Avalonia.Thickness(0, 0, 0, 1)
                    : new Avalonia.Thickness(0, 1, 0, 0), content.BorderThickness);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(ExpandDirection.Down)]
    [InlineData(ExpandDirection.Up)]
    public async Task Initially_expanded_content_uses_its_measured_height(ExpandDirection direction)
    {
        var expander = new Expander
        {
            Header = "Initially open",
            ExpandDirection = direction,
            IsExpanded = true,
            Content = new Border { Height = 80 }
        };
        var window = CreateWindow(expander, false, ThemeVariant.Light);
        try
        {
            var content = ContentBorder(expander, "ExpanderContent");
            var translation = Assert.IsType<TranslateTransform>(content.RenderTransform);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.True(direction == ExpandDirection.Down ? translation.Y < 0 : translation.Y > 0);
            Assert.InRange(Math.Abs(translation.Y), 0.001, content.Bounds.Height);
            await Task.Delay(400);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, translation.Y, 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(ExpandDirection.Down, "Light")]
    [InlineData(ExpandDirection.Up, "Dark")]
    [InlineData(ExpandDirection.Left, "Light")]
    [InlineData(ExpandDirection.Right, "Dark")]
    public void Borderless_headers_remain_transparent_in_every_state(ExpandDirection direction, string variant)
    {
        var expander = new Expander { Header = "Borderless", ExpandDirection = direction };
        expander.Classes.Add("borderless");
        var window = CreateWindow(expander, false, variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark);
        try
        {
            var header = Header(expander);
            var background = Assert.Single(header.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "ToggleButtonBackground");
            var states = (IPseudoClasses)header.Classes;

            foreach (bool expanded in new[] { false, true })
            {
                expander.IsExpanded = expanded;
                foreach (string state in new[] { ":pointerover", ":pressed", ":disabled" })
                {
                    states.Set(state, true);
                    Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(background.Background).Color);
                    Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(background.BorderBrush).Color);
                    states.Set(state, false);
                }
            }

            expander.Classes.Remove("borderless");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            header = Header(expander);
            states = (IPseudoClasses)header.Classes;
            background = Assert.Single(header.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "ToggleButtonBackground");
            states.Set(":pointerover", true);
            Assert.NotEqual(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(background.Background).Color);
            Assert.NotEqual(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(background.BorderBrush).Color);
        }
        finally
        {
            window.Close();
        }
    }

    private static Window CreateWindow(Control content, bool mica, ThemeVariant variant)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            var window = new Window { Content = content, Width = 600, Height = 400, RequestedThemeVariant = variant };
            window.Styles.Add(theme);
            Assert.True(window.TryFindResource(typeof(Expander), out var expanderTheme));
            foreach (var expander in content.GetLogicalDescendants().Prepend(content).OfType<Expander>())
                expander.Theme = Assert.IsType<ControlTheme>(expanderTheme);
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return window;
        }
        finally
        {
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    private static ToggleButton Header(Expander expander) =>
        Assert.Single(expander.GetVisualDescendants().OfType<ToggleButton>(), header => header.Name == "ExpanderHeader");

    private static Border ContentBorder(Expander expander, string name) =>
        Assert.Single(expander.GetVisualDescendants().OfType<Border>(), border => border.Name == name);
}
