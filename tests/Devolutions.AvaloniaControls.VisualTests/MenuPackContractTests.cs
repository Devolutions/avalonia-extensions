using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvaloniaFluentTheme = Avalonia.Themes.Fluent.FluentTheme;
using Avalonia.Threading;
using Devolutions.AvaloniaTheme.DevExpress;
using SampleApp;
using SampleApp.DemoPages;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

/// <summary>
/// Guards the DevExpress menu pack contract.
///
/// The full theme and the externally consumable menu pack both source their menu
/// tokens from <c>Accents/MenuResources.axaml</c>. Historically these were two
/// separate files, which silently drifted apart twice (a 1px item offset from
/// mismatched popup padding, and a 816-vs-456 popup max width). These tests make
/// that class of regression impossible to merge:
///
///   1. Every DevExMenu* token resolves to the SAME value under the full theme
///      and under "host theme + menu pack".
///   2. Menu tokens are immune to host-theme overrides — a host that redefines
///      the generic Fluent keys we used to alias must not move our menus.
/// </summary>
[Collection("VisualTests")]
public class MenuPackContractTests
{
    private const string PackUri = "avares://Devolutions.AvaloniaTheme.DevExpress/Controls/MenuPack.styles.axaml";

    /// <summary>
    /// Every resource key the menu templates depend on. Keys here are owned by the
    /// pack and must be prefixed.
    ///
    /// Typography is owned, not inherited: menus in a branded section must match the
    /// platform-themed menus elsewhere in the app, and pinning it also keeps menu
    /// geometry identical across hosts.
    /// </summary>
    private static readonly string[] MenuTokens =
    {
        "DevExMenuFontFamily",
        "DevExMenuFontSize",
        "DevExMenuFontWeight",
        "DevExMenuBackgroundBrush",
        "DevExMenuPopupBorderBrush",
        "DevExMenuItemBackground",
        "DevExMenuItemForeground",
        "DevExMenuItemKeyboardAcceleratorTextForeground",
        "DevExMenuItemForegroundPointerOver",
        "DevExMenuItemKeyboardAcceleratorTextForegroundPointerOver",
        "DevExMenuItemBackgroundPressed",
        "DevExMenuItemForegroundPressed",
        "DevExMenuItemKeyboardAcceleratorTextForegroundPressed",
        "DevExMenuItemBackgroundDisabled",
        "DevExMenuItemForegroundDisabled",
        "DevExMenuItemKeyboardAcceleratorTextForegroundDisabled",
        "DevExMenuItemPointerOverBackground",
        "DevExMenuSeparatorBrush",
        "DevExMenuItemChevronBrush",
        "DevExMenuSvgItemDefaultCss",
        "DevExMenuSvgItemDisabledCss",
        "DevExMenuBarHeight",
        "DevExMenuPopupBorderThickness",
        "DevExMenuPopupPadding",
        "DevExMenuFlyoutPresenterPadding",
        "DevExMenuTopLevelPopupPadding",
        "DevExMenuPopupMargin",
        "DevExMenuPopupCornerRadius",
        "DevExMenuTopLevelPopupCornerRadius",
        "DevExMenuPopupShadow",
        "DevExMenuPopupMaxWidth",
        "DevExMenuPopupMinHeight",
        "DevExMenuHorizontalFlyoutMinWidth",
        "DevExMenuSubMenuPopupHorizontalOffset",
        "DevExMenuSeparatorMargin",
        "DevExMenuSeparatorHeight",
        "DevExMenuSeparatorIconColumnWidth",
        "DevExMenuBarSeparatorMargin",
        "DevExMenuBarSeparatorWidth",
        "DevExMenuNestedItemMinHeight",
        "DevExMenuNestedItemPadding",
        "DevExMenuIconPresenterMargin",
        "DevExMenuInputGestureTextMargin",
        "DevExMenuItemChevronPathData",
        "DevExMenuItemCheckMarkPathData",
    };

    private static Window ShowWithFullTheme(ThemeVariant variant)
    {
        var theme = new DevolutionsDevExpressTheme();
        theme.BeginInit();
        theme.EndInit();

        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();
        return window;
    }

    private static Window ShowWithPackOverHostTheme(ThemeVariant variant, Action<Window>? applyHostOverrides = null)
    {
        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(new AvaloniaFluentTheme());
        applyHostOverrides?.Invoke(window);

        // Resolved exactly the way an external consumer would: by avares URI only,
        // with no reference to the theme object itself.
        var uri = new Uri(PackUri);
        window.Styles.Add(new StyleInclude(uri) { Source = uri });

        window.Show();
        return window;
    }

