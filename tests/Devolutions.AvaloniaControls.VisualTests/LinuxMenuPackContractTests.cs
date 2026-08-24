using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using AvaloniaFluentTheme = Avalonia.Themes.Fluent.FluentTheme;
using Devolutions.AvaloniaTheme.Linux;
using Xunit;

namespace Devolutions.AvaloniaControls.VisualTests;

/// <summary>
/// Guards the Linux (Yaru) menu pack contract.
///
/// The full theme and the externally consumable menu pack both source their menu tokens from
/// <c>Accents/MenuResources.axaml</c>, so the two cannot drift apart.
///
/// This matters more for Linux than for the other packs. The Linux menu templates were derived
/// from Fluent and consumed Fluent's own generic keys (MenuFlyoutItemBackground,
/// MenuFlyoutPresenterBackground, CheckMarkPathData, ...). That is invisible under the full
/// theme but breaks the pack: layered over DevExpress or MacOS, every one of those keys
/// resolves to the *host's* value, so "Linux" menus render in the host's colours.
/// </summary>
[Collection("VisualTests")]
public class LinuxMenuPackContractTests
{
    private const string PackUri = "avares://Devolutions.AvaloniaTheme.Linux/Controls/MenuPack.styles.axaml";

    /// <summary>
    /// Every resource key the Linux menu templates depend on. All are owned by the pack and
    /// prefixed, so a host theme cannot silently redefine them.
    /// </summary>
    private static readonly string[] MenuTokens =
    {
        "LinuxMenuPresenterBackground",
        "LinuxMenuPresenterBorderBrush",
        "LinuxMenuSeparatorBrush",
        "LinuxMenuItemBackground",
        "LinuxMenuItemForeground",
        "LinuxMenuItemBackgroundPointerOver",
        "LinuxMenuItemForegroundPointerOver",
        "LinuxMenuItemBackgroundPressed",
        "LinuxMenuItemForegroundPressed",
        "LinuxMenuItemBackgroundDisabled",
        "LinuxMenuItemForegroundDisabled",
        "LinuxMenuItemKeyboardAcceleratorTextForeground",
        "LinuxMenuItemKeyboardAcceleratorTextForegroundPointerOver",
        "LinuxMenuItemKeyboardAcceleratorTextForegroundPressed",
        "LinuxMenuItemKeyboardAcceleratorTextForegroundDisabled",
        "LinuxMenuScrollArrowForeground",
        "LinuxMenuScrollArrowForegroundPointerOver",
        "LinuxMenuItemChevronMargin",
        "LinuxMenuFontFamily",
        "LinuxMenuFontSize",
        "LinuxMenuFontWeight",
        "LinuxMenuBarHeight",
        "LinuxMenuTopLevelItemPadding",
        "LinuxMenuTopLevelIconMargin",
        "LinuxMenuPresenterBorderThickness",
        "LinuxMenuPresenterPadding",
        "LinuxMenuPopupCornerRadius",
        "LinuxMenuPopupMaxWidth",
        "LinuxMenuPopupMinHeight",
        "LinuxMenuHorizontalFlyoutMinWidth",
        "LinuxMenuScrollerMargin",
        "LinuxMenuSubMenuPopupHorizontalOffset",
        "LinuxMenuSeparatorHeight",
        "LinuxMenuSeparatorMargin",
        "LinuxMenuBarSeparatorMargin",
        "LinuxMenuBarSeparatorWidth",
        "LinuxMenuItemMinHeight",
        "LinuxMenuItemPadding",
        "LinuxMenuIconPresenterMargin",
        "LinuxMenuInputGestureTextMargin",
        "LinuxMenuHorizontalItemMargin",
        "LinuxMenuItemIconSize",
        "LinuxMenuItemToggleColumnMinWidth",
        "LinuxMenuItemChevronWidth",
        "LinuxMenuItemChevronHeight",
        "LinuxMenuItemChevronPathData",
        "LinuxMenuItemCheckMarkPathData",
        "LinuxMenuScrollArrowSize",
        "LinuxMenuScrollViewer",
        "LinuxMenuItemIconTheme",
    };

    private static Window ShowWithFullTheme(ThemeVariant variant)
    {
        var theme = new DevolutionsLinuxYaruTheme();
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

        // Resolved exactly the way an external consumer would: by avares URI only, with no
        // reference to the theme object itself.
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
            result[key] = Describe(value);
        }

        return result;
    }

    /// <summary>
    /// Describes a token by its effective value so equivalent brush representations compare
    /// as equal.
    /// </summary>
    private static string Describe(object? value) => value switch
    {
        ISolidColorBrush brush => FormattableString.Invariant(
            $"rgba({brush.Color.R},{brush.Color.G},{brush.Color.B},{brush.Color.A / 255d * brush.Opacity:F3})"),
        null => "<null>",
        _ => value.ToString() ?? "<null>",
    };

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
                "Menu pack has drifted from the full Linux theme:\n  " + string.Join("\n  ", drift));
        }
        finally
        {
            fullWindow.Close();
            packWindow.Close();
        }
    }

    /// <summary>
    /// The pack is layered over a host theme we do not control. A host that defines the generic
    /// Fluent keys the Linux menus used to consume must not be able to move menu visuals.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Host_theme_overrides_do_not_leak_into_menu_tokens(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        Window baseline = ShowWithPackOverHostTheme(variant);
        Window hostile = ShowWithPackOverHostTheme(variant, window =>
        {
            // Exactly the keys the Linux menu templates used to read straight from the host.
            window.Resources["MenuFlyoutPresenterBackground"] = new SolidColorBrush(Colors.Magenta);
            window.Resources["MenuFlyoutPresenterBorderBrush"] = new SolidColorBrush(Colors.Magenta);
            window.Resources["MenuFlyoutPresenterBorderThemeThickness"] = new Thickness(9);
            window.Resources["MenuFlyoutPresenterThemePadding"] = new Thickness(12);
            window.Resources["MenuFlyoutItemBackground"] = new SolidColorBrush(Colors.Magenta);
            window.Resources["MenuFlyoutItemForeground"] = new SolidColorBrush(Colors.Magenta);
            window.Resources["MenuFlyoutItemBackgroundPointerOver"] = new SolidColorBrush(Colors.Magenta);
            window.Resources["MenuFlyoutItemThemePadding"] = new Thickness(21);
            window.Resources["MenuFlyoutItemMinHeight"] = 99d;
            window.Resources["MenuFlyoutItemChevronMargin"] = new Thickness(21);
            window.Resources["MenuIconPresenterMargin"] = new Thickness(21);
            window.Resources["MenuInputGestureTextMargin"] = new Thickness(21);
            window.Resources["MenuFlyoutScrollerMargin"] = new Thickness(21);
            window.Resources["MenuFlyoutSeparatorThemeHeight"] = 7d;
            window.Resources["MenuFlyoutSeparatorThemePadding"] = new Thickness(21);
            window.Resources["SubMenuPopupHorizontalOffset"] = 99d;
            window.Resources["FlyoutThemeMaxWidth"] = 1234d;
            window.Resources["MenuFlyoutThemeMinHeight"] = 99d;
            window.Resources["OverlayCornerRadius"] = new CornerRadius(20);
            window.Resources["MenuBarHeight"] = 99d;
            window.Resources["CheckMarkPathData"] = StreamGeometry.Parse("M 0,0 L 5,5");
            window.Resources["DefaultFontFamily"] = new FontFamily("Comic Sans MS");
            window.Resources["ControlContentThemeFontSize"] = 42d;
        });

        try
        {
            Dictionary<string, string> expected = Resolve(baseline, variant);
            Dictionary<string, string> actual = Resolve(hostile, variant);

            var leaked = MenuTokens
                .Where(key => expected[key] != actual[key])
                .Select(key => $"{key}: expected='{expected[key]}' withHostOverrides='{actual[key]}'")
                .ToList();

            Assert.True(leaked.Count == 0,
                "Host theme overrides leaked into Linux menu tokens:\n  " + string.Join("\n  ", leaked));
        }
        finally
        {
            baseline.Close();
            hostile.Close();
        }
    }

    /// <summary>
    /// The menu templates must only read resources the pack owns.
    /// </summary>
    /// <remarks>
    ///   This is the check that would have caught the original problem. The token-parity tests
    ///   above verify that <c>LinuxMenu*</c> keys resolve correctly, but they cannot see a
    ///   template that bypasses them and reads a generic Fluent key such as
    ///   <c>MenuFlyoutItemForeground</c> straight from the host.
    /// </remarks>
    [AvaloniaFact]
    public void Menu_templates_only_read_owned_resources()
    {
        // Consumed from Fluent by design: re-basing HorizontalMenuItem would mean duplicating
        // Fluent's top-level item template, which the pack deliberately avoids. Documented as
        // the pack's one inherited prerequisite in README.md.
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "FluentTopLevelMenuItem",
        };

        string[] menuFiles =
        {
            "Accents/MenuResources.axaml",
            "Controls/ContextMenu.axaml",
            "Controls/MenuFlyoutPresenter.axaml",
            "Controls/Menu.axaml",
            "Controls/MenuItem.axaml",
            "Controls/Menu.styles.axaml",
            "Controls/Separator.styles.axaml",
        };

        string themeRoot = FindThemeRoot();
        var offenders = new List<string>();

        foreach (string relativePath in menuFiles)
        {
            string path = Path.Combine(themeRoot, relativePath);
            Assert.True(File.Exists(path), $"Expected menu file '{relativePath}' to exist at '{path}'.");

            string markup = File.ReadAllText(path);

            // Design-time previews are not part of the shipped contract.
            markup = StripDesignPreview(markup);

            foreach (Match match in Regex.Matches(markup, @"\{(?:Dynamic|Static)Resource\s+([A-Za-z0-9_]+)\}"))
            {
                string key = match.Groups[1].Value;
                if (!key.StartsWith("LinuxMenu", StringComparison.Ordinal) && allowed.Add(key))
                {
                    offenders.Add($"{relativePath}: {{...Resource {key}}}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Linux menu templates read resources the pack does not own, so they resolve to the " +
            "host theme's values when the pack is layered on top:\n  " + string.Join("\n  ", offenders));
    }

    private static string StripDesignPreview(string markup) =>
        Regex.Replace(markup, "<Design.PreviewWith>.*?</Design.PreviewWith>", string.Empty, RegexOptions.Singleline);

    private static string FindThemeRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "src", "Devolutions.AvaloniaTheme.Linux");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Devolutions.AvaloniaTheme.Linux from " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Rendered menu geometry must be identical under the full theme and under the pack on any
    /// host, which is what a consumer actually sees.
    /// </summary>
    /// <remarks>
    ///   The token tests above compare resource values; this compares the properties the menu
    ///   actually renders with. It is the check that catches a host theme winning through a
    ///   channel the tokens do not cover — for example a host <c>Style</c> outranking the
    ///   <c>MenuItem</c> <c>ControlTheme</c>, or an inherited font changing row heights.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Fluent")]
    [InlineData("DevExpress")]
    public void Rendered_menu_geometry_matches_the_full_theme_on_any_host(string host)
    {
        string expected = DescribeRenderedMenu(hostTheme: null);
        string actual = DescribeRenderedMenu(host);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Opens a context menu and describes what it renders with.
    /// </summary>
    /// <param name="hostTheme">
    ///   Host theme to layer the pack over, or <c>null</c> to use the full Linux theme with no
    ///   pack (the reference rendering).
    /// </param>
    private static string DescribeRenderedMenu(string? hostTheme, Style? hostStyle = null)
    {
        IStyle theme = hostTheme switch
        {
            "DevExpress" => new Devolutions.AvaloniaTheme.DevExpress.DevolutionsDevExpressTheme(),
            "Fluent" => new AvaloniaFluentTheme(),
            _ => new DevolutionsLinuxYaruTheme(),
        };

        if (theme is ISupportInitialize initializable)
        {
            initializable.BeginInit();
            initializable.EndInit();
        }

        var page = new UserControl();
        var window = new Window
        {
            Content = page,
            Width = 800,
            Height = 600,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(theme);

        if (hostStyle is not null)
        {
            window.Styles.Add(hostStyle);
        }

        if (hostTheme is not null)
        {
            var uri = new Uri(PackUri);
            page.Styles.Add(new StyleInclude(uri) { Source = uri });
        }

        var item = new MenuItem { Header = "Item 0" };
        var separator = new Separator();
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(item);
        contextMenu.Items.Add(separator);
        contextMenu.Items.Add(new MenuItem { Header = "Item 1" });

        var target = new Border { Width = 300, Height = 200, ContextMenu = contextMenu };
        page.Content = target;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            contextMenu.Open(target);
            Dispatcher.UIThread.RunJobs();

            return string.Join(
                "\n",
                FormattableString.Invariant($"popupHeight={contextMenu.Bounds.Height:F2}"),
                $"popupBackground={Describe(contextMenu.Background)}",
                $"popupBorder={Describe(contextMenu.BorderBrush)}",
                $"popupCornerRadius={contextMenu.CornerRadius}",
                FormattableString.Invariant($"itemHeight={item.Bounds.Height:F2}"),
                FormattableString.Invariant($"itemMinHeight={item.MinHeight}"),
                $"itemPadding={item.Padding}",
                FormattableString.Invariant($"itemFontSize={item.FontSize}"),
                $"itemFontWeight={item.FontWeight}",
                $"itemFontFamily={item.FontFamily}",
                $"itemForeground={Describe(item.Foreground)}",
                $"itemBackground={Describe(item.Background)}",
                FormattableString.Invariant($"separatorHeight={separator.DesiredSize.Height:F2}"),
                $"separatorMargin={separator.Margin}",
                $"separatorBackground={Describe(separator.Background)}");
        }
        finally
        {
            contextMenu.Close();
            window.Close();
        }
    }

    /// <summary>
    /// A host <c>Style</c> targeting <c>MenuItem</c> must not change pinned menu typography.
    /// </summary>
    /// <remarks>
    ///   The token tests cannot see this: the <c>LinuxMenu*</c> tokens still resolve correctly,
    ///   but a Style setter outranks the <c>ControlTheme</c> setter that applies them, so the
    ///   host wins on the control. Every pinned typography property therefore has to be
    ///   re-applied at Style level. <c>FontWeight</c> was missed originally and did leak.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("FontFamily")]
    [InlineData("FontSize")]
    [InlineData("FontWeight")]
    [InlineData("Background")]
    [InlineData("Foreground")]
    public void Host_menu_item_styles_do_not_change_pinned_typography(string property)
    {
        string expected = DescribeRenderedMenu(hostTheme: "Fluent");

        string actual = DescribeRenderedMenu(hostTheme: "Fluent", HostileMenuItemStyle(property));

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// A host <c>Style</c> targeting <c>MenuItem</c> must not change the menu bar either.
    /// </summary>
    /// <remarks>
    ///   Menu-bar items are direct children of <c>Menu</c>, so the sealing selector for popup
    ///   rows (<c>MenuFlyoutPresenter/ContextMenu/MenuItem</c> descendants) does not reach them.
    ///   They were left exposed originally: a host style changed the menu bar's font, padding
    ///   and — most visibly — its height.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("FontFamily")]
    [InlineData("FontSize")]
    [InlineData("FontWeight")]
    [InlineData("Padding")]
    [InlineData("MinHeight")]
    [InlineData("Background")]
    [InlineData("Foreground")]
    public void Host_menu_item_styles_do_not_change_the_menu_bar(string property)
    {
        string expected = DescribeRenderedMenuBar(hostStyle: null);
        string actual = DescribeRenderedMenuBar(HostileMenuItemStyle(property));

        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///   Builds a host <c>Style</c> that tries to override one pinned menu property.
    /// </summary>
    private static Style HostileMenuItemStyle(string property)
    {
        var style = new Style(x => x.OfType<MenuItem>());
        style.Setters.Add(property switch
        {
            "FontFamily" => new Setter(TemplatedControl.FontFamilyProperty, new FontFamily("Comic Sans MS")),
            "FontSize" => new Setter(TemplatedControl.FontSizeProperty, 42d),
            "Padding" => new Setter(TemplatedControl.PaddingProperty, new Thickness(30)),
            "Background" => new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Colors.Magenta)),
            "Foreground" => new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(Colors.Magenta)),
            "MinHeight" => new Setter(Layoutable.MinHeightProperty, 77d),
            _ => new Setter(TemplatedControl.FontWeightProperty, FontWeight.Bold),
        });

        return style;
    }

    /// <summary>
    ///   Renders a menu bar with the pack over Fluent and describes what its top-level item
    ///   renders with.
    /// </summary>
    private static string DescribeRenderedMenuBar(Style? hostStyle)
    {
        var page = new UserControl();
        var window = new Window
        {
            Content = page,
            Width = 700,
            Height = 400,
            RequestedThemeVariant = ThemeVariant.Light,
        };
        window.Styles.Add(new AvaloniaFluentTheme());

        if (hostStyle is not null)
        {
            window.Styles.Add(hostStyle);
        }

        var uri = new Uri(PackUri);
        page.Styles.Add(new StyleInclude(uri) { Source = uri });

        var topLevelItem = new MenuItem { Header = "File" };
        topLevelItem.Items.Add(new MenuItem { Header = "Open" });

        var menu = new Menu();
        menu.Items.Add(topLevelItem);
        page.Content = menu;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            return string.Join(
                "\n",
                $"fontFamily={topLevelItem.FontFamily}",
                FormattableString.Invariant($"fontSize={topLevelItem.FontSize}"),
                $"fontWeight={topLevelItem.FontWeight}",
                $"padding={topLevelItem.Padding}",
                $"background={Describe(topLevelItem.Background)}",
                $"foreground={Describe(topLevelItem.Foreground)}",
                FormattableString.Invariant($"minHeight={topLevelItem.MinHeight}"),
                FormattableString.Invariant($"height={topLevelItem.Bounds.Height:F2}"));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// SVG menu icons must be recoloured by the pack, not left at their embedded fill.
    /// </summary>
    /// <remarks>
    ///   The full theme applies this from <c>GlobalStyles.axaml</c>, which the pack does not
    ///   ship. Without a pack-owned equivalent a consumer over a foreign host got no
    ///   <c>Css</c> at all — in the dark variant that leaves dark icons on a dark popup.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Svg_menu_icons_are_recoloured_identically_by_pack_and_full_theme(string variantName)
    {
        ThemeVariant variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        string expected = DescribeMenuSvgCss(usePack: false, variant);
        string actual = DescribeMenuSvgCss(usePack: true, variant);

        Assert.Equal(expected, actual);
        Assert.DoesNotContain("<null>", actual);
    }

    /// <summary>
    ///   Renders a menu whose item carries an SVG icon and reports the CSS applied to it.
    /// </summary>
    private static string DescribeMenuSvgCss(bool usePack, ThemeVariant variant)
    {
        // The test host application installs a platform theme of its own; clear it so only the
        // theme under test supplies menu styles.
        var app = Application.Current!;
        var savedStyles = new List<IStyle>(app.Styles);
        app.Styles.Clear();

        var page = new UserControl();
        var window = new Window { Content = page, Width = 600, Height = 400, RequestedThemeVariant = variant };

        try
        {
            if (usePack)
            {
                window.Styles.Add(new AvaloniaFluentTheme());
                var packUri = new Uri(PackUri);
                page.Styles.Add(new StyleInclude(packUri) { Source = packUri });
            }
            else
            {
                var theme = new DevolutionsLinuxYaruTheme();
                theme.BeginInit();
                theme.EndInit();
                window.Styles.Add(theme);
            }

            // The Svg control comes from a transitive package reference, so it is resolved
            // reflectively rather than compiled against.
            Type svgType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .First(type => type.Name == "Svg" && typeof(Control).IsAssignableFrom(type));

            var svg = (Control)Activator.CreateInstance(svgType, new Uri("avares://SampleApp/"))!;
            svgType.GetProperty("Path")!.SetValue(svg, "/Assets/Computer.svg");

            var item = new MenuItem { Header = "Open", Icon = svg };
            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(item);

            var target = new Border { Width = 200, Height = 100, ContextMenu = contextMenu };
            page.Content = target;

            window.Show();
            Dispatcher.UIThread.RunJobs();
            contextMenu.Open(target);
            Dispatcher.UIThread.RunJobs();

            var cssProperty = (AvaloniaProperty)svgType.GetField("CssProperty")!.GetValue(null)!;
            string css = svg.GetValue(cssProperty)?.ToString() ?? "<null>";

            contextMenu.Close();
            return css;
        }
        finally
        {
            window.Close();
            app.Styles.Clear();
            foreach (IStyle style in savedStyles)
            {
                app.Styles.Add(style);
            }
        }
    }
}
