namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

/// <summary>
/// Used by DevExpress's TabControl theme.
/// </summary>
/// <remarks>
/// Behavior that observes visibility changes of all TabItems within a TabControl.
/// When any child TabItem's IsVisible property changes, increments a VisibilityVersion
/// attached property on the TabControl to trigger re-evaluation of bindings that depend
/// on adjacent tab visibility (e.g., TabBorderVisibilityConverter).
/// </remarks>
internal static class TabItemVisibilityObserverBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TabControl, bool>("Enable", typeof(TabItemVisibilityObserverBehavior));

    public static readonly AttachedProperty<int> VisibilityVersionProperty =
        AvaloniaProperty.RegisterAttached<TabControl, int>("VisibilityVersion", typeof(TabItemVisibilityObserverBehavior));

    private static readonly ConditionalWeakTable<TabControl, TabItemObservationState> States = new();

    static TabItemVisibilityObserverBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is TabControl tabControl)
            {
                bool enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    Enable(tabControl);
                }
                else
                {
                    Disable(tabControl);
                }
            }
        });
    }

    public static void SetEnable(TabControl element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(TabControl element) => element.GetValue(EnableProperty);

    public static void SetVisibilityVersion(TabControl element, int value) => element.SetValue(VisibilityVersionProperty, value);

    public static int GetVisibilityVersion(TabControl element) => element.GetValue(VisibilityVersionProperty);

    private static void Enable(TabControl tabControl)
    {
        if (States.TryGetValue(tabControl, out _))
        {
            // Already enabled
            return;
        }

        var state = new TabItemObservationState(tabControl);
        States.Add(tabControl, state);
    }

    private static void Disable(TabControl tabControl)
    {
        if (!States.TryGetValue(tabControl, out var state))
        {
            return;
        }

        state.Dispose();
        States.Remove(tabControl);
    }

    private sealed class TabItemObservationState : IDisposable
    {
        private readonly Dictionary<TabItem, IDisposable> subscriptions = new();
        private readonly TabControl tabControl;
        private bool disposed;

        public TabItemObservationState(TabControl tabControl)
        {
            this.tabControl = tabControl;

            // Watch for items collection changes
            if (tabControl.Items is System.Collections.Specialized.INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += this.OnItemsCollectionChanged;
            }

            // Clean up when control is unloaded
            tabControl.Unloaded += this.OnTabControlUnloaded;

            // Wait for template to be applied and items to be available
            if (tabControl.IsLoaded)
            {
                this.ObserveExistingTabItems();
            }
            else
            {
                tabControl.Loaded += this.OnTabControlLoaded;
            }
        }

        private void OnTabControlLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.disposed) return;
            
            this.tabControl.Loaded -= this.OnTabControlLoaded;
            this.ObserveExistingTabItems();
        }

        private void OnTabControlUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.disposed) return;
            
            // Clean up subscriptions but don't fully dispose - we'll re-enable when loaded again
            this.CleanupSubscriptions();
            
            // Re-subscribe to Loaded event to re-enable when control is loaded again
            this.tabControl.Loaded += this.OnTabControlLoaded;
        }

        public void ObserveExistingTabItems()
        {
            if (this.disposed) return;

            // Observe all current TabItems
            foreach (var item in this.tabControl.Items.OfType<TabItem>())
            {
                this.ObserveTabItem(item);
            }
        }

        private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.disposed) return;

            // Handle added items
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<TabItem>())
                {
                    this.ObserveTabItem(item);
                }
            }

            // Handle removed items
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<TabItem>())
                {
                    this.UnobserveTabItem(item);
                }
            }
        }

        private void ObserveTabItem(TabItem tabItem)
        {
            if (this.disposed) return;

            if (!this.subscriptions.ContainsKey(tabItem))
            {
                // Subscribe to IsVisible property changes
                var subscription = tabItem
                    .GetObservable(Visual.IsVisibleProperty)
                    .Subscribe(_ => this.OnTabItemVisibilityChanged());
                this.subscriptions[tabItem] = subscription;
            }
        }

        private void UnobserveTabItem(TabItem tabItem)
        {
            if (this.subscriptions.TryGetValue(tabItem, out var subscription))
            {
                subscription.Dispose();
                this.subscriptions.Remove(tabItem);
            }
        }

        private void OnTabItemVisibilityChanged()
        {
            if (this.disposed) return;

            // Increment the version counter to trigger binding updates
            int currentVersion = GetVisibilityVersion(this.tabControl);
            SetVisibilityVersion(this.tabControl, currentVersion + 1);
        }

        private void CleanupSubscriptions()
        {
            // Dispose all TabItem subscriptions
            foreach (var subscription in this.subscriptions.Values)
            {
                subscription.Dispose();
            }
            this.subscriptions.Clear();
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;

            this.tabControl.Loaded -= this.OnTabControlLoaded;
            this.tabControl.Unloaded -= this.OnTabControlUnloaded;

            // Unsubscribe from items collection
            if (this.tabControl.Items is System.Collections.Specialized.INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged -= this.OnItemsCollectionChanged;
            }

            // Clean up all TabItem subscriptions
            this.CleanupSubscriptions();
        }
    }
}