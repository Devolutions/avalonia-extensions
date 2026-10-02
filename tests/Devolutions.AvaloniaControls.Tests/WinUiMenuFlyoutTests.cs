using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using System.Reflection;
using Xunit;

namespace Devolutions.AvaloniaControls.Tests;

[Collection("StylesTest")]
public class WinUiMenuFlyoutTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Svg_icons_follow_theme_and_disabled_state_in_flyout_and_submenu(bool mica)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        var app = Application.Current!;
        var savedStyles = new List<IStyle>(app.Styles);
        app.Styles.Clear();
        Window? window = null;
        try
        {
            var svgType = Assembly.Load("Svg.Controls.Avalonia").GetTypes()
                .Single(type => type.Name == "Svg" && typeof(Control).IsAssignableFrom(type));
            var cssProperty = (AvaloniaProperty)svgType.GetField("CssProperty")!.GetValue(null)!;
            Control Icon()
            {
                var icon = (Control)Activator.CreateInstance(svgType, new Uri("avares://SampleApp/"))!;
                svgType.GetProperty("Path")!.SetValue(icon, "/Assets/Computer.svg");
                return icon;
            }

            var enabledIcon = Icon();
            var disabledIcon = Icon();
            var nestedIcon = Icon();
            var nestedDisabledIcon = Icon();
            var submenu = new MenuItem { Header = "More" };
            submenu.Items.Add(new MenuItem { Header = "Child", Icon = nestedIcon });
            submenu.Items.Add(new MenuItem { Header = "Disabled child", Icon = nestedDisabledIcon, IsEnabled = false });
            var presenter = new MenuFlyoutPresenter
            {
                Items =
                {
                    new MenuItem { Header = "Enabled", Icon = enabledIcon },
                    new MenuItem { Header = "Disabled", Icon = disabledIcon, IsEnabled = false },
                    submenu
                }
            };
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            var target = new Border { Width = 200, Height = 100 };
            window = new Window { Content = target, Width = 600, Height = 400, RequestedThemeVariant = ThemeVariant.Light };
            window.Styles.Add(theme);
            window.Show();
            var flyout = new MenuFlyout();
            flyout.Popup.Child = presenter;
            flyout.ShowAt(target);
            Dispatcher.UIThread.RunJobs();
            submenu.IsSubMenuOpen = true;
            Assert.Contains(enabledIcon.GetLogicalAncestors(), ancestor => ancestor is MenuItem);
            Assert.Contains(enabledIcon.GetVisualAncestors(), ancestor => ancestor is MenuItem);
            Assert.True(window.TryFindResource("MenuFlyoutItemIconCss", ThemeVariant.Light, out var iconCss));
            Assert.Equal(".st0{fill:#010101;}", iconCss);

            void AssertCss(string normal, string disabled)
            {
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(normal, enabledIcon.GetValue(cssProperty));
                Assert.Equal(disabled, disabledIcon.GetValue(cssProperty));
                Assert.Equal(normal, nestedIcon.GetValue(cssProperty));
                Assert.Equal(disabled, nestedDisabledIcon.GetValue(cssProperty));
            }

            AssertCss(".st0{fill:#010101;}", ".st0,.st1,.st2,.st3{fill:#bdbdbd;}");
            window.RequestedThemeVariant = ThemeVariant.Dark;
            AssertCss(".st0{fill:#f5f5f5;}", ".st0,.st1,.st2,.st3{fill:#656565;}");
            flyout.Hide();
        }
        finally
        {
            window?.Close();
            app.Styles.Clear();
            foreach (var style in savedStyles)
                app.Styles.Add(style);
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData(PlacementMode.Bottom)]
    [InlineData(PlacementMode.Top)]
    public void Open_flyout_enters_from_placement_target(PlacementMode placement)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();
        var target = new Button
        {
            Content = "Open", Width = 80, Height = 32,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var window = new Window { Content = target, Width = 500, Height = 500 };
        window.Styles.Add(theme);
        window.Show();
        try
        {
            var flyout = new MenuFlyout { Placement = placement };
            flyout.Items.Add(new MenuItem { Header = "Item" });
            bool attachedWithPopup = false;
            flyout.Popup.Child = new MenuFlyoutPresenter();
            flyout.Popup.Child.AttachedToVisualTree += (_, _) =>
                attachedWithPopup = flyout.Popup.Child.GetLogicalAncestors().Contains(flyout.Popup);
            flyout.ShowAt(target);
            var presenter = Assert.IsType<MenuFlyoutPresenter>(flyout.Popup.Child);
            Assert.Contains(flyout.Popup, presenter.GetLogicalAncestors());
            Assert.True(attachedWithPopup);
            var surface = Assert.Single(presenter.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "LayoutRoot");
            var translation = Assert.IsType<TranslateTransform>(surface.RenderTransform);
            var offset = surface.PointToScreen(new Point(0, 0)).Y - translation.Y <
                target.PointToScreen(new Point(0, 0)).Y ? 8 : -8;
            Assert.Equal(offset, translation.Y);
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(Math.Abs(translation.Y), 0, Math.Abs(offset));
            flyout.Hide();
            flyout.ShowAt(target);
            Assert.Equal(offset, Assert.IsType<TranslateTransform>(surface.RenderTransform).Y);
            flyout.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void Flyout_and_submenu_have_shadow_padding_and_small_chevron(string variantName, bool mica)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        Window? window = null;
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();

            var submenu = new MenuItem { Header = "More", Items = { new MenuItem { Header = "Child" } } };
            var presenter = new MenuFlyoutPresenter { Items = { submenu } };
            window = new Window
            {
                RequestedThemeVariant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light
            };
            window.Styles.Add(theme);
            window.Content = presenter;
            window.Show();

            presenter.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();
            var surface = Assert.Single(presenter.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "LayoutRoot");
            Assert.Equal(new Thickness(8), surface.Margin);
            Assert.NotEqual(default, surface.BoxShadow);
            Assert.Equal(new Thickness(0, 2), surface.Padding);
            Assert.Equal(456, surface.MaxWidth);
            Assert.Equal(32, surface.MinHeight);

            var arrow = Assert.Single(submenu.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>(),
                path => path.Name == "PART_ChevronPath");
            Assert.Equal(5.25, arrow.Width);
            Assert.Equal(8, arrow.Height);
            Assert.Equal(new Thickness(12, 0, 3, 0), arrow.Margin);

            submenu.IsSubMenuOpen = true;
            var popup = Assert.Single(submenu.GetVisualDescendants().OfType<Popup>(),
                control => control.Name == "PART_Popup");
            var childSurface = Assert.IsType<Border>(popup.Child);
            var entrance = Assert.IsType<TranslateTransform>(childSurface.RenderTransform);
            var expectedDirection = childSurface.PointToScreen(new Point(0, 0)).Y - entrance.Y <
                submenu.PointToScreen(new Point(0, 0)).Y ? 8 : -8;
            Assert.Equal(expectedDirection, entrance.Y);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new Thickness(0, 2), childSurface.Padding);
            Assert.Equal(new Thickness(8), childSurface.Margin);
            Assert.NotEqual(default, childSurface.BoxShadow);
            Assert.Equal(456, childSurface.MaxWidth);
            Assert.Equal(32, childSurface.MinHeight);
            Assert.Equal(-12, popup.HorizontalOffset);
            Assert.Equal(-4, popup.HorizontalOffset + childSurface.Margin.Left);

            var parentRight = surface.PointToScreen(new Point(surface.Bounds.Width, 0)).X;
            var submenuLeft = childSurface.PointToScreen(new Point(0, 0)).X;
            Assert.InRange(submenuLeft - parentRight, -6, -2);
            var chevronRight = arrow.PointToScreen(new Point(arrow.Bounds.Width, 0)).X;
            Assert.InRange(parentRight - chevronRight, 14, 19);

            submenu.IsSubMenuOpen = false;
            Assert.Equal(0, childSurface.Opacity);
            Assert.Null(childSurface.RenderTransform);
            submenu.IsSubMenuOpen = true;
            var reopenedEntrance = Assert.IsType<TranslateTransform>(childSurface.RenderTransform);
            Assert.NotSame(entrance, reopenedEntrance);
            Assert.Equal(expectedDirection, reopenedEntrance.Y);
        }
        finally
        {
            window?.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }
}
