namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Converters;
using Helpers;
using Microsoft.Extensions.DependencyInjection;

public class BindingTogglerExtension : MarkupExtension, IDisposable
{
    private IBinding? resolvedWhenFalseBinding;
    private IBinding? resolvedWhenTrueBinding;

    public BindingTogglerExtension(IBinding conditionBinding, object whenTrueBinding, object whenFalseBinding)
    {
        this.ConditionBinding = conditionBinding;
        this.WhenTrueBinding = whenTrueBinding;
        this.WhenFalseBinding = whenFalseBinding;
    }

    [ConstructorArgument("conditionBinding")]
    public IBinding ConditionBinding { get; init; }

    [ConstructorArgument("whenTrueBinding")]
    public object WhenTrueBinding { get; init; }

    [ConstructorArgument("whenFalseBinding")]
    public object WhenFalseBinding { get; init; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var provideTarget = serviceProvider.GetService<IProvideValueTarget>();
        var setter = provideTarget?.TargetObject as Setter;
        var targetType = setter?.Property?.PropertyType;

        if (targetType is not null)
        {
            this.resolvedWhenTrueBinding ??=
                MarkupExtensionHelpers.GetBinding(this.WhenTrueBinding, targetType) ?? ObservableHelpers.Empty.ToBinding();
            this.resolvedWhenFalseBinding ??=
                MarkupExtensionHelpers.GetBinding(this.WhenFalseBinding, targetType) ?? ObservableHelpers.Empty.ToBinding();
        }
        else
        {
            this.resolvedWhenTrueBinding ??=
                MarkupExtensionHelpers.GetBinding<object?>(this.WhenTrueBinding) ?? ObservableHelpers.Empty.ToBinding();
            this.resolvedWhenFalseBinding ??=
                MarkupExtensionHelpers.GetBinding<object?>(this.WhenFalseBinding) ?? ObservableHelpers.Empty.ToBinding();
        }

        return new MultiBinding
        {
            Converter = DevoMultiConverters.BooleanToChoiceConverter,
            Bindings =
            [
                this.ConditionBinding, this.resolvedWhenTrueBinding, this.resolvedWhenFalseBinding,
            ],
        };
    }

    public void Dispose()
    {
        (this.resolvedWhenFalseBinding as IDisposable)?.Dispose();
        this.resolvedWhenFalseBinding = null;
        (this.resolvedWhenTrueBinding as IDisposable)?.Dispose();
        this.resolvedWhenTrueBinding = null;
    }
}