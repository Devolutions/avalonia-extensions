namespace Devolutions.AvaloniaControls.Extensions;

using Avalonia;
using Avalonia.Controls;

public static class AvaloniaExtensions
{
    /// <summary>
    /// Keeps <paramref name="pseudoClass"/> in sync with a boolean property, so a theme can select on it.
    /// Replaces Irihi.Avalonia.Shared's AvaloniaPropertyExtension.AffectsPseudoClass, which was one of the
    /// last two things tying us to that package.
    /// </summary>
    public static void AffectsPseudoClass<TControl>(this AvaloniaProperty<bool> property, string pseudoClass)
        where TControl : Control =>
        property.Changed.AddClassHandler<TControl, bool>((control, args) =>
            ((IPseudoClasses)control.Classes).Set(pseudoClass, args.NewValue.GetValueOrDefault()));

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