namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Helpers;

public abstract class AbstractMultipleValueBinding<TIn> : MarkupExtension
{
    private readonly WeakReference<BindingBase>[]? bindings;

    protected AbstractMultipleValueBinding(object b1, object b2)
    {
        if (GetBinding(b1) is BindingBase bA && GetBinding(b2) is BindingBase bB)
        {
            this.bindings =
            [
                new WeakReference<BindingBase>(bA),
                new WeakReference<BindingBase>(bB),
            ];
        }
    }

    protected AbstractMultipleValueBinding(object b1, object b2, params object[] bindings)
    {
        if (GetBinding(b1) is BindingBase bA && GetBinding(b2) is BindingBase bB)
        {
            this.bindings =
            [
                new WeakReference<BindingBase>(bA),
                new WeakReference<BindingBase>(bB),
                ..bindings.Select(GetBinding).SkipNulls().Select(static b => new WeakReference<BindingBase>(b)),
            ];
        }
    }

    protected abstract IMultiValueConverter MultiValueConverter { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (this.bindings is null) return AvaloniaProperty.UnsetValue;

        return new MultiBinding
        {
            Bindings = this.bindings.Select(b => b.TryGetTarget(out BindingBase? t) ? t : null).SkipNulls().ToArray(),
            Converter = this.MultiValueConverter,
        };
    }

    private static BindingBase? GetBinding(object? v) => MarkupExtensionHelpers.GetBinding<TIn>(v);
}