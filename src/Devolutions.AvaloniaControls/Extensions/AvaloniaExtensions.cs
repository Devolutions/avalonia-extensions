namespace Devolutions.AvaloniaControls.Extensions;

using Avalonia;

public static class AvaloniaExtensions
{
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
}