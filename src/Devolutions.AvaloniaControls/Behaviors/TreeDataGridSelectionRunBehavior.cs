namespace Devolutions.AvaloniaControls.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
/// Updates are coalesced through <see cref="Dispatcher.UIThread"/> at
/// <see cref="DispatcherPriority.Render"/> so that rapid consecutive layout passes (for example
/// during fast scrolling) collapse into a single sweep.
/// </para>
/// </summary>
public static class TreeDataGridSelectionRunBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGrid, bool>("Enable", typeof(TreeDataGridSelectionRunBehavior));

    private const string RunFirstClass = ":sel-run-first";

    private const string RunMiddleClass = ":sel-run-middle";

    private const string RunLastClass = ":sel-run-last";

    private static readonly ConditionalWeakTable<TreeDataGrid, SelectionRunState> States = new();

    static TreeDataGridSelectionRunBehavior()
    {
        EnableProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is TreeDataGrid treeDataGrid)
            {
                bool enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    Enable(treeDataGrid);
                }
                else
                {
                    Disable(treeDataGrid);
                }
            }
        });
    }

    public static void SetEnable(TreeDataGrid element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(TreeDataGrid element) => element.GetValue(EnableProperty);

    private static void Enable(TreeDataGrid treeDataGrid)
    {
        if (States.TryGetValue(treeDataGrid, out _))
        {
            return;
        }

        var state = new SelectionRunState(treeDataGrid);
        States.Add(treeDataGrid, state);
    }

    private static void Disable(TreeDataGrid treeDataGrid)
    {
        if (!States.TryGetValue(treeDataGrid, out var state))
        {
            return;
        }

        state.Dispose();
        States.Remove(treeDataGrid);
    }

    private sealed class SelectionRunState : IDisposable
    {
        private readonly TreeDataGrid treeDataGrid;
        private readonly Dictionary<int, bool> selectionByRowIndex = new();
        private DispatcherOperation? scheduledUpdate;
        private bool disposed;

        public SelectionRunState(TreeDataGrid treeDataGrid)
        {
            this.treeDataGrid = treeDataGrid;
            this.treeDataGrid.LayoutUpdated += this.OnLayoutUpdated;
            this.treeDataGrid.SelectionChanged += this.OnSelectionChanged;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => this.ScheduleUpdate();

        private void OnSelectionChanged(object? sender, TreeDataGridSelectionChangedEventArgs e) => this.ScheduleUpdate();

        private void ScheduleUpdate()
        {
            if (this.disposed) return;

            this.scheduledUpdate?.Abort();
            this.scheduledUpdate = Dispatcher.UIThread.InvokeAsync(this.UpdatePseudoClasses, DispatcherPriority.Render);
        }

        private void UpdatePseudoClasses()
        {
            if (this.disposed) return;

            this.scheduledUpdate = null;

            TreeDataGridRowsPresenter? presenter = this.treeDataGrid.RowsPresenter;
            if (presenter is null) return;

            // A row's neighbours decide its run position, so the whole viewport is mapped first.
            this.selectionByRowIndex.Clear();

            foreach (Visual child in presenter.GetVisualChildren())
            {
                if (child is TreeDataGridRow row && row.RowIndex >= 0)
                {
                    this.selectionByRowIndex[row.RowIndex] = row.IsSelected;
                }
            }

            // Realized rows are always walked, never short-circuited on an empty selection: a
            // recycled container must have any stale run class cleared before it is reused.
            foreach (Visual child in presenter.GetVisualChildren())
            {
                if (child is not TreeDataGridRow row)
                {
                    continue;
                }

                bool first = false;
                bool middle = false;
                bool last = false;
                int index = row.RowIndex;

                if (index >= 0 && row.IsSelected)
                {
                    bool previousSelected = this.IsSelectedAt(index - 1);
                    bool nextSelected = this.IsSelectedAt(index + 1);

                    first = !previousSelected && nextSelected;
                    middle = previousSelected && nextSelected;
                    last = previousSelected && !nextSelected;
                }

                SetRunClasses(row, first, middle, last);
            }
        }

        private bool IsSelectedAt(int rowIndex) =>
            this.selectionByRowIndex.TryGetValue(rowIndex, out bool isSelected) && isSelected;

        private static void SetRunClasses(TreeDataGridRow row, bool first, bool middle, bool last)
        {
            var classes = (IPseudoClasses)row.Classes;
            classes.Set(RunFirstClass, first);
            classes.Set(RunMiddleClass, middle);
            classes.Set(RunLastClass, last);
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;

            this.scheduledUpdate?.Abort();
            this.scheduledUpdate = null;

            this.treeDataGrid.LayoutUpdated -= this.OnLayoutUpdated;
            this.treeDataGrid.SelectionChanged -= this.OnSelectionChanged;
            this.selectionByRowIndex.Clear();

            // Clean up pseudo-classes from any currently realized rows
            TreeDataGridRowsPresenter? presenter = this.treeDataGrid.RowsPresenter;
            if (presenter is null) return;

            foreach (Visual child in presenter.GetVisualChildren())
            {
                if (child is TreeDataGridRow row)
                {
                    SetRunClasses(row, false, false, false);
                }
            }
        }
    }
}
