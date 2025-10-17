namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Helpers;

public class WindowActiveBindingTogglerExtension : MarkupExtension
{
    private readonly WeakReference<IBinding?> weakResolvedActiveBinding = new(null);

    private readonly WeakReference<IBinding?> weakResolvedInactiveBinding = new(null);

    public WindowActiveBindingTogglerExtension(object whenActiveBinding, object whenInactiveBinding)
    {
        this.WhenActiveBinding = whenActiveBinding;
        this.WhenInactiveBinding = whenInactiveBinding;
    }

    [ConstructorArgument("whenActiveBinding")]
    public object? WhenActiveBinding { get; private set; }

    [ConstructorArgument("whenInactiveBinding")]
    public object? WhenInactiveBinding { get; private set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        IProvideValueTarget? provideTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        Setter? setter = provideTarget?.TargetObject as Setter;
        Type? targetType = setter?.Property?.PropertyType;

        this.weakResolvedActiveBinding.TryGetTarget(out IBinding? resolvedActiveBinding);
        if (resolvedActiveBinding is null && this.WhenActiveBinding is not null)
        {
            resolvedActiveBinding = targetType is not null
                ? MarkupExtensionHelpers.GetBinding(this.WhenActiveBinding, targetType)
                : MarkupExtensionHelpers.GetBinding<object?>(this.WhenActiveBinding);
            this.weakResolvedActiveBinding.SetTarget(resolvedActiveBinding);
        }

        this.weakResolvedInactiveBinding.TryGetTarget(out IBinding? resolvedInactiveBinding);
        if (resolvedInactiveBinding is null && this.WhenInactiveBinding is not null)
        {
            resolvedInactiveBinding = targetType is not null
                ? MarkupExtensionHelpers.GetBinding(this.WhenInactiveBinding, targetType)
                : MarkupExtensionHelpers.GetBinding<object?>(this.WhenInactiveBinding);
            this.weakResolvedInactiveBinding.SetTarget(resolvedInactiveBinding);
        }

        var ret = new BindingTogglerExtension(
            WindowIsActiveBindingExtension.CreateIsActiveBinding(),
            resolvedActiveBinding,
            resolvedInactiveBinding
        ).ProvideValue(serviceProvider);

        this.WhenActiveBinding = null;
        this.WhenInactiveBinding = null;

        return ret;
    }
}