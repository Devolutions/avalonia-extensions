namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

public static class HorizontalWheelBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("Enable", typeof(HorizontalWheelBehavior));

    static HorizontalWheelBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is ScrollViewer sv)
            {
                var enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    sv.AddHandler(InputElement.PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
                }
                else
                {
                    sv.RemoveHandler(InputElement.PointerWheelChangedEvent, OnWheel);
                }
            }
        });
    }

    public static void SetEnable(ScrollViewer element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(ScrollViewer element) => element.GetValue(EnableProperty);

    private static void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        if ((e.KeyModifiers & KeyModifiers.Shift) != 0) return;

        if (sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled ||
            sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
        {
            return;
        }

        var raw = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
        if (raw == 0) return;

        var pixels = raw * 48;

        var maxX = Math.Max(0, sv.Extent.Width - sv.Viewport.Width);
        var newX = Math.Clamp(sv.Offset.X - pixels, 0, maxX);

        sv.Offset = new Vector(newX, sv.Offset.Y);
        e.Handled = true;
    }
}