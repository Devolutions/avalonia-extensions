using Avalonia;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaFluentTheme = Avalonia.Themes.Fluent.FluentTheme;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.MacOS;
using Devolutions.AvaloniaTheme.MacOS.Internal;
using SampleApp;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

[Collection("VisualTests")]
public class MacOsMenuPackContractTests
{
    private const string PackUri = "avares://Devolutions.AvaloniaTheme.MacOS/Controls/MenuPack.styles.axaml";

    private static readonly string[] MenuTokens =
    {
        "MacOsMenuPopupBackgroundBrush",
        "MacOsMenuPopupInnerBorderHighlightBrush",
        "MacOsMenuPopupBorderBrush",
        "MacOsMenuItemForegroundBrush",
        "MacOsMenuForegroundHighBrush",
        "MacOsMenuForegroundMidLowBrush",
        "MacOsMenuForegroundLowBrush",
        "MacOsMenuAccentForegroundBrush",
        "MacOsMenuSelectedBackgroundBrush",
        "MacOsMenuPressedBackgroundBrush",
        "MacOsMenuItemPointerOverBackgroundBrush",
        "MacOsMenuSeparatorBrush",
        "MacOsMenuPopupBorderThickness",
        "MacOsMenuFontSize",
        "MacOsMenuHeaderFontSizeSmall",
        "MacOsMenuChevronSize",
        "MacOsMenuSelectionCornerRadius",
        "MacOsMenuPopupMargin",
        "MacOsMenuPopupInnerBorderThickness",
        "MacOsMenuPopupCornerRadius",
        "MacOsMenuPopupShadow",
        "MacOsMenuPopupMaxWidth",
        "MacOsMenuPopupMinHeight",
        "MacOsMenuPopupPadding",
        "MacOsMenuFlyoutPresenterPadding",
        "MacOsMenuFlyoutScrollerMargin",
        "MacOsMenuHorizontalFlyoutMinWidth",
        "MacOsMenuSubMenuPopupHorizontalOffset",
        "MacOsMenuSubMenuPopupVerticalOffset",
        "MacOsMenuPopupHorizontalOffset",
        "MacOsMenuPopupVerticalOffset",
        "MacOsMenuToolBarPopupVerticalOffset",
        "MacOsMenuBarPadding",
        "MacOsMenuItemPadding",
        "MacOsMenuItemMinHeight",
        "MacOsMenuIconPresenterMargin",
        "MacOsMenuInputGestureTextMargin",
        "MacOsMenuItemIconPadding",
        "MacOsMenuToolBarItemPadding",
        "MacOsMenuToolBarItemIconPadding",
        "MacOsMenuToolBarItemActiveBackgroundMargin",
        "MacOsMenuItemActiveBackgroundMargin",
        "MacOsMenuSeparatorHeight",
        "MacOsMenuSeparatorPadding",
        "MacOsMenuChevronPath",
        "MacOsMenuCheckMarkPath",
    };

