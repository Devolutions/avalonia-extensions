namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;

public static class DevoTreeDataGridExtensions
{
    public static readonly AttachedProperty<object?> ToolTipProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGridColumn, object?>("ToolTip", typeof(DevoTreeDataGridExtensions));

    public static void SetToolTip(TreeDataGridColumn element, object? value) => element.SetValue(ToolTipProperty, value);

    public static object? GetToolTip(TreeDataGridColumn element) => element.GetValue(ToolTipProperty);
}
