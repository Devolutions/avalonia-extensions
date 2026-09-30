using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Xunit;
using Path = Avalonia.Controls.Shapes.Path;

namespace Devolutions.AvaloniaControls.Tests;

public class WinUiCheckBoxGlyphTests
{
    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Checked_glyph_remains_unfilled_when_pressed_or_disabled(bool pressed, bool disabled)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = true };
        theme.BeginInit();
        theme.EndInit();

        var checkBox = new TestCheckBox { IsChecked = true, IsEnabled = !disabled };
        var window = new Window { Content = checkBox };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(CheckBox), out var checkBoxTheme));
        checkBox.Theme = Assert.IsType<ControlTheme>(checkBoxTheme);
        window.Show();

        try
        {
            checkBox.SetPressed(pressed);
            Path glyph = Assert.Single(checkBox.GetVisualDescendants().OfType<Path>());
            Assert.Null(glyph.Fill);
            Assert.NotNull(glyph.Stroke);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Indeterminate_glyph_keeps_its_fill_when_pressed_or_disabled(bool pressed, bool disabled)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = true };
        theme.BeginInit();
        theme.EndInit();

        var checkBox = new TestCheckBox { IsThreeState = true, IsChecked = null, IsEnabled = !disabled };
        var window = new Window { Content = checkBox };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(CheckBox), out var checkBoxTheme));
        checkBox.Theme = Assert.IsType<ControlTheme>(checkBoxTheme);
        window.Show();

        try
        {
            checkBox.SetPressed(pressed);
            Path glyph = Assert.Single(checkBox.GetVisualDescendants().OfType<Path>());
            Assert.IsAssignableFrom<IBrush>(glyph.Fill);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class TestCheckBox : CheckBox
    {
        public void SetPressed(bool pressed) => PseudoClasses.Set(":pressed", pressed);
    }
}
