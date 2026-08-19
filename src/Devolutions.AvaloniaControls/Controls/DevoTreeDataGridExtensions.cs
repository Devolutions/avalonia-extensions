namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;

public static class DevoTreeDataGridExtensions
{
    public static readonly AttachedProperty<object?> ToolTipProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGridColumn, object?>("ToolTip", typeof(DevoTreeDataGridExtensions));

    /// <summary>
    /// Content shown inside the column header alongside the caption, positioned by
    /// <see cref="HeaderAdornmentPositionProperty"/>.
    /// <para>
    /// The adornment is measured but deliberately excluded from the header's desired width, so showing,
    /// hiding or resizing it never resizes an <see cref="GridLength.Auto"/> column.
    /// </para>
    /// </summary>
    /// <remarks>
    /// A <see cref="Control"/> value is hosted directly, so each column needs its own instance; anything
    /// else is wrapped in a <see cref="ContentPresenter"/>. Bind the content's own properties to make it
    /// react at runtime, rather than swapping this value.
    /// </remarks>
    public static readonly AttachedProperty<object?> HeaderAdornmentProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGridColumn, object?>("HeaderAdornment", typeof(DevoTreeDataGridExtensions));

    /// <summary>
    /// Where the header adornment sits. Defaults to <see cref="HeaderAdornmentPosition.Right"/>.
    /// </summary>
    /// <remarks>
    /// Settable on the column, or on the adornment control itself, where a value takes precedence over the
    /// column's. Set it on the control when the position changes at runtime: a
    /// <see cref="TreeDataGridColumn"/> is a plain <see cref="AvaloniaObject"/> with no DataContext, so a
    /// XAML binding cannot resolve against it, and a change made on a column after layout cannot schedule
    /// one either.
    /// </remarks>
    public static readonly AttachedProperty<HeaderAdornmentPosition> HeaderAdornmentPositionProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, HeaderAdornmentPosition>(
            "HeaderAdornmentPosition",
            typeof(DevoTreeDataGridExtensions));

    public static void SetToolTip(TreeDataGridColumn element, object? value) => element.SetValue(ToolTipProperty, value);

    public static object? GetToolTip(TreeDataGridColumn element) => element.GetValue(ToolTipProperty);

    public static void SetHeaderAdornment(TreeDataGridColumn element, object? value) =>
        element.SetValue(HeaderAdornmentProperty, value);

    public static object? GetHeaderAdornment(TreeDataGridColumn element) => element.GetValue(HeaderAdornmentProperty);

    public static void SetHeaderAdornmentPosition(AvaloniaObject element, HeaderAdornmentPosition value) =>
        element.SetValue(HeaderAdornmentPositionProperty, value);

    public static HeaderAdornmentPosition GetHeaderAdornmentPosition(AvaloniaObject element) =>
        element.GetValue(HeaderAdornmentPositionProperty);
}