    private static Window ShowWithFullTheme(ThemeVariant variant)
    {
        App.CurrentTheme = null;
        App.SetTheme(new MacOsClassicTheme());

        var window = new Window { RequestedThemeVariant = variant };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Window ShowWithPackOverHostTheme(ThemeVariant variant, Action<Window>? applyHostOverrides = null)
    {
        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(new AvaloniaFluentTheme());
        applyHostOverrides?.Invoke(window);

        var uri = new Uri(PackUri);
        window.Styles.Add(new StyleInclude(uri) { Source = uri });

        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Dictionary<string, string> Resolve(Window window, ThemeVariant variant)
    {
        var result = new Dictionary<string, string>();
        foreach (string key in MenuTokens)
        {
            Assert.True(window.TryFindResource(key, variant, out object? value),
                $"Menu token '{key}' did not resolve ({variant}).");
            result[key] = value?.ToString() ?? "<null>";
        }

        return result;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Pack_and_full_theme_resolve_all_menu_tokens(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        Window fullWindow = ShowWithFullTheme(variant);
        Window packWindow = ShowWithPackOverHostTheme(variant);

        try
        {
            Resolve(fullWindow, variant);
            Resolve(packWindow, variant);
        }
        finally
        {
            fullWindow.Close();
            packWindow.Close();
        }
    }

    [AvaloniaFact]
    public void MenuPack_uses_liquid_glass_resource_variant_when_supported()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        Devolutions.AvaloniaTheme.MacOS.Controls.MacMenuPackStyles.ApplyTo(window.Styles);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.TryFindResource("MacOsMenuPopupMargin", ThemeVariant.Light, out object? popupMargin),
                "LiquidGlass MenuPack should include the MacOsMenuPopupMargin token.");
            Assert.True(window.TryFindResource("MacOsMenuItemPadding", ThemeVariant.Light, out object? itemPadding),
                "LiquidGlass MenuPack should include the MacOsMenuItemPadding token.");

            Thickness popupThickness = Assert.IsType<Thickness>(popupMargin);
            Thickness itemThickness = Assert.IsType<Thickness>(itemPadding);

            Assert.Equal(12, popupThickness.Left);
            Assert.Equal(4, popupThickness.Top);
            Assert.Equal(12, popupThickness.Right);
            Assert.Equal(29, popupThickness.Bottom);

            Assert.Equal(9, itemThickness.Left);
            Assert.Equal(4, itemThickness.Top);
            Assert.Equal(7, itemThickness.Right);
            Assert.Equal(4, itemThickness.Bottom);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void Pack_aliases_follow_the_active_macos_menu_variant()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new AvaloniaFluentTheme());

        var macOsTheme = new DevolutionsMacOsTheme();
        macOsTheme.BeginInit();
        macOsTheme.EndInit();
        window.Styles.Add(macOsTheme);

        Devolutions.AvaloniaTheme.MacOS.Controls.MacMenuPackStyles.ApplyTo(window.Styles);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.TryFindResource("MacOsMenuPopupMargin", ThemeVariant.Light, out object? menuPopupMargin),
                "MacOs menu popup margin should resolve in the active LiquidGlass pack.");
            Assert.True(window.TryFindResource("PopupMargin", ThemeVariant.Light, out object? genericPopupMargin),
                "Legacy popup margin alias should resolve in the active LiquidGlass pack.");
            Assert.True(window.TryFindResource("MacOsMenuItemPadding", ThemeVariant.Light, out object? menuItemPadding),
                "MacOs menu item padding should resolve in the active LiquidGlass pack.");
            Assert.True(window.TryFindResource("MenuItemPadding", ThemeVariant.Light, out object? genericItemPadding),
                "Legacy menu item padding alias should resolve in the active LiquidGlass pack.");

            Assert.Equal(menuPopupMargin, genericPopupMargin);
            Assert.Equal(menuItemPadding, genericItemPadding);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void MacMenuPackPage_uses_active_MacOS_variant_when_added_to_the_sample_app()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var page = new SampleApp.DemoPages.MacMenuPackMenuDemo();
        var window = new Window { Content = page, RequestedThemeVariant = ThemeVariant.Light };

        try
        {
            Assert.True(page.TryFindResource("MacOsMenuPopupMargin", ThemeVariant.Light, out object? popupMargin),
                "Menu pack page should resolve the active MacOS popup margin.");
            Assert.True(page.TryFindResource("MacOsMenuItemPadding", ThemeVariant.Light, out object? itemPadding),
                "Menu pack page should resolve the active MacOS item padding.");

            Thickness popupThickness = Assert.IsType<Thickness>(popupMargin);
            Thickness itemThickness = Assert.IsType<Thickness>(itemPadding);

            Assert.Equal(12, popupThickness.Left);
            Assert.Equal(4, popupThickness.Top);
            Assert.Equal(12, popupThickness.Right);
            Assert.Equal(29, popupThickness.Bottom);

            Assert.Equal(9, itemThickness.Left);
            Assert.Equal(4, itemThickness.Top);
            Assert.Equal(7, itemThickness.Right);
            Assert.Equal(4, itemThickness.Bottom);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void Host_theme_overrides_do_not_leak_into_menu_tokens()
    {
        ThemeVariant variant = ThemeVariant.Light;

        Window baseline = ShowWithPackOverHostTheme(variant);
        Window hostile = ShowWithPackOverHostTheme(variant, window =>
        {
            window.Resources["MenuFlyoutPresenterBorderThemeThickness"] = new Thickness(0);
            window.Resources["MenuFlyoutPresenterThemePadding"] = new Thickness(12);
            window.Resources["FlyoutThemeMaxWidth"] = 1234d;
            window.Resources["MenuFlyoutThemeMinHeight"] = 99d;
            window.Resources["HorizontalMenuFlyoutThemeMinWidth"] = 99d;
            window.Resources["MenuFlyoutSeparatorThemeHeight"] = 7d;
            window.Resources["MenuFlyoutSeparatorThemePadding"] = new Thickness(40, 5, 40, 5);
            window.Resources["PopupMargin"] = new Thickness(99);
            window.Resources["PopupBorderBrush"] = "Red";
            window.Resources["PopupCornerRadius"] = new CornerRadius(20);
            window.Resources["PopupInnerBorderThickness"] = new Thickness(9);
            window.Resources["PopupInnerBorderHighlightBrush"] = "Orange";
            window.Resources["MenuBarPadding"] = new Thickness(22);
            window.Resources["MenuItemPadding"] = new Thickness(20);
            window.Resources["MenuItemMinHeight"] = 99d;
            window.Resources["MenuItemPointerOverBackgroundBrush"] = "Yellow";
            window.Resources["SubMenuPopupHorizontalOffset"] = 19d;
            window.Resources["SubMenuPopupVerticalOffset"] = 23d;
            window.Resources["MenuPopupHorizontalOffset"] = 31d;
            window.Resources["MenuPopupVerticalOffset"] = 29d;
            window.Resources["MenuToolBarPopupVerticalOffset"] = 21d;
        });

        try
        {
            Dictionary<string, string> expected = Resolve(baseline, variant);
            Dictionary<string, string> actual = Resolve(hostile, variant);

            var leaks = MenuTokens
                .Where(key => expected[key] != actual[key])
                .Select(key => $"{key}: expected='{expected[key]}' underHostileHost='{actual[key]}'")
                .ToList();

            Assert.True(leaks.Count == 0,
                "Host theme resources leaked into MacOS menu tokens:\n  " + string.Join("\n  ", leaks));
        }
        finally
        {
            baseline.Close();
            hostile.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(true, "#FFF8F9F9", 0.93)]
    [InlineData(false, "#FFE7E7E7", 1.0)]
    public void Full_theme_menu_background_follows_active_macos_variant(
        bool liquidGlass,
        string expectedColor,
        double expectedOpacity)
    {
        MacOSVersionDetector.SetTestOverride(liquidGlass);

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        var macOsTheme = new DevolutionsMacOsTheme();
        macOsTheme.BeginInit();
        macOsTheme.EndInit();
        window.Styles.Add(macOsTheme);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(
                window.TryFindResource("MacOsMenuPopupBackgroundBrush", ThemeVariant.Light, out object? menuBackground),
                "Full MacOS theme should resolve the menu popup background brush.");

            ISolidColorBrush brush = Assert.IsAssignableFrom<ISolidColorBrush>(menuBackground);
            Assert.Equal(expectedColor, brush.Color.ToString(), ignoreCase: true);
            Assert.Equal(expectedOpacity, brush.Opacity, 3);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData(true, "#FFF8F9F9", 0.93)]
    [InlineData(false, "#FFE7E7E7", 1.0)]
    public void Pack_menu_background_follows_active_macos_variant(
        bool liquidGlass,
        string expectedColor,
        double expectedOpacity)
    {
        MacOSVersionDetector.SetTestOverride(liquidGlass);

        var page = new UserControl();
        var window = new Window { Content = page, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new AvaloniaFluentTheme());
        Devolutions.AvaloniaTheme.MacOS.Controls.MacMenuPackStyles.ApplyTo(page.Styles);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(
                page.TryFindResource("MacOsMenuPopupBackgroundBrush", ThemeVariant.Light, out object? menuBackground),
                "Menu pack should resolve the menu popup background brush over a non-MacOS host theme.");

            ISolidColorBrush brush = Assert.IsAssignableFrom<ISolidColorBrush>(menuBackground);
            Assert.Equal(expectedColor, brush.Color.ToString(), ignoreCase: true);
            Assert.Equal(expectedOpacity, brush.Opacity, 3);
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void Switching_to_a_non_macos_theme_restores_menu_pack_platform_detection()
    {
        // Simulates selecting "MacOS - classic" (which pins the process-global override)
        // and then switching to a non-MacOS base theme.
        App.CurrentTheme = null;
        App.SetTheme(new MacOsClassicTheme());
        Assert.False(MacOSVersionDetector.IsLiquidGlassSupported());

        App.SetTheme(new SampleApp.FluentTheme());

        try
        {
            Assert.Equal(
                OperatingSystem.IsMacOS() && OperatingSystem.IsMacOSVersionAtLeast(26),
                MacOSVersionDetector.IsLiquidGlassSupported());
        }
        finally
        {
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void Readme_documented_xaml_element_follows_liquid_glass()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new AvaloniaFluentTheme());
        window.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            Assert.True(window.TryFindResource("MacOsMenuPopupMargin", ThemeVariant.Light, out object? margin));
            Assert.Equal(new Thickness(12, 4, 12, 29), Assert.IsType<Thickness>(margin));
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("ButtonBackgroundBrush")]
    [InlineData("SeparatorBrush")]
    [InlineData("TextBoxBorderBrush")]
    [InlineData("LayoutBackgroundMidBrush")]
    [InlineData("PopupBackgroundBrush")]
    public void Pack_does_not_publish_non_menu_macos_resources(string nonMenuKey)
    {
        MacOSVersionDetector.SetTestOverride(true);

        var page = new UserControl();
        var window = new Window { Content = page, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new AvaloniaFluentTheme());

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            bool hostFound = page.TryFindResource(nonMenuKey, ThemeVariant.Light, out object? hostValue);

            page.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());
            Dispatcher.UIThread.RunJobs();

            bool packFound = page.TryFindResource(nonMenuKey, ThemeVariant.Light, out object? packValue);

            Assert.Equal(
                hostFound,
                packFound);
            if (hostFound)
            {
                Assert.Same(hostValue, packValue);
            }
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   The pack pins its LiquidGlass values while the full theme derives them from its own
    ///   resources, so this guards the two representations against drifting apart.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(true, "Light")]
    [InlineData(true, "Dark")]
    [InlineData(false, "Light")]
    [InlineData(false, "Dark")]
    public void Pack_menu_tokens_match_the_full_theme(bool liquidGlass, string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        MacOSVersionDetector.SetTestOverride(liquidGlass);

        var fullTheme = new DevolutionsMacOsTheme();
        fullTheme.BeginInit();
        fullTheme.EndInit();
        var fullWindow = new Window { RequestedThemeVariant = variant };
        fullWindow.Styles.Add(fullTheme);

        var packPage = new UserControl();
        var packWindow = new Window { Content = packPage, RequestedThemeVariant = variant };
        packWindow.Styles.Add(new AvaloniaFluentTheme());
        packPage.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());

        try
        {
            fullWindow.Show();
            packWindow.Show();
            Dispatcher.UIThread.RunJobs();

            var mismatches = new List<string>();

            foreach (string token in MenuTokenNames.All)
            {
                fullWindow.TryFindResource(token, variant, out object? fullValue);
                packPage.TryFindResource(token, variant, out object? packValue);

                if (DescribeToken(fullValue) != DescribeToken(packValue))
                {
                    mismatches.Add($"{token}: theme='{DescribeToken(fullValue)}' pack='{DescribeToken(packValue)}'");
                }
            }

            Assert.True(
                mismatches.Count == 0,
                $"Menu pack drifted from the full theme (liquidGlass={liquidGlass}, {variant}):\n  "
                + string.Join("\n  ", mismatches));
        }
        finally
        {
            packWindow.Close();
            fullWindow.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    /// <summary>
    ///   Describes a token by its effective value so that equivalent brush representations
    ///   (for example <c>#d9000000</c> and black at 85% opacity) compare as equal.
    /// </summary>
    private static string DescribeToken(object? value) => value switch
    {
        ISolidColorBrush brush =>
            string.Format(
                CultureInfo.InvariantCulture,
                "rgba({0},{1},{2},{3:F2})",
                brush.Color.R,
                brush.Color.G,
                brush.Color.B,
                brush.Color.A / 255d * brush.Opacity),
        null => "<null>",
        _ => $"{value.GetType().Name}|{value}",
    };


    /// <summary>
    ///   Menu row geometry and typography must be applied at Style level, not only through the
    ///   MenuItem ControlTheme.
    /// </summary>
    /// <remarks>
    ///   Host themes set menu metrics with Styles of their own (for example the DevExpress
    ///   theme's Menu.styles.axaml), and a Style setter outranks a ControlTheme setter. Relying
    ///   on the ControlTheme alone regressed once already: menus kept the MacOS chrome while the
    ///   host resized every row.
    ///   Asserted structurally because rendering two themes in one headless process is not
    ///   currently reliable.
    /// </remarks>
    [AvaloniaFact]
    public void Pack_applies_menu_row_geometry_at_style_level()
    {
        MacOSVersionDetector.SetTestOverride(true);

        try
        {
            var pack = new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack();

            List<Style> nestedItemStyles = Flatten(pack)
                .Where(style => style.Selector?.ToString() is { } selector
                                && selector.Contains("MenuItem")
                                && selector.Contains(":not(:separator)"))
                .ToList();

            Assert.True(
                nestedItemStyles.Count > 0,
                "The pack must style nested menu items so host-theme Styles cannot resize menu rows.");

            List<string> styledProperties = nestedItemStyles
                .SelectMany(style => style.Setters)
                .OfType<Setter>()
                .Select(setter => setter.Property?.Name ?? string.Empty)
                .ToList();

            Assert.Contains("MinHeight", styledProperties);
            Assert.Contains("Padding", styledProperties);
            Assert.Contains("FontFamily", styledProperties);
            Assert.Contains("FontSize", styledProperties);
            Assert.Contains("LineHeight", styledProperties);
        }
        finally
        {
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    private static IEnumerable<Style> Flatten(IStyle style)
    {
        if (style is StyleInclude include)
        {
            if (include.Loaded is { } loaded)
            {
                foreach (Style descendant in Flatten(loaded))
                {
                    yield return descendant;
                }
            }

            yield break;
        }

        if (style is Style typed)
        {
            yield return typed;
        }

        foreach (IStyle child in style.Children.OfType<IStyle>())
        {
            foreach (Style descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }


    /// <summary>
    ///   Menu separator metrics must survive a host theme that positions separators itself.
    /// </summary>
    /// <remarks>
    ///   The DevExpress theme aligns menu separators to its icon column from a behavior that
    ///   assigns <c>Margin</c> as a local value, which outranks every style. Before it was scoped
    ///   to menus that theme actually owns, it also re-positioned separators in menus styled by
    ///   this pack, making the popup noticeably shorter under a DevExpress host.
    /// </remarks>
    [AvaloniaFact]
    public void Menu_separator_metrics_survive_a_devexpress_host()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var hostTheme = new Devolutions.AvaloniaTheme.DevExpress.DevolutionsDevExpressTheme();
        hostTheme.BeginInit();
        hostTheme.EndInit();

        var page = new UserControl();
        var window = new Window { Content = page, Width = 800, Height = 600, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(hostTheme);
        page.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());

        var separator = new Separator();
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(new MenuItem { Header = "Item", Icon = new TextBlock { Text = "*" } });
        contextMenu.Items.Add(separator);
        contextMenu.Items.Add(new MenuItem { Header = "Other" });

        var target = new Border { Width = 300, Height = 200, ContextMenu = contextMenu };
        page.Content = target;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            contextMenu.Open(target);
            Dispatcher.UIThread.RunJobs();

            Assert.True(
                page.TryFindResource("MacOsMenuSeparatorPadding", ThemeVariant.Light, out object? expectedMargin),
                "The pack must own the menu separator margin.");

            Assert.Equal(expectedMargin, separator.Margin);
        }
        finally
        {
            contextMenu.Close();
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }
}
