namespace Devolutions.AvaloniaControls.Helpers;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;

[RequiresUnreferencedCode("BindingEvaluator required preserved types")]
public sealed class BindingEvaluator<TDataContext>
{
    private readonly BindingEvaluator inner;

    public BindingEvaluator(StyledElement anchor)
    {
        this.inner = new BindingEvaluator(anchor, typeof(TDataContext));
    }

    public Expression<Func<TDataContext, string>>? BuildFormattedGetterExpression(IBinding? binding)
    {
        return this.inner.BuildFormattedGetterExpression<TDataContext>(binding);
    }

    public Expression<Func<TDataContext, object?>> BuildRawGetterExpression(IBinding binding)
    {
        return this.inner.BuildRawGetterExpression<TDataContext>(binding);
    }
}

[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
public sealed partial class BindingEvaluator
{
    private static readonly MethodInfo objectToStringMethod = typeof(object).GetMethod(nameof(ToString))!;

    private static readonly Dictionary<string, Type?> resolvedTypeCache = new(StringComparer.Ordinal);

    private Dictionary<string, ParentPathRewrite?>? parentPathRewriteCache;

    private Dictionary<Type, object?>? resolvedAncestorCache;

    private readonly StyledElement anchor;

    private readonly Type dataContextType;

    public BindingEvaluator(StyledElement anchor, Type dataContextType)
    {
        this.anchor = anchor;
        this.dataContextType = dataContextType;
    }

    public static BindingEvaluator? FromItemsControl(ItemsControl anchor)
    {
        if (GetTypeFromItemsSource(anchor) is { } dataContextType)
        {
            return new BindingEvaluator(anchor, dataContextType);
        }

        return null;
    }

    public static Type? GetTypeFromItemsSource(ItemsControl itemsControl)
        => itemsControl.ItemsSource?.GetType() is { } itemsSourceType
            ? GetTypeFromItemsSource(itemsSourceType)
            : null;

    public static Type? GetTypeFromItemsSource(Type itemsType)
        => itemsType
            .GetInterfaces()
            .FirstOrDefault(static iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];

    public Func<object, string>? BuildFormattedGetter(IBinding? binding)
    {
        if (binding is null)
        {
            return null;
        }

        if (this.TryGetSimpleBindingGetter(binding, out Func<object, string>? getter))
        {
            return getter;
        }

        if (this.TryBuildIntermediateGetter(binding, out getter))
        {
            return getter;
        }

        return this.BuildFrameworkDelegatedGetter(binding);
    }

    public Expression<Func<TDataContext, string>>? BuildFormattedGetterExpression<TDataContext>(IBinding? binding)
    {
        if (binding is null)
        {
            return null;
        }

        if (TryGetSimpleBindingExpression(binding, out Expression<Func<TDataContext, string>>? expression))
        {
            return expression;
        }

        if (TryBuildIntermediateExpression(binding, out expression))
        {
            return expression;
        }

        return this.BuildFrameworkDelegatedGetterExpression<TDataContext>(binding);
    }

    public Func<object, object?> BuildRawGetter(IBinding binding)
    {
        if (this.TryBuildFastPathRawGetter(binding, out Func<object, object?>? getter))
        {
            return getter;
        }

        return this.BuildProxyRawGetter(binding);
    }

    public Expression<Func<TDataContext, object?>> BuildRawGetterExpression<TDataContext>(IBinding binding)
    {
        if (TryBuildFastPathRawExpression(binding, out Expression<Func<TDataContext, object?>>? expression))
        {
            return expression;
        }

        return this.BuildProxyRawGetterExpression<TDataContext>(binding);
    }

    private static Expression<Func<TDataContext, string>>? BuildFastPathExpression<TDataContext>(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        ParameterExpression rowParameter = Expression.Parameter(typeof(TDataContext), "row");
        Expression propertyAccess = rowParameter;

        try
        {
            foreach (string propertyName in path.Split('.'))
            {
                propertyAccess = Expression.PropertyOrField(propertyAccess, propertyName);
            }
        }
        catch
        {
            return null;
        }

        if (propertyAccess.Type == typeof(string))
        {
            Expression body = Expression.Coalesce(propertyAccess, Expression.Constant(string.Empty));
            return Expression.Lambda<Func<TDataContext, string>>(body, rowParameter);
        }

        Expression toStringExpr = ToStringExpression(propertyAccess);
        Expression<Func<TDataContext, string>> innerLambda = Expression.Lambda<Func<TDataContext, string>>(toStringExpr, rowParameter);
        Func<TDataContext, string?> compiledGetter = innerLambda.Compile();

        ParameterExpression outerParam = Expression.Parameter(typeof(TDataContext), "row");
        Expression outerBody = Expression.Invoke(Expression.Constant(compiledGetter), outerParam);

        return Expression.Lambda<Func<TDataContext, string>>(outerBody, outerParam);
    }

    private static Expression<Func<TDataContext, object?>>? BuildFastPathRawExpression<TDataContext>(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        ParameterExpression rowParameter = Expression.Parameter(typeof(TDataContext), "row");
        Expression propertyAccess = rowParameter;

        try
        {
            foreach (string propertyName in path.Split('.'))
            {
                propertyAccess = Expression.PropertyOrField(propertyAccess, propertyName);
            }
        }
        catch
        {
            return null;
        }

        Expression body = propertyAccess.Type.IsValueType ? Expression.Convert(propertyAccess, typeof(object)) : propertyAccess;
        return Expression.Lambda<Func<TDataContext, object?>>(body, rowParameter);
    }

    private static Func<object, object?> BuildPropertyGetter(string path, Type rootType)
    {
        ParameterExpression param = Expression.Parameter(typeof(object), "root");
        Expression access = Expression.Convert(param, rootType);

        foreach (string propertyName in path.Split('.'))
        {
            access = Expression.PropertyOrField(access, propertyName);
        }

        if (access.Type.IsValueType)
        {
            access = Expression.Convert(access, typeof(object));
        }

        return Expression.Lambda<Func<object, object?>>(access, param).Compile();
    }

    [GeneratedRegex(@"\(\(([A-Za-z_][\w.:]*)\)([A-Za-z_]\w*)\)")]
    private static partial Regex CompiledCastRegex();

    private static StyledElement? FindLogicalAncestorOfType(StyledElement start, Type ancestorType)
    {
        StyledElement? current = start;
        while (current is not null)
        {
            if (ancestorType.IsInstanceOfType(current))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private static Type? GetOrResolveType(string typeName)
    {
        if (resolvedTypeCache.TryGetValue(typeName, out Type? cached))
        {
            return cached;
        }

        Type? resolved = ResolveType(typeName);
        resolvedTypeCache[typeName] = resolved;
        return resolved;
    }

    private static string? GetSimplePathWithoutExtras(IBinding binding)
    {
        string? pathString = binding switch
            {
                Binding { Path: { Length: > 0 } path } b when b.Converter is null
                    && b.StringFormat is null
                    && b.FallbackValue == AvaloniaProperty.UnsetValue
                    && b.TargetNullValue == AvaloniaProperty.UnsetValue
                    && b.Source == AvaloniaProperty.UnsetValue
                    && string.IsNullOrEmpty(b.ElementName)
                    && b.RelativeSource is null => path,
                CompiledBindingExtension c when c.Converter is null
                    && c.StringFormat is null
                    && c.FallbackValue == AvaloniaProperty.UnsetValue
                    && c.TargetNullValue == AvaloniaProperty.UnsetValue
                    && c.Source == AvaloniaProperty.UnsetValue => c.Path.ToString(),
                _ => null,
            };

        return !string.IsNullOrEmpty(pathString) && IsSimpleDotPath(pathString) ? pathString : null;
    }

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

    [GeneratedRegex(@"^\$parent\[([A-Za-z_][\w.:]*)\]\.")]
    private static partial Regex ParentPrefixRegex();

    private static Type? ResolveType(string typeName)
    {
        Type? type = Type.GetType(typeName);
        if (type is not null)
        {
            return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type candidate in assembly.GetTypes())
            {
                if (candidate.Name == typeName || candidate.FullName == typeName)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static Expression ToStringExpression(Expression value)
    {
        if (value.Type == typeof(string))
        {
            return Expression.Coalesce(value, Expression.Constant(string.Empty));
        }

        if (value.Type.IsValueType)
        {
            return Expression.Call(value, objectToStringMethod);
        }

        Expression toString = Expression.Call(value, objectToStringMethod);
        Expression toStringOrEmpty = Expression.Coalesce(toString, Expression.Constant(string.Empty));
        return Expression.Condition(Expression.Equal(value, Expression.Constant(null, value.Type)), Expression.Constant(string.Empty), toStringOrEmpty);
    }

    private static bool TryBuildFastPathRawExpression<TDataContext>(IBinding binding, [NotNullWhen(true)] out Expression<Func<TDataContext, object?>>? expression)
    {
        string? pathString = GetSimplePathWithoutExtras(binding);
        if (string.IsNullOrEmpty(pathString))
        {
            expression = null;
            return false;
        }

        expression = BuildFastPathRawExpression<TDataContext>(pathString);
        return expression is not null;
    }

    private bool TryBuildFastPathRawGetter(IBinding binding, [NotNullWhen(true)] out Func<object, object?>? getter)
    {
        string? pathString = GetSimplePathWithoutExtras(binding);
        if (string.IsNullOrEmpty(pathString))
        {
            getter = null;
            return false;
        }

        try
        {
            getter = BuildPropertyGetter(pathString, this.dataContextType);
            return true;
        }
        catch
        {
            getter = null;
            return false;
        }
    }

    private static bool TryBuildIntermediateExpression<TDataContext>(IBinding binding, [NotNullWhen(true)] out Expression<Func<TDataContext, string>>? expression)
    {
        if (!TryGetIntermediateParts(binding, out string? path, out object? source, out IValueConverter? converter, out object? converterParameter,
                out CultureInfo? converterCulture, out string? stringFormat, out object? fallbackValue, out object? targetNullValue))
        {
            expression = null;
            return false;
        }

        Func<object, string> getter = BuildIntermediateGetter(path, source, converter, converterParameter, converterCulture, stringFormat, fallbackValue, targetNullValue, typeof(TDataContext));
        expression = WrapObjectDelegateAsExpression<TDataContext>(getter);
        return true;
    }

    private bool TryBuildIntermediateGetter(IBinding binding, [NotNullWhen(true)] out Func<object, string>? getter)
    {
        if (!TryGetIntermediateParts(binding, out string? path, out object? source, out IValueConverter? converter, out object? converterParameter,
                out CultureInfo? converterCulture, out string? stringFormat, out object? fallbackValue, out object? targetNullValue))
        {
            getter = null;
            return false;
        }

        getter = BuildIntermediateGetter(path, source, converter, converterParameter, converterCulture, stringFormat, fallbackValue, targetNullValue, this.dataContextType);
        return true;
    }

    private static Func<object, string> BuildIntermediateGetter(
        string path,
        object? source,
        IValueConverter? converter,
        object? converterParameter,
        CultureInfo? converterCulture,
        string? stringFormat,
        object? fallbackValue,
        object? targetNullValue,
        Type rowType)
    {
        Type rootType = source is not null ? source.GetType() : rowType;
        Func<object, object?> propertyGetter = BuildPropertyGetter(path, rootType);
        CultureInfo culture = converterCulture ?? CultureInfo.CurrentCulture;
        string? fallbackString = fallbackValue?.ToString();
        string? targetNullString = targetNullValue?.ToString();

        return row =>
            {
                try
                {
                    object? value = propertyGetter(source ?? row);

                    if (converter is not null)
                    {
                        value = converter.Convert(value, typeof(string), converterParameter, culture);
                    }

                    if (value is null && targetNullString is not null)
                    {
                        return targetNullString;
                    }

                    if (stringFormat is not null)
                    {
                        return string.Format(culture, stringFormat, value);
                    }

                    return value as string ?? value?.ToString() ?? string.Empty;
                }
                catch
                {
                    return fallbackString ?? string.Empty;
                }
            };
    }

    private static bool TryGetIntermediateParts(
        IBinding binding,
        [NotNullWhen(true)] out string? path,
        out object? source,
        out IValueConverter? converter,
        out object? converterParameter,
        out CultureInfo? converterCulture,
        out string? stringFormat,
        out object? fallbackValue,
        out object? targetNullValue)
    {
        switch (binding)
        {
            case Binding { Path: { Length: > 0 } p } b when string.IsNullOrEmpty(b.ElementName) && b.RelativeSource is null && IsSimpleDotPath(p):
                path = p;
                source = b.Source == AvaloniaProperty.UnsetValue ? null : b.Source;
                converter = b.Converter;
                converterParameter = b.ConverterParameter;
                converterCulture = b.ConverterCulture;
                stringFormat = b.StringFormat;
                fallbackValue = b.FallbackValue == AvaloniaProperty.UnsetValue ? null : b.FallbackValue;
                targetNullValue = b.TargetNullValue == AvaloniaProperty.UnsetValue ? null : b.TargetNullValue;
                return true;

            case CompiledBindingExtension c:
                string pathString = c.Path.ToString();
                if (pathString.Length == 0 || !IsSimpleDotPath(pathString))
                {
                    break;
                }

                path = pathString;
                source = c.Source == AvaloniaProperty.UnsetValue ? null : c.Source;
                converter = c.Converter;
                converterParameter = c.ConverterParameter;
                converterCulture = c.ConverterCulture;
                stringFormat = c.StringFormat;
                fallbackValue = c.FallbackValue == AvaloniaProperty.UnsetValue ? null : c.FallbackValue;
                targetNullValue = c.TargetNullValue == AvaloniaProperty.UnsetValue ? null : c.TargetNullValue;
                return true;
        }

        path = null;
        source = null;
        converter = null;
        converterParameter = null;
        converterCulture = null;
        stringFormat = null;
        fallbackValue = null;
        targetNullValue = null;
        return false;
    }

    private static bool TryGetSimpleBindingExpression<TDataContext>(IBinding binding, [NotNullWhen(true)] out Expression<Func<TDataContext, string>>? expression)
    {
        string? pathString = GetSimplePathWithoutExtras(binding);
        if (!string.IsNullOrEmpty(pathString))
        {
            expression = BuildFastPathExpression<TDataContext>(pathString);
            if (expression is not null)
            {
                return true;
            }
        }

        expression = null;
        return false;
    }

    private bool TryGetSimpleBindingGetter(IBinding binding, [NotNullWhen(true)] out Func<object, string>? getter)
    {
        string? pathString = GetSimplePathWithoutExtras(binding);
        if (string.IsNullOrEmpty(pathString))
        {
            getter = null;
            return false;
        }

        try
        {
            Func<object, object?> rawGetter = BuildPropertyGetter(pathString, this.dataContextType);
            getter = row => rawGetter(row) is { } value ? value as string ?? value.ToString() ?? string.Empty : string.Empty;
            return true;
        }
        catch
        {
            getter = null;
            return false;
        }
    }

    private static Expression<Func<TDataContext, string>> WrapObjectDelegateAsExpression<TDataContext>(Func<object, string> getter)
    {
        ParameterExpression rowParameter = Expression.Parameter(typeof(TDataContext), "row");
        Expression body = Expression.Invoke(Expression.Constant(getter), Expression.Convert(rowParameter, typeof(object)));
        return Expression.Lambda<Func<TDataContext, string>>(body, rowParameter);
    }

    private Func<object, string> BuildFrameworkDelegatedGetter(IBinding binding)
    {
        binding = this.RewriteParentBindingIfNeeded(binding);
        BindingEvaluatorProxyElement proxy = new(this.anchor, binding);

        return row =>
            {
                try
                {
                    object? value = proxy.Evaluate(row);
                    return value as string ?? value?.ToString() ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            };
    }

    [RequiresUnreferencedCode("Types used in reflection bindings will need to be excluded from trimming")]
    private Expression<Func<TDataContext, string>> BuildFrameworkDelegatedGetterExpression<TDataContext>(IBinding binding)
    {
        Func<object, string> getter = this.BuildFrameworkDelegatedGetter(binding);
        return WrapObjectDelegateAsExpression<TDataContext>(getter);
    }

    [RequiresUnreferencedCode("Types used in reflection bindings will need to be excluded from trimming")]
    private Func<object, object?> BuildProxyRawGetter(IBinding binding)
    {
        binding = this.RewriteParentBindingIfNeeded(binding);
        BindingEvaluatorProxyElement proxy = new(this.anchor, binding);

        return row =>
            {
                try
                {
                    return proxy.Evaluate(row);
                }
                catch
                {
                    return null;
                }
            };
    }

    private Expression<Func<TDataContext, object?>> BuildProxyRawGetterExpression<TDataContext>(IBinding binding)
    {
        Func<object, object?> getter = this.BuildProxyRawGetter(binding);
        ParameterExpression rowParameter = Expression.Parameter(typeof(TDataContext), "row");
        Expression body = Expression.Invoke(Expression.Constant(getter), Expression.Convert(rowParameter, typeof(object)));
        return Expression.Lambda<Func<TDataContext, object?>>(body, rowParameter);
    }

    private ParentPathRewrite? GetOrCreatePathRewrite(string rawPath)
    {
        this.parentPathRewriteCache ??= new(StringComparer.Ordinal);

        if (this.parentPathRewriteCache.TryGetValue(rawPath, out ParentPathRewrite? cached))
        {
            return cached;
        }

        Match match = ParentPrefixRegex().Match(rawPath);
        if (!match.Success)
        {
            this.parentPathRewriteCache[rawPath] = null;
            return null;
        }

        string typeString = match.Groups[1].Value;
        string remainingPath = rawPath[match.Length..];
        remainingPath = CompiledCastRegex().Replace(remainingPath, "$2");

        if (GetOrResolveType(typeString) is not Type ancestorType)
        {
            this.parentPathRewriteCache[rawPath] = null;
            return null;
        }

        ParentPathRewrite rewrite = new(remainingPath, ancestorType);
        this.parentPathRewriteCache[rawPath] = rewrite;
        return rewrite;
    }

    private object? GetOrFindAncestor(Type ancestorType)
    {
        this.resolvedAncestorCache ??= [];

        if (this.resolvedAncestorCache.TryGetValue(ancestorType, out object? cached))
        {
            return cached;
        }

        object? ancestor = FindLogicalAncestorOfType(this.anchor, ancestorType);
        this.resolvedAncestorCache[ancestorType] = ancestor;
        return ancestor;
    }

    private IBinding RewriteParentBindingIfNeeded(IBinding binding)
    {
        if (binding is not Binding { Path: { Length: > 0 } path } reflectionBinding)
        {
            return binding;
        }

        if (this.GetOrCreatePathRewrite(path) is not ParentPathRewrite rewrite || this.GetOrFindAncestor(rewrite.AncestorType) is not object ancestor)
        {
            return binding;
        }

        return new Binding(rewrite.CleanedPath)
            {
                Source = ancestor,
                Converter = reflectionBinding.Converter,
                ConverterParameter = reflectionBinding.ConverterParameter,
                ConverterCulture = reflectionBinding.ConverterCulture,
                StringFormat = reflectionBinding.StringFormat,
                FallbackValue = reflectionBinding.FallbackValue,
                TargetNullValue = reflectionBinding.TargetNullValue,
                Mode = reflectionBinding.Mode,
            };
    }

    private sealed record ParentPathRewrite(string CleanedPath, Type AncestorType);
    
    // TODO: Somehow cascade this IDisposable outward in order to properly
    //       dispose it after usage and possibly prevent some memory leaks
    internal sealed class BindingEvaluatorProxyElement : StyledElement, IDisposable
    {
        public static readonly DirectProperty<BindingEvaluatorProxyElement, object?> ValueProperty =
            AvaloniaProperty.RegisterDirect<BindingEvaluatorProxyElement, object?>(nameof(Value), static o => o.value, static (o, v) => o.value = v);

        private object? value;

        public object? Value
        {
            get => this.value;
            set => this.SetAndRaise(ValueProperty, ref this.value, value);
        }

        private readonly IDisposable valueSubscription;

        public BindingEvaluatorProxyElement(ILogical anchor, IBinding binding)
        {
            ((ISetLogicalParent)this).SetParent(anchor);
#pragma warning disable CS0618 // Type or member is obsolete
            this.valueSubscription = this.Bind(ValueProperty, binding, anchor: anchor);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public object? Evaluate(object dataContext)
        {
            this.DataContext = dataContext;
            var val = this.Value;
            this.DataContext = null;
            return val;
        }

        public void Dispose()
        {
            this.valueSubscription.Dispose();
            ((ISetLogicalParent)this).SetParent(null);
        }
    }
}
