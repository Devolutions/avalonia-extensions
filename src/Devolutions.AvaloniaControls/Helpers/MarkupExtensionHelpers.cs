namespace Devolutions.AvaloniaControls.Helpers;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    public static IBinding? GetBinding<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicMethods)] TIn>(object? v)
    {
        switch (v)
        {
            case null:
                return null;
            case IBinding binding:
                return binding;
            case ClrPropertyInfo clrProperty:
                // Support for Avalonia compiled bindings — wrap the property info as a static value
                return ObservableHelpers.ValueBinding(clrProperty);
            case string str:
            {
                bool isParsable = typeof(TIn).GetInterfaces()
                    .Any(static c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IParsable<>));
                if (isParsable)
                {
                    TIn? value = ParseString<TIn>(str);
                    return ObservableHelpers.ValueBinding(value);
                }

                break;
            }
        }

        if (v is TIn t) return ObservableHelpers.ValueBinding(t);

        return ConvertViaTypeDescriptor<TIn>(v);
    }

    /// <summary>
    /// Resolves a binding for a value whose target type comes from an unannotated source
    /// (e.g. <see cref="Avalonia.AvaloniaProperty.PropertyType"/>).
    /// The suppression is safe because all types in our assemblies are preserved via ILLink.Descriptors.xml.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "AvaloniaProperty.PropertyType returns Type without DynamicallyAccessedMembers. " +
                        "Types are preserved via assembly-level ILLink descriptor (preserve=\"all\").")]
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "AvaloniaProperty.PropertyType returns Type without DynamicallyAccessedMembers. " +
                        "Types are preserved via assembly-level ILLink descriptor (preserve=\"all\").")]
    public static IBinding? GetBindingForPropertyType(object? v, Type type)
    {
        return GetBinding(v, type);
    }

    public static IBinding? GetBinding(object? v, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        // Unwrap Nullable<T> to get the underlying type for IParsable check
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        switch (v)
        {
            case null:
                return null;
            case IBinding binding:
                return binding;
            case ClrPropertyInfo clrProperty:
                // Support for Avalonia compiled bindings — wrap the property info as a static value
                return ObservableHelpers.ValueBinding(clrProperty);
            case string str:
            {
                if (underlyingType == typeof(string))
                {
                    return ObservableHelpers.ValueBinding(str);
                }

                bool isParsable = underlyingType.GetInterfaces()
                    .Any(static c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IParsable<>));
                if (isParsable)
                {
                    MethodInfo? parseMethod = underlyingType.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]);
                    return ObservableHelpers.ValueBinding(parseMethod?.Invoke(null, [str, CultureInfo.InvariantCulture]));
                }

                break;
            }
        }

        return ConvertViaTypeDescriptor(v, underlyingType);
    }

    /// <summary>
    /// Attempts to convert a value to the target type using TypeDescriptor and wraps it in a binding.
    /// Isolated to keep the [RequiresUnreferencedCode] suppression scoped as tightly as possible.
    /// TypeDescriptor.GetConverter is inherently reflection-based but is safe here because the
    /// assembly is preserved via ILLink.Descriptors.xml (preserve="all").
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "TypeDescriptor.GetConverter is used as a last-resort fallback. Types are preserved via assembly-level ILLink descriptor.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "TypeDescriptor.GetConverter is used as a last-resort fallback. Types are preserved via assembly-level ILLink descriptor.")]
    private static IBinding? ConvertViaTypeDescriptor<TIn>(object v)
    {
        return TypeDescriptor.GetConverter(v.GetType()).ConvertTo(v, typeof(TIn)) is TIn t2
            ? ObservableHelpers.ValueBinding(t2)
            : null;
    }

    /// <summary>
    /// Non-generic overload for TypeDescriptor conversion.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "TypeDescriptor.GetConverter is used as a last-resort fallback. Types are preserved via assembly-level ILLink descriptor.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "TypeDescriptor.GetConverter is used as a last-resort fallback. Types are preserved via assembly-level ILLink descriptor.")]
    private static IBinding? ConvertViaTypeDescriptor(object v, Type underlyingType)
    {
        object? t = TypeDescriptor.GetConverter(underlyingType).ConvertFrom(null, CultureInfo.InvariantCulture, v);
        return t is not null
            ? ObservableHelpers.ValueBinding(t)
            : null;
    }

    private static T Parse<T>(string stringValue, T _) where T : IParsable<T> =>
        T.Parse(stringValue, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a string to <typeparamref name="TIn"/> using dynamic dispatch (JIT) or reflection (AOT).
    /// The dynamic/reflection warnings are suppressed because:
    /// - ParseViaDynamic is only called when RuntimeFeature.IsDynamicCodeSupported is true (JIT mode)
    /// - ParseViaReflection uses typeof(TIn).GetMethod which is safe because TIn has [DynamicallyAccessedMembers(PublicMethods)]
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "ParseViaDynamic is guarded by RuntimeFeature.IsDynamicCodeSupported; never called under AOT.")]
    private static TIn ParseString<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TIn>(string str)
    {
        return IsDynamicCodeSupported
            ? ParseViaDynamic<TIn>(str)
            : ParseViaReflection<TIn>(str);
    }

    // Dynamic path - isolated in separate method to prevent AoT inlining
    // This method is only called when RuntimeFeature.IsDynamicCodeSupported is true
    [RequiresDynamicCode("Uses the 'dynamic' keyword which requires runtime code generation.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "ParseViaDynamic is only called when RuntimeFeature.IsDynamicCodeSupported is true (JIT mode). In AOT mode, ParseViaReflection is used instead.")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TIn ParseViaDynamic<TIn>(string stringValue)
    {
        return Parse(stringValue, (dynamic)default(TIn)!);
    }

    // AoT-compatible version that uses reflection instead of dynamic
    // This is automatically used when RuntimeFeature.IsDynamicCodeSupported is false (Native AoT)
    private static TIn ParseViaReflection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TIn>(string stringValue)
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
