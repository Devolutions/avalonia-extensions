namespace Devolutions.AvaloniaControls.Helpers;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Utilities;

public static class MarkupExtensionHelpers
{
    // Cache the result of IsDynamicCodeSupported check for performance
    private static readonly bool IsDynamicCodeSupported = RuntimeFeature.IsDynamicCodeSupported;

    /// <summary>
    /// Resolves a binding for a value whose target type is statically known to be parsable.
    /// <para>
    /// Prefer this over <see cref="GetBinding{TIn}(object?)"/> whenever the caller can satisfy
    /// <c>IParsable&lt;TIn&gt;</c>: the <c>TIn.Parse</c> below is a constrained call that both the JIT and
    /// ILC resolve statically, so parsing a XAML literal costs nothing at startup. The unconstrained
    /// overload has to discover the parse method at runtime instead, and its <c>dynamic</c> path builds a
    /// call site on first use — which loads Microsoft.CSharp and System.Linq.Expressions and emits a
    /// DynamicMethod, on the UI thread, during the first layout pass that touches the template.
    /// </para>
    /// </summary>
    public static BindingBase? GetParsableBinding<TIn>(object? v)
        where TIn : IParsable<TIn>
    {
        switch (v)
        {
            case null:
                return null;
            case BindingBase binding:
                return binding;
            case ClrPropertyInfo clrProperty:
                // Support for Avalonia compiled bindings — wrap the property info as a static value
                return ObservableHelpers.ValueBinding(clrProperty);
            case string str:
                // XAML literals are culture-invariant: "-1" and "0.25" must parse the same way
                // regardless of the thread culture the app happens to be running under.
                return ObservableHelpers.ValueBinding(TIn.Parse(str, CultureInfo.InvariantCulture));
        }

        if (v is TIn t) return ObservableHelpers.ValueBinding(t);

        return ConvertViaTypeDescriptor<TIn>(v);
    }

    // Dynamic approach is slightly faster than using reflection, see: https://stackoverflow.com/a/78582306
    // However, we support both for AoT compatibility (auto-detected at runtime).
    // Callers that can satisfy IParsable<TIn> should use GetParsableBinding<TIn> instead, which needs
    // neither: it reaches the same Parse method through a constrained call.
    public static BindingBase? GetBinding<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicMethods)] TIn>(object? v)
    {
        switch (v)
        {
            case null:
                return null;
            case BindingBase binding:
                return binding;
            case ClrPropertyInfo clrProperty:
                // Support for Avalonia compiled bindings — wrap the property info as a static value
                return ObservableHelpers.ValueBinding(clrProperty);
            case string str:
            {
                var tin = typeof(TIn);
                if (tin == typeof(string))
                {
                    return ObservableHelpers.ValueBinding(str);
                }
                else if (tin == typeof(Thickness))
                {
                    return ObservableHelpers.ValueBinding(Thickness.Parse(str));
                }
                else if (tin == typeof(CornerRadius))
                {
                    return ObservableHelpers.ValueBinding(CornerRadius.Parse(str));
                }
                else if (tin == typeof(GridLength))
                {
                    return ObservableHelpers.ValueBinding(GridLength.Parse(str));
                }
                
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
    public static BindingBase? GetBindingForPropertyType(object? v, Type type)
    {
        return GetBinding(v, type);
    }

    public static BindingBase? GetBinding(object? v, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        // Unwrap Nullable<T> to get the underlying type for IParsable check
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        switch (v)
        {
            case null:
                return null;
            case BindingBase binding:
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
                else if (underlyingType == typeof(Thickness))
                {
                    return ObservableHelpers.ValueBinding(Thickness.Parse(str));
                }
                else if (underlyingType == typeof(CornerRadius))
                {
                    return ObservableHelpers.ValueBinding(CornerRadius.Parse(str));
                }
                else if (underlyingType == typeof(GridLength))
                {
                    return ObservableHelpers.ValueBinding(GridLength.Parse(str));
                }

                // Same primitives as ParseString's fast paths, by Type rather than by closed generic.
                // bool has to be here too: its IParsable<bool>.Parse is an explicit interface
                // implementation, so the GetMethod lookup below returns null and the null-conditional
                // Invoke would silently bind null instead of false/true.
                if (ParsePrimitive(str, underlyingType) is { } primitive)
                {
                    return ObservableHelpers.ValueBinding(primitive);
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
    private static BindingBase? ConvertViaTypeDescriptor<TIn>(object v)
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
    private static BindingBase? ConvertViaTypeDescriptor(object v, Type underlyingType)
    {
        object? t = TypeDescriptor.GetConverter(underlyingType).ConvertFrom(null, CultureInfo.InvariantCulture, v);
        return t is not null
            ? ObservableHelpers.ValueBinding(t)
            : null;
    }

    /// <summary>
    /// Parses the primitives that XAML literals actually use, selected by <see cref="Type"/> rather than by
    /// closed generic. Returns <c>null</c> when <paramref name="type"/> is not one of them, so the caller can
    /// fall through to its own slower lookup. Always invariant: a XAML literal means the same thing in every
    /// culture.
    /// </summary>
    private static object? ParsePrimitive(string str, Type type)
    {
        if (type == typeof(double)) return double.Parse(str, CultureInfo.InvariantCulture);
        if (type == typeof(int)) return int.Parse(str, CultureInfo.InvariantCulture);
        if (type == typeof(bool)) return bool.Parse(str);
        if (type == typeof(float)) return float.Parse(str, CultureInfo.InvariantCulture);
        if (type == typeof(decimal)) return decimal.Parse(str, CultureInfo.InvariantCulture);
        return null;
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
        // Fast paths for the primitives XAML literals actually use. `typeof(TIn) == typeof(X)` folds to a
        // constant per closed generic, so the JIT drops every branch but one and this costs nothing at
        // runtime; it is also AoT-friendly. Without them, the first literal parsed on the JIT would build a
        // `dynamic` call site — ~62ms and 1.5MB on first use, plus Microsoft.CSharp and
        // System.Linq.Expressions loaded on the UI thread.
        //
        // bool is not merely an optimisation here: System.Boolean implements IParsable<bool>.Parse as an
        // *explicit* interface implementation, so the ParseViaReflection lookup below cannot find it and
        // throws. Keep bool on this path.
        if (typeof(TIn) == typeof(double)) return (TIn)(object)double.Parse(str, CultureInfo.InvariantCulture);
        if (typeof(TIn) == typeof(int)) return (TIn)(object)int.Parse(str, CultureInfo.InvariantCulture);
        if (typeof(TIn) == typeof(bool)) return (TIn)(object)bool.Parse(str);
        if (typeof(TIn) == typeof(float)) return (TIn)(object)float.Parse(str, CultureInfo.InvariantCulture);
        if (typeof(TIn) == typeof(decimal)) return (TIn)(object)decimal.Parse(str, CultureInfo.InvariantCulture);

        // Anything else keeps the original fallback chain.
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
