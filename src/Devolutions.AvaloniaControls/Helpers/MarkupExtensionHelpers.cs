namespace Devolutions.AvaloniaControls.Helpers;

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Data.Core;

public static class MarkupExtensionHelpers
{
    // Cache the result of IsDynamicCodeSupported check for performance
    private static readonly bool IsDynamicCodeSupported = RuntimeFeature.IsDynamicCodeSupported;

    // Dynamic approach is slightly faster than using reflection, see: https://stackoverflow.com/a/78582306
    // However, we support both for AoT compatibility (auto-detected at runtime)
    public static IBinding? GetBinding<TIn>(object? v)
    {
        switch (v)
        {
            case null:
                return null;
            case IBinding binding:
                return binding;
            case ClrPropertyInfo clrProperty:
                // Support for Avalonia compiled bindings
                return new Binding
                {
                    Mode = BindingMode.OneWay,
                    Source = clrProperty,
                };
            case string str:
            {
                var isParsable = typeof(TIn).GetInterfaces()
                    .Any(static c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IParsable<>));
                if (isParsable)
                {
                    // Runtime detection: Use dynamic if available (fast), otherwise use reflection (AoT-compatible)
                    TIn? value = IsDynamicCodeSupported
                        ? ParseViaDynamic<TIn>(str)     // Fast path for non-AoT
                        : ParseViaReflection<TIn>(str); // AoT-compatible path

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
            case ClrPropertyInfo clrProperty:
                // Support for Avalonia compiled bindings
                return new Binding
                {
                    Mode = BindingMode.OneWay,
                    Source = clrProperty,
                };
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

    // Dynamic path - isolated in separate method to prevent AoT inlining
    // This method is only called when RuntimeFeature.IsDynamicCodeSupported is true
    private static TIn ParseViaDynamic<TIn>(string stringValue)
    {
        return Parse(stringValue, (dynamic)default(TIn)!);
    }

    // AoT-compatible version that uses reflection instead of dynamic
    // This is automatically used when RuntimeFeature.IsDynamicCodeSupported is false (Native AoT)
    private static TIn ParseViaReflection<TIn>(string stringValue)
    {
        // Call the static IParsable<T>.Parse method via reflection
        MethodInfo? parseMethod = typeof(TIn).GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), typeof(IFormatProvider)],
            null);

        if (parseMethod is null)
        {
            throw new InvalidOperationException($"Type {typeof(TIn).Name} does not implement IParsable<T> correctly.");
        }

        object? result = parseMethod.Invoke(null, [stringValue, CultureInfo.InvariantCulture]);
        return result is not null ? (TIn)result : throw new InvalidOperationException($"Parse returned null for {stringValue}");
    }
}