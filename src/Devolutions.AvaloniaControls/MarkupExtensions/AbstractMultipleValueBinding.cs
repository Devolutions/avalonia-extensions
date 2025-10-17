namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Helpers;

public abstract class AbstractMultipleValueBinding<TIn> : MarkupExtension
{
    private readonly WeakReference<IBinding>[]? bindings;

    protected AbstractMultipleValueBinding(object b1, object b2)
    {
        if (GetBinding(b1) is IBinding bA && GetBinding(b2) is IBinding bB)
        {
            this.bindings =
            [
                new WeakReference<IBinding>(bA),
                new WeakReference<IBinding>(bB),
            ];
        }
    }

    protected AbstractMultipleValueBinding(object b1, object b2, params object[] bindings)
    {
        if (GetBinding(b1) is IBinding bA && GetBinding(b2) is IBinding bB)
        {
            this.bindings =
            [
                new WeakReference<IBinding>(bA),
                new WeakReference<IBinding>(bB),
                ..bindings.Select(GetBinding).SkipNulls().Select(static b => new WeakReference<IBinding>(b)),
            ];
        }
    }

    protected abstract IMultiValueConverter MultiValueConverter { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (this.bindings is null) return AvaloniaProperty.UnsetValue;

        return new MultiBinding
        {
            Bindings = this.bindings.Select(b => b.TryGetTarget(out IBinding? t) ? t : null).SkipNulls().ToArray(),
            Converter = this.MultiValueConverter,
        };
    }

    private static IBinding? GetBinding(object? v) => MarkupExtensionHelpers.GetBinding<TIn>(v);
}