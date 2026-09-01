namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using Xunit;

/// <summary>
///   Guards the visible gap between a menu's background edge and the first row inside it.
/// </summary>
/// <remarks>
///   The gap is composed of two tokens, and the inner border is a top/left-only bevel
///   ("1 1 0 0"), so the padding has to give that pixel back on those two sides for the gap to
///   come out even. That makes the padding values look wrong in isolation and invites someone to
///   "tidy" them into a symmetric pair, which silently puts the top and left 1pt out. These tests
///   pin the composed result instead of the raw values, so the intent survives that edit.
/// </remarks>
[Collection("VisualTests")]
public class MacOsPopupInsetTests
{
    private const double ExpectedInset = 4;

    private static Window ShowLiquidGlass(ThemeVariant variant)
    {
        MacOSVersionDetector.SetTestOverride(true);
        var theme = new DevolutionsMacOsTheme();
        theme.BeginInit();
        theme.EndInit();
        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Thickness Get(Window w, string key, ThemeVariant variant)
    {
        Assert.True(w.TryFindResource(key, variant, out object? value), $"'{key}' should resolve.");
        return Assert.IsType<Thickness>(value);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Menu_inset_is_even_on_all_sides(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowLiquidGlass(variant);
        try
        {
            // Menus split the inset across two tokens: the padding supplies the horizontal gap and
            // the items-presenter margin the vertical one.
            Thickness bevel = Get(w, "MacOsMenuPopupInnerBorderThickness", variant);
            Thickness padding = Get(w, "MacOsMenuPopupPadding", variant);
            Thickness scroller = Get(w, "MacOsMenuFlyoutScrollerMargin", variant);

            Assert.Equal(ExpectedInset, bevel.Left + padding.Left);
            Assert.Equal(ExpectedInset, padding.Right);
            Assert.Equal(ExpectedInset, bevel.Top + scroller.Top);
            Assert.Equal(ExpectedInset, scroller.Bottom);
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }
}
