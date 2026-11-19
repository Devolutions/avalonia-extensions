using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Devolutions.AvaloniaTheme.WinUI.Controls;

public class MenuFlyoutInput
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutInput, Control, bool>("IsEnabled", inherits: true);

    public static readonly AttachedProperty<bool> IsKeyboardNavigationProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutInput, Control, bool>("IsKeyboardNavigation", inherits: true);

    private static readonly AttachedProperty<bool> IsPointerSelectionProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutInput, MenuFlyoutPresenter, bool>("IsPointerSelection");

    static MenuFlyoutInput()
    {
        MenuItem.PointerEnteredItemEvent.AddClassHandler<MenuFlyoutPresenter>((presenter, _) =>
        {
            if (GetIsEnabled(presenter))
                presenter.SetValue(IsPointerSelectionProperty, true);
        });
        IsEnabledProperty.Changed.AddClassHandler<MenuFlyoutPresenter>((presenter, change) =>
        {
            if (change.GetNewValue<bool>())
            {
                presenter.AttachedToVisualTree += Attached;
                presenter.DetachedFromVisualTree += Detached;
                presenter.AddHandler(InputElement.GotFocusEvent, GotFocus, handledEventsToo: true);
                presenter.AddHandler(InputElement.GettingFocusEvent, GettingFocus, handledEventsToo: true);
                presenter.AddHandler(InputElement.KeyDownEvent, KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
                presenter.AddHandler(InputElement.PointerPressedEvent, PointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
                presenter.AddHandler(InputElement.PointerMovedEvent, PointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
            }
            else
            {
                presenter.AttachedToVisualTree -= Attached;
                presenter.DetachedFromVisualTree -= Detached;
                presenter.RemoveHandler(InputElement.GotFocusEvent, GotFocus);
                presenter.RemoveHandler(InputElement.GettingFocusEvent, GettingFocus);
                presenter.RemoveHandler(InputElement.KeyDownEvent, KeyDown);
                presenter.RemoveHandler(InputElement.PointerPressedEvent, PointerPressed);
                presenter.RemoveHandler(InputElement.PointerMovedEvent, PointerMoved);
                presenter.ClearValue(IsKeyboardNavigationProperty);
                presenter.ClearValue(IsPointerSelectionProperty);
            }
        });
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    public static bool GetIsKeyboardNavigation(Control control) => control.GetValue(IsKeyboardNavigationProperty);

    public static void SetIsKeyboardNavigation(Control control, bool value) => control.SetValue(IsKeyboardNavigationProperty, value);

    private static void Attached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        var presenter = (MenuFlyoutPresenter)sender!;
        // PopupFlyoutBase focuses its first item without preserving the opener's navigation method.
        var focused = TopLevel.GetTopLevel(presenter)?.FocusManager?.GetFocusedElement() as Control;
        SetIsKeyboardNavigation(presenter, focused?.Classes.Contains(":focus-visible") == true ||
            (focused is not null && GetIsKeyboardNavigation(focused)));
    }

    private static void Detached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        var presenter = (MenuFlyoutPresenter)sender!;
        presenter.ClearValue(IsKeyboardNavigationProperty);
        presenter.ClearValue(IsPointerSelectionProperty);
    }

    private static void GettingFocus(object? sender, FocusChangingEventArgs args)
    {
        var presenter = (MenuFlyoutPresenter)sender!;
        // Preserve sibling focus on hover, but allow focus to leave a closing submenu.
        if (presenter.GetValue(IsPointerSelectionProperty) &&
            args.NavigationMethod == NavigationMethod.Unspecified &&
            args.OldFocusedElement is MenuItem { Parent: SelectingItemsControl parent } &&
            args.NewFocusedElement is MenuItem { IsPointerOver: true } next &&
            ReferenceEquals(parent, next.Parent))
            args.TryCancel();
    }

    private static void GotFocus(object? sender, FocusChangedEventArgs args)
    {
        if (args.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
            SetIsKeyboardNavigation((MenuFlyoutPresenter)sender!, true);
        else if (args.NavigationMethod == NavigationMethod.Pointer)
            SetIsKeyboardNavigation((MenuFlyoutPresenter)sender!, false);
    }

    private static void KeyDown(object? sender, KeyEventArgs args)
    {
        var presenter = (MenuFlyoutPresenter)sender!;
        presenter.ClearValue(IsPointerSelectionProperty);
        SetIsKeyboardNavigation(presenter, true);
        if (TopLevel.GetTopLevel(presenter)?.FocusManager?.GetFocusedElement() is MenuItem focused &&
            focused.Parent is SelectingItemsControl parent)
            parent.SelectedIndex = parent.IndexFromContainer(focused);
    }

    private static void PointerPressed(object? sender, PointerPressedEventArgs args)
    {
        var presenter = (MenuFlyoutPresenter)sender!;
        presenter.ClearValue(IsPointerSelectionProperty);
        SetIsKeyboardNavigation(presenter, false);
    }

    private static void PointerMoved(object? sender, PointerEventArgs args) =>
        ((MenuFlyoutPresenter)sender!).ClearValue(IsPointerSelectionProperty);
}
