namespace Devolutions.AvaloniaControls.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

/// <summary>
/// This behavior sets :flat or :hierarchical pseudo-classes on a TreeDataGrid dynamically
/// depending on its Source type.
/// </summary>
public static class TreeDataGridPseudoTypeBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TreeDataGrid, bool>("Enable", typeof(TreeDataGridPseudoTypeBehavior));

    private static readonly ConditionalWeakTable<TreeDataGrid, TreeDataGridPseudoTypeState> States = new();

    static TreeDataGridPseudoTypeBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
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

        var state = new TreeDataGridPseudoTypeState(treeDataGrid);
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

    private sealed class TreeDataGridPseudoTypeState : IDisposable
    {
        private readonly TreeDataGrid treeDataGrid;
        private IDisposable? sourceSubscription;
        private bool disposed;

        public TreeDataGridPseudoTypeState(TreeDataGrid treeDataGrid)
        {
            this.treeDataGrid = treeDataGrid;

            this.sourceSubscription = treeDataGrid
                .GetObservable(TreeDataGrid.SourceProperty)
                .Subscribe(_ => this.UpdatePseudoClasses());

            this.UpdatePseudoClasses();
        }

        private void UpdatePseudoClasses()
        {
            if (this.disposed) return;

            bool isHierarchical = this.treeDataGrid.Source?.IsHierarchical ?? false;

            ((IPseudoClasses)this.treeDataGrid.Classes).Set(":hierarchical", isHierarchical);
            ((IPseudoClasses)this.treeDataGrid.Classes).Set(":flat", !isHierarchical);
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;

            this.sourceSubscription?.Dispose();
            this.sourceSubscription = null;

            ((IPseudoClasses)this.treeDataGrid.Classes).Set(":hierarchical", false);
            ((IPseudoClasses)this.treeDataGrid.Classes).Set(":flat", false);
        }
    }
}
