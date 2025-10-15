namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Helpers;

public class DynamicResourceTogglerExtension : MarkupExtension
{
    public DynamicResourceTogglerExtension(object conditionBinding, object whenTrueResourceKey, object whenFalseResourceKey)
    {
        this.ConditionBinding = new WeakReference<IBinding>(conditionBinding as IBinding ?? MarkupExtensionHelpers.GetBinding<bool>(conditionBinding)!);
        this.WhenTrueResourceKey = whenTrueResourceKey;
        this.WhenFalseResourceKey = whenFalseResourceKey;
    }

    [ConstructorArgument("conditionBinding")]
    public WeakReference<IBinding> ConditionBinding { get; init; }

    [ConstructorArgument("whenTrueResourceKey")]
    public object WhenTrueResourceKey { get; init; }

    [ConstructorArgument("whenFalseResourceKey")]
    public object WhenFalseResourceKey { get; init; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (!this.ConditionBinding.TryGetTarget(out IBinding? conditionBinding)) return AvaloniaProperty.UnsetValue;

        return new BindingTogglerExtension(
            conditionBinding,
            new DynamicResourceExtension(this.WhenTrueResourceKey).ProvideValue(serviceProvider),
            new DynamicResourceExtension(this.WhenFalseResourceKey).ProvideValue(serviceProvider)
        ).ProvideValue(serviceProvider);
    }
}