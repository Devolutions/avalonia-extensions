namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

/// <summary>
/// Behavior that adds pseudo-classes to TabItems indicating their position relative to the selected tab.
/// Adds :left-of-selected, :right-of-selected and :next-to-selected pseudo-classes.
/// </summary>
/// <remarks>
/// It also additionally add the :first-tab, and :last-tab pseudo-classes
/// </remarks>
internal static class TabItemPositionBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TabControl, bool>("Enable", typeof(TabItemPositionBehavior));

    private static readonly ConditionalWeakTable<TabControl, TabItemPositionState> States = new();

    static TabItemPositionBehavior()
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

    private static void Enable(TabControl tabControl)
    {
        if (States.TryGetValue(tabControl, out _))
        {
            // Already enabled
            return;
        }

        var state = new TabItemPositionState(tabControl);
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

    private sealed class TabItemPositionState : IDisposable
    {
        private readonly TabControl tabControl;
        private readonly List<IDisposable> subscriptions = new();
        private readonly Dictionary<TabItem, IDisposable> visibilitySubscriptions = new();
        private bool disposed;

        public TabItemPositionState(TabControl tabControl)
        {
            this.tabControl = tabControl;

            tabControl.Unloaded += this.OnTabControlUnloaded;

            // Initial update when control is loaded
            if (tabControl.IsLoaded)
            {
                this.SetupSubscriptions();
                this.UpdateAllTabItemPositions();
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
            
            this.SetupSubscriptions();
            this.UpdateAllTabItemPositions();
        }

        private void OnTabControlUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.disposed) return;
            
            this.CleanupSubscriptions();
            
            // Re-subscribe to Loaded event to re-enable when control is loaded again
            this.tabControl.Loaded += this.OnTabControlLoaded;
        }

        private void SetupSubscriptions()
        {
            var selectedIndexSubscription = this.tabControl
                .GetObservable(SelectingItemsControl.SelectedIndexProperty)
                .Subscribe(_ => this.UpdateAllTabItemPositions());
            this.subscriptions.Add(selectedIndexSubscription);

            if (this.tabControl.Items is System.Collections.Specialized.INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += this.OnItemsCollectionChanged;
            }

            this.SubscribeToVisibilityChanges();
        }

        private void SubscribeToVisibilityChanges()
        {
            if (this.disposed) return;

            foreach (var sub in this.visibilitySubscriptions.Values)
            {
                sub.Dispose();
            }
            this.visibilitySubscriptions.Clear();

            foreach (var item in this.tabControl.GetRealizedContainers().OfType<TabItem>())
            {
                var subscription = item
                    .GetObservable(Visual.IsVisibleProperty)
                    .Subscribe(_ => this.UpdateAllTabItemPositions());
                this.visibilitySubscriptions[item] = subscription;
            }
        }

        private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.disposed) return;
            
            this.SubscribeToVisibilityChanges();
            this.UpdateAllTabItemPositions();
        }

        private void UpdateAllTabItemPositions()
        {
            if (this.disposed) return;

            TabItem? selectedTab = this.tabControl.ContainerFromItem(this.tabControl.SelectedItem!) as TabItem;

            bool isFirst = true;
            bool isSelectedFound = false;
            bool foundRightToSelected = false;
            TabItem? previousVisibleTab = null;
            
            TabItem? lastVisibleTab = null;
            foreach (var tab in this.tabControl.GetRealizedContainers().OfType<TabItem>())
            {
                if (tab.IsVisible)
                {
                    bool isSelected = tab == selectedTab;
                    ((IPseudoClasses)tab.Classes).Set(":left-of-selected", !isSelected && !isSelectedFound);
                    ((IPseudoClasses)tab.Classes).Set(":right-of-selected", !isSelected && isSelectedFound);
                    ((IPseudoClasses)tab.Classes).Set(":first-tab", isFirst);
                    ((IPseudoClasses)tab.Classes).Set(":last-tab", false);
                    ((IPseudoClasses)tab.Classes).Set(":next-to-selected", false);

                    if (isSelected && previousVisibleTab is not null)
                    {
                        ((IPseudoClasses)previousVisibleTab.Classes).Set(":next-to-selected", true);
                    }
                    if (!isSelected && isSelectedFound && !foundRightToSelected)
                    {
                        ((IPseudoClasses)tab.Classes).Set(":next-to-selected", true);
                        foundRightToSelected = true;
                    }

                    isFirst = false;
                    isSelectedFound = isSelectedFound || isSelected;
                    lastVisibleTab = tab;
                    previousVisibleTab = tab;
                }
                else
                {
                    ClearPseudoClasses(tab);
                }
            }

            if (lastVisibleTab is not null)
            {
                ((IPseudoClasses)lastVisibleTab.Classes).Set(":last-tab", true);
            }
        }

        private void ClearPseudoClasses(TabItem tabItem)
        {
            ((IPseudoClasses)tabItem.Classes).Set(":left-of-selected", false);
            ((IPseudoClasses)tabItem.Classes).Set(":right-of-selected", false);
            ((IPseudoClasses)tabItem.Classes).Set(":first-tab", false);
            ((IPseudoClasses)tabItem.Classes).Set(":last-tab", false);
            ((IPseudoClasses)tabItem.Classes).Set(":next-to-selected", false);
        }

        private void CleanupSubscriptions()
        {
            // Dispose observable subscriptions (SelectedIndex, etc.)
            foreach (var subscription in this.subscriptions)
            {
                subscription.Dispose();
            }
            this.subscriptions.Clear();

            if (this.tabControl.Items is System.Collections.Specialized.INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged -= this.OnItemsCollectionChanged;
            }

            foreach (var subscription in this.visibilitySubscriptions.Values)
            {
                subscription.Dispose();
            }
            this.visibilitySubscriptions.Clear();
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;

            this.tabControl.Loaded -= this.OnTabControlLoaded;
            this.tabControl.Unloaded -= this.OnTabControlUnloaded;

            this.CleanupSubscriptions();
            foreach (var tabItem in this.tabControl.GetRealizedContainers().OfType<TabItem>())
            {
                this.ClearPseudoClasses(tabItem);
            }
        }
    }
}
