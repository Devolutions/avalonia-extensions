namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;
using Helpers;

public class DynamicResourceTogglerExtension : MarkupExtension
{
    private readonly WeakReference<BindingBase?> weakConditionBinding;

    public DynamicResourceTogglerExtension(object conditionBinding, object whenTrueResourceKey, object whenFalseResourceKey)
    {
        this.ConditionBinding = MarkupExtensionHelpers.GetBinding<bool>(conditionBinding);
        this.weakConditionBinding = new WeakReference<BindingBase?>(this.ConditionBinding);
        this.WhenTrueResourceKey = whenTrueResourceKey;
        this.WhenFalseResourceKey = whenFalseResourceKey;
    }

    [ConstructorArgument("conditionBinding")]
    public BindingBase? ConditionBinding { get; private set; }

    [ConstructorArgument("whenTrueResourceKey")]
    public object WhenTrueResourceKey { get; init; }

    [ConstructorArgument("whenFalseResourceKey")]
    public object WhenFalseResourceKey { get; init; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (!this.weakConditionBinding.TryGetTarget(out BindingBase? conditionBinding)) return AvaloniaProperty.UnsetValue;

        var ret = new BindingTogglerExtension(
            conditionBinding,
            new DynamicResourceExtension(this.WhenTrueResourceKey).ProvideValue(serviceProvider),
            new DynamicResourceExtension(this.WhenFalseResourceKey).ProvideValue(serviceProvider)
        ).ProvideValue(serviceProvider);

        this.ConditionBinding = null;

        return ret;
    }
}