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
    public void Drop_down_inset_is_even_on_all_sides(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowLiquidGlass(variant);
        try
        {
            // ComboBox, EditableComboBox, MultiComboBox and AutoCompleteBox all share this
            // composition: the popup surface supplies the bevel and padding, the row its margin.
            Thickness bevel = Get(w, "PopupInnerBorderThickness", variant);
            Thickness padding = Get(w, "PopupPadding", variant);
            Thickness row = Get(w, "ComboBoxItemMargin", variant);

            Assert.Equal(ExpectedInset, bevel.Left + padding.Left + row.Left);
            Assert.Equal(ExpectedInset, bevel.Top + padding.Top + row.Top);
            Assert.Equal(ExpectedInset, bevel.Right + padding.Right + row.Right);
            Assert.Equal(ExpectedInset, bevel.Bottom + padding.Bottom + row.Bottom);
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   A ComboBox popup is positioned so the selected row lands over the closed control, and that
    ///   offset is a constant that has to track the padding above the first row. Nothing recomputes
    ///   it, so this is the only thing stopping the two drifting apart.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Popup_offset_constant_tracks_the_padding_above_the_first_row(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowLiquidGlass(variant);
        try
        {
            Thickness shadow = Get(w, "ComboBoxShadowMargin", variant);
            Thickness padding = Get(w, "PopupPadding", variant);
            Assert.True(w.TryFindResource("InitialFirstItemDistance", variant, out object? distance));

            Assert.Equal(shadow.Bottom + padding.Top, System.Convert.ToDouble(distance));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   The calendar popup and a plain ListBox compose the inset differently, so they carry their
    ///   own padding keys. If either is pointed back at the shared one, tuning the drop-down inset
    ///   silently distorts them.
    /// </summary>
    [AvaloniaFact]
    public void Calendar_and_list_box_do_not_share_the_drop_down_padding()
    {
        Window w = ShowLiquidGlass(ThemeVariant.Dark);
        try
        {
            Assert.True(w.TryFindResource("CalendarPopupPadding", ThemeVariant.Dark, out object? cal));
            Assert.True(w.TryFindResource("ListBoxPadding", ThemeVariant.Dark, out object? list));
            Assert.NotEqual(Get(w, "PopupPadding", ThemeVariant.Dark), Assert.IsType<Thickness>(cal));
            Assert.NotEqual(Get(w, "PopupPadding", ThemeVariant.Dark), Assert.IsType<Thickness>(list));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   MaxDropDownHeight is applied to the Popup, so it covers the chrome as well as the list.
    ///   PopupTrimHeight is subtracted to get back to the usable list height, and the chrome that
    ///   makes the difference is PopupMargin's vertical total. Nothing recomputes it, so a change to
    ///   the margin silently mis-clamps how far a popup may shift.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Popup_trim_height_tracks_the_popup_margin(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowLiquidGlass(variant);
        try
        {
            Thickness margin = Get(w, "PopupMargin", variant);
            Assert.True(w.TryFindResource("PopupTrimHeight", variant, out object? trim));
            Assert.Equal(margin.Top + margin.Bottom, System.Convert.ToDouble(trim));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   A selected row should look the same whether it is in a menu or a drop-down. The two carry
    ///   separate tokens - menus cannot share ComboBox's, which ComboBox itself needs - so nothing
    ///   but this keeps them at the same radius.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Menu_and_drop_down_rows_share_a_corner_radius(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window w = ShowLiquidGlass(variant);
        try
        {
            Assert.True(w.TryFindResource("MenuSelectionCornerRadius", variant, out object? menu));
            Assert.True(w.TryFindResource("ComboBoxItemCornerRadius", variant, out object? row));
            Assert.Equal(Assert.IsType<CornerRadius>(menu), Assert.IsType<CornerRadius>(row));
        }
        finally
        {
            w.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
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
