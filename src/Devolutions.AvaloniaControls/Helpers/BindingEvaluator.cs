namespace Devolutions.AvaloniaControls.Helpers;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;

/// <summary>
/// Evaluates an <see cref="IBinding"/> against arbitrary source objects that are not
/// part of the logical tree (e.g., items in a custom virtualized list, group keys, etc.).
/// <para>
/// Compiled bindings authored in XAML (<c>{Binding Foo}</c> with <c>x:DataType</c>) capture
/// their source at instantiation time relative to the surrounding XAML scope, so the
/// naive approach of swapping <c>DataContext</c> on a hidden target does not redirect
/// them to a different source. <see cref="BindingEvaluator"/> works around this with a
/// three-tier resolution strategy, mirroring the approach used in
/// <c>Devolutions.AvaloniaUI.Controls.DevoDataGrid</c>:
/// </para>
/// <list type="number">
///   <item><b>Fast path</b> – simple DataContext-relative dot-path with no extras.
///     Compiles a property-path getter against the source's runtime type. Pure expression
///     tree; no per-call allocation.</item>
///   <item><b>Intermediate path</b> – simple dot-path with extras (Converter, StringFormat,
///     FallbackValue, TargetNullValue, explicit Source). Compiles the path getter once and
///     wraps it in a closure that applies the extras.</item>
///   <item><b>Framework-delegated fallback</b> – tree-relative bindings (<c>ElementName</c>,
///     <c>RelativeSource</c>, <c>$parent</c>) and non-trivial compiled-binding paths are
///     resolved by Avalonia's own pipeline through a <see cref="BindingProxyElement"/>
///     logically parented to the supplied <c>host</c>. The binding is attached once;
///     per-call the proxy's <c>DataContext</c> is swapped and the resolved value is read
///     synchronously via a <see cref="DirectProperty{TOwner,TValue}"/>.</item>
/// </list>
/// </summary>
public sealed class BindingEvaluator
{
    private readonly IBinding binding;
    private readonly Control host;

    // Fast/intermediate path state: compiled getter bound to a specific root type.
    private Type? resolverRootType;
    private Func<object, object?>? resolver;

    // Fallback path state.
    private BindingProxyElement? proxy;
    private bool fallbackInitialized;

    public BindingEvaluator(IBinding binding, Control host)
    {
        this.binding = binding ?? throw new ArgumentNullException(nameof(binding));
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <summary>
    /// Resolves the binding against <paramref name="source"/> and returns the raw value
    /// (boxed when a value type).
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2026",
        Justification = "Expression-based fast paths are guarded with reflection-friendly fallbacks; types referenced in XAML are preserved.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Expression.Compile is used in trim-safe contexts; falls back to proxy resolution otherwise.")]
    public object? Evaluate(object? source)
    {
        if (source is null)
        {
            return null;
        }

        // Try fast/intermediate path: compile a getter for source.GetType() lazily.
        Type sourceType = source.GetType();
        if (this.resolver is null || this.resolverRootType != sourceType)
        {
            this.resolver = TryBuildCompiledResolver(this.binding, sourceType);
            this.resolverRootType = sourceType;
        }

        if (this.resolver is not null)
        {
            try
            {
                return this.resolver(source);
            }
            catch
            {
                // Fall through to proxy fallback.
            }
        }

        return this.EvaluateViaProxy(source);
    }

    /// <summary>
    /// Resolves the binding and converts the result to <typeparamref name="T"/>.
    /// <para>
    /// - If the raw value is already a <typeparamref name="T"/>, it is returned as-is.<br/>
    /// - If <typeparamref name="T"/> is <see cref="string"/>, falls back to <c>ToString()</c>.<br/>
    /// - If the raw value implements <see cref="IConvertible"/>, uses <see cref="Convert.ChangeType(object,Type,IFormatProvider)"/>.<br/>
    /// - Otherwise returns <paramref name="defaultValue"/>.
    /// </para>
    /// </summary>
    public T? EvaluateAs<T>(object? source, T? defaultValue = default)
    {
        object? value = this.Evaluate(source);

        if (value is T typed)
        {
            return typed;
        }

        if (typeof(T) == typeof(string))
        {
            return (T?)(object?)(value?.ToString());
        }

        if (value is IConvertible convertible)
        {
            try
            {
                return (T?)Convert.ChangeType(convertible, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                // Ignored — fall through to default.
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Convenience wrapper for the most common case: resolve and coerce to <see cref="string"/>.
    /// Returns <see cref="string.Empty"/> when the result is null.
    /// </summary>
    public string EvaluateAsString(object? source) =>
        this.Evaluate(source)?.ToString() ?? string.Empty;

    // ──────────────────────────────────────────────────────────────────────
    //  Compiled-resolver builder (fast + intermediate path)
    // ──────────────────────────────────────────────────────────────────────

    [RequiresUnreferencedCode("Expression.PropertyOrField requires preserved type members at runtime.")]
    private static Func<object, object?>? TryBuildCompiledResolver(IBinding binding, Type sourceType)
    {
        if (!TryExtractSimpleBinding(binding, out SimpleBindingDescriptor descriptor))
        {
            return null;
        }

        // If the binding has an explicit Source, the path is rooted there; otherwise on source.
        Type rootType = descriptor.Source is not null ? descriptor.Source.GetType() : sourceType;

        Func<object, object?> pathGetter;
        try
        {
            pathGetter = BuildPropertyGetter(descriptor.Path, rootType);
        }
        catch
        {
            return null;
        }

        // Fast path: no extras — bare path getter.
        if (descriptor.IsBare)
        {
            return descriptor.Source is { } src
                ? _ => pathGetter(src)
                : pathGetter;
        }

        // Intermediate path: capture extras in a closure.
        IValueConverter? converter = descriptor.Converter;
        object? converterParameter = descriptor.ConverterParameter;
        CultureInfo culture = descriptor.ConverterCulture ?? CultureInfo.CurrentCulture;
        string? stringFormat = descriptor.StringFormat;
        object? fallbackValue = descriptor.FallbackValue;
        object? targetNullValue = descriptor.TargetNullValue;
        object? sourceOverride = descriptor.Source;

        return root =>
        {
            try
            {
                object? value = pathGetter(sourceOverride ?? root);

                if (converter is not null)
                {
                    value = converter.Convert(value, typeof(object), converterParameter, culture);
                }

                if (value is null && targetNullValue is not null)
                {
                    value = targetNullValue;
                }

                if (stringFormat is not null)
                {
                    return string.Format(culture, stringFormat, value);
                }

                return value;
            }
            catch
            {
                return fallbackValue;
            }
        };
    }

    /// <summary>
    /// Compiles a dot-separated property path into a <c>Func&lt;object, object?&gt;</c>
    /// that walks the path from the root object and returns the leaf value (boxed when a value type).
    /// </summary>
    [RequiresUnreferencedCode("Expression.PropertyOrField requires preserved type members at runtime.")]
    private static Func<object, object?> BuildPropertyGetter(string path, Type rootType)
    {
        ParameterExpression param = Expression.Parameter(typeof(object), "root");
        Expression access = Expression.Convert(param, rootType);

        foreach (string propertyName in path.Split('.'))
        {
            access = Expression.PropertyOrField(access, propertyName);
        }

        // Box value types to object.
        if (access.Type.IsValueType)
        {
            access = Expression.Convert(access, typeof(object));
        }

        return Expression.Lambda<Func<object, object?>>(access, param).Compile();
    }

    /// <summary>
    /// Returns <see langword="true"/> if the binding is a reflection <see cref="Binding"/>
    /// or a <see cref="CompiledBindingExtension"/> with a simple dot-path. Captures any
    /// supported extras in <paramref name="descriptor"/>.
    /// </summary>
    private static bool TryExtractSimpleBinding(IBinding binding, out SimpleBindingDescriptor descriptor)
    {
        switch (binding)
        {
            case Binding b
                when !string.IsNullOrEmpty(b.Path)
                    && string.IsNullOrEmpty(b.ElementName)
                    && b.RelativeSource is null
                    && IsSimpleDotPath(b.Path):
            {
                descriptor = new SimpleBindingDescriptor(
                    Path: b.Path,
                    Source: b.Source == AvaloniaProperty.UnsetValue ? null : b.Source,
                    Converter: b.Converter,
                    ConverterParameter: b.ConverterParameter,
                    ConverterCulture: b.ConverterCulture,
                    StringFormat: b.StringFormat,
                    FallbackValue: b.FallbackValue == AvaloniaProperty.UnsetValue ? null : b.FallbackValue,
                    TargetNullValue: b.TargetNullValue == AvaloniaProperty.UnsetValue ? null : b.TargetNullValue);
                return true;
            }

            case CompiledBindingExtension c:
            {
                string pathString = c.Path.ToString();
                if (string.IsNullOrEmpty(pathString) || !IsSimpleDotPath(pathString))
                {
                    descriptor = default;
                    return false;
                }

                descriptor = new SimpleBindingDescriptor(
                    Path: pathString,
                    Source: c.Source == AvaloniaProperty.UnsetValue ? null : c.Source,
                    Converter: c.Converter,
                    ConverterParameter: c.ConverterParameter,
                    ConverterCulture: c.ConverterCulture,
                    StringFormat: c.StringFormat,
                    FallbackValue: c.FallbackValue == AvaloniaProperty.UnsetValue ? null : c.FallbackValue,
                    TargetNullValue: c.TargetNullValue == AvaloniaProperty.UnsetValue ? null : c.TargetNullValue);
                return true;
            }

            default:
                descriptor = default;
                return false;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the string is a simple dot-separated property path
    /// containing only letters, digits, underscores, and dots (e.g. <c>Foo.Bar.Baz</c>).
    /// </summary>
    private static bool IsSimpleDotPath(string path)
    {
        foreach (char c in path)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Framework-delegated fallback (proxy)
    // ──────────────────────────────────────────────────────────────────────

    private object? EvaluateViaProxy(object source)
    {
        if (!this.fallbackInitialized)
        {
            this.proxy = new BindingProxyElement();
            ((ISetLogicalParent)this.proxy).SetParent(this.host);

#pragma warning disable CS0618 // Bind overload with anchor parameter is required for tree-relative bindings
            this.proxy.Bind(BindingProxyElement.ValueProperty, this.binding, anchor: this.host);
#pragma warning restore CS0618

            this.fallbackInitialized = true;
        }

        try
        {
            this.proxy!.DataContext = source;
            return this.proxy.Value;
        }
        catch
        {
            return null;
        }
    }

    private readonly record struct SimpleBindingDescriptor(
        string Path,
        object? Source,
        IValueConverter? Converter,
        object? ConverterParameter,
        CultureInfo? ConverterCulture,
        string? StringFormat,
        object? FallbackValue,
        object? TargetNullValue)
    {
        /// <summary>True when the binding has no extras (pure DataContext-relative path).</summary>
        public bool IsBare =>
            this.Source is null
            && this.Converter is null
            && this.ConverterParameter is null
            && this.StringFormat is null
            && this.FallbackValue is null
            && this.TargetNullValue is null;
    }

    /// <summary>
    /// Minimal binding proxy: the lightest possible <see cref="StyledElement"/> subclass that
    /// exposes a single <see cref="DirectProperty{TOwner,TValue}"/> backed by a plain field.
    /// </summary>
    private sealed class BindingProxyElement : StyledElement
    {
        public static readonly DirectProperty<BindingProxyElement, object?> ValueProperty =
            AvaloniaProperty.RegisterDirect<BindingProxyElement, object?>(
                nameof(Value),
                static o => o.value,
                static (o, v) => o.value = v);

        private object? value;

        public object? Value
        {
            get => this.value;
            set => this.SetAndRaise(ValueProperty, ref this.value, value);
        }
    }
}
