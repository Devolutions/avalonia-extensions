namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Works around an Avalonia limitation where a <see cref="Grid"/> shared-size group never
/// shrinks once it has measured a width.
///
/// <para>
/// Menu item templates align their leading toggle (check/radio) and icon columns across
/// sibling items using <c>Grid.IsSharedSizeScope</c> + <c>SharedSizeGroup</c>. When the set of
/// occupied leading columns shrinks — the last checked item is unchecked, the last item with an
/// icon is removed or hidden, or the <c>single-icon-slot</c> class is toggled off/on — the vacated
/// column keeps its previously measured width, leaving a phantom empty column that pushes the
/// header text across.
/// </para>
///
/// <para>
/// This behavior is attached to every <see cref="MenuItem"/> (via the control theme). All items
/// that live under the same shared-size scope share a single <see cref="ScopeState"/>, which
/// observes the inputs that decide the occupied leading columns — each item's <c>Icon</c>,
/// <c>IsChecked</c> and <c>IsVisible</c>, the item collection, and the owner's <c>single-icon-slot</c>
/// class — and, whenever that "needed columns" signature changes, rebuilds the scope by toggling
/// <c>Grid.IsSharedSizeScope</c> off and back on. Rebuilding forces a fresh measurement pass so
/// columns that are no longer needed collapse to zero.
/// </para>
/// </summary>
public static class MenuSharedSizeRefreshBehavior
{
    private const string SingleIconSlotClass = "single-icon-slot";

    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("Enable", typeof(MenuSharedSizeRefreshBehavior));

    // One state per shared-size scope owner, shared by every menu item under it.
    private static readonly ConditionalWeakTable<Control, ScopeState> states = new();

    static MenuSharedSizeRefreshBehavior()
    {
        EnableProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is not MenuItem menuItem)
            {
                return;
            }

            if (args.NewValue.GetValueOrDefault<bool>())
            {
                menuItem.AttachedToVisualTree += OnItemAttached;
                if (menuItem.GetVisualParent() is not null)
                {
                    Register(menuItem);
                }
            }
            else
            {
                menuItem.AttachedToVisualTree -= OnItemAttached;
            }
        });
    }

    public static void SetEnable(MenuItem element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(MenuItem element) => element.GetValue(EnableProperty);

    private static void OnItemAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            Register(menuItem);
        }
    }

    private static void Register(MenuItem menuItem)
    {
        Control? scopeOwner = FindScopeOwner(menuItem);
        if (scopeOwner is null)
        {
            return;
        }

        if (!states.TryGetValue(scopeOwner, out _))
        {
            states.Add(scopeOwner, new ScopeState(scopeOwner));
        }
    }

    private static Control? FindScopeOwner(Visual from)
    {
        foreach (Visual ancestor in from.GetVisualAncestors())
        {
            if (ancestor is Control control && Grid.GetIsSharedSizeScope(control))
            {
                return control;
            }
        }

        return null;
    }

    /// <summary>The leading columns/modes a shared-size scope currently needs to reserve.</summary>
    [Flags]
    private enum LeadingColumns
    {
        None = 0,
        SingleIconSlot = 1 << 0,
        Icon = 1 << 1,
        Check = 1 << 2,
    }

    private sealed class ScopeState
    {
        private readonly Control scopeOwner;
        private readonly ItemsControl? owner;
        private readonly Dictionary<MenuItem, IDisposable[]> itemSubscriptions = new();
        private LeadingColumns? lastColumns;
        private DispatcherOperation? scheduled;
        private bool rescoping;
        private bool disposed;

        public ScopeState(Control scopeOwner)
        {
            this.scopeOwner = scopeOwner;
            this.owner = scopeOwner.TemplatedParent as ItemsControl ?? scopeOwner.FindAncestorOfType<ItemsControl>();

            this.scopeOwner.DetachedFromVisualTree += this.OnScopeOwnerDetached;

            if (this.owner is not null)
            {
                this.owner.ContainerPrepared += this.OnContainerPrepared;
                this.owner.ContainerClearing += this.OnContainerClearing;
                ((INotifyCollectionChanged)this.owner.Classes).CollectionChanged += this.OnClassesChanged;

                foreach (Control container in this.owner.GetRealizedContainers())
                {
                    this.Observe(container);
                }
            }

            this.Evaluate();
        }

        private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            this.Observe(e.Container);
            this.Evaluate();
        }

        private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
        {
            this.Unobserve(e.Container);
            this.Evaluate();
        }

        private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // The Classes collection also churns for pseudo-classes (:pointerover, :selected, ...);
            // only react when the single-icon-slot class itself is added or removed.
            if (e.Action == NotifyCollectionChangedAction.Reset
                || e.NewItems?.OfType<string>().Contains(SingleIconSlotClass) == true
                || e.OldItems?.OfType<string>().Contains(SingleIconSlotClass) == true)
            {
                this.Evaluate();
            }
        }

        private void Observe(Control container)
        {
            if (container is not MenuItem menuItem || this.itemSubscriptions.ContainsKey(menuItem))
            {
                return;
            }

            // Anything that changes which leading columns an item occupies.
            this.itemSubscriptions[menuItem] = new[]
            {
                menuItem.GetObservable(Visual.IsVisibleProperty).Subscribe(_ => this.Evaluate()),
                menuItem.GetObservable(MenuItem.IconProperty).Subscribe(_ => this.Evaluate()),
                menuItem.GetObservable(MenuItem.IsCheckedProperty).Subscribe(_ => this.Evaluate()),
            };
        }

        private void Unobserve(Control container)
        {
            if (container is MenuItem menuItem && this.itemSubscriptions.Remove(menuItem, out IDisposable[]? subscriptions))
            {
                foreach (IDisposable subscription in subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }

        private void Evaluate()
        {
            if (this.disposed || this.rescoping)
            {
                return;
            }

            LeadingColumns columns = this.ComputeColumns();
            if (columns == this.lastColumns)
            {
                return;
            }

            this.lastColumns = columns;
            this.ScheduleRescope();
        }

        private LeadingColumns ComputeColumns()
        {
            LeadingColumns columns = LeadingColumns.None;

            // Whether the single-icon-slot mode is active for this scope (class typically sits on
            // the menu container that owns these items, or an ancestor menu).
            bool singleIconSlot = this.scopeOwner.GetVisualAncestors()
                .Prepend(this.scopeOwner)
                .Any(v => v is StyledElement styled and (ContextMenu or Menu or MenuFlyoutPresenter or MenuItem)
                          && styled.Classes.Contains(SingleIconSlotClass));
            if (singleIconSlot)
            {
                columns |= LeadingColumns.SingleIconSlot;
            }

            foreach (MenuItem item in this.itemSubscriptions.Keys)
            {
                if (!item.IsVisible)
                {
                    continue;
                }

                if (item.Icon is not null)
                {
                    columns |= LeadingColumns.Icon;
                }

                if (item.IsChecked)
                {
                    columns |= LeadingColumns.Check;
                }
            }

            return columns;
        }

        private void ScheduleRescope()
        {
            this.scheduled?.Abort();
            this.scheduled = Dispatcher.UIThread.InvokeAsync(this.Rescope, DispatcherPriority.Render);
        }

        private void Rescope()
        {
            this.scheduled = null;

            if (this.disposed || !Grid.GetIsSharedSizeScope(this.scopeOwner))
            {
                return;
            }

            // Drop and recreate the shared-size scope so every column is measured from scratch;
            // columns with no content this pass collapse instead of keeping a stale width.
            this.rescoping = true;
            Grid.SetIsSharedSizeScope(this.scopeOwner, false);
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (!this.disposed)
                    {
                        Grid.SetIsSharedSizeScope(this.scopeOwner, true);
                    }

                    this.rescoping = false;
                },
                DispatcherPriority.Background);
        }

        private void OnScopeOwnerDetached(object? sender, VisualTreeAttachmentEventArgs e) => this.Dispose();

        private void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.scheduled?.Abort();
            this.scopeOwner.DetachedFromVisualTree -= this.OnScopeOwnerDetached;

            if (this.owner is not null)
            {
                this.owner.ContainerPrepared -= this.OnContainerPrepared;
                this.owner.ContainerClearing -= this.OnContainerClearing;
                ((INotifyCollectionChanged)this.owner.Classes).CollectionChanged -= this.OnClassesChanged;
            }

            foreach (IDisposable[] subscriptions in this.itemSubscriptions.Values)
            {
                foreach (IDisposable subscription in subscriptions)
                {
                    subscription.Dispose();
                }
            }

            this.itemSubscriptions.Clear();

            // Register keeps a single state per scope owner, so this is the only entry; drop it only
            // if it is still ours (never clobber a newer state registered after a detach/re-attach).
            if (states.TryGetValue(this.scopeOwner, out ScopeState? current) && current == this)
            {
                states.Remove(this.scopeOwner);
            }
        }
    }
}
