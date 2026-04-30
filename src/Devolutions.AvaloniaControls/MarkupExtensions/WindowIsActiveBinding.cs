namespace Devolutions.AvaloniaControls.MarkupExtensions;

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Converters;

public class WindowIsActiveBindingExtension : MarkupExtension
{
    /// <summary>
    /// Creates a <see cref="MultiBinding"/> that resolves the nearest window's <c>IsActive</c> state.
    /// </summary>
    /// <remarks>
    /// Uses reflection-based <see cref="Binding"/> because the TemplatedParent-sourced ancestor walk
    /// (<c>Source = new Binding { RelativeSource = TemplatedParent }</c>) has no AOT-safe equivalent
    /// in <c>CompiledBindingExtension</c>. The four bindings try, in order:
    /// <list type="number">
    ///   <item>INativeWindowAvaloniaHost ancestor from TemplatedParent context</item>
    ///   <item>INativeWindowAvaloniaHost ancestor from visual tree</item>
    ///   <item>Window ancestor from TemplatedParent context</item>
    ///   <item>Window ancestor from visual tree</item>
    /// </list>
    /// The <see cref="DevoMultiConverters.FirstNonNullValueMultiConverter"/> picks the first non-null
    /// result, falling back to <c>true</c> if no ancestor is found.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Reflection bindings are required here: the TemplatedParent-sourced ancestor walk " +
                         "(Source = new Binding { RelativeSource = TemplatedParent }) has no CompiledBindingExtension equivalent. " +
                         "All target types (Window, INativeWindowAvaloniaHost) are preserved via ILLink descriptors.")]
    public static BindingBase CreateIsActiveBinding() =>
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

                Helpers.ObservableHelpers.ValueBinding(true),
            ],
        };

    public override object ProvideValue(IServiceProvider serviceProvider) => CreateIsActiveBinding();
}
