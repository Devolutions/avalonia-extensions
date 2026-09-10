namespace Devolutions.AvaloniaControls.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Shared machinery behind <see cref="TreeDataGridAlternatingRowBehavior"/> and
/// <see cref="TreeDataGridSelectionRunBehavior"/>.
///
/// <para>
/// Both decorations answer the same question — what pseudo-classes does each realized row carry
/// right now — and both have to recompute after every layout pass, because
/// <c>TreeDataGridRow.RowIndex</c> is a plain CLR property that raises no change notification and
/// goes stale as rows are recycled during virtualisation. Running them as two independent
/// behaviors meant two subscriptions, two dispatcher operations and two walks of the realized rows
/// per layout pass, so they share one of each here.
/// </para>
///
/// <para>
/// The two decorations stay independently switchable: a theme may enable either without the other,
/// and each clears only the pseudo-classes it owns when it is turned off. The decorator itself is
/// retired once neither is left on.
/// </para>
///
/// <para>
/// Updates are coalesced through <see cref="Dispatcher.UIThread"/> at
/// <see cref="DispatcherPriority.Render"/> so that rapid consecutive layout passes (for example
/// during fast scrolling) collapse into a single sweep, without blocking higher-priority input or
/// layout work.
/// </para>
/// </summary>
internal sealed class TreeDataGridRowDecorator : IDisposable
{
    private const string OddRowClass = ":odd-row";

    private const string RunFirstClass = ":sel-run-first";

    private const string RunMiddleClass = ":sel-run-middle";

    private const string RunLastClass = ":sel-run-last";

    private static readonly ConditionalWeakTable<TreeDataGrid, TreeDataGridRowDecorator> Decorators = new();

    private readonly TreeDataGrid treeDataGrid;

    private readonly Dictionary<int, bool> selectionByRowIndex = new();

    private DispatcherOperation? scheduledUpdate;

    private bool alternatingRows;

    private bool selectionRuns;

    private bool disposed;

    private TreeDataGridRowDecorator(TreeDataGrid treeDataGrid)
    {
        this.treeDataGrid = treeDataGrid;
        this.treeDataGrid.LayoutUpdated += this.OnLayoutUpdated;
    }

    /// <summary>
    ///   Turns the <c>:odd-row</c> pseudo-class on or off for this grid.
    /// </summary>
    public static void SetAlternatingRows(TreeDataGrid treeDataGrid, bool enabled)
    {
        TreeDataGridRowDecorator? decorator = Resolve(treeDataGrid, create: enabled);
        if (decorator is null || decorator.alternatingRows == enabled) return;

        decorator.alternatingRows = enabled;

        if (enabled)
        {
            decorator.ScheduleUpdate();

            return;
        }

        decorator.ForEachRealizedRow(static row => SetClass(row, OddRowClass, false));
        decorator.RetireIfUnused(treeDataGrid);
    }

    /// <summary>
    ///   Turns the <c>:sel-run-first</c>, <c>:sel-run-middle</c> and <c>:sel-run-last</c>
    ///   pseudo-classes on or off for this grid.
    /// </summary>
    public static void SetSelectionRuns(TreeDataGrid treeDataGrid, bool enabled)
    {
        TreeDataGridRowDecorator? decorator = Resolve(treeDataGrid, create: enabled);
        if (decorator is null || decorator.selectionRuns == enabled) return;

        decorator.selectionRuns = enabled;

        // Changing the selection invalidates no layout, so run positions need a notification of
        // their own. Alternating rows depend only on row index and do not.
        if (enabled)
        {
            treeDataGrid.SelectionChanged += decorator.OnSelectionChanged;
            decorator.ScheduleUpdate();

            return;
        }

        treeDataGrid.SelectionChanged -= decorator.OnSelectionChanged;
        decorator.ForEachRealizedRow(static row => SetRunClasses(row, false, false, false));
        decorator.RetireIfUnused(treeDataGrid);
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
    }

    private static TreeDataGridRowDecorator? Resolve(TreeDataGrid treeDataGrid, bool create)
    {
        if (Decorators.TryGetValue(treeDataGrid, out TreeDataGridRowDecorator? decorator))
        {
            return decorator;
        }

        if (!create) return null;

        decorator = new TreeDataGridRowDecorator(treeDataGrid);
        Decorators.Add(treeDataGrid, decorator);

        return decorator;
    }

    private static void SetClass(TreeDataGridRow row, string pseudoClass, bool value) =>
        ((IPseudoClasses)row.Classes).Set(pseudoClass, value);

    private static void SetRunClasses(TreeDataGridRow row, bool first, bool middle, bool last)
    {
        var classes = (IPseudoClasses)row.Classes;
        classes.Set(RunFirstClass, first);
        classes.Set(RunMiddleClass, middle);
        classes.Set(RunLastClass, last);
    }

    private void RetireIfUnused(TreeDataGrid treeDataGrid)
    {
        if (this.alternatingRows || this.selectionRuns) return;

        this.Dispose();
        Decorators.Remove(treeDataGrid);
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

        if (this.selectionRuns)
        {
            // A row's neighbours decide its run position, so the whole viewport is mapped first.
            this.selectionByRowIndex.Clear();

            foreach (Visual child in presenter.GetVisualChildren())
            {
                if (child is TreeDataGridRow row && row.RowIndex >= 0)
                {
                    this.selectionByRowIndex[row.RowIndex] = row.IsSelected;
                }
            }
        }

        // Realized rows are always walked, never short-circuited on an empty selection: a
        // recycled container must have any stale class cleared before it is reused.
        foreach (Visual child in presenter.GetVisualChildren())
        {
            if (child is not TreeDataGridRow row)
            {
                continue;
            }

            if (this.alternatingRows)
            {
                SetClass(row, OddRowClass, row.RowIndex % 2 == 1);
            }

            if (this.selectionRuns)
            {
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
    }

    private bool IsSelectedAt(int rowIndex) =>
        this.selectionByRowIndex.TryGetValue(rowIndex, out bool isSelected) && isSelected;

    private void ForEachRealizedRow(Action<TreeDataGridRow> action)
    {
        TreeDataGridRowsPresenter? presenter = this.treeDataGrid.RowsPresenter;
        if (presenter is null) return;

        foreach (Visual child in presenter.GetVisualChildren())
        {
            if (child is TreeDataGridRow row)
            {
                action(row);
            }
        }
    }
}
