namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Converters;

public class WindowIsActiveBindingExtension : MarkupExtension
{
    private static readonly ClrPropertyInfo nativeWindowHostIsActivePropertyInfo = new(
        nameof(INativeWindowAvaloniaHost<AvaloniaObject>.IsActive),
        target => ((INativeWindowAvaloniaHost<AvaloniaObject>)target).IsActive,
        null,
        typeof(bool));

    /// <summary>
    /// Creates a <see cref="MultiBinding"/> that resolves the nearest window's <c>IsActive</c> state.
    /// </summary>
    /// <remarks>
    /// Uses compiled bindings. The four bindings try, in order:
    /// <list type="number">
    ///   <item>INativeWindowAvaloniaHost ancestor from TemplatedParent context</item>
    ///   <item>INativeWindowAvaloniaHost ancestor from visual tree</item>
    ///   <item>Window ancestor from TemplatedParent context</item>
    ///   <item>Window ancestor from visual tree</item>
    /// </list>
    /// The <see cref="DevoMultiConverters.FirstNonNullValueMultiConverter"/> picks the first non-null
    /// result, falling back to <c>true</c> if no ancestor is found.
    /// </remarks>
    public static BindingBase CreateIsActiveBinding() =>
        new MultiBinding
        {
            Converter = DevoMultiConverters.FirstNonNullValueMultiConverter,
            Bindings =
            [
                CreateIsActiveBinding(typeof(INativeWindowAvaloniaHost<AvaloniaObject>), nativeWindowHostIsActivePropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor, true),
                CreateIsActiveBinding(typeof(INativeWindowAvaloniaHost<AvaloniaObject>), nativeWindowHostIsActivePropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor, false),
                CreateIsActiveBinding(typeof(Window), WindowBase.IsActiveProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor, true),
                CreateIsActiveBinding(typeof(Window), WindowBase.IsActiveProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor, false),

                Helpers.ObservableHelpers.ValueBinding(true),
            ],
        };

    private static CompiledBinding CreateIsActiveBinding(
        Type ancestorType,
        IPropertyInfo isActiveProperty,
        Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> accessorFactory,
        bool fromTemplatedParent)
    {
        CompiledBindingPathBuilder pathBuilder = new();

        if (fromTemplatedParent)
        {
            pathBuilder.TemplatedParent();
        }

        return new CompiledBinding
        {
            Path = pathBuilder
                .VisualAncestor(ancestorType, 0)
                .Property(isActiveProperty, accessorFactory)
                .Build(),
        };
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => CreateIsActiveBinding();
}
