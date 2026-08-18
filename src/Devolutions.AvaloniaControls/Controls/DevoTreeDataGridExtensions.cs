namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;

public static class DevoTreeDataGridExtensions
{
    public static readonly AttachedProperty<object?> ToolTipProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGridColumn, object?>("ToolTip", typeof(DevoTreeDataGridExtensions));

    /// <summary>
    /// Content pinned to the right edge of the column header, after the sort indicator.
    /// <para>
    /// The adornment is measured but deliberately excluded from the header's desired width, so showing or
    /// hiding it never resizes an <see cref="GridLength.Auto"/> column. The header caption ellipsizes to
    /// make room for it instead.
    /// </para>
    /// </summary>
    /// <remarks>
    /// A <see cref="Control"/> value is hosted directly, so each column needs its own instance; anything
    /// else is wrapped in a <see cref="ContentPresenter"/>. Bind the content's own properties to make it
    /// react at runtime, rather than swapping this value.
    /// </remarks>
    public static readonly AttachedProperty<object?> HeaderRightAdornmentProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGridColumn, object?>("HeaderRightAdornment", typeof(DevoTreeDataGridExtensions));

    public static void SetToolTip(TreeDataGridColumn element, object? value) => element.SetValue(ToolTipProperty, value);

    public static object? GetToolTip(TreeDataGridColumn element) => element.GetValue(ToolTipProperty);

    /// <summary>
    /// Set on an adornment control to make it span the whole header instead of hugging its content, for
    /// cases like an in-header search field. Bind it to switch between the two at runtime.
    /// </summary>
    /// <remarks>
    /// The adornment stays excluded from the header's desired width either way, so filling the header
    /// never resizes an <see cref="GridLength.Auto"/> column.
    /// </remarks>
    public static readonly AttachedProperty<bool> FillsHeaderWidthProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FillsHeaderWidth", typeof(DevoTreeDataGridExtensions));

    public static void SetHeaderRightAdornment(TreeDataGridColumn element, object? value) =>
        element.SetValue(HeaderRightAdornmentProperty, value);

    public static object? GetHeaderRightAdornment(TreeDataGridColumn element) =>
        element.GetValue(HeaderRightAdornmentProperty);

    public static void SetFillsHeaderWidth(Control element, bool value) =>
        element.SetValue(FillsHeaderWidthProperty, value);

    public static bool GetFillsHeaderWidth(Control element) => element.GetValue(FillsHeaderWidthProperty);
}
