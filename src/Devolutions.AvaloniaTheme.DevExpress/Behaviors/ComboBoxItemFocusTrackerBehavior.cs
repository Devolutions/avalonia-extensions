namespace Devolutions.AvaloniaTheme.DevExpress.Behaviors;

using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

internal static class ComboBoxItemFocusTrackerBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ComboBoxItem, bool>("Enable", typeof(ComboBoxItemFocusTrackerBehavior));

    public static readonly AttachedProperty<bool> HasTransientComboBoxItemHighlightProperty =
        AvaloniaProperty.RegisterAttached<ItemsPresenter, bool>("HasTransientComboBoxItemHighlight", typeof(ComboBoxItemFocusTrackerBehavior));

    private static readonly ConditionalWeakTable<ComboBoxItem, ItemState> itemStates = new();
    private static readonly ConditionalWeakTable<ItemsPresenter, PresenterState> presenterStates = new();

    static ComboBoxItemFocusTrackerBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not ComboBoxItem item)
            {
                return;
            }

            bool enable = args.NewValue.GetValueOrDefault<bool>();
            if (enable)
            {
                Enable(item);
            }
            else
            {
                Disable(item);
            }
        });
    }

    public static void SetEnable(ComboBoxItem element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(ComboBoxItem element) => element.GetValue(EnableProperty);

    public static void SetHasTransientComboBoxItemHighlight(ItemsPresenter element, bool value) => element.SetValue(HasTransientComboBoxItemHighlightProperty, value);

    public static bool GetHasTransientComboBoxItemHighlight(ItemsPresenter element) => element.GetValue(HasTransientComboBoxItemHighlightProperty);

    private static void Enable(ComboBoxItem item)
    {
        if (itemStates.TryGetValue(item, out _))
        {
            return;
        }

        var state = new ItemState();

        void ScheduleUpdate()
        {
            if (state.IsDisposed)
            {
                return;
            }

            if (state.IsUpdatePending)
            {
                state.IsUpdateDirty = true;
                return;
            }

            state.IsUpdatePending = true;
            state.IsUpdateDirty = false;

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    do
                    {
                        state.IsUpdateDirty = false;

                        if (state.IsDisposed)
                        {
                            break;
                        }

                        UpdateOwningItemsPresenter(item, state);
                    }
                    while (state.IsUpdateDirty);
                }
                finally
                {
                    state.IsUpdatePending = false;
                }
            }, DispatcherPriority.Background);
        }

        EventHandler<GotFocusEventArgs> gotFocusHandler = (_, _) => ScheduleUpdate();
        EventHandler<RoutedEventArgs> lostFocusHandler = (_, _) => ScheduleUpdate();
        EventHandler<PointerEventArgs> pointerEnteredHandler = (_, _) => ScheduleUpdate();
        EventHandler<PointerEventArgs> pointerExitedHandler = (_, _) => ScheduleUpdate();
        EventHandler<VisualTreeAttachmentEventArgs> attachedHandler = (_, _) => ScheduleUpdate();
        EventHandler<VisualTreeAttachmentEventArgs> detachedHandler = (_, _) => ScheduleUpdate();

        item.GotFocus += gotFocusHandler;
        item.LostFocus += lostFocusHandler;
        item.PointerEntered += pointerEnteredHandler;
        item.PointerExited += pointerExitedHandler;
        item.AttachedToVisualTree += attachedHandler;
        item.DetachedFromVisualTree += detachedHandler;

        state.Disposables.Add(Disposable.Create(() =>
        {
            item.GotFocus -= gotFocusHandler;
            item.LostFocus -= lostFocusHandler;
            item.PointerEntered -= pointerEnteredHandler;
            item.PointerExited -= pointerExitedHandler;
            item.AttachedToVisualTree -= attachedHandler;
            item.DetachedFromVisualTree -= detachedHandler;
        }));

        var focusWithinSubscription = item
            .GetObservable(InputElement.IsKeyboardFocusWithinProperty)
            .Subscribe(_ => ScheduleUpdate());
        state.Disposables.Add(focusWithinSubscription);

        itemStates.Add(item, state);

        ScheduleUpdate();
    }

    private static void Disable(ComboBoxItem item)
    {
        if (!itemStates.TryGetValue(item, out ItemState? state))
        {
            return;
        }

        itemStates.Remove(item);
        state.IsDisposed = true;
        state.Disposables.Dispose();

        ApplyItemSnapshot(state, presenter: null, isPointerOver: false, isFocusVisible: false);
    }

    private static void UpdateOwningItemsPresenter(ComboBoxItem sourceItem, ItemState state)
    {
        ItemsPresenter? itemsPresenter = sourceItem.FindAncestorOfType<ItemsPresenter>();
        bool isPointerOver = sourceItem.IsPointerOver;
        bool isFocusVisible = ((IPseudoClasses)sourceItem.Classes).Contains(":focus-visible");

        ApplyItemSnapshot(state, itemsPresenter, isPointerOver, isFocusVisible);
    }

    private static void ApplyItemSnapshot(ItemState state, ItemsPresenter? presenter, bool isPointerOver, bool isFocusVisible)
    {
        ItemsPresenter? oldPresenter = state.Presenter;

        if (oldPresenter is not null)
        {
            PresenterState oldPresenterState = presenterStates.GetOrCreateValue(oldPresenter);
            if (state.IsPointerOver)
            {
                oldPresenterState.PointerOverCount = Math.Max(0, oldPresenterState.PointerOverCount - 1);
            }

            if (state.IsFocusVisible)
            {
                oldPresenterState.FocusVisibleCount = Math.Max(0, oldPresenterState.FocusVisibleCount - 1);
            }
        }

        if (presenter is not null)
        {
            PresenterState presenterState = presenterStates.GetOrCreateValue(presenter);
            if (isPointerOver)
            {
                presenterState.PointerOverCount += 1;
            }

            if (isFocusVisible)
            {
                presenterState.FocusVisibleCount += 1;
            }
        }

        state.Presenter = presenter;
        state.IsPointerOver = isPointerOver;
        state.IsFocusVisible = isFocusVisible;

        if (oldPresenter is not null)
        {
            UpdatePresenterFlags(oldPresenter);
        }

        if (presenter is not null && !ReferenceEquals(presenter, oldPresenter))
        {
            UpdatePresenterFlags(presenter);
        }
    }

    private static void UpdatePresenterFlags(ItemsPresenter presenter)
    {
        PresenterState state = presenterStates.GetOrCreateValue(presenter);
        bool hasTransientHighlight = state.FocusVisibleCount > 0 || state.PointerOverCount > 0;

        if (presenter.GetValue(HasTransientComboBoxItemHighlightProperty) != hasTransientHighlight)
        {
            presenter.SetValue(HasTransientComboBoxItemHighlightProperty, hasTransientHighlight);
        }
    }

    private sealed class ItemState
    {
        public CompositeDisposable Disposables { get; } = new();

        public bool IsUpdatePending { get; set; }

        public bool IsUpdateDirty { get; set; }

        public bool IsDisposed { get; set; }

        public ItemsPresenter? Presenter { get; set; }

        public bool IsPointerOver { get; set; }

        public bool IsFocusVisible { get; set; }
    }

    private sealed class PresenterState
    {
        public int PointerOverCount { get; set; }

        public int FocusVisibleCount { get; set; }
    }
}