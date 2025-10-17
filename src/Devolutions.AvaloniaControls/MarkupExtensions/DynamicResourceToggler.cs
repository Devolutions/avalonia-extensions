namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Helpers;

public class DynamicResourceTogglerExtension : MarkupExtension
{
    private readonly WeakReference<IBinding?> weakConditionBinding;

    public DynamicResourceTogglerExtension(object conditionBinding, object whenTrueResourceKey, object whenFalseResourceKey)
    {
        this.ConditionBinding = MarkupExtensionHelpers.GetBinding<bool>(conditionBinding);
        this.weakConditionBinding = new WeakReference<IBinding?>(this.ConditionBinding);
        this.WhenTrueResourceKey = whenTrueResourceKey;
        this.WhenFalseResourceKey = whenFalseResourceKey;
    }

    [ConstructorArgument("conditionBinding")]
    public IBinding? ConditionBinding { get; private set; }

    [ConstructorArgument("whenTrueResourceKey")]
    public object WhenTrueResourceKey { get; init; }

    [ConstructorArgument("whenFalseResourceKey")]
    public object WhenFalseResourceKey { get; init; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (!this.weakConditionBinding.TryGetTarget(out IBinding? conditionBinding)) return AvaloniaProperty.UnsetValue;

        var ret = new BindingTogglerExtension(
            conditionBinding,
            new DynamicResourceExtension(this.WhenTrueResourceKey).ProvideValue(serviceProvider),
            new DynamicResourceExtension(this.WhenFalseResourceKey).ProvideValue(serviceProvider)
        ).ProvideValue(serviceProvider);

        this.ConditionBinding = null;

        return ret;
    }
}