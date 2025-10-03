namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Converters;

public class WindowIsActiveBindingExtension : MarkupExtension
{
    public static IBinding CreateIsActiveBinding() =>
        new MultiBinding
        {
            Converter = DevoMultiConverters.FirstNonNullValueMultiConverter,
            Bindings =
            [
                new Binding
                {
                    Path = "IsActive",
                    RelativeSource = new RelativeSource
                    {
                        Mode = RelativeSourceMode.FindAncestor,
                        AncestorType = typeof(INativeWindowAvaloniaHost<AvaloniaObject>),
                    },
                    Source = new Binding
                    {
                        RelativeSource = new RelativeSource
                        {
                            Mode = RelativeSourceMode.TemplatedParent,
                        },
                    },
                },
                new Binding
                {
                    Path = "IsActive",
                    RelativeSource = new RelativeSource
                    {
                        Mode = RelativeSourceMode.FindAncestor,
                        AncestorType = typeof(INativeWindowAvaloniaHost<AvaloniaObject>),
                    },
                },
                new Binding
                {
                    Path = "IsActive",
                    RelativeSource = new RelativeSource
                    {
                        Mode = RelativeSourceMode.FindAncestor,
                        AncestorType = typeof(Window),
                    },
                    Source = new Binding
                    {
                        RelativeSource = new RelativeSource
                        {
                            Mode = RelativeSourceMode.TemplatedParent,
                        },
                    },
                },
                new Binding
                {
                    Path = "IsActive",
                    RelativeSource = new RelativeSource
                    {
                        Mode = RelativeSourceMode.FindAncestor,
                        AncestorType = typeof(Window),
                    },
                },

                new Binding { Source = true },
            ],
        };

    public override object ProvideValue(IServiceProvider serviceProvider) => CreateIsActiveBinding();
}