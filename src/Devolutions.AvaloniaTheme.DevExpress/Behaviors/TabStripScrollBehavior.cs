namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal static class TabStripScrollBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<TabControl, bool>("Enable", typeof(TabStripScrollBehavior));

    private static readonly ConditionalWeakTable<TabControl, TabStripScrollState> States = new();

    static TabStripScrollBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not TabControl tabControl)
            {
                return;
            }

            bool enable = args.NewValue.GetValueOrDefault<bool>();
            if (enable)
            {
                Enable(tabControl);
            }
            else
            {
                Disable(tabControl);
            }
        });
    }

    public static void SetEnable(TabControl element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(TabControl element) => element.GetValue(EnableProperty);

    private static void Enable(TabControl tabControl)
    {
        if (States.TryGetValue(tabControl, out _))
        {
            return;
        }

        States.Add(tabControl, new TabStripScrollState(tabControl));
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

    private sealed class TabStripScrollState : IDisposable
    {
        private readonly TabControl tabControl;
        private CompositeDisposable compositeDisposable = [];
        private CompositeDisposable templateSubscriptions = [];
        private ScrollViewer? tabHeaderScrollViewer;
        private RepeatButton? scrollLeftButton;
        private RepeatButton? scrollRightButton;
        private bool disposed;

        public TabStripScrollState(TabControl tabControl)
        {
            this.tabControl = tabControl;

            this.tabControl.TemplateApplied += this.OnTemplateApplied;
            this.tabControl.Loaded += this.OnTabControlLoaded;
            this.tabControl.Unloaded += this.OnTabControlUnloaded;

            this.compositeDisposable.Add(this.tabControl
                .GetObservable(SelectingItemsControl.SelectedIndexProperty)
                .Subscribe(_ => this.ScrollSelectedTabIntoView()));

            this.compositeDisposable.Add(this.tabControl
                .GetObservable(TabControl.TabStripPlacementProperty)
                .Subscribe(_ => this.UpdateScrollButtonState()));

            if (this.tabControl.Items is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged += this.OnItemsChanged;
                this.compositeDisposable.Add(Disposable.Create(() => notifyCollectionChanged.CollectionChanged -= this.OnItemsChanged));
            }

            this.TryAttachTemplateParts();
        }

        private void OnTabControlLoaded(object? sender, RoutedEventArgs e)
        {
            this.TryAttachTemplateParts();
            this.UpdateScrollButtonState();
            this.ScrollSelectedTabIntoView();
        }

        private void OnTabControlUnloaded(object? sender, RoutedEventArgs e)
        {
            this.templateSubscriptions.Dispose();
            this.templateSubscriptions = [];
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            this.TryAttachTemplateParts(e.NameScope);
        }

        private void TryAttachTemplateParts(INameScope? nameScope = null)
        {
            if (this.disposed)
            {
                return;
            }

            // Dispose subscriptions from old parts BEFORE overwriting the field references.
            // Otherwise closures inside templateSubscriptions would detach from the new parts.
            this.templateSubscriptions.Dispose();
            this.templateSubscriptions = [];

            this.tabHeaderScrollViewer = nameScope?.Find<ScrollViewer>("PART_TabHeaderScrollViewer")
                                       ?? this.tabControl.FindDescendantOfType<ScrollViewer>("PART_TabHeaderScrollViewer");
            this.scrollLeftButton = nameScope?.Find<RepeatButton>("PART_TabScrollLeftButton")
                                    ?? this.tabControl.FindDescendantOfType<RepeatButton>("PART_TabScrollLeftButton");
            this.scrollRightButton = nameScope?.Find<RepeatButton>("PART_TabScrollRightButton")
                                     ?? this.tabControl.FindDescendantOfType<RepeatButton>("PART_TabScrollRightButton");

            if (this.tabHeaderScrollViewer is null || this.scrollLeftButton is null || this.scrollRightButton is null)
            {
                return;
            }

            // Capture in locals so the disposal closures always reference these specific instances.
            var leftButton = this.scrollLeftButton;
            var rightButton = this.scrollRightButton;

            leftButton.Click += this.OnScrollLeftClick;
            rightButton.Click += this.OnScrollRightClick;

            this.templateSubscriptions.Add(Disposable.Create(() => leftButton.Click -= this.OnScrollLeftClick));
            this.templateSubscriptions.Add(Disposable.Create(() => rightButton.Click -= this.OnScrollRightClick));
            this.templateSubscriptions.Add(this.tabHeaderScrollViewer.GetObservable(ScrollViewer.OffsetProperty).Subscribe(_ => this.UpdateScrollButtonState()));
            this.templateSubscriptions.Add(this.tabHeaderScrollViewer.GetObservable(ScrollViewer.ViewportProperty).Subscribe(_ => this.UpdateScrollButtonState()));
            this.templateSubscriptions.Add(this.tabHeaderScrollViewer.GetObservable(ScrollViewer.ExtentProperty).Subscribe(_ => this.UpdateScrollButtonState()));
            this.templateSubscriptions.Add(this.tabHeaderScrollViewer.GetObservable(Visual.BoundsProperty).Subscribe(_ => this.UpdateScrollButtonState()));

            this.UpdateScrollButtonState();
            this.ScrollSelectedTabIntoView();
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateScrollButtonState();
            this.ScrollSelectedTabIntoView();
        }

        private void OnScrollLeftClick(object? sender, RoutedEventArgs e)
        {
            if (this.tabHeaderScrollViewer is null)
            {
                return;
            }

            this.ScrollBy(-this.GetScrollStep());
        }

        private void OnScrollRightClick(object? sender, RoutedEventArgs e)
        {
            if (this.tabHeaderScrollViewer is null)
            {
                return;
            }

            this.ScrollBy(this.GetScrollStep());
        }

        private void ScrollBy(double delta)
        {
            if (this.tabHeaderScrollViewer is null)
            {
                return;
            }

            double maxOffsetX = Math.Max(0, this.tabHeaderScrollViewer.Extent.Width - this.tabHeaderScrollViewer.Viewport.Width);
            double newOffsetX = Math.Clamp(this.tabHeaderScrollViewer.Offset.X + delta, 0, maxOffsetX);
            this.tabHeaderScrollViewer.Offset = new Vector(newOffsetX, this.tabHeaderScrollViewer.Offset.Y);
        }

        private double GetScrollStep()
        {
            if (this.tabHeaderScrollViewer is null)
            {
                return 80;
            }

            return Math.Max(80, this.tabHeaderScrollViewer.Viewport.Width * 0.5);
        }

        private void ScrollSelectedTabIntoView()
        {
            if (this.disposed || this.tabControl.TabStripPlacement != Dock.Top)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (this.disposed || this.tabControl.SelectedIndex < 0)
                {
                    return;
                }

                if (this.tabControl.ContainerFromIndex(this.tabControl.SelectedIndex) is Control selectedContainer)
                {
                    selectedContainer.BringIntoView();
                    this.UpdateScrollButtonState();
                }
            }, DispatcherPriority.Background);
        }

        private void UpdateScrollButtonState()
        {
            if (this.scrollLeftButton is null || this.scrollRightButton is null || this.tabHeaderScrollViewer is null)
            {
                return;
            }

            if (this.tabControl.TabStripPlacement != Dock.Top)
            {
                this.scrollLeftButton.IsVisible = false;
                this.scrollRightButton.IsVisible = false;
                this.scrollLeftButton.IsEnabled = false;
                this.scrollRightButton.IsEnabled = false;
                return;
            }

            double maxOffsetX = Math.Max(0, this.tabHeaderScrollViewer.Extent.Width - this.tabHeaderScrollViewer.Viewport.Width);
            bool hasOverflow = maxOffsetX > 0.5;

            this.scrollLeftButton.IsVisible = hasOverflow;
            this.scrollRightButton.IsVisible = hasOverflow;

            if (!hasOverflow)
            {
                this.scrollLeftButton.IsEnabled = false;
                this.scrollRightButton.IsEnabled = false;
                return;
            }

            double offsetX = this.tabHeaderScrollViewer.Offset.X;
            this.scrollLeftButton.IsEnabled = offsetX > 0.5;
            this.scrollRightButton.IsEnabled = offsetX < maxOffsetX - 0.5;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.tabControl.TemplateApplied -= this.OnTemplateApplied;
            this.tabControl.Loaded -= this.OnTabControlLoaded;
            this.tabControl.Unloaded -= this.OnTabControlUnloaded;
            this.templateSubscriptions.Dispose();
            this.compositeDisposable.Dispose();
        }
    }

    private static T? FindDescendantOfType<T>(this Visual visual, string name)
        where T : StyledElement
    {
        return visual.GetVisualDescendants()
            .OfType<T>()
            .FirstOrDefault(element => element.Name == name && element.TemplatedParent == visual);
    }
}
