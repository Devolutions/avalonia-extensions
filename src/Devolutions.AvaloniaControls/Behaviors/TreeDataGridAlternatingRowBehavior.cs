namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia;
using Avalonia.Controls;

/// <summary>
/// Attached behavior that sets <c>:odd-row</c> pseudo-classes on realized
/// <see cref="TreeDataGridRow"/> controls based on their current <c>RowIndex</c>.
///
/// <para>
/// <c>TreeDataGridRow.RowIndex</c> is a plain CLR property that does not raise
/// change notifications. When rows are recycled during virtualisation the binding
/// to <c>RowIndex</c> becomes stale, producing incorrect alternating colours.
/// </para>
///
/// <para>
/// After each layout pass <c>RowIndex</c> is therefore read imperatively from every realized row
/// and the <c>:odd-row</c> pseudo-class toggled accordingly. The sweep itself lives in
/// <see cref="TreeDataGridRowDecorator"/>, which this shares with
/// <see cref="TreeDataGridSelectionRunBehavior"/> so that enabling both costs one pass rather than
/// two; see there for how it is scheduled.
/// </para>
/// </summary>
public static class TreeDataGridAlternatingRowBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGrid, bool>("Enable", typeof(TreeDataGridAlternatingRowBehavior));

    static TreeDataGridAlternatingRowBehavior()
    {
        EnableProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is TreeDataGrid treeDataGrid)
            {
                TreeDataGridRowDecorator.SetAlternatingRows(treeDataGrid, args.NewValue.GetValueOrDefault<bool>());
            }
        });
    }

    public static void SetEnable(TreeDataGrid element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(TreeDataGrid element) => element.GetValue(EnableProperty);
}
