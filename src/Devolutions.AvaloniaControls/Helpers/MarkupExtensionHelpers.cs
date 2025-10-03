namespace Devolutions.AvaloniaControls.Helpers;

using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;

public static class MarkupExtensionHelpers
{
    // Dynamic approach is slightly faster than using reflection, see: https://stackoverflow.com/a/78582306
    public static IBinding? GetBinding<TIn>(object? v)
    {
        switch (v)
        {
            case null:
                return null;
            case IBinding binding:
                return binding;
            case string str:
            {
                var isParsable = typeof(TIn).GetInterfaces()
                    .Any(static c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IParsable<>));
                if (isParsable)
                {
                    TIn value = Parse(str, (dynamic)default(TIn)!);
                    return ObservableHelpers.ValueBinding(value);
                }

                break;
            }
        }
        
        if (v is TIn t) return ObservableHelpers.ValueBinding(t);

        return TypeDescriptor.GetConverter(v.GetType()).ConvertTo(v, typeof(TIn)) is TIn t2
            ? ObservableHelpers.ValueBinding(t2)
            : null;
    }

    public static IBinding? GetBinding(object? v, Type type)
    {
        switch (v)
        {
            case null:
                return null;
            case IBinding binding:
                return binding;
            case string str:
            {
                var isParsable = type.GetInterfaces().Any(static c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IParsable<>));
                if (isParsable)
                {
                    var parseMethod = type.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]);
                    return ObservableHelpers.ValueBinding(parseMethod?.Invoke(type, [str, CultureInfo.InvariantCulture]));
                }

                break;
            }
        }

        var t = TypeDescriptor.GetConverter(v.GetType()).ConvertTo(v, type);
        return t is not null
            ? ObservableHelpers.ValueBinding(t)
            : null;
    }

    private static T Parse<T>(string stringValue, T _) where T : IParsable<T> =>
        T.Parse(stringValue, CultureInfo.InvariantCulture);
}