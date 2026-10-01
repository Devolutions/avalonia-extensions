using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Devolutions.AvaloniaTheme.WinUI.Controls;

public class MenuFlyoutEntrance
{
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(180);

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutEntrance, Control, bool>("IsEnabled");

    static MenuFlyoutEntrance()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, change) =>
        {
            if (control is MenuFlyoutPresenter presenter)
            {
                if (change.GetNewValue<bool>())
                    presenter.AttachedToVisualTree += PresenterAttached;
                else
                    presenter.AttachedToVisualTree -= PresenterAttached;
            }
            else if (control is Popup popup)
            {
                if (change.GetNewValue<bool>())
                {
                    popup.Opened += PopupOpened;
                    popup.Closed += PopupClosed;
                    popup.AttachedToVisualTree += PopupAttached;
                }
                else
                {
                    popup.Opened -= PopupOpened;
                    popup.Closed -= PopupClosed;
                    popup.AttachedToVisualTree -= PopupAttached;
                }
            }
        });
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    private static void PresenterAttached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        var presenter = (MenuFlyoutPresenter)sender!;
        var popup = presenter.GetLogicalAncestors().OfType<Popup>().FirstOrDefault();
        if (popup is null)
            return;

        var surface = presenter.GetVisualDescendants().OfType<Border>()
            .First(border => border.Name == "LayoutRoot");
        surface.Opacity = 0;
        popup.Opened += PopupOpened;
        popup.Closed += PopupClosed;
        void Detached(object? sender, VisualTreeAttachmentEventArgs args)
        {
            popup.Opened -= PopupOpened;
            popup.Closed -= PopupClosed;
            presenter.DetachedFromVisualTree -= Detached;
        }
        presenter.DetachedFromVisualTree += Detached;
    }

    private static void PopupAttached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        if (((Popup)sender!).Child is Border surface)
            surface.Opacity = 0;
    }

    private static Border GetSurface(Popup popup) =>
        popup.Child switch
        {
            Border surface => surface,
            MenuFlyoutPresenter presenter => presenter.GetVisualDescendants().OfType<Border>()
                .First(border => border.Name == "LayoutRoot"),
            _ => throw new InvalidOperationException("MenuFlyout popup has no themed surface.")
        };

    private static void PopupOpened(object? sender, EventArgs args)
    {
        var popup = (Popup)sender!;
        var surface = GetSurface(popup);
        var target = popup.PlacementTarget ?? popup.GetLogicalAncestors().OfType<MenuItem>().FirstOrDefault() ??
            throw new InvalidOperationException("MenuFlyout popup has no placement target.");
        var targetTop = target.PointToScreen(new Point(0, 0)).Y;
        var surfaceTop = surface.PointToScreen(new Point(0, 0)).Y;
        var translation = new TranslateTransform(0, surfaceTop < targetTop ? 8 : -8);

        surface.Transitions = null;
        surface.Opacity = 0;
        surface.RenderTransform = translation;
        surface.Transitions = new Transitions
        {
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = Duration, Easing = new CubicEaseOut() }
        };
        translation.Transitions = new Transitions
        {
            new DoubleTransition { Property = TranslateTransform.YProperty, Duration = Duration, Easing = new CubicEaseOut() }
        };

        Dispatcher.UIThread.Post(() =>
        {
            if (popup.IsOpen && ReferenceEquals(surface.RenderTransform, translation))
            {
                surface.Opacity = 1;
                translation.Y = 0;
            }
        }, DispatcherPriority.Render);
    }

    private static void PopupClosed(object? sender, EventArgs args)
    {
        var surface = GetSurface((Popup)sender!);
        surface.Transitions = null;
        surface.Opacity = 0;
        surface.RenderTransform = null;
    }
}
