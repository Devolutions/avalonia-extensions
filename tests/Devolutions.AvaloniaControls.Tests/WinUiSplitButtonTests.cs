using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Xunit;

namespace Devolutions.AvaloniaControls.Tests;

[Collection("StylesTest")]
public class WinUiSplitButtonTests
{
    [AvaloniaTheory]
    [InlineData("Light", false, false)]
    [InlineData("Dark", false, false)]
    [InlineData("Light", true, false)]
    [InlineData("Dark", true, false)]
    [InlineData("Light", false, true)]
    [InlineData("Dark", false, true)]
    [InlineData("Light", true, true)]
    [InlineData("Dark", true, true)]
    public void Halves_have_independent_pointer_states_and_disabled_brushes(string variant, bool mica, bool accent)
    {
        var button = new SplitButton { Content = "Configure", Margin = new Thickness(20), CornerRadius = new CornerRadius(8) };
        if (accent)
            button.Classes.Add("accent");
        var window = CreateWindow(button, mica, variant);
        try
        {
            var primary = Part(button, "PART_PrimaryButton");
            var secondary = Part(button, "PART_SecondaryButton");
            string palette = accent ? "Checked" : "";
            Assert.Equal(29, button.Bounds.Height);
            Assert.Equal(35, secondary.Bounds.Width);
            var glyph = Assert.Single(secondary.GetVisualDescendants().OfType<PathIcon>());
            Assert.Equal(9, glyph.Width);
            Assert.Equal(4.5, glyph.Height);
            Assert.Equal(9, glyph.Data!.Bounds.Width);
            Assert.Equal(4.5, glyph.Data.Bounds.Height);
            Assert.False(primary.Focusable);
            Assert.False(secondary.Focusable);
            Assert.True(button.Focusable);
            Assert.Equal(new CornerRadius(8, 0, 0, 8), primary.CornerRadius);
            Assert.Equal(new CornerRadius(0, 8, 8, 0), secondary.CornerRadius);
            var divider = Assert.Single(button.GetVisualDescendants().OfType<Border>(), b => b.Name == "DividerBackgroundGrid");
            Assert.Equal(1, divider.Bounds.Width);
            Assert.Equal(button.Bounds.Height, divider.Bounds.Height);
            Assert.Equal(Avalonia.Media.BackgroundSizing.OuterBorderEdge, divider.BackgroundSizing);
            Assert.Equal(new Thickness(1, 0, 0, 0), divider.BorderThickness);
            AssertBrush(window, divider.Background, $"SplitButtonBackground{palette}");
            AssertBrush(window, divider.BorderBrush, $"SplitButtonBorderBrush{palette}Divider");
            AssertPalette(window, primary, palette);
            AssertPalette(window, secondary, palette, foreground: accent ? "Checked" : "Secondary");

            button.Classes.Set("accent", !accent);
            AssertPalette(window, primary, accent ? "" : "Checked");
            AssertPalette(window, secondary, accent ? "" : "Checked", foreground: accent ? "Secondary" : "Checked");
            button.Classes.Set("accent", accent);

            foreach (var target in new[] { primary, secondary })
            {
                var other = target == primary ? secondary : primary;
                var point = Center(target, window);
                window.MouseMove(point);
                AssertPalette(window, target, palette + "PointerOver");
                AssertPalette(window, other, palette, foreground: accent ? "Checked" : other == secondary ? "Secondary" : "");
                window.MouseDown(point, MouseButton.Left);
                AssertPalette(window, target, palette + "Pressed",
                    foreground: !accent && target == secondary ? "SecondaryPressed" : palette + "Pressed",
                    border: accent ? "Checked" : "Pressed");
                AssertPalette(window, other, palette, foreground: accent ? "Checked" : other == secondary ? "Secondary" : "");
                window.MouseMove(new Point(1, 1));
                window.MouseUp(new Point(1, 1), MouseButton.Left);
            }

            button.IsEnabled = false;
            AssertBrush(window, divider.Background, $"SplitButtonBackground{palette}Disabled");
            AssertPalette(window, primary, palette + "Disabled");
            AssertPalette(window, secondary, palette + "Disabled");
            button.IsEnabled = true;
            window.RequestedThemeVariant = variant == "Light" ? ThemeVariant.Dark : ThemeVariant.Light;
            AssertPalette(window, primary, palette);
            AssertPalette(window, secondary, palette, foreground: accent ? "Checked" : "Secondary");
        }
        finally
        {
            window.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Keyboard_and_flyout_states_press_both_halves_without_duplicate_commands(bool accent)
    {
        var command = new CountingCommand();
        var button = new SplitButton
        {
            Content = "Configure",
            Command = command,
            CommandParameter = "parameter",
            Flyout = new Flyout { Content = "Options" },
            Margin = new Thickness(20)
        };
        if (accent)
            button.Classes.Add("accent");
        var window = CreateWindow(button, false, "Light");
        try
        {
            var primary = Part(button, "PART_PrimaryButton");
            var secondary = Part(button, "PART_SecondaryButton");
            string palette = accent ? "Checked" : "";
            int clicks = 0;
            button.Click += (_, _) => clicks++;
            button.Focus();
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            AssertPalette(window, primary, palette + "Pressed");
            AssertPalette(window, secondary, palette + "Pressed", foreground: accent ? "CheckedPressed" : "SecondaryPressed");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.Equal(1, command.Executions);
            Assert.Equal(1, clicks);
            Assert.Equal("parameter", command.LastParameter);

            var primaryPoint = Center(primary, window);
            window.MouseDown(primaryPoint, MouseButton.Left);
            window.MouseUp(primaryPoint, MouseButton.Left);
            Assert.Equal(2, command.Executions);
            Assert.Equal(2, clicks);
            window.MouseMove(new Point(1, 1));
            button.Focus();
            window.KeyPress(Key.F4, RawInputModifiers.None, PhysicalKey.F4, null);
            window.KeyRelease(Key.F4, RawInputModifiers.None, PhysicalKey.F4, null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(button.Flyout.IsOpen);
            AssertPalette(window, primary, palette + "Pressed");
            AssertPalette(window, secondary, palette + "Pressed", foreground: accent ? "CheckedPressed" : "SecondaryPressed");
            Assert.Equal(2, command.Executions);
            button.Flyout.Hide();
            AssertPalette(window, primary, palette);

            var secondaryPoint = Center(secondary, window);
            window.MouseDown(secondaryPoint, MouseButton.Left);
            window.MouseUp(secondaryPoint, MouseButton.Left);
            Assert.True(button.Flyout.IsOpen);
            Assert.Equal(2, command.Executions);
            button.Flyout.Hide();
        }
        finally
        {
            button.Flyout.Hide();
            window.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void ToggleSplitButton_uses_checked_palette_and_retains_toggle_behavior(string variant)
    {
        var button = new ToggleSplitButton
        {
            Content = "Toggle",
            Margin = new Thickness(20),
            Flyout = new Flyout { Content = "Options" }
        };
        var window = CreateWindow(button, false, variant);
        try
        {
            var primary = Part(button, "PART_PrimaryButton");
            var secondary = Part(button, "PART_SecondaryButton");
            button.Focus();
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.True(button.IsChecked);
            AssertPalette(window, primary, "Checked");
            AssertPalette(window, secondary, "Checked");
            window.MouseMove(Center(primary, window));
            AssertPalette(window, primary, "CheckedPointerOver");
            AssertPalette(window, secondary, "Checked");
            window.MouseDown(Center(primary, window), MouseButton.Left);
            AssertPalette(window, primary, "CheckedPressed", border: "Checked");
            AssertPalette(window, secondary, "Checked");
            window.MouseMove(new Point(1, 1));
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            button.Focus();
            window.KeyPress(Key.F4, RawInputModifiers.None, PhysicalKey.F4, null);
            window.KeyRelease(Key.F4, RawInputModifiers.None, PhysicalKey.F4, null);
            Assert.True(button.Flyout.IsOpen);
            AssertPalette(window, primary, "CheckedPressed");
            AssertPalette(window, secondary, "CheckedPressed");
            button.Flyout.Hide();
            button.IsEnabled = false;
            AssertPalette(window, primary, "CheckedDisabled");
            AssertPalette(window, secondary, "CheckedDisabled");
        }
        finally
        {
            button.Flyout.Hide();
            window.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    private static Window CreateWindow(SplitButton button, bool mica, string variant)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();
        var window = new Window
        {
            Content = button,
            Width = 400,
            Height = 120,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(button is ToggleSplitButton ? typeof(ToggleSplitButton) : typeof(SplitButton), out var controlTheme));
        button.Theme = Assert.IsType<ControlTheme>(controlTheme);
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static Button Part(SplitButton button, string name) =>
        Assert.Single(button.GetVisualDescendants().OfType<Button>(), b => b.Name == name);

    private static Point Center(Button button, Window window) =>
        button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;

    private static void AssertPalette(Window window, Button button, string state, string? foreground = null, string? border = null)
    {
        var presenter = Assert.Single(button.GetVisualDescendants().OfType<ContentPresenter>(), p => p.Name == "PART_ContentPresenter");
        AssertBrush(window, presenter.Background, "SplitButtonBackground" + state);
        AssertBrush(window, presenter.BorderBrush, "SplitButtonBorderBrush" + (border ?? state));
        AssertBrush(window, presenter.Foreground, "SplitButtonForeground" + (foreground ?? state));
        foreach (var glyph in button.GetVisualDescendants().OfType<PathIcon>())
            Assert.Same(presenter.Foreground, glyph.Foreground);
    }

    private static void AssertBrush(Window window, object? actual, string key)
    {
        Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var expected), $"Missing {key}");
        Assert.Same(expected, actual);
    }

    private sealed class CountingCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public int Executions { get; private set; }
        public object? LastParameter { get; private set; }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            Executions++;
            LastParameter = parameter;
        }
    }
}
