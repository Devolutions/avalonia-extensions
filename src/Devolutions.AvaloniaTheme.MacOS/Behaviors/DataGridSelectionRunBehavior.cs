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
/// A neighbour that is realized is asked directly, through <see cref="DataGridRow.IsSelected"/>:
/// the row is matched by index and no comparison between items takes place. Only a neighbour that
/// is not realized — at most the row just above and the row just below the viewport — is resolved
/// by looking its item up in the selection, so a run continuing past the edge of the viewport
/// still keeps a squared-off end there rather than appearing to stop at the edge.
/// </para>
///
/// <para>
/// That fallback is the one place where items are compared, and it compares them by reference
/// rather than by <see cref="object.Equals"/>. The row that would pay for a wrong answer is the
/// realized one at the edge of the viewport, and that row is fully visible whenever the scroll
/// offset lands on a row boundary, so a value-equal duplicate just outside the viewport would put
/// a visibly wrong corner on it.
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

    private const string RowsPresenterName = "PART_RowsPresenter";

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
        private readonly Dictionary<int, bool> realizedSelection = new();
        private readonly List<DataGridRow> realizedRows = new();
        private DataGridRowsPresenter? rowsPresenter;
        private DispatcherOperation? scheduledUpdate;
        private HashSet<object>? selectedItemsCache;
        private int selectedItemsCacheCount = -1;
        private bool selectionDirty = true;
        private bool disposed;

        public SelectionRunState(DataGrid dataGrid)
        {
            this.dataGrid = dataGrid;
            this.dataGrid.TemplateApplied += this.OnTemplateApplied;
            this.dataGrid.LayoutUpdated += this.OnLayoutUpdated;
            this.dataGrid.SelectionChanged += this.OnSelectionChanged;
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) =>
            this.rowsPresenter = e.NameScope.Find<DataGridRowsPresenter>(RowsPresenterName);

        private void OnLayoutUpdated(object? sender, EventArgs e) => this.ScheduleUpdate();

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Drop the old set here rather than leaving it for the next GetSelectedSet to
            // replace. That only runs for an off-viewport neighbour, so a selection that stops
            // needing one would otherwise keep the previous selection's items alive for as long
            // as the grid lives.
            this.selectedItemsCache = null;
            this.selectedItemsCacheCount = -1;
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

            // Map the viewport first: a row's neighbours decide its run position, and a realized
            // neighbour answers for itself far more reliably than its item does. Neighbours may
            // come later in child order, so nothing can be decided during this walk -- collect
            // the rows as we go and let the second phase iterate those instead of the children.
            //
            // The list is a buffer reused across sweeps so that collecting the viewport does not
            // allocate. It is emptied in the finally below rather than left populated, so it
            // never holds rows between sweeps; the finally also means an aborted sweep cannot
            // leave stale rows behind for the next one to reprocess.
            this.realizedSelection.Clear();

            try
            {
                foreach (Visual child in presenter.GetVisualChildren())
                {
                    if (child is not DataGridRow row)
                    {
                        continue;
                    }

                    this.realizedRows.Add(row);

                    if (row.Index >= 0)
                    {
                        this.realizedSelection[row.Index] = row.IsSelected;
                    }
                }

                // A run needs at least two selected rows to exist at all, so anything less skips
                // straight to clearing. Realized rows are always walked, never short-circuited: a
                // recycled container must have any stale run class cleared before it is reused.
                // Grouping is what decides whether runs apply at all; whether the view happens to
                // support indexing only decides how far off-screen we can see.
                bool merge = IsUngrouped(this.dataGrid) && (this.dataGrid.SelectedItems?.Count ?? 0) >= 2;
                IList? view = GetIndexableView(this.dataGrid);

                foreach (DataGridRow row in this.realizedRows)
                {
                    bool first = false;
                    bool middle = false;
                    bool last = false;

                    if (merge && row.IsSelected)
                    {
                        int index = row.Index;
                        bool previousSelected = this.IsSelectedAt(view, index - 1);
                        bool nextSelected = this.IsSelectedAt(view, index + 1);

                        first = !previousSelected && nextSelected;
                        middle = previousSelected && nextSelected;
                        last = previousSelected && !nextSelected;
                    }

                    SetRunClasses(row, first, middle, last);
                }
            }
            finally
            {
                this.realizedRows.Clear();
            }
        }

        /// <summary>
        ///   Answers whether the row at <paramref name="index"/> is selected, preferring the
        ///   realized row because it knows its own state exactly. Falling back to the item lookup
        ///   only happens for a neighbour outside the viewport, and it is that fallback — not the
        ///   realized path — that depends on how the item type defines equality.
        /// </summary>
        private bool IsSelectedAt(IList? view, int index)
        {
            if (this.realizedSelection.TryGetValue(index, out bool isSelected))
            {
                return isSelected;
            }

            if (view is null || index < 0 || index >= view.Count)
            {
                return false;
            }

            HashSet<object>? selected = this.GetSelectedSet();

            return selected is not null && view[index] is { } item && selected.Contains(item);
        }

        /// <summary>
        ///   Returns the rows presenter for the template currently in force.
        ///
        ///   <para>
        ///   The control theme enables this behavior while styling, which runs before the template
        ///   is applied, so <see cref="OnTemplateApplied"/> supplies the presenter and the common
        ///   path here is a plain field read — no walking the visual tree once per layout pass.
        ///   </para>
        ///
        ///   <para>
        ///   Walking is the fallback for the two cases that leaves: a behavior enabled on a grid
        ///   that is already templated, where TemplateApplied will not fire again, and a presenter
        ///   that left the tree without a new template arriving. Both resolve on the first sweep
        ///   that needs them, and until one does the grid has next to no visual children to walk.
        ///   </para>
        /// </summary>
        private DataGridRowsPresenter? GetRowsPresenter()
        {
            if (this.rowsPresenter is null || this.rowsPresenter.GetVisualParent() is null)
            {
                this.rowsPresenter = FindRowsPresenter(this.dataGrid);
            }

            return this.rowsPresenter;
        }

        private static DataGridRowsPresenter? FindRowsPresenter(DataGrid dataGrid) =>
            dataGrid.GetVisualDescendants().OfType<DataGridRowsPresenter>().FirstOrDefault();

        /// <summary>
        ///   Returns the selected items as a lookup set, or <c>null</c> when there is no run to
        ///   draw (fewer than two selected rows).
        ///
        ///   <para>
        ///   Only <see cref="IsSelectedAt"/> calls this, and only for a neighbour outside the
        ///   viewport, so a selection that does not run past the edge never builds the set at all
        ///   and nothing is held on to.
        ///   </para>
        ///
        ///   <para>
        ///   When it is needed the set is cached and rebuilt only as the selection changes.
        ///   Rebuilding it per pass would make the cost scale with the size of the selection
        ///   rather than the viewport, which is measurable once a large grid is fully selected: at
        ///   20k selected rows it roughly doubled the per-layout cost during scrolling.
        ///   </para>
        /// </summary>
        private HashSet<object>? GetSelectedSet()
        {
            IList? selectedItems = this.dataGrid.SelectedItems;
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

        private static bool IsUngrouped(DataGrid dataGrid) => dataGrid.CollectionView is { IsGrouping: false };

        /// <summary>
        ///   Returns the view as an indexable list, or <c>null</c> when it does not offer one.
        ///
        ///   <para>
        ///   Only the off-viewport fallback needs random access by index.
        ///   <see cref="IDataGridCollectionView"/> guarantees no more than
        ///   <see cref="IEnumerable"/>, so a custom view need not be an <see cref="IList"/> even
        ///   though the built-in <c>DataGridCollectionView</c> is. When it is not, an unrealized
        ///   neighbour is treated as unselected and only the ends of a run at the edge of the
        ///   viewport lose their squared corner -- run styling itself keeps working from the
        ///   realized rows.
        ///   </para>
        /// </summary>
        private static IList? GetIndexableView(DataGrid dataGrid) =>
            dataGrid.CollectionView is { IsGrouping: false } view ? view as IList : null;

        /// <summary>
        ///   Builds the lookup set on reference identity rather than <see cref="object.Equals"/>.
        ///
        ///   <para>
        ///   Two distinct rows holding equal items are different instances, so identity is what
        ///   actually answers "is the row at this index selected". Value equality cannot: items
        ///   are frequently records, and an unselected row holding a duplicate of a selected item
        ///   would be read as selected, squaring off an end that the run does not reach.
        ///   </para>
        ///
        ///   <para>
        ///   This is hardening rather than a fix for a reproducible failure. DataGrid resolves an
        ///   item to a row by value equality itself, so asking it to select the second of two
        ///   equal items selects the first instead, and the mismatch mostly cannot arise through
        ///   <see cref="DataGrid.SelectedItems"/> at all. Whatever it does put in that collection
        ///   comes from the view, so matching on identity costs nothing.
        ///   </para>
        ///
        ///   <para>
        ///   The trade-off is a view over boxed value types, where the indexer hands back a fresh
        ///   box each call and no lookup can match. That degrades to treating an off-viewport
        ///   neighbour as unselected, which is what TreeDataGrid does for every neighbour it
        ///   cannot see -- a rounded end where a squared one belongs, rather than a wrong answer.
        ///   </para>
        /// </summary>
        private static HashSet<object> BuildSelectedSet(IList selectedItems)
        {
            HashSet<object> selected = new(selectedItems.Count, ReferenceEqualityComparer.Instance);

            foreach (object? item in selectedItems)
            {
                if (item is not null)
                {
                    selected.Add(item);
                }
            }

            return selected;
        }

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
            this.realizedSelection.Clear();
            this.realizedRows.Clear();

            this.dataGrid.TemplateApplied -= this.OnTemplateApplied;
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
