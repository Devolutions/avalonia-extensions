namespace Devolutions.AvaloniaControls.MarkupExtensions;

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Converters;

public class ScrollBarVisibilityBindingExtension : MarkupExtension
{
    private readonly IMultiValueConverter converter;

    private readonly string scrollViewerName;

    private readonly string visibilityPropertyName;

    public ScrollBarVisibilityBindingExtension(string scrollViewerName, Orientation orientation)
    {
        this.scrollViewerName = scrollViewerName.TrimStart('#');
        (this.converter, AvaloniaProperty visibilityProperty) = orientation switch
        {
            Orientation.Vertical => (DevoConverters.IsVerticalScrollBarVisibleConverter,
                ScrollViewer.VerticalScrollBarVisibilityProperty),
            Orientation.Horizontal => (DevoConverters.IsHorizontalScrollBarVisibleConverter,
                ScrollViewer.HorizontalScrollBarVisibilityProperty),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
        };

        this.visibilityPropertyName = visibilityProperty.Name;
    }

    // Avalonia 12 sealed MultiBinding and made Binding.NameScope internal, so this can no longer
    // derive from MultiBinding nor set the namescope by hand. Build the element-name child bindings
    // through ReflectionBindingExtension.ProvideValue, which captures the template namescope from the
    // service provider, then assemble them into a MultiBinding (same behavior as before).
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Element-name reflection bindings are required because the element name is supplied by markup extension argument.")]
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new MultiBinding
        {
            Converter = this.converter,
            Bindings =
            [
                this.ElementBinding(this.visibilityPropertyName, serviceProvider),
                this.ElementBinding(ScrollViewer.ExtentProperty.Name, serviceProvider),
                this.ElementBinding(ScrollViewer.ViewportProperty.Name, serviceProvider),
            ],
        };

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Element-name reflection bindings are required because the element name is supplied by markup extension argument.")]
    private BindingBase ElementBinding(string path, IServiceProvider serviceProvider) =>
        new ReflectionBindingExtension(path) { ElementName = this.scrollViewerName }.ProvideValue(serviceProvider);
}
