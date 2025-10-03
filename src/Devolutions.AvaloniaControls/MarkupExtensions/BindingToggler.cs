namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Converters;
using Helpers;

public class BindingTogglerExtension : MarkupExtension
{
    private readonly WeakReference<IBinding?> resolvedWhenFalseBinding = new(null);
    private readonly WeakReference<IBinding?> resolvedWhenTrueBinding = new(null);

    public BindingTogglerExtension(IBinding conditionBinding, object? whenTrueBinding, object? whenFalseBinding)
    {
        this.ConditionBinding = new WeakReference<IBinding>(conditionBinding);
        this.WhenTrueBinding = whenTrueBinding;
        this.WhenFalseBinding = whenFalseBinding;
    }

    [ConstructorArgument("conditionBinding")]
    public WeakReference<IBinding> ConditionBinding { get; init; }

    [ConstructorArgument("whenTrueBinding")]
    public object? WhenTrueBinding { get; private set; }

    [ConstructorArgument("whenFalseBinding")]
    public object? WhenFalseBinding { get; private set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        IProvideValueTarget? provideTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        Setter? setter = provideTarget?.TargetObject as Setter;
        Type? targetType = setter?.Property?.PropertyType;

        this.resolvedWhenTrueBinding.TryGetTarget(out IBinding? resolvedTrueBinding);
        if (resolvedTrueBinding is null && this.WhenTrueBinding is not null)
        {
            resolvedTrueBinding = targetType is not null
                ? MarkupExtensionHelpers.GetBinding(this.WhenTrueBinding, targetType)
                : MarkupExtensionHelpers.GetBinding<object?>(this.WhenTrueBinding);
            this.resolvedWhenTrueBinding.SetTarget(resolvedTrueBinding);
            this.WhenTrueBinding = null;
        }

        this.resolvedWhenFalseBinding.TryGetTarget(out IBinding? resolvedFalseBinding);
        if (resolvedFalseBinding is null && this.WhenFalseBinding is not null)
        {
            resolvedFalseBinding = targetType is not null
                ? MarkupExtensionHelpers.GetBinding(this.WhenFalseBinding, targetType)
                : MarkupExtensionHelpers.GetBinding<object?>(this.WhenFalseBinding);
            this.resolvedWhenFalseBinding.SetTarget(resolvedFalseBinding);
            this.WhenFalseBinding = null;
        }

        if (!this.ConditionBinding.TryGetTarget(out IBinding? condition)) return null!;

        return new MultiBinding
        {
            Converter = DevoMultiConverters.BooleanToChoiceConverter,
            Bindings =
            [
                condition, resolvedTrueBinding!, resolvedFalseBinding!,
            ],
        };
    }
}