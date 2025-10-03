namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Helpers;

public abstract class AbstractMultipleValueBinding<TIn> : MarkupExtension
{
    private readonly WeakReference<IBinding>[]? bindings;

    private MultiBinding? multiBindingInstance;

    protected AbstractMultipleValueBinding(object a, object b)
    {
        if (GetBinding(a) is IBinding bA && GetBinding(b) is IBinding bB)
        {
            this.bindings =
            [
                new WeakReference<IBinding>(bA),
                new WeakReference<IBinding>(bB),
            ];
        }
    }

    protected AbstractMultipleValueBinding(object a, object b, params object[] bindings) : this(a, b)
    {
        if (GetBinding(a) is IBinding bA && GetBinding(b) is IBinding bB)
        {
            this.bindings =
            [
                new WeakReference<IBinding>(bA),
                new WeakReference<IBinding>(bB),
                ..bindings.Select(GetBinding).SkipNulls().Select(static b => new WeakReference<IBinding>(b)).ToArray(),
            ];
        }
    }

    protected abstract IMultiValueConverter MultiValueConverter { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (this.bindings is null) return null!;

        this.multiBindingInstance ??= new MultiBinding
        {
            Bindings = this.bindings.Select(b => b.TryGetTarget(out IBinding? t) ? t : null).SkipNulls().ToArray(),
            Converter = this.MultiValueConverter,
        };

        return this.multiBindingInstance;
    }

    private static IBinding? GetBinding(object? v) => MarkupExtensionHelpers.GetBinding<TIn>(v);
}