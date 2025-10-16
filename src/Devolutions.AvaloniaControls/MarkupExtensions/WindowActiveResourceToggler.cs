namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

public class WindowActiveResourceTogglerExtension : MarkupExtension
{
    public WindowActiveResourceTogglerExtension(object activeResourceKey, object inactiveResourceKey)
    {
        this.ActiveResourceKey = activeResourceKey;
        this.InactiveResourceKey = inactiveResourceKey;
    }

    [ConstructorArgument("activeResourceKey")]
    public object ActiveResourceKey { get; init; }

    [ConstructorArgument("inactiveResourceKey")]
    public object InactiveResourceKey { get; init; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Design.IsDesignMode)
        {
            return new DynamicResourceExtension(this.ActiveResourceKey).ProvideValue(serviceProvider);
        }

        return new WindowActiveBindingTogglerExtension(
            new DynamicResourceExtension(this.ActiveResourceKey).ProvideValue(serviceProvider),
            new DynamicResourceExtension(this.InactiveResourceKey).ProvideValue(serviceProvider)
        ).ProvideValue(serviceProvider);
    }
}