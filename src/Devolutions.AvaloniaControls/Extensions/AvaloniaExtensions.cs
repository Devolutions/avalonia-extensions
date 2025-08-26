namespace Devolutions.AvaloniaControls.Extensions;

using Avalonia;
using Avalonia.Controls;

public static class AvaloniaExtensions
{
    public enum HorizontalScrollDirection
    {
        Left,
        Right,
    }

    public static void SetValueIfChanged<T>(this AvaloniaObject obj, StyledProperty<T?> property, T? value, bool canSetToNull = true) where T : struct, IEquatable<T>
    {
        var cur = obj.GetValue(property);
        if ((value is not null || canSetToNull) && !cur.Equals(value))
        {
            obj.SetValue(property, value);
        }
    }

    public static void SetValueIfChanged<T>(this AvaloniaObject obj, StyledProperty<T> property, T value) where T : struct, IEquatable<T>
    {
        var cur = obj.GetValue(property);
        if (!cur.Equals(value))
        {
            obj.SetValue(property, value);
        }
    }

    public static void SetValueIfChanged<T>(this AvaloniaObject obj, StyledProperty<T?> property, T? value, bool canSetToNull = true) where T : class, IEquatable<T>
    {
        var cur = obj.GetValue(property);
        if ((value is not null || canSetToNull) && cur != value)
        {
            obj.SetValue(property, value);
        }
    }

    public static void ScrollHorizontally(this ScrollViewer sv, HorizontalScrollDirection direction = HorizontalScrollDirection.Right, double step = 48)
    {
        if (direction == HorizontalScrollDirection.Left) step *= -1;

        var maxX = Math.Max(0, sv.Extent.Width - sv.Viewport.Width);
        var newX = Math.Clamp(sv.Offset.X + step, 0, maxX);
        sv.Offset = new Vector(newX, sv.Offset.Y);
    }
}