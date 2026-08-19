// ReSharper disable MergeIntoPattern
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

[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator require preserved types")]
public sealed class BindingEvaluator<TDataContext>
{
    private readonly BindingEvaluator inner;

    public BindingEvaluator(StyledElement anchor)
    {
        this.inner = new BindingEvaluator(anchor, typeof(TDataContext));
    }

    public Expression<Func<TDataContext, string>>? BuildFormattedGetterExpression(BindingBase? binding)
    {
        return this.inner.BuildFormattedGetterExpression<TDataContext>(binding);
    }

    public Expression<Func<TDataContext, object?>> BuildRawGetterExpression(BindingBase binding)
    {
        return this.inner.BuildRawGetterExpression<TDataContext>(binding);
    }
}

[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator require preserved types")]
public sealed partial class BindingEvaluator
{
    private static readonly MethodInfo objectToStringMethod = typeof(object).GetMethod(nameof(ToString))!;

    private static readonly Dictionary<string, Type?> resolvedTypeCache = new(StringComparer.Ordinal);

    private Dictionary<string, ParentPathRewrite?>? parentPathRewriteCache;

    private Dictionary<Type, object?>? resolvedAncestorCache;

    private readonly StyledElement anchor;

    private readonly Type dataContextType;
    
    [GeneratedRegex(@"\(\(([A-Za-z_][\w.:]*)\)([A-Za-z_]\w*)\)")]
    private static partial Regex CompiledCastRegex();
    
    [GeneratedRegex(@"^\$parent\[([A-Za-z_][\w.:]*)\]\.")]
    private static partial Regex ParentPrefixRegex();

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

    public Func<object, string>? BuildFormattedGetter(BindingBase? binding)
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

    public Expression<Func<TDataContext, string>>? BuildFormattedGetterExpression<TDataContext>(BindingBase? binding)
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

    public Func<object, object?> BuildRawGetter(BindingBase binding)
    {
        if (this.TryBuildFastPathRawGetter(binding, out Func<object, object?>? getter))
        {
            return getter;
        }

        if (this.TryBuildIntermediateRawGetter(binding, out getter))
        {
            return getter;
        }

        return this.BuildProxyRawGetter(binding);
    }

    public Expression<Func<TDataContext, object?>> BuildRawGetterExpression<TDataContext>(BindingBase binding)
    {
        if (TryBuildFastPathRawExpression(binding, out Expression<Func<TDataContext, object?>>? expression))
        {
            return expression;
        }

        if (TryBuildIntermediateRawExpression(binding, out expression))
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
        Expression propertyAccess;

        try
        {
            propertyAccess = BuildNullSafePropertyAccess(rowParameter, path);
        }
        catch
        {
            return null;
        }

        ParameterExpression value = Expression.Variable(typeof(object), "value");
        Expression missingValue = Expression.OrElse(
            Expression.ReferenceEqual(value, Expression.Constant(null)),
            Expression.ReferenceEqual(value, Expression.Constant(AvaloniaProperty.UnsetValue)));
        Expression valueAsString = Expression.Coalesce(
            Expression.Call(value, objectToStringMethod),
            Expression.Constant(string.Empty));
        Expression body = Expression.Block(
            [value],
            Expression.Assign(value, propertyAccess),
            Expression.Condition(missingValue, Expression.Constant(string.Empty), valueAsString));
        return Expression.Lambda<Func<TDataContext, string>>(body, rowParameter);
    }

    private static Expression<Func<TDataContext, object?>>? BuildFastPathRawExpression<TDataContext>(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        ParameterExpression rowParameter = Expression.Parameter(typeof(TDataContext), "row");
        Expression body;

        try
        {
            body = BuildNullSafePropertyAccess(rowParameter, path);
        }
        catch
        {
            return null;
        }

        return Expression.Lambda<Func<TDataContext, object?>>(body, rowParameter);
    }

    private static Func<object, object?> BuildPropertyGetter(string path, Type rootType)
    {
        ParameterExpression param = Expression.Parameter(typeof(object), "root");
        Expression access = BuildNullSafePropertyAccess(Expression.Convert(param, rootType), path);

        return Expression.Lambda<Func<object, object?>>(access, param).Compile();
    }

    private static Expression BuildNullSafePropertyAccess(Expression root, string path) =>
        BuildNullSafePropertyAccess(root, path.Split('.'), 0);

    private static Expression BuildNullSafePropertyAccess(Expression receiver, string[] propertyNames, int index)
    {
        while (true)
        {
            if (index == propertyNames.Length)
            {
                return Expression.Convert(receiver, typeof(object));
            }

            if (receiver.Type.IsValueType && Nullable.GetUnderlyingType(receiver.Type) is null)
            {
                receiver = Expression.PropertyOrField(receiver, propertyNames[index++]);
                continue;
            }

            ParameterExpression receiverValue = Expression.Variable(receiver.Type, $"segment{index}");
            return Expression.Block([receiverValue],
                Expression.Assign(receiverValue, receiver),
                Expression.Condition(Expression.Equal(receiverValue, Expression.Constant(null, receiver.Type)),
                    Expression.Constant(AvaloniaProperty.UnsetValue, typeof(object)),
                    BuildNullSafePropertyAccess(Expression.PropertyOrField(receiverValue, propertyNames[index]), propertyNames, index + 1)));
        }
    }

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

    private static string? GetSimplePathWithoutExtras(BindingBase binding)
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
                CompiledBinding c when c.Converter is null
                    && c.StringFormat is null
                    && c.FallbackValue == AvaloniaProperty.UnsetValue
                    && c.TargetNullValue == AvaloniaProperty.UnsetValue
                    && c.Source == AvaloniaProperty.UnsetValue => c.Path?.ToString(),
                // Note: `CompiledBindingExtension` IS a `CompiledBinding`
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

    private static Type? ResolveType(string typeName)
    {
        try
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
        } catch (Exception) { /* miam */ }

        return null;
    }

    private static bool TryBuildFastPathRawExpression<TDataContext>(BindingBase binding, [NotNullWhen(true)] out Expression<Func<TDataContext, object?>>? expression)
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

    private bool TryBuildFastPathRawGetter(BindingBase binding, [NotNullWhen(true)] out Func<object, object?>? getter)
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

    private static bool TryBuildIntermediateRawExpression<TDataContext>(
        BindingBase binding,
        [NotNullWhen(true)] out Expression<Func<TDataContext, object?>>? expression)
    {
        if (!TryGetIntermediateRawParts(binding, out string? path, out object? fallbackValue, out object? targetNullValue))
        {
            expression = null;
            return false;
        }

        try
        {
            Func<object, object?> getter = BuildIntermediateRawGetter(path, fallbackValue, targetNullValue, typeof(TDataContext));
            expression = WrapRawObjectDelegateAsExpression<TDataContext>(getter);
            return true;
        }
        catch
        {
            expression = null;
            return false;
        }
    }

    private bool TryBuildIntermediateRawGetter(BindingBase binding, [NotNullWhen(true)] out Func<object, object?>? getter)
    {
        if (!TryGetIntermediateRawParts(binding, out string? path, out object? fallbackValue, out object? targetNullValue))
        {
            getter = null;
            return false;
        }

        try
        {
            getter = BuildIntermediateRawGetter(path, fallbackValue, targetNullValue, this.dataContextType);
            return true;
        }
        catch
        {
            getter = null;
            return false;
        }
    }

    private static Func<object, object?> BuildIntermediateRawGetter(
        string path,
        object? fallbackValue,
        object? targetNullValue,
        Type rowType)
    {
        Func<object, object?> propertyGetter = BuildPropertyGetter(path, rowType);

        return row =>
            {
                try
                {
                    object? value = propertyGetter(row);
                    if (ReferenceEquals(value, AvaloniaProperty.UnsetValue))
                    {
                        return fallbackValue;
                    }

                    if (value is null && !ReferenceEquals(targetNullValue, AvaloniaProperty.UnsetValue))
                    {
                        return targetNullValue;
                    }

                    return value;
                }
                catch
                {
                    return fallbackValue;
                }
            };
    }

    private static bool TryGetIntermediateRawParts(
        BindingBase binding,
        [NotNullWhen(true)] out string? path,
        out object? fallbackValue,
        out object? targetNullValue)
    {
        switch (binding)
        {
            case Binding { Path: { Length: > 0 } bindingPath } b
                when b.Converter is null
                     && b.StringFormat is null
                     && b.Source == AvaloniaProperty.UnsetValue
                     && string.IsNullOrEmpty(b.ElementName)
                     && b.RelativeSource is null:
                path = bindingPath;
                fallbackValue = b.FallbackValue;
                targetNullValue = b.TargetNullValue;
                break;

            // Note: `CompiledBindingExtension` IS a `CompiledBinding`.
            case CompiledBinding c
                when c.Converter is null
                     && c.StringFormat is null
                     && c.Source == AvaloniaProperty.UnsetValue:
                path = c.Path?.ToString();
                fallbackValue = c.FallbackValue;
                targetNullValue = c.TargetNullValue;
                break;

            default:
                path = null;
                fallbackValue = null;
                targetNullValue = null;
                return false;
        }

        return !string.IsNullOrEmpty(path) && IsSimpleDotPath(path);
    }

    private static bool TryBuildIntermediateExpression<TDataContext>(BindingBase binding, [NotNullWhen(true)] out Expression<Func<TDataContext, string>>? expression)
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

    private bool TryBuildIntermediateGetter(BindingBase binding, [NotNullWhen(true)] out Func<object, string>? getter)
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

                    if (value == AvaloniaProperty.UnsetValue)
                    {
                        return fallbackString ?? string.Empty;
                    }

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
        BindingBase binding,
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

            case CompiledBinding c:
            {
                string? pathString = c.Path?.ToString();
                if (string.IsNullOrEmpty(pathString) || !IsSimpleDotPath(pathString))
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
            
            // Note: `CompiledBindingExtension` IS a `CompiledBinding`
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

    private static bool TryGetSimpleBindingExpression<TDataContext>(BindingBase binding, [NotNullWhen(true)] out Expression<Func<TDataContext, string>>? expression)
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

    private bool TryGetSimpleBindingGetter(BindingBase binding, [NotNullWhen(true)] out Func<object, string>? getter)
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
            getter = row => rawGetter(row) is { } value && value != AvaloniaProperty.UnsetValue
                ? value as string ?? value.ToString() ?? string.Empty
                : string.Empty;
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

    private static Expression<Func<TDataContext, object?>> WrapRawObjectDelegateAsExpression<TDataContext>(Func<object, object?> getter)
    {
        ParameterExpression rowParameter = Expression.Parameter(typeof(TDataContext), "row");
        Expression body = Expression.Invoke(Expression.Constant(getter), Expression.Convert(rowParameter, typeof(object)));
        return Expression.Lambda<Func<TDataContext, object?>>(body, rowParameter);
    }

    private Func<object, string> BuildFrameworkDelegatedGetter(BindingBase binding)
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

    private Expression<Func<TDataContext, string>> BuildFrameworkDelegatedGetterExpression<TDataContext>(BindingBase binding)
    {
        Func<object, string> getter = this.BuildFrameworkDelegatedGetter(binding);
        return WrapObjectDelegateAsExpression<TDataContext>(getter);
    }

    private Func<object, object?> BuildProxyRawGetter(BindingBase binding)
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

    private Expression<Func<TDataContext, object?>> BuildProxyRawGetterExpression<TDataContext>(BindingBase binding)
    {
        Func<object, object?> getter = this.BuildProxyRawGetter(binding);
        return WrapRawObjectDelegateAsExpression<TDataContext>(getter);
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

    private BindingBase RewriteParentBindingIfNeeded(BindingBase binding)
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

        private readonly BindingExpressionBase valueSubscription;

        public BindingEvaluatorProxyElement(ILogical anchor, BindingBase binding)
        {
            ((ISetLogicalParent)this).SetParent(anchor);
            
            // Null at construction; it is set at Evaluation.
            // We NEED to null it now in order to prevent the binding's creation from throwing due to
            // missmatched DataContext type if we inherited a DataContext of a different type.
            this.DataContext = null;
            
            this.valueSubscription = this.Bind(ValueProperty, binding);
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
