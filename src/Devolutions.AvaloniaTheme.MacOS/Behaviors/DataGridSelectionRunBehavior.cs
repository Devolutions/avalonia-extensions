namespace Devolutions.AvaloniaTheme.MacOS.Behaviors;

using System.Collections;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Attached behavior that marks each realized <see cref="DataGridRow"/> with its position inside a
/// contiguous run of selected rows, using the <c>:sel-run-first</c>, <c>:sel-run-middle</c> and
/// <c>:sel-run-last</c> pseudo-classes.
///
/// <para>
/// A theme uses these to draw a multi-row selection as one shape rather than a stack of separate
/// pills: the first row of a run keeps only its top corners, the last row only its bottom corners,
/// and every row between them is squared off. A lone selected row is given no pseudo-class at all,
/// because the unconditional row style already rounds all four corners.
/// </para>
///
/// <para>
/// Neighbours are looked up through <see cref="DataGrid.CollectionView"/> instead of the realized
/// rows, so a run continuing past the top or bottom of the viewport still keeps a squared-off end
/// there rather than appearing to stop at the viewport edge.
/// </para>
///
/// <para>
/// Grouped views are deliberately left untouched. <see cref="DataGridRow.Index"/> counts rows only,
/// skipping group-header slots, so two rows sitting in different groups can hold consecutive
/// indexes without being visually adjacent — and the slot number that would tell them apart is not
/// public. Rows in a grouped view therefore keep the fully rounded selection they have always had.
/// </para>
///
/// <para>
/// Updates are coalesced through <see cref="Dispatcher.UIThread"/> at
/// <see cref="DispatcherPriority.Render"/> so that rapid consecutive layout passes (for example
/// during fast scrolling) collapse into a single sweep.
/// </para>
/// </summary>
internal static class DataGridSelectionRunBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, bool>("Enable", typeof(DataGridSelectionRunBehavior));

    private const string RunFirstClass = ":sel-run-first";

    private const string RunMiddleClass = ":sel-run-middle";

    private const string RunLastClass = ":sel-run-last";

    private static readonly ConditionalWeakTable<DataGrid, SelectionRunState> States = new();

    static DataGridSelectionRunBehavior()
    {
        EnableProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is DataGrid dataGrid)
            {
                bool enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    Enable(dataGrid);
                }
                else
                {
                    Disable(dataGrid);
                }
            }
        });
    }

    public static void SetEnable(DataGrid element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(DataGrid element) => element.GetValue(EnableProperty);

    private static void Enable(DataGrid dataGrid)
    {
        if (States.TryGetValue(dataGrid, out _))
        {
            return;
        }

        var state = new SelectionRunState(dataGrid);
        States.Add(dataGrid, state);
    }

    private static void Disable(DataGrid dataGrid)
    {
        if (!States.TryGetValue(dataGrid, out var state))
        {
            return;
        }

        state.Dispose();
        States.Remove(dataGrid);
    }

    private sealed class SelectionRunState : IDisposable
    {
        private readonly DataGrid dataGrid;
        private DataGridRowsPresenter? rowsPresenter;
        private DispatcherOperation? scheduledUpdate;
        private HashSet<object>? selectedItemsCache;
        private int selectedItemsCacheCount = -1;
        private bool selectionDirty = true;
        private bool disposed;

        public SelectionRunState(DataGrid dataGrid)
        {
            this.dataGrid = dataGrid;
            this.dataGrid.LayoutUpdated += this.OnLayoutUpdated;
            this.dataGrid.SelectionChanged += this.OnSelectionChanged;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => this.ScheduleUpdate();

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            this.selectionDirty = true;
            this.ScheduleUpdate();
        }

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

            DataGridRowsPresenter? presenter = this.GetRowsPresenter();
            if (presenter is null) return;

            // A run needs at least two selected rows to exist at all, so anything less skips
            // straight to clearing. Realized rows are always walked, never short-circuited: a
            // recycled container must have any stale run class cleared before it is reused.
            IList? selectedItems = this.dataGrid.SelectedItems;
            IList? view = GetUngroupedView(this.dataGrid);
            HashSet<object>? selected = this.GetSelectedSet(selectedItems);
            bool merge = view is not null && selected is not null;

            foreach (Visual child in presenter.GetVisualChildren())
            {
                if (child is not DataGridRow row)
                {
                    continue;
                }

                bool first = false;
                bool middle = false;
                bool last = false;

                if (merge && row.IsSelected)
                {
                    int index = row.Index;
                    bool previousSelected = IsSelectedAt(view!, selected!, index - 1);
                    bool nextSelected = IsSelectedAt(view!, selected!, index + 1);

                    first = !previousSelected && nextSelected;
                    middle = previousSelected && nextSelected;
                    last = previousSelected && !nextSelected;
                }

                SetRunClasses(row, first, middle, last);
            }
        }

        private DataGridRowsPresenter? GetRowsPresenter()
        {
            // The presenter is created with the template, which may not have been applied yet the
            // first time this runs, so resolve lazily and keep retrying until it appears.
            this.rowsPresenter ??= this.dataGrid.GetVisualDescendants().OfType<DataGridRowsPresenter>().FirstOrDefault();

            return this.rowsPresenter;
        }

        /// <summary>
        ///   Returns the selected items as a lookup set, or <c>null</c> when there is no run to
        ///   draw (fewer than two selected rows).
        ///
        ///   <para>
        ///   The set is cached and rebuilt only when the selection changes. Rebuilding it on every
        ///   pass would make the cost scale with the size of the selection rather than the
        ///   viewport, which is measurable once a large grid is fully selected: at 20k selected
        ///   rows it roughly doubled the per-layout cost during scrolling.
        ///   </para>
        /// </summary>
        private HashSet<object>? GetSelectedSet(IList? selectedItems)
        {
            int count = selectedItems?.Count ?? 0;

            if (count < 2)
            {
                this.selectedItemsCache = null;
                this.selectedItemsCacheCount = count;
                this.selectionDirty = false;

                return null;
            }

            // The count check is a backstop for selection edits that arrive without a
            // SelectionChanged notification.
            if (!this.selectionDirty && this.selectedItemsCache is not null && this.selectedItemsCacheCount == count)
            {
                return this.selectedItemsCache;
            }

            this.selectedItemsCache = BuildSelectedSet(selectedItems!);
            this.selectedItemsCacheCount = count;
            this.selectionDirty = false;

            return this.selectedItemsCache;
        }

        private static IList? GetUngroupedView(DataGrid dataGrid)
        {
            IDataGridCollectionView? view = dataGrid.CollectionView;

            return view is { IsGrouping: false } ? view as IList : null;
        }

        private static HashSet<object> BuildSelectedSet(IList selectedItems)
        {
            HashSet<object> selected = new(selectedItems.Count);

            foreach (object? item in selectedItems)
            {
                if (item is not null)
                {
                    selected.Add(item);
                }
            }

            return selected;
        }

        private static bool IsSelectedAt(IList view, HashSet<object> selected, int index) =>
            index >= 0 && index < view.Count && view[index] is { } item && selected.Contains(item);

        private static void SetRunClasses(DataGridRow row, bool first, bool middle, bool last)
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
            this.selectedItemsCache = null;

            this.dataGrid.LayoutUpdated -= this.OnLayoutUpdated;
            this.dataGrid.SelectionChanged -= this.OnSelectionChanged;

            // Clean up pseudo-classes from any currently realized rows
            DataGridRowsPresenter? presenter = this.rowsPresenter;
            if (presenter is null) return;

            foreach (Visual child in presenter.GetVisualChildren())
            {
                if (child is DataGridRow row)
                {
                    SetRunClasses(row, false, false, false);
                }
            }
        }
    }
}
