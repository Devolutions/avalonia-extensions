namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Styling;
using Converters;
using Helpers;

public class BindingTogglerExtension : MarkupExtension
{
  private readonly WeakReference<BindingBase?> resolvedWhenFalseBinding = new(null);
  private readonly WeakReference<BindingBase?> resolvedWhenTrueBinding = new(null);

  private readonly WeakReference<BindingBase> weakConditionBinding;

  public BindingTogglerExtension(BindingBase conditionBinding, object? whenTrueBinding, object? whenFalseBinding)
  {
    this.ConditionBinding = conditionBinding;
    this.weakConditionBinding = new WeakReference<BindingBase>(conditionBinding);
    this.WhenTrueBinding = whenTrueBinding;
    this.WhenFalseBinding = whenFalseBinding;
  }

  [ConstructorArgument("conditionBinding")]
  public BindingBase? ConditionBinding { get; private set; }

  [ConstructorArgument("whenTrueBinding")]
  public object? WhenTrueBinding { get; private set; }

  [ConstructorArgument("whenFalseBinding")]
  public object? WhenFalseBinding { get; private set; }

  public override object ProvideValue(IServiceProvider serviceProvider)
  {
    var provideTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
    Type? targetType = null;
    if (provideTarget?.TargetObject is Setter setter)
    {
      targetType = setter?.Property?.PropertyType;
    }
    else if (provideTarget?.TargetProperty is AvaloniaProperty avaloniaProperty)
    {
      targetType = avaloniaProperty.PropertyType;
    }

    this.resolvedWhenTrueBinding.TryGetTarget(out BindingBase? resolvedTrueBinding);
    if (resolvedTrueBinding is null && this.WhenTrueBinding is not null)
    {
      resolvedTrueBinding = targetType is not null
        ? MarkupExtensionHelpers.GetBindingForPropertyType(this.WhenTrueBinding, targetType)
        : MarkupExtensionHelpers.GetBinding<object?>(this.WhenTrueBinding);
      this.resolvedWhenTrueBinding.SetTarget(resolvedTrueBinding);
    }

    this.resolvedWhenFalseBinding.TryGetTarget(out BindingBase? resolvedFalseBinding);
    if (resolvedFalseBinding is null && this.WhenFalseBinding is not null)
    {
      resolvedFalseBinding = targetType is not null
        ? MarkupExtensionHelpers.GetBindingForPropertyType(this.WhenFalseBinding, targetType)
        : MarkupExtensionHelpers.GetBinding<object?>(this.WhenFalseBinding);
      this.resolvedWhenFalseBinding.SetTarget(resolvedFalseBinding);
    }

    if (!this.weakConditionBinding.TryGetTarget(out BindingBase? condition)) return AvaloniaProperty.UnsetValue;

    var multiBinding = new MultiBinding
    {
      Converter = DevoMultiConverters.BooleanToChoiceConverter,
      Bindings =
      [
        condition, resolvedTrueBinding!, resolvedFalseBinding!
      ]
    };

    this.WhenTrueBinding = null;
    this.WhenFalseBinding = null;
    this.ConditionBinding = null;

    return multiBinding;
  }
}