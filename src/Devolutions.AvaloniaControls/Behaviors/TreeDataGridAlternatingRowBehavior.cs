namespace Devolutions.AvaloniaControls.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
/// This behavior subscribes to the parent <see cref="TreeDataGrid"/>'s
/// <c>LayoutUpdated</c> event and, after each layout pass, imperatively reads
/// <c>RowIndex</c> from every realized row and toggles the <c>:odd-row</c>
/// pseudo-class accordingly.  The update is coalesced via
/// <see cref="Dispatcher.UIThread"/> at <see cref="DispatcherPriority.Render"/>
/// so that rapid consecutive layout passes (e.g. during fast scrolling) are
/// debounced into a single pseudo-class sweep without blocking higher-priority
/// input or layout work.
/// </para>
/// </summary>
public static class TreeDataGridAlternatingRowBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGrid, bool>("Enable", typeof(TreeDataGridAlternatingRowBehavior));

    private static readonly ConditionalWeakTable<TreeDataGrid, AlternatingRowState> States = new();

    static TreeDataGridAlternatingRowBehavior()
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

        var state = new AlternatingRowState(treeDataGrid);
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

    private sealed class AlternatingRowState : IDisposable
    {
        private readonly TreeDataGrid treeDataGrid;
        private DispatcherOperation? scheduledUpdate;
        private bool disposed;

        public AlternatingRowState(TreeDataGrid treeDataGrid)
        {
            this.treeDataGrid = treeDataGrid;
            this.treeDataGrid.LayoutUpdated += this.OnLayoutUpdated;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (this.disposed) return;

            this.scheduledUpdate?.Abort();
            this.scheduledUpdate = Dispatcher.UIThread.InvokeAsync(this.UpdatePseudoClasses, DispatcherPriority.Render);
        }

        private void UpdatePseudoClasses()
        {
            if (this.disposed) return;

            TreeDataGridRowsPresenter? rowsPresenter = this.treeDataGrid.RowsPresenter;
            if (rowsPresenter is null) return;

            foreach (Visual child in rowsPresenter.GetVisualChildren())
            {
                if (child is TreeDataGridRow row)
                {
                    bool isOdd = row.RowIndex % 2 == 1;
                    ((IPseudoClasses)row.Classes).Set(":odd-row", isOdd);
                }
            }

            this.scheduledUpdate = null;
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;
            
            this.scheduledUpdate?.Abort();

            this.treeDataGrid.LayoutUpdated -= this.OnLayoutUpdated;

            // Clean up pseudo-classes from any currently realized rows
            TreeDataGridRowsPresenter? rowsPresenter = this.treeDataGrid.RowsPresenter;
            if (rowsPresenter is null) return;

            foreach (Visual child in rowsPresenter.GetVisualChildren())
            {
                if (child is TreeDataGridRow row)
                {
                    ((IPseudoClasses)row.Classes).Set(":odd-row", false);
                }
            }
        }
    }
}
