namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia;
using Avalonia.Controls;

/// <summary>
/// Attached behavior that marks each realized <see cref="TreeDataGridRow"/> with its position
/// inside a contiguous run of selected rows, using the <c>:sel-run-first</c>,
/// <c>:sel-run-middle</c> and <c>:sel-run-last</c> pseudo-classes.
///
/// <para>
/// A theme uses these to draw a multi-row selection as one shape rather than a stack of separate
/// pills: the first row of a run keeps only its top corners, the last row only its bottom corners,
/// and every row between them is squared off. A lone selected row is given no pseudo-class at all,
/// because the unconditional row style already rounds all four corners.
/// </para>
///
/// <para>
/// Rows are matched up by <see cref="TreeDataGridRow.RowIndex"/>, which is flat: the hierarchy has
/// already been expanded into a linear row list by the time it reaches the grid, so index adjacency
/// is visual adjacency for both flat and hierarchical sources.
/// </para>
///
/// <para>
/// Only realized rows are inspected, which keeps each pass viewport-sized. The trade-off is that a
/// run continuing past the top or bottom of the viewport keeps a rounded end there, because the
/// neighbour that would square it off does not exist yet. The rows at the viewport edge are
/// normally clipped mid-height by the scroll viewport, so this is rarely visible. Querying the
/// selection model by index instead would avoid it, but the interface that answers by row index
/// (<c>ITreeDataGridSelectionInteraction</c>) is internal in Avalonia.Controls.TreeDataGrid 12.0.1,
/// the lowest version this package accepts.
/// </para>
///
/// <para>
/// The sweep itself lives in <see cref="TreeDataGridRowDecorator"/>, which this shares with
/// <see cref="TreeDataGridAlternatingRowBehavior"/> so that enabling both costs one pass rather
/// than two; see there for how it is scheduled.
/// </para>
/// </summary>
public static class TreeDataGridSelectionRunBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGrid, bool>("Enable", typeof(TreeDataGridSelectionRunBehavior));

    static TreeDataGridSelectionRunBehavior()
    {
        EnableProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is TreeDataGrid treeDataGrid)
            {
                TreeDataGridRowDecorator.SetSelectionRuns(treeDataGrid, args.NewValue.GetValueOrDefault<bool>());
            }
        });
    }

    public static void SetEnable(TreeDataGrid element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(TreeDataGrid element) => element.GetValue(EnableProperty);
}
