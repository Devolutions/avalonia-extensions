namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

/// <summary>
/// AOT-safe markup extension that creates a compiled binding to an <see cref="AvaloniaProperty"/> on Self.
/// Replaces <c>{ReflectionBinding PropertyName, RelativeSource={RelativeSource Self}}</c> which cannot
/// be compiled when nested inside another markup extension's constructor arguments.
/// </summary>
/// <remarks>
/// Extends <see cref="CompiledBinding"/> directly, in the same way as <see cref="CompiledBindingExtension"/>, so that
/// the XAML compiler sees the return type as <c>BindingBase</c>, avoiding the "Unable to convert
/// Object to BindingBase" error that occurs when a <c>MarkupExtension</c> returns <c>object</c>.
/// All <c>BindingBase</c> properties (Converter, Mode, etc.) are inherited and work as expected.
/// <para>
/// The property name is resolved by the XAML compiler against the enclosing <c>ControlTheme.TargetType</c>
/// scope, so <c>{SelfBinding Index}</c> works inside a <c>DataGridRow</c> style. IDE autocompletion
/// does not know the DataType however, because the Avalonia XAML compiler only processes
/// <c>DataType</c> on the exact <c>CompiledBinding</c> type (not subclasses).
/// </para>
/// </remarks>
/// <example>
/// Instead of:
///   {ReflectionBinding Index, RelativeSource={RelativeSource Self}, Converter={x:Static Converters.MyConverter}}
/// Use:
///   {SelfBinding Index, Converter={x:Static Converters.MyConverter}}
/// </example>
public class SelfBindingExtension : CompiledBinding
{
    public SelfBindingExtension(AvaloniaProperty property)
    {
        this.Path = new CompiledBindingPathBuilder()
            .Self()
            .Property(property, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
            .Build();
    }
    
    public CompiledBinding ProvideValue(IServiceProvider? provider)
    {
        return new CompiledBinding()
        {
            Path = this.Path,
            Delay = this.Delay,
            Converter = this.Converter,
            ConverterCulture = this.ConverterCulture,
            ConverterParameter = this.ConverterParameter,
            TargetNullValue = this.TargetNullValue,
            FallbackValue = this.FallbackValue,
            Mode = this.Mode,
            Priority = this.Priority,
            StringFormat = this.StringFormat,
            Source = this.Source,
            UpdateSourceTrigger = this.UpdateSourceTrigger
        };
    }

    public Type? DataType { get; set; }
}
