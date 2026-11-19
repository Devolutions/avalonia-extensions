using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Platform;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
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
    [InlineData("Light", false, false)]
    [InlineData("Dark", false, false)]
    [InlineData("Light", true, false)]
    [InlineData("Dark", true, false)]
    [InlineData("Light", false, true)]
    [InlineData("Dark", false, true)]
    [InlineData("Light", true, true)]
    [InlineData("Dark", true, true)]
    public void Flyout_item_keyboard_focus_is_independent_of_pointer_highlight(string variantName, bool mica, bool ordinaryButton)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        Window? window = null;
        var flyout = new MenuFlyout();
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            var first = new MenuItem { Header = "First" };
            var second = new MenuItem { Header = "Second" };
            flyout.Items.Add(first);
            flyout.Items.Add(second);
            Button button = ordinaryButton ? new Button() : new DropDownButton();
            button.Content = "Open";
            button.Flyout = flyout;
            button.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            var anchor = new Button { Content = "Before" };
            window = new Window
            {
                Width = 600, Height = 400,
                RequestedThemeVariant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light
            };
            window.Styles.Add(theme);
            window.Content = new StackPanel { Margin = new Thickness(30), Children = { anchor, button } };
            window.Show();
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);
            Assert.True(first.IsFocused);
            Assert.False(Assert.IsType<MenuFlyoutPresenter>(flyout.Popup.Child).Focusable);
            Assert.False(first.IsPointerOver);
            Border Part(MenuItem item, string name) =>
                Assert.Single(item.GetVisualDescendants().OfType<Border>(), border => border.Name == name);
            var firstSurface = Part(first, "PART_LayoutRoot");
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(firstSurface.Background).Color.A);
            Assert.False(Part(first, "FocusRing").IsVisible);

            var root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(first));
            root.MouseMove(first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), root)!.Value);
            Assert.True(first.IsPointerOver);
            Assert.NotEqual(0, Assert.IsAssignableFrom<ISolidColorBrush>(firstSurface.Background).Color.A);
            root.MouseMove(new Point(-10, -10));
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(firstSurface.Background).Color.A);
            root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            root.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            var focused = new[] { first, second }.Single(item => item.IsFocused);
            Assert.Same(second, focused);
            Assert.True(Part(focused, "FocusRing").IsVisible);
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(Part(focused, "PART_LayoutRoot").Background).Color.A);
            int clicks = 0;
            focused.Click += (_, _) => clicks++;
            root.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            root.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Assert.Equal(1, clicks);
            Assert.False(flyout.IsOpen);

            anchor.Focus();
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.Contains(":focus-visible", button.Classes);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);
            root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(first));
            Assert.True(first.IsFocused);
            Assert.True(Part(first, "FocusRing").IsVisible);
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(firstSurface.Background).Color.A);

            root.MouseMove(second.TranslatePoint(new Point(second.Bounds.Width / 2, second.Bounds.Height / 2), root)!.Value);
            Assert.True(Part(first, "FocusRing").IsVisible);
            Assert.False(Part(second, "FocusRing").IsVisible);
            Assert.NotEqual(0, Assert.IsAssignableFrom<ISolidColorBrush>(Part(second, "PART_LayoutRoot").Background).Color.A);
            root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            root.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Assert.True(second.IsFocused);
            Assert.True(Part(second, "FocusRing").IsVisible);
            root.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            root.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Assert.True(first.IsFocused);
            Assert.True(Part(first, "FocusRing").IsVisible);
            Assert.NotEqual(0, Assert.IsAssignableFrom<ISolidColorBrush>(Part(second, "PART_LayoutRoot").Background).Color.A);
            root.MouseMove(new Point(-10, -10));
            root.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            root.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.True(second.IsFocused);
            Assert.True(Part(second, "FocusRing").IsVisible);
            Assert.False(Part(first, "FocusRing").IsVisible);
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(Part(second, "PART_LayoutRoot").Background).Color.A);
            root.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            root.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Assert.False(flyout.IsOpen);

            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.True(flyout.IsOpen);
            Assert.True(first.IsFocused);
            Assert.False(Part(first, "FocusRing").IsVisible);
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(firstSurface.Background).Color.A);
            root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(first));
            var secondPoint = second.TranslatePoint(new Point(second.Bounds.Width / 2, second.Bounds.Height / 2), root)!.Value;
            root.MouseMove(secondPoint);
            Assert.True(first.IsFocused);
            root.MouseDown(secondPoint, MouseButton.Left);
            root.MouseUp(secondPoint, MouseButton.Left);
            Assert.Equal(2, clicks);
            Assert.False(flyout.IsOpen);
        }
        finally
        {
            flyout.Hide();
            window?.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void Inline_presenter_keeps_focus_on_items_instead_of_the_entire_menu(string variantName, bool mica)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        Window? window = null;
        var originalMenuShowDelay = DefaultMenuInteractionHandler.MenuShowDelay;
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            var anchor = new Button { Content = "Before" };
            var child = new MenuItem { Header = "Child" };
            var first = new MenuItem { Header = "First", Items = { child } };
            var outside = new MenuItem { Header = "Outside the flyout" };
            var presenter = new MenuFlyoutPresenter { Items = { first, new MenuItem { Header = "Second" } } };
            window = new Window
            {
                Width = 600, Height = 400,
                RequestedThemeVariant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light
            };
            window.Styles.Add(theme);
            window.Content = new StackPanel { Children = { anchor, presenter, outside } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            anchor.Focus();
            Assert.False(presenter.IsFocused);
            Assert.False(presenter.Focus());
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Assert.True(first.IsFocused);
            Assert.True(Assert.Single(first.GetVisualDescendants().OfType<Border>(), b => b.Name == "FocusRing").IsVisible);
            var surface = Assert.Single(first.GetVisualDescendants().OfType<Border>(), b => b.Name == "PART_LayoutRoot");
            Assert.Equal(0, Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color.A);
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(first.IsSubMenuOpen);
            Assert.True(child.IsFocused);
            Assert.True(Assert.Single(child.GetVisualDescendants().OfType<Border>(), b => b.Name == "FocusRing").IsVisible);
            var childRoot = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(child));
            childRoot.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
            childRoot.KeyRelease(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
            Assert.False(first.IsSubMenuOpen);
            Assert.True(first.IsFocused);
            Assert.True(Assert.Single(first.GetVisualDescendants().OfType<Border>(), b => b.Name == "FocusRing").IsVisible);
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(child.IsFocused);
            DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero;
            var second = Assert.IsType<MenuItem>(presenter.ContainerFromIndex(1));
            window.MouseMove(second.TranslatePoint(new Point(second.Bounds.Width / 2, second.Bounds.Height / 2), window)!.Value);
            Dispatcher.UIThread.RunJobs();
            Assert.False(first.IsSubMenuOpen);
            Assert.True(second.IsFocused);
            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            window.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Assert.True(first.IsFocused);
            outside.Focus(NavigationMethod.Tab);
            Assert.True(outside.IsFocused);
            Assert.False(outside.IsSet(Control.FocusAdornerProperty));
        }
        finally
        {
            DefaultMenuInteractionHandler.MenuShowDelay = originalMenuShowDelay;
            window?.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Data_bound_menu_keyboard_navigation_selects_source_items(bool submenu)
    {
        Window? window = null;
        var flyout = new MenuFlyout();
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            var items = new[] { "First", "Second", "Third" };
            var parent = new MenuItem { Header = "More", ItemsSource = items };
            if (submenu)
                flyout.Items.Add(parent);
            else
                flyout.ItemsSource = items;
            var button = new DropDownButton { Content = "Open", Flyout = flyout };
            window = new Window { Width = 600, Height = 400 };
            window.Styles.Add(theme);
            window.Content = button;
            window.Show();
            flyout.ShowAt(button);
            Dispatcher.UIThread.RunJobs();
            var presenter = Assert.IsType<MenuFlyoutPresenter>(flyout.Popup.Child);
            var root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(presenter));
            if (submenu)
            {
                Assert.True(parent.Focus(NavigationMethod.Tab));
                root.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                root.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                Dispatcher.UIThread.RunJobs();
            }

            SelectingItemsControl menu = submenu ? parent : presenter;
            var first = Assert.IsType<MenuItem>(menu.ContainerFromIndex(0));
            var second = Assert.IsType<MenuItem>(menu.ContainerFromIndex(1));
            var third = Assert.IsType<MenuItem>(menu.ContainerFromIndex(2));
            root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(first));
            Assert.True(first.Focus(NavigationMethod.Tab));
            Assert.True(first.IsFocused);
            Assert.Equal(items[0], menu.SelectedItem);
            root.KeyPress(Key.LeftShift, RawInputModifiers.Shift, PhysicalKey.ShiftLeft, null);
            root.KeyRelease(Key.LeftShift, RawInputModifiers.None, PhysicalKey.ShiftLeft, null);
            Assert.Equal(items[0], menu.SelectedItem);
            root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            root.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Assert.True(second.IsFocused);
            Assert.Equal(items[1], menu.SelectedItem);
            root.MouseMove(third.TranslatePoint(new Point(third.Bounds.Width / 2, third.Bounds.Height / 2), root)!.Value);
            Assert.True(second.IsFocused);
            Assert.Equal(items[2], menu.SelectedItem);
            root.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            root.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Assert.True(first.IsFocused);
            Assert.Equal(items[0], menu.SelectedItem);
            var clicks = 0;
            first.Click += (_, _) => clicks++;
            root.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            root.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Assert.Equal(1, clicks);
            Assert.False(flyout.IsOpen);
        }
        finally
        {
            flyout.Hide();
            window?.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void Dropdownbutton_flyout_and_submenu_use_opaque_fallback(string variantName, bool mica)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        Window? window = null;
        var flyout = new MenuFlyout();
        try
        {
            var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
            theme.BeginInit();
            theme.EndInit();
            var submenu = new MenuItem { Header = "More", Items = { new MenuItem { Header = "Child" } } };
            flyout.Items.Add(submenu);
            var button = new DropDownButton { Content = "Open", Flyout = flyout };
            window = new Window
            {
                Content = button,
                Width = 600,
                Height = 400,
                RequestedThemeVariant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light
            };
            window.Styles.Add(theme);
            window.Show();
            flyout.ShowAt(button);
            Dispatcher.UIThread.RunJobs();

            var presenter = Assert.IsType<MenuFlyoutPresenter>(flyout.Popup.Child);
            var surface = Assert.Single(presenter.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "LayoutRoot");
            submenu.IsSubMenuOpen = true;
            Dispatcher.UIThread.RunJobs();
            var popup = Assert.Single(submenu.GetVisualDescendants().OfType<Popup>(),
                control => control.Name == "PART_Popup");
            Assert.True(popup.IsOpen);
            var childSurface = Assert.IsType<Border>(popup.Child);
            var expected = Color.Parse(variantName == "Dark" ? "#FF2D2D2D" : "#FFF9F9F9");
            foreach (var border in new[] { surface, childSurface })
            {
                var brush = Assert.IsAssignableFrom<ISolidColorBrush>(border.Background);
                Assert.Equal(expected, brush.Color);
                Assert.Equal(1.0, brush.Opacity);
                Assert.NotEqual(default, border.BoxShadow);
            }
        }
        finally
        {
            flyout.Hide();
            window?.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

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

    [AvaloniaFact]
    public void Presenter_padding_max_width_and_min_height_overrides_flow_through_template()
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();

        // Instance-level overrides must still apply: the template binds Padding/MaxWidth/MinHeight
        // via TemplateBinding (rather than reading the theme resources directly), so a consumer
        // overriding these properties isn't silently ignored by the rendered surface.
        var presenter = new MenuFlyoutPresenter
        {
            Items = { new MenuItem { Header = "Item" } },
            Padding = new Thickness(20),
            MaxWidth = 300,
            MinHeight = 64,
        };
        var window = new Window();
        window.Styles.Add(theme);
        window.Content = presenter;
        window.Show();
        try
        {
            presenter.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();
            var surface = Assert.Single(presenter.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "LayoutRoot");
            Assert.Equal(new Thickness(20), surface.Padding);
            Assert.Equal(300, surface.MaxWidth);
            Assert.Equal(64, surface.MinHeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Separator_styling_is_scoped_to_menu_descendants()
    {
        // Regression guard: Separator styling must not leak into standalone/non-menu
        // separators (e.g. SampleApp's SeparatorDemo, which WinUI does not style).
        // See Controls/MenuFlyoutSeparator.styles.axaml for the scoped selector.
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();

        var plainSeparator = new Separator();
        var menuSeparator = new Separator();
        var presenter = new MenuFlyoutPresenter
        {
            Items = { new MenuItem { Header = "Item" }, menuSeparator }
        };
        var panel = new StackPanel { Children = { plainSeparator, presenter } };
        var window = new Window();
        window.Styles.Add(theme);
        window.Content = panel;
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.TryFindResource("MenuFlyoutSeparatorBackground", window.ActualThemeVariant,
                out object? menuBackground));

            Assert.NotEqual(menuBackground, plainSeparator.Background);

            Assert.Equal(menuBackground, menuSeparator.Background);
            Assert.Equal(1, menuSeparator.Height);
            Assert.Equal(new Thickness(12, 4), menuSeparator.Margin);
        }
        finally
        {
            window.Close();
        }
    }
}
