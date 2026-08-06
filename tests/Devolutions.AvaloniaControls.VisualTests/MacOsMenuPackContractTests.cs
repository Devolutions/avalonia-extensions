using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvaloniaFluentTheme = Avalonia.Themes.Fluent.FluentTheme;
using Avalonia.Threading;
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

    private static readonly (string MenuToken, string LegacyToken)[] FullThemeParityMappings =
    {
        ("MacOsMenuPopupBackgroundBrush", "PopupBackgroundBrush"),
        ("MacOsMenuPopupInnerBorderHighlightBrush", "PopupInnerBorderHighlightBrush"),
        ("MacOsMenuPopupBorderBrush", "PopupBorderBrush"),
        ("MacOsMenuItemForegroundBrush", "MenuItemForegroundBrush"),
        ("MacOsMenuForegroundHighBrush", "ForegroundHighBrush"),
        ("MacOsMenuForegroundMidLowBrush", "ForegroundMidLowBrush"),
        ("MacOsMenuForegroundLowBrush", "ForegroundLowBrush"),
        ("MacOsMenuAccentForegroundBrush", "ControlForegroundAccentHighBrush"),
        ("MacOsMenuSelectedBackgroundBrush", "LayoutBackgroundMidBrush"),
        ("MacOsMenuPressedBackgroundBrush", "LayoutBackgroundHighBrush"),
        ("MacOsMenuItemPointerOverBackgroundBrush", "MenuItemPointerOverBackgroundBrush"),
        ("MacOsMenuSeparatorBrush", "SeparatorBrush"),
        ("MacOsMenuPopupBorderThickness", "MenuFlyoutPresenterBorderThickness"),
        ("MacOsMenuFontSize", "ControlFontSize"),
        ("MacOsMenuHeaderFontSizeSmall", "MenuHeaderFontSizeSmall"),
        ("MacOsMenuChevronSize", "TreeViewItemChevronSize"),
        ("MacOsMenuSelectionCornerRadius", "SelectionCornerRadius"),
        ("MacOsMenuPopupMargin", "PopupMargin"),
        ("MacOsMenuPopupInnerBorderThickness", "PopupInnerBorderThickness"),
        ("MacOsMenuPopupCornerRadius", "PopupCornerRadius"),
        ("MacOsMenuPopupShadow", "PopupShadow"),
        ("MacOsMenuPopupMaxWidth", "FlyoutThemeMaxWidth"),
        ("MacOsMenuPopupMinHeight", "MenuFlyoutThemeMinHeight"),
        ("MacOsMenuPopupPadding", "MenuFlyoutPadding"),
        ("MacOsMenuFlyoutPresenterPadding", "MenuFlyoutPresenterThemePadding"),
        ("MacOsMenuFlyoutScrollerMargin", "MenuFlyoutScrollerMargin"),
        ("MacOsMenuHorizontalFlyoutMinWidth", "HorizontalMenuFlyoutThemeMinWidth"),
        ("MacOsMenuSubMenuPopupHorizontalOffset", "SubMenuPopupHorizontalOffset"),
        ("MacOsMenuSubMenuPopupVerticalOffset", "SubMenuPopupVerticalOffset"),
        ("MacOsMenuPopupHorizontalOffset", "MenuPopupHorizontalOffset"),
        ("MacOsMenuPopupVerticalOffset", "MenuPopupVerticalOffset"),
        ("MacOsMenuToolBarPopupVerticalOffset", "MenuToolBarPopupVerticalOffset"),
        ("MacOsMenuBarPadding", "MenuBarPadding"),
        ("MacOsMenuItemPadding", "MenuItemPadding"),
        ("MacOsMenuItemMinHeight", "MenuItemMinHeight"),
        ("MacOsMenuIconPresenterMargin", "MenuIconPresenterMargin"),
        ("MacOsMenuInputGestureTextMargin", "MenuInputGestureTextMargin"),
        ("MacOsMenuItemIconPadding", "MenuItemIconPadding"),
        ("MacOsMenuToolBarItemPadding", "MenuToolBarItemPadding"),
        ("MacOsMenuToolBarItemIconPadding", "MenuToolBarItemIconPadding"),
        ("MacOsMenuToolBarItemActiveBackgroundMargin", "MenuToolBarItemActiveBackgroundMargin"),
        ("MacOsMenuItemActiveBackgroundMargin", "MenuItemActiveBackgroundMargin"),
        ("MacOsMenuSeparatorHeight", "MenuFlyoutSeparatorThemeHeight"),
        ("MacOsMenuSeparatorPadding", "MenuFlyoutSeparatorThemePadding"),
        ("MacOsMenuChevronPath", "ChevronPath"),
        ("MacOsMenuCheckMarkPath", "CheckMarkPath"),
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
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Full_theme_macos_menu_tokens_match_legacy_tokens(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        Window fullWindow = ShowWithFullTheme(variant);

        try
        {
            var mismatches = new List<string>();

            foreach ((string menuToken, string legacyToken) in FullThemeParityMappings)
            {
                Assert.True(fullWindow.TryFindResource(menuToken, variant, out object? menuValue),
                    $"MacOS menu token '{menuToken}' did not resolve ({variant}).");
                Assert.True(fullWindow.TryFindResource(legacyToken, variant, out object? legacyValue),
                    $"Legacy token '{legacyToken}' did not resolve ({variant}).");

                if (!Equals(menuValue?.ToString(), legacyValue?.ToString()))
                {
                    mismatches.Add($"{menuToken} != {legacyToken} | menu='{menuValue}' legacy='{legacyValue}'");
                }
            }

            Assert.True(mismatches.Count == 0,
                "MacOS menu tokens diverged from legacy full-theme tokens:\n  " + string.Join("\n  ", mismatches));
        }
        finally
        {
            fullWindow.Close();
        }
    }
}
