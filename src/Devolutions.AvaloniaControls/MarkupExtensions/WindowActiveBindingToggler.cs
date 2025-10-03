namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia.Markup.Xaml;

public class WindowActiveBindingTogglerExtension : MarkupExtension
{
    private BindingTogglerExtension? toggleBindingExtension;

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
        this.toggleBindingExtension ??= new BindingTogglerExtension(
            WindowIsActiveBindingExtension.CreateIsActiveBinding(),
            this.WhenActiveBinding,
            this.WhenInactiveBinding
        );
        this.WhenActiveBinding = null;
        this.WhenInactiveBinding = null;

        return this.toggleBindingExtension.ProvideValue(serviceProvider);
    }
}