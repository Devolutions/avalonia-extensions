namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

/// <summary>
/// AOT-safe markup extension that creates a compiled binding to an <see cref="AvaloniaProperty"/> on Self.
/// Replaces <c>{ReflectionBinding PropertyName, RelativeSource={RelativeSource Self}}</c> which cannot
/// be compiled when nested inside another markup extension's constructor arguments.
/// </summary>
/// <remarks>
/// Extends <see cref="CompiledBindingExtension"/> directly so that the XAML compiler sees the
/// return type as <c>IBinding</c> (via <c>BindingBase</c>), avoiding the "Unable to convert
/// Object to IBinding" error that occurs when a <c>MarkupExtension</c> returns <c>object</c>.
/// All <c>BindingBase</c> properties (Converter, Mode, etc.) are inherited and work as expected.
/// <para>
/// The property name is resolved by the XAML compiler against the enclosing <c>ControlTheme.TargetType</c>
/// scope, so <c>{SelfBinding Index}</c> works inside a <c>DataGridRow</c> style. IDE autocompletion
/// does not know the DataType however, because the Avalonia XAML compiler only processes
/// <c>DataType</c> on the exact <c>CompiledBindingExtension</c> type (not subclasses).
/// </para>
/// </remarks>
/// <example>
/// Instead of:
///   {ReflectionBinding Index, RelativeSource={RelativeSource Self}, Converter={x:Static Converters.MyConverter}}
/// Use:
///   {SelfBinding Index, Converter={x:Static Converters.MyConverter}}
/// </example>
public class SelfBindingExtension : CompiledBindingExtension
{
    public SelfBindingExtension(AvaloniaProperty property)
        : base(
            new CompiledBindingPathBuilder()
                .Self()
                .Property(property, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
                .Build())
    {
    }
}
