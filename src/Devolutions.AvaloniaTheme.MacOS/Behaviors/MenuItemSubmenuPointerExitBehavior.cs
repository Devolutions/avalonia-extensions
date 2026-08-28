namespace Devolutions.AvaloniaTheme.MacOS.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>
/// Prevents Avalonia from scheduling its one-shot submenu close when the pointer exits
/// a menu item whose submenu is already open.
/// </summary>
internal static class MenuItemSubmenuPointerExitBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("Enable", typeof(MenuItemSubmenuPointerExitBehavior));

    static MenuItemSubmenuPointerExitBehavior()
    {
        EnableProperty.Changed.AddClassHandler<MenuItem>(OnEnableChanged);
    }

    public static bool GetEnable(MenuItem element) => element.GetValue(EnableProperty);

    public static void SetEnable(MenuItem element, bool value) => element.SetValue(EnableProperty, value);

    private static void OnEnableChanged(MenuItem menuItem, AvaloniaPropertyChangedEventArgs args)
    {
        menuItem.RemoveHandler(MenuItem.PointerExitedItemEvent, OnPointerExitedItem);

        if (args.NewValue is true)
        {
            menuItem.AddHandler(MenuItem.PointerExitedItemEvent, OnPointerExitedItem, RoutingStrategies.Bubble);
        }
    }

    private static void OnPointerExitedItem(object? sender, RoutedEventArgs args)
    {
        if (sender is MenuItem menuItem &&
            ReferenceEquals(args.Source, menuItem) &&
            menuItem.IsSubMenuOpen)
        {
            args.Handled = true;
        }
    }
}
