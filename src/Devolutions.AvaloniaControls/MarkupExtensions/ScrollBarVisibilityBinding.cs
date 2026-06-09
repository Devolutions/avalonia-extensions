namespace Devolutions.AvaloniaControls.MarkupExtensions;

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Converters;
using Irihi.Avalonia.Shared.MarkupExtensions;

public class ScrollBarVisibilityBindingExtension : MultiBinding, IMarkupExtension
{
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Element-name reflection bindings are required because the element name is supplied by markup extension argument.")]
    public ScrollBarVisibilityBindingExtension(string scrollViewerName, Orientation orientation)
    {
        scrollViewerName = scrollViewerName.TrimStart('#');
        (IMultiValueConverter converter, AvaloniaProperty visibilityProperty) = orientation switch
        {
            Orientation.Vertical => (DevoConverters.IsVerticalScrollBarVisibleConverter,
                ScrollViewer.VerticalScrollBarVisibilityProperty),
            Orientation.Horizontal => (DevoConverters.IsHorizontalScrollBarVisibleConverter,
                ScrollViewer.HorizontalScrollBarVisibilityProperty),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
        };

        this.Converter = converter;
        this.Bindings =
        [
            new Binding { ElementName = scrollViewerName, Path = visibilityProperty.Name },
            new Binding { ElementName = scrollViewerName, Path = ScrollViewer.ExtentProperty.Name },
            new Binding { ElementName = scrollViewerName, Path = ScrollViewer.ViewportProperty.Name },
        ];
    }

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(INameScope)) is INameScope nameScope)
        {
            WeakReference<INameScope?> weakNameScope = new(nameScope);
            foreach (Binding binding in this.Bindings.Cast<Binding>())
            {
                binding.NameScope = weakNameScope;
            }
        }

        return this;
    }
}
