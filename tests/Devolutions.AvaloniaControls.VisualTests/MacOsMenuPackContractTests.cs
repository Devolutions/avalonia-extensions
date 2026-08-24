using Avalonia;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
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
        "MacOsMenuScrollBarButtonArrowForeground",
        "MacOsMenuScrollBarButtonArrowForegroundPointerOver",
        "MacOsMenuScrollBarButtonArrowIconFontSize",
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
    public void MacOsMenuScrollViewer_uses_pack_owned_arrow_resources_even_when_host_defines_generic_values()
    {
        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        window.Resources["ScrollBarButtonArrowForeground"] = Brushes.Red;
        window.Resources["ScrollBarButtonArrowForegroundPointerOver"] = Brushes.Blue;
        window.Resources["ScrollBarButtonArrowIconFontSize"] = 99.0;
        window.Styles.Add(new AvaloniaFluentTheme());
        Devolutions.AvaloniaTheme.MacOS.Controls.MacMenuPackStyles.ApplyTo(window.Styles);

        Assert.True(window.TryFindResource("MacOsMenuScrollBarButtonArrowForeground", ThemeVariant.Light, out object? packForeground));
        Assert.True(window.TryFindResource("MacOsMenuScrollBarButtonArrowForegroundPointerOver", ThemeVariant.Light, out object? packPointerOver));
        Assert.True(window.TryFindResource("MacOsMenuScrollBarButtonArrowIconFontSize", ThemeVariant.Light, out object? packSize));

        ISolidColorBrush hostForeground = Assert.IsAssignableFrom<ISolidColorBrush>(window.Resources["ScrollBarButtonArrowForeground"]);
        ISolidColorBrush packBrush = Assert.IsAssignableFrom<ISolidColorBrush>(packForeground);
        Assert.Equal(Colors.Red, hostForeground.Color);
        Assert.NotEqual(hostForeground.Color, packBrush.Color);

        ISolidColorBrush hostPointerOver = Assert.IsAssignableFrom<ISolidColorBrush>(window.Resources["ScrollBarButtonArrowForegroundPointerOver"]);
        ISolidColorBrush packPointerOverBrush = Assert.IsAssignableFrom<ISolidColorBrush>(packPointerOver);
        Assert.Equal(Colors.Blue, hostPointerOver.Color);
        Assert.NotEqual(hostPointerOver.Color, packPointerOverBrush.Color);

        Assert.Equal(12d, Assert.IsType<double>(packSize));

        string xaml = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../src/Devolutions.AvaloniaTheme.MacOS/Accents/MenuResources.axaml"));
        Assert.Contains("MacOsMenuScrollBarButtonArrowForeground", xaml);
        Assert.Contains("MacOsMenuScrollBarButtonArrowForegroundPointerOver", xaml);
        Assert.Contains("MacOsMenuScrollBarButtonArrowIconFontSize", xaml);
        Assert.DoesNotContain("ScrollBarButtonArrowForeground", xaml.Replace("MacOsMenuScrollBarButtonArrowForeground", ""));
        Assert.DoesNotContain("ScrollBarButtonArrowForegroundPointerOver", xaml.Replace("MacOsMenuScrollBarButtonArrowForegroundPointerOver", ""));
        Assert.DoesNotContain("ScrollBarButtonArrowIconFontSize", xaml.Replace("MacOsMenuScrollBarButtonArrowIconFontSize", ""));
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
    ///   Menu row geometry and typography must be pinned at Style level for the host-invariant
    ///   properties, while the class-dependent top-level sizing remains in the ControlTheme.
    /// </summary>
    /// <remarks>
    ///   Host themes set menu metrics with Styles of their own, and a Style setter outranks a
    ///   ControlTheme setter. Relying on the ControlTheme alone regressed once already: menus kept
    ///   the MacOS chrome while the host resized every row. For MacOS top-level toolbars, however,
    ///   FontSize and Padding are intentionally class-driven in the ControlTheme; a blanket fixed
    ///   override would break the MacOS_Theme_MenuLabelBelowIcon variant.
    /// </remarks>
    [AvaloniaFact]
    public void Pack_applies_host_invariant_menu_metrics_at_style_level()
    {
        MacOSVersionDetector.SetTestOverride(true);

        try
        {
            var pack = new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack();

            List<Style> topLevelStyles = Flatten(pack)
                .Where(style => style.Selector?.ToString() is { } selector
                                && selector.Contains("Menu > MenuItem:not(:separator)"))
                .ToList();

            Assert.True(
                topLevelStyles.Count > 0,
                "The pack must seal the menu-bar item metrics that are host-invariant.");

            List<string> styledProperties = topLevelStyles
                .SelectMany(style => style.Setters)
                .OfType<Setter>()
                .Select(setter => setter.Property?.Name ?? string.Empty)
                .ToList();

            Assert.Contains("MinHeight", styledProperties);
            Assert.Contains("FontFamily", styledProperties);
            Assert.Contains("LineHeight", styledProperties);
            Assert.DoesNotContain("Padding", styledProperties);
            Assert.DoesNotContain("FontSize", styledProperties);
        }
        finally
        {
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void Menu_bar_item_class_switching_still_uses_the_macos_toolbar_values()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var defaultWindow = new Window { RequestedThemeVariant = ThemeVariant.Light };
        defaultWindow.Styles.Add(new AvaloniaFluentTheme());
        defaultWindow.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());

        var toolbarWindow = new Window { RequestedThemeVariant = ThemeVariant.Light };
        toolbarWindow.Styles.Add(new AvaloniaFluentTheme());
        toolbarWindow.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());

        var defaultMenu = new Menu();
        var defaultItem = new MenuItem { Header = "File" };
        defaultMenu.Items.Add(defaultItem);
        defaultWindow.Content = defaultMenu;

        var toolbarMenu = new Menu { Classes = { "MacOS_Theme_MenuLabelBelowIcon" } };
        var toolbarItem = new MenuItem { Header = "File" };
        toolbarMenu.Items.Add(toolbarItem);
        toolbarWindow.Content = toolbarMenu;

        try
        {
            defaultWindow.Show();
            toolbarWindow.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(13d, defaultItem.FontSize);
            Assert.Equal(new Thickness(9, 4, 7, 2), defaultItem.Padding);

            Assert.Equal(10d, toolbarItem.FontSize);
            Assert.Equal(new Thickness(4, 0, 4, 4), toolbarItem.Padding);
        }
        finally
        {
            defaultWindow.Close();
            toolbarWindow.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaFact]
    public void Host_menu_item_styles_do_not_change_the_menu_bar()
    {
        MacOSVersionDetector.SetTestOverride(true);

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new AvaloniaFluentTheme());
        window.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());

        var menu = new Menu();
        var item = new MenuItem { Header = "File" };
        menu.Items.Add(item);
        window.Content = menu;

        window.Styles.Add(new Style(x => x.OfType<MenuItem>())
        {
            Setters =
            {
                new Setter(MenuItem.MinHeightProperty, 77d),
                new Setter(MenuItem.BackgroundProperty, Brushes.Fuchsia),
                new Setter(MenuItem.ForegroundProperty, Brushes.Fuchsia),
                new Setter(TextBlock.LineHeightProperty, 42d),
                new Setter(TextBlock.FontFamilyProperty, new FontFamily("Times New Roman"))
            }
        });

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0d, item.MinHeight);
            Assert.NotEqual(Brushes.Fuchsia, item.Background);
            Assert.NotEqual(Brushes.Fuchsia, item.Foreground);
            Assert.NotEqual(new FontFamily("Times New Roman"), item.FontFamily);
            Assert.True(double.IsNaN((double)item.GetValue(TextBlock.LineHeightProperty)));
        }
        finally
        {
            window.Close();
            MacOSVersionDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light", ".st0 {fill : #000000; }", ".st0 {fill : #FFFFFF; }")]
    [InlineData("Dark", ".st0 {fill : #B8B8B8; }", ".st0 {fill : #DEECF9; }")]
    public void Svg_menu_icons_are_recoloured_identically_by_pack_and_full_theme(
        string variantName,
        string expectedDefaultCss,
        string expectedPointerOverCss)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var previousStyles = Application.Current?.Styles.ToList() ?? [];
        Application.Current?.Styles.Clear();

        var fullWindow = new Window { RequestedThemeVariant = variant };
        var packWindow = new Window { RequestedThemeVariant = variant };

        var fullTheme = new DevolutionsMacOsTheme();
        fullTheme.BeginInit();
        fullTheme.EndInit();
        fullWindow.Styles.Add(fullTheme);

        packWindow.Styles.Add(new AvaloniaFluentTheme());
        packWindow.Styles.Add(new Devolutions.AvaloniaTheme.MacOS.Controls.MacOsMenuPack());

        try
        {
            Assert.True(fullWindow.TryFindResource("MacOsMenuSvgItemDefaultCss", variant, out object? fullDefaultCss),
                "Full MacOS theme should resolve the default SVG CSS resource.");
            Assert.True(packWindow.TryFindResource("MacOsMenuSvgItemDefaultCss", variant, out object? packDefaultCss),
                "Menu pack should resolve the default SVG CSS resource.");
            Assert.Equal(expectedDefaultCss, fullDefaultCss?.ToString());
            Assert.Equal(expectedDefaultCss, packDefaultCss?.ToString());

            Assert.True(fullWindow.TryFindResource("MacOsMenuSvgItemPointerOverCss", variant, out object? fullPointerOverCss),
                "Full MacOS theme should resolve the pointer-over SVG CSS resource.");
            Assert.True(packWindow.TryFindResource("MacOsMenuSvgItemPointerOverCss", variant, out object? packPointerOverCss),
                "Menu pack should resolve the pointer-over SVG CSS resource.");
            Assert.Equal(expectedPointerOverCss, fullPointerOverCss?.ToString());
            Assert.Equal(expectedPointerOverCss, packPointerOverCss?.ToString());
        }
        finally
        {
            fullWindow.Close();
            packWindow.Close();
            Application.Current?.Styles.Clear();
            foreach (var style in previousStyles)
            {
                Application.Current?.Styles.Add(style);
            }
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