    private static Dictionary<string, string> Resolve(Window window, ThemeVariant variant)
    {
        var result = new Dictionary<string, string>();
        foreach (string key in MenuTokens)
        {
            Assert.True(window.TryFindResource(key, variant, out object? value),
                $"Menu token '{key}' did not resolve ({variant}). Every token the menu templates " +
                "consume must be defined in Accents/MenuResources.axaml.");
            result[key] = value?.ToString() ?? "<null>";
        }

        return result;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Pack_and_full_theme_resolve_identical_menu_tokens(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        Window fullWindow = ShowWithFullTheme(variant);
        Window packWindow = ShowWithPackOverHostTheme(variant);

        try
        {
            Dictionary<string, string> full = Resolve(fullWindow, variant);
            Dictionary<string, string> pack = Resolve(packWindow, variant);

            var drift = MenuTokens
                .Where(key => full[key] != pack[key])
                .Select(key => $"{key}: fullTheme='{full[key]}' packOverFluent='{pack[key]}'")
                .ToList();

            Assert.True(drift.Count == 0,
                "Menu pack has drifted from the full DevExpress theme:\n  " + string.Join("\n  ", drift));
        }
        finally
        {
            fullWindow.Close();
            packWindow.Close();
        }
    }

    /// <summary>
    /// The menu pack is layered over a host theme we do not control. A host that
    /// defines the generic Fluent/theme keys the menus used to alias must not be
    /// able to move menu visuals. This reproduces the MacOS regression where a
    /// host-defined zero border thickness erased the popup outline.
    /// </summary>
    [AvaloniaFact]
    public void Host_theme_overrides_do_not_leak_into_menu_tokens()
    {
        ThemeVariant variant = ThemeVariant.Light;

        Window baseline = ShowWithPackOverHostTheme(variant);
        Window hostile = ShowWithPackOverHostTheme(variant, window =>
        {
            // Values a foreign host theme might plausibly define.
            window.Resources["MenuFlyoutPresenterBorderThemeThickness"] = new Thickness(0);
            window.Resources["ComboBoxDropdownBorderPadding"] = new Thickness(12);
            window.Resources["MenuFlyoutPresenterThemePadding"] = new Thickness(12);
            window.Resources["FlyoutThemeMaxWidth"] = 1234d;
            window.Resources["MenuFlyoutThemeMinHeight"] = 99d;
            window.Resources["HorizontalMenuFlyoutThemeMinWidth"] = 99d;
            window.Resources["ContextMenuBorderPadding"] = new Thickness(9);
            window.Resources["ContextMenuCornerRadius"] = new CornerRadius(20);
            window.Resources["OverlayCornerRadius"] = new CornerRadius(20);
            window.Resources["MenuFlyoutSeparatorThemeHeight"] = 7d;
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
                "Host theme resources leaked into menu tokens. Tokens in MenuResources.axaml must be " +
                "pinned literals, never StaticResource aliases to host-owned keys:\n  " + string.Join("\n  ", leaks));
        }
        finally
        {
            baseline.Close();
            hostile.Close();
        }
    }

    /// <summary>
    /// A fast pointer move can jump from the parent item to outside the submenu without
    /// ever entering the popup. Avalonia normally queues a delayed close on that exit,
    /// so the DevExpress behavior must intercept the open parent's exit event.
    /// </summary>
    [AvaloniaFact]
    public void Open_submenu_does_not_queue_close_when_pointer_exits_parent()
    {
        var parent = new MenuItem
        {
            Header = "Parent",
            Items = { new MenuItem { Header = "Child" } },
        };
        var sibling = new MenuItem { Header = "Sibling" };
        var presenter = new MenuFlyoutPresenter { Items = { parent, sibling } };
        Window window = ShowWithFullTheme(ThemeVariant.Light);
        TimeSpan originalMenuShowDelay = DefaultMenuInteractionHandler.MenuShowDelay;

        try
        {
            window.Content = presenter;
            Dispatcher.UIThread.RunJobs();

            presenter.SelectedIndex = 0;
            parent.IsSubMenuOpen = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(parent.IsSubMenuOpen);

            DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero;
            var exit = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, parent);
            parent.RaiseEvent(exit);
            Dispatcher.UIThread.RunJobs();

            Assert.True(exit.Handled);
            Assert.True(parent.IsSubMenuOpen);

            sibling.RaiseEvent(new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, sibling));
            Dispatcher.UIThread.RunJobs();

            Assert.False(parent.IsSubMenuOpen);
        }
        finally
        {
            DefaultMenuInteractionHandler.MenuShowDelay = originalMenuShowDelay;
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// The pack is consumed page-scoped in the SampleApp (a StyleInclude inside
    /// UserControl.Styles) while a full theme is loaded application-wide. Merging a
    /// resource dictionary into a scope that already contains the same key throws at
    /// construction time ("An item with the same key has already been added"), which
    /// is exactly how this pack broke repeatedly while the token contract was being
    /// built. Layering the pack over each supported host theme reproduces that path.
    /// </summary>
    [AvaloniaTheory]
    [MemberData(nameof(PackPageCases))]
    public void MenuPack_demo_pages_layer_over_host_theme_without_collisions(Type pageType, string hostThemeName)
    {
        // Themes must be applied application-wide before any UI is created, exactly
        // as VisualRegressionTests does.
        App.CurrentTheme = null;
        App.SetTheme(hostThemeName switch
        {
            "MacClassic" => new MacOsClassicTheme(),
            "LiquidGlass" => new MacOsLiquidGlassTheme(),
            "Linux" => new LinuxYaruTheme(),
            "DevExpress" => new DevExpressTheme(),
            _ => throw new ArgumentOutOfRangeException(nameof(hostThemeName), hostThemeName, "Unknown theme"),
        });

        var content = (Control)Activator.CreateInstance(pageType)!;
        var window = new Window { Width = 1200, Height = 920, Content = content };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            }

            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// The pack must not restyle controls OUTSIDE menus.
    ///
    /// Regression guard: the pack used to merge Separator.axaml, which is keyed
    /// `{x:Type Separator}` — an implicit ControlTheme. Merging it silently made
    /// DevExpress's separator the default for the whole consuming app, and because
    /// its values (SeparatorBrush / SeparatorMargin) ship only with the full theme,
    /// a plain separator over a Fluent host rendered opaque black and full-bleed
    /// instead of the host's subtle inset line.
    ///
    /// This is a differential test: it needs no baseline image, only the assertion
    /// that adding the pack changes nothing outside menus.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Pack_does_not_restyle_controls_outside_menus(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        static string DescribePlainSeparator(Window window)
        {
            var separator = new Separator();
            var panel = new StackPanel { Width = 200 };
            panel.Children.Add(new TextBlock { Text = "above" });
            panel.Children.Add(separator);
            panel.Children.Add(new TextBlock { Text = "below" });
            window.Content = panel;
            window.Show();
            Dispatcher.UIThread.RunJobs();

            string description =
                $"background={separator.Background?.ToString() ?? "<null>"} " +
                $"margin={separator.Margin} height={separator.Height} bounds={separator.Bounds}";

            window.Close();
            return description;
        }

        var hostOnly = new Window { RequestedThemeVariant = variant, Width = 400, Height = 300 };
        hostOnly.Styles.Add(new AvaloniaFluentTheme());
        string withoutPack = DescribePlainSeparator(hostOnly);

        var hostPlusPack = new Window { RequestedThemeVariant = variant, Width = 400, Height = 300 };
        hostPlusPack.Styles.Add(new AvaloniaFluentTheme());
        var uri = new Uri(PackUri);
        hostPlusPack.Styles.Add(new StyleInclude(uri) { Source = uri });
        string withPack = DescribePlainSeparator(hostPlusPack);

        Assert.True(withoutPack == withPack,
            "The menu pack changed a Separator that is not inside any menu. The pack must only " +
            "style menu content.\n" +
            $"  host only        : {withoutPack}\n" +
            $"  host + menu pack : {withPack}");
    }

    /// <summary>
    /// MenuPack demo pages should stay navigable under SimpleTheme and show
    /// prerequisite guidance instead of crashing.
    /// </summary>
    [AvaloniaTheory]
    [MemberData(nameof(SimplePackPageCases))]
    public void MenuPack_demo_pages_do_not_crash_on_simple_theme(Type pageType)
    {
        App.CurrentTheme = null;
        App.SetTheme(new SimpleTheme());

        var content = (Control)Activator.CreateInstance(pageType)!;
        var window = new Window { Width = 1200, Height = 920, Content = content };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var note = content.FindControl<Control>("SimpleThemePrerequisiteNote");
            var supportedContent = content.FindControl<Control>("SupportedDemoRoot");

            Assert.NotNull(note);
            Assert.NotNull(supportedContent);
            Assert.True(note.IsVisible, $"{pageType.Name}: Simple prerequisite note should be visible.");
            Assert.False(supportedContent.IsVisible, $"{pageType.Name}: MenuPack demo content should be hidden on Simple.");
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    public static IEnumerable<object[]> PackPageCases()
    {
        Type[] pages =
        {
            typeof(MenuPackAbout),
            typeof(MenuPackMenuDemo),
            typeof(MenuPackContextMenuDemo),
            typeof(MenuPackMenuFlyoutDemo),
        };

        // DevExpress is the highest collision risk (pack merged alongside the full
        // theme that already defines every DevExMenu* key). The others are the
        // "foreign host" cases that previously leaked into menu visuals.
        string[] hostThemes = { "DevExpress", "MacClassic", "LiquidGlass", "Linux" };

        foreach (Type page in pages)
        {
            foreach (string host in hostThemes)
            {
                yield return [page, host];
            }
        }
    }

    public static IEnumerable<object[]> SimplePackPageCases()
    {
        yield return [typeof(MenuPackAbout)];
        yield return [typeof(MenuPackMenuDemo)];
        yield return [typeof(MenuPackContextMenuDemo)];
        yield return [typeof(MenuPackMenuFlyoutDemo)];
    }
}
