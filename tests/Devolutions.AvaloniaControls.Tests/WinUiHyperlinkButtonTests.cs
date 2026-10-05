using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Xunit;

namespace Devolutions.AvaloniaControls.Tests;

public class WinUiHyperlinkButtonTests
{
    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void Text_remains_undecorated_in_all_states(string variant, bool explicitContent)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();

        var button = new TestHyperlinkButton
        {
            Content = explicitContent ? new TextBlock { Text = "Link" } : "Link"
        };
        var window = new Window
        {
            Content = button,
            RequestedThemeVariant = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(HyperlinkButton), out var controlTheme));
        button.Theme = Assert.IsType<ControlTheme>(controlTheme);
        window.Show();

        try
        {
            foreach (bool visited in new[] { false, true })
            {
                button.SetState(":visited", visited);
                foreach (string state in new[] { "", ":pointerover", ":pressed", ":disabled" })
                {
                    button.SetState(":pointerover", state is ":pointerover" or ":pressed");
                    button.SetState(":pressed", state == ":pressed");
                    button.IsEnabled = state != ":disabled";
                    window.UpdateLayout();

                    var text = Assert.Single(button.GetVisualDescendants().OfType<TextBlock>());
                    Assert.True(text.TextDecorations is null || text.TextDecorations.Count == 0);
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class TestHyperlinkButton : HyperlinkButton
    {
        public void SetState(string state, bool active) => PseudoClasses.Set(state, active);
    }
}
