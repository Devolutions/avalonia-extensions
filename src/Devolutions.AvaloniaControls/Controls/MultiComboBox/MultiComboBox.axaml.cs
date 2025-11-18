// ReSharper disable InconsistentNaming

namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using static Extensions.AvaloniaExtensions;
using AvaloniaPropertyExtension = Irihi.Avalonia.Shared.Helpers.AvaloniaPropertyExtension;
using ObservableExtension = Irihi.Avalonia.Shared.Helpers.ObservableExtension;
using RoutedEventExtension = Irihi.Avalonia.Shared.Helpers.RoutedEventExtension;

public enum MultiComboBoxOverflowMode
{
    Scroll,
    Wrap,
}

[TemplatePart("PART_SelectionScrollViewer", typeof(ScrollViewer), IsRequired = false)]
[TemplatePart("PART_SelectAllItem", typeof(MultiComboBoxSelectAllItem), IsRequired = false)]
[TemplatePart("PART_FilterTextBox", typeof(TextBox), IsRequired = false)]
[TemplatePart("PART_ItemsListPresenter", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart(PART_BackgroundBorder, typeof(Border))]
[PseudoClasses(PC_DropDownOpen, PC_Empty)]
public partial class MultiComboBox : SelectingItemsControl
{
    public const string PART_BackgroundBorder = "PART_BackgroundBorder";
    public const string PC_DropDownOpen = ":dropdownopen";
    public const string PC_Empty = ":selection-empty";

    public static readonly StyledProperty<MultiComboBoxOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<MultiComboBox, MultiComboBoxOverflowMode>(nameof(OverflowMode));

    public static readonly StyledProperty<string?> SelectAllTextProperty =
        AvaloniaProperty.Register<MultiComboBox, string?>(nameof(SelectAllText));

    public static readonly StyledProperty<string?> AllSelectedTextProperty =
        AvaloniaProperty.Register<MultiComboBox, string?>(nameof(AllSelectedText));

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<MultiComboBox, string>(nameof(FilterText), "Find items");

    public static readonly StyledProperty<string?> FilterValueProperty =
        AvaloniaProperty.Register<MultiComboBox, string?>(nameof(FilterValue), defaultBindingMode: BindingMode.TwoWay, inherits: true);

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        ComboBox.IsDropDownOpenProperty.AddOwner<MultiComboBox>();

    public static readonly StyledProperty<bool?> ShowFilterProperty =
        AvaloniaProperty.Register<MultiComboBox, bool?>(nameof(ShowFilter));

    public static readonly DirectProperty<MultiComboBox, bool> IsFilterEnabledProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, bool>(nameof(IsFilterEnabled), static c => c.IsFilterEnabled);

    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<MultiComboBox, double>(
            nameof(MaxDropDownHeight));

    public static readonly StyledProperty<double> MaxSelectionBoxHeightProperty =
        AvaloniaProperty.Register<MultiComboBox, double>(
            nameof(MaxSelectionBoxHeight));

    public new static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<MultiComboBox, IList?>(
            nameof(SelectedItems));

    public static readonly StyledProperty<object?> InnerLeftContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(InnerLeftContent));

    public static readonly StyledProperty<object?> InnerRightContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(InnerRightContent));

    public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
        AvaloniaProperty.Register<MultiComboBox, IDataTemplate?>(
            nameof(SelectedItemTemplate));

    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<MultiComboBox>();

    public static readonly StyledProperty<object?> PopupInnerTopContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(PopupInnerTopContent));

    public static readonly StyledProperty<object?> PopupInnerBottomContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(PopupInnerBottomContent));

    public static readonly StyledProperty<ITemplate<Panel>?> SelectedItemsPanelProperty =
        AvaloniaProperty.Register<MultiComboBox, ITemplate<Panel>?>(nameof(SelectedItemsPanel));

    public static readonly DirectProperty<MultiComboBox, ScrollBarVisibility> ScrollbarVisibilityProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ScrollBarVisibility>(nameof(ScrollbarVisibility),
            o => o.ScrollbarVisibility);

    public static readonly DirectProperty<MultiComboBox, ITemplate<Panel>> EffectiveSelectedItemsPanelProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ITemplate<Panel>>(nameof(EffectiveSelectedItemsPanel),
            o => o.EffectiveSelectedItemsPanel);

    public static readonly DirectProperty<MultiComboBox, IDataTemplate?> EffectiveSelectedItemTemplateProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, IDataTemplate?>(nameof(EffectiveSelectedItemTemplate),
            o => o.EffectiveSelectedItemTemplate);

    public static readonly DirectProperty<MultiComboBox, bool?> IsAllSelectedProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, bool?>(nameof(IsAllSelected),
            o => o.IsAllSelected);

    private static readonly ITemplate<Panel> DefaultSelectedItemsPanel =
        new FuncTemplate<Panel>(() => new VirtualizingStackPanel { Orientation = Orientation.Horizontal, Background = Brushes.Transparent });


    private static readonly ITemplate<Panel> DefaultSelectedItemsWrapPanel =
        new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal, Background = Brushes.Transparent });


    // Default panel for the inner control (forwarded via property binding)
    private static readonly ITemplate<Panel?> DefaultPanel =
        new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

    private readonly AvaloniaList<object?> filteredItems = [];

    private readonly InnerMultiComboBoxList innerList;

    private TextBox? filterTextbox;

    private ContentPresenter? itemsListPresenter;

    private Border? rootBorder;

    private MultiComboBoxSelectAllItem? selectAllItem;

    private ScrollViewer? selectionScrollViewer;

    private bool updateInternal;

    static MultiComboBox()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBox>(true);
        ItemsPanelProperty.OverrideDefaultValue<MultiComboBox>(DefaultPanel);
        AvaloniaPropertyExtension.AffectsPseudoClass<MultiComboBox>(IsDropDownOpenProperty, PC_DropDownOpen);
        SelectedItemsProperty.Changed.AddClassHandler<MultiComboBox, IList?>((box, args) => box.OnSelectedItemsChanged(args));
    }

    public MultiComboBox()
    {
        this.SelectedItems = new AvaloniaList<object>();

        // Create inner control for filtered display
        // Forward parent's ItemsPanel property to inner control - parent never uses it directly
        this.innerList = new InnerMultiComboBoxList(this)
        {
            Name = "PART_ItemsList",
            ItemsSource = this.filteredItems,
            [!ItemsPanelProperty] = this[!ItemsPanelProperty], // Forward to inner control
            [!ItemTemplateProperty] = this[!ItemTemplateProperty],
            [!ScrollViewer.HorizontalScrollBarVisibilityProperty] = this[!ScrollViewer.HorizontalScrollBarVisibilityProperty],
            [!ScrollViewer.VerticalScrollBarVisibilityProperty] = this[!ScrollViewer.VerticalScrollBarVisibilityProperty],
        };

        ObservableExtension.Subscribe(this.GetObservable(OverflowModeProperty),
            _ =>
            {
                this.RaiseDirectPropertyChanged(ScrollbarVisibilityProperty);
                this.RaiseDirectPropertyChanged(EffectiveSelectedItemsPanelProperty);
            });
        ObservableExtension.Subscribe(this.GetObservable(SelectedItemTemplateProperty), _ => this.RaiseDirectPropertyChanged(EffectiveSelectedItemTemplateProperty));
        ObservableExtension.Subscribe(this.GetObservable(ItemTemplateProperty), _ => this.RaiseDirectPropertyChanged(EffectiveSelectedItemTemplateProperty));
        this.GetObservable(ItemCountProperty).Subscribe(_ =>
        {
            this.RaiseDirectPropertyChanged(IsFilterEnabledProperty);
        });

        this.ItemsView.CollectionChanged += (_, _) => this.ApplyFilter(null);

        ObservableExtension.Subscribe(IsDropDownOpenProperty.Changed, args => this.OnDropDownOpenChanged(args.GetOldAndNewValue<bool>()));
    }

    public ITemplate<Panel>? SelectedItemsPanel
    {
        get => this.GetValue(SelectedItemsPanelProperty);
        set => this.SetValue(SelectedItemsPanelProperty, value);
    }

    public MultiComboBoxOverflowMode OverflowMode
    {
        get => this.GetValue(OverflowModeProperty);
        set => this.SetValue(OverflowModeProperty, value);
    }

    public string? SelectAllText
    {
        get => this.GetValue(SelectAllTextProperty);
        set => this.SetValue(SelectAllTextProperty, value);
    }

    public string? AllSelectedText
    {
        get => this.GetValue(AllSelectedTextProperty);
        set => this.SetValue(AllSelectedTextProperty, value);
    }

    public string FilterText
    {
        get => this.GetValue(FilterTextProperty);
        set => this.SetValue(FilterTextProperty, value);
    }

    public string? FilterValue
    {
        get => this.GetValue(FilterValueProperty);
        set => this.SetValue(FilterValueProperty, value);
    }

    public bool? ShowFilter
    {
        get => this.GetValue(ShowFilterProperty);
        set
        {
            bool oldIsFilterEnabled = this.IsFilterEnabled;
            this.SetValue(ShowFilterProperty, value);
            this.RaisePropertyChanged(IsFilterEnabledProperty, oldIsFilterEnabled, this.IsFilterEnabled);
        }
    }

    public bool IsFilterEnabled => this.ShowFilter ?? this.ItemCount > 20;

    private bool IsFilterFocused => this.filterTextbox?.IsFocused == true;

    public bool IsDropDownOpen
    {
        get => this.GetValue(IsDropDownOpenProperty);
        private set => this.SetValue(IsDropDownOpenProperty, value);
    }

    public double MaxDropDownHeight
    {
        get => this.GetValue(MaxDropDownHeightProperty);
        set => this.SetValue(MaxDropDownHeightProperty, value);
    }

    public double MaxSelectionBoxHeight
    {
        get => this.GetValue(MaxSelectionBoxHeightProperty);
        set => this.SetValue(MaxSelectionBoxHeightProperty, value);
    }

    public new IList? SelectedItems
    {
        get => this.GetValue(SelectedItemsProperty);
        set => this.SetValue(SelectedItemsProperty, value);
    }

    [InheritDataTypeFromItems(nameof(SelectedItems))]
    public IDataTemplate? SelectedItemTemplate
    {
        get => this.GetValue(SelectedItemTemplateProperty);
        set => this.SetValue(SelectedItemTemplateProperty, value);
    }

    public string? Watermark
    {
        get => this.GetValue(WatermarkProperty);
        set => this.SetValue(WatermarkProperty, value);
    }

    public object? InnerLeftContent
    {
        get => this.GetValue(InnerLeftContentProperty);
        set => this.SetValue(InnerLeftContentProperty, value);
    }

    public object? InnerRightContent
    {
        get => this.GetValue(InnerRightContentProperty);
        set => this.SetValue(InnerRightContentProperty, value);
    }

    public object? PopupInnerTopContent
    {
        get => this.GetValue(PopupInnerTopContentProperty);
        set => this.SetValue(PopupInnerTopContentProperty, value);
    }

    public object? PopupInnerBottomContent
    {
        get => this.GetValue(PopupInnerBottomContentProperty);
        set => this.SetValue(PopupInnerBottomContentProperty, value);
    }

    public ScrollBarVisibility ScrollbarVisibility => this.OverflowMode switch
    {
        MultiComboBoxOverflowMode.Scroll => ScrollBarVisibility.Auto,
        MultiComboBoxOverflowMode.Wrap => ScrollBarVisibility.Disabled,
        _ => ScrollBarVisibility.Auto,
    };

    public ITemplate<Panel> EffectiveSelectedItemsPanel => this.SelectedItemsPanel ?? this.OverflowMode switch
    {
        MultiComboBoxOverflowMode.Scroll => DefaultSelectedItemsPanel,
        MultiComboBoxOverflowMode.Wrap => DefaultSelectedItemsWrapPanel,
        _ => DefaultSelectedItemsPanel,
    };

    public IDataTemplate? EffectiveSelectedItemTemplate => this.SelectedItemTemplate ?? this.ItemTemplate;

    public bool? IsAllSelected
    {
        get
        {
            if (this.SelectedItems is null) return false;

            return this.SelectedItems.Count switch
            {
                0 => false,
                var i when i == this.Items.Count => true,
                _ => null,
            };
        }
    }

    private void OnDropDownOpenChanged((bool oldValue, bool newValue) oldAndNewValue)
    {
        if (oldAndNewValue.oldValue == oldAndNewValue.newValue) return;

        if (oldAndNewValue.newValue)
        {
            this.OnDropDownOpened();
        }
        else
        {
            this.OnDropDownClosed();
        }
    }

    private void OnDropDownOpened()
    {
        if (this.filterTextbox is { } filter)
        {
            // Clear filter text (this will trigger ApplyFilter which restores original items)
            filter.Text = null;
        }

        this.ApplyFilter(null);
    }

    private void OnDropDownClosed()
    {
        // Clear filtered items
        this.filteredItems.Clear();
    }

    private void RaiseDirectPropertyChanged<T>(DirectPropertyBase<T> property)
    {
        T val = this.GetValue(property);
        this.RaisePropertyChanged(property, default!, val);
    }

    private MultiComboBoxItem? GetMultiComboBoxItemContainer(object? item)
    {
        if (item is null) return null;
        // Containers are now managed by innerItemsList, not the parent
        return item as MultiComboBoxItem ?? this.innerList.ContainerFromItem(item) as MultiComboBoxItem;
    }

    public void Remove(object? o)
    {
        if (o is StyledElement s)
        {
            object? data = s.DataContext;
            this.SelectedItems?.Remove(data);
            object? item = this.Items.FirstOrDefault(a => ReferenceEquals(a, data));
            if (item is not null)
            {
                Control? container = this.ContainerFromItem(item);
                if (container is MultiComboBoxItem t)
                {
                    t.IsSelected = false;
                }
            }
        }
    }

    public void SelectAll()
    {
        if (this.SelectedItems is null) return;

        this.updateInternal = true;
        this.selectAllItem?.BeginUpdate();

        this.SelectedItems.Clear();

        foreach (object? item in this.Items)
        {
            this.GetMultiComboBoxItemContainer(item)?.BeginUpdate();
        }

        foreach (object? item in this.Items)
        {
            if (this.GetMultiComboBoxItemContainer(item) is MultiComboBoxItem multiComboBoxItem)
            {
                this.SelectedItems.Add(multiComboBoxItem.DataContext);
                multiComboBoxItem.Select();
            }
            else
            {
                this.SelectedItems.Add(item);
            }
        }

        foreach (object? item in this.Items)
        {
            this.GetMultiComboBoxItemContainer(item)?.EndUpdate();
        }

        this.selectAllItem?.EndUpdate();

        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count is null or 0);
        this.selectAllItem?.UpdateSelection();

        // Containers are now managed by innerItemsList
        Controls? containers = this.innerList.Presenter?.Panel?.Children;
        if (containers is not null)
        {
            foreach (Control? container in containers)
            {
                if (container is MultiComboBoxItem i)
                {
                    i.Select();
                }
            }
        }

        this.RaiseDirectPropertyChanged(IsAllSelectedProperty);

        this.updateInternal = false;
    }

    public void DeselectAll()
    {
        this.SelectedItems?.Clear();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.FixInitializationFromAxamlItems();
        this.selectAllItem?.UpdateSelection();
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Store reference to ContentPresenter and set inner control
        this.itemsListPresenter = e.NameScope.Find<ContentPresenter>("PART_ItemsListPresenter");
        if (this.itemsListPresenter is not null)
        {
            this.itemsListPresenter.Content = this.innerList;
        }

        this.selectionScrollViewer = e.NameScope.Find<ScrollViewer>("PART_SelectionScrollViewer");
        this.selectAllItem = e.NameScope.Find<MultiComboBoxSelectAllItem>("PART_SelectAllItem");
        this.filterTextbox = e.NameScope.Find<TextBox>("PART_FilterTextBox");

        if (this.filterTextbox is { } partFilterTextBox)
        {
            partFilterTextBox[!!TextBox.TextProperty] = this[!!FilterValueProperty];
            partFilterTextBox.GetObservable(TextBox.TextProperty).Subscribe(this.ApplyFilter);
        }

        this.selectAllItem?.UpdateSelection();

        RoutedEventExtension.RemoveHandler(PointerPressedEvent, this.OnBackgroundPointerPressed, this.rootBorder);
        this.rootBorder = e.NameScope.Find<Border>(PART_BackgroundBorder);
        RoutedEventExtension.AddHandler(PointerPressedEvent, this.OnBackgroundPointerPressed, this.rootBorder);
        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count == 0);

        if (this.selectAllItem is { } sai)
        {
            ObservableExtension.Subscribe(sai.GetObservable(IsSelectedProperty),
                isSelected =>
                {
                    if (this.updateInternal) return;

                    if (isSelected)
                    {
                        this.Selection.SelectAll();
                    }
                    else
                    {
                        this.Selection.Clear();
                    }
                });
        }
    }

    private void ApplyFilter(string? filterText)
    {
        filterText ??= this.filterTextbox?.Text;
        bool hasFilterText = !string.IsNullOrWhiteSpace(filterText);

        // Re-populate filtered items based on current filter
        this.filteredItems.Clear();

        // IEnumerable source = this.ItemsSource ?? this.Items;
        foreach (object? item in this.ItemsView)
        {
            if (!hasFilterText || this.ItemMatchesFilter(item, filterText))
            {
                // If we have concrete `MultiComboBoxItem` instances, they are owned by us 
                // ("us" being `MultiComboBox` because we are a `ItemsControl`).
                //
                // ItemsControl must own their containers.
                // We don't realize unrealized container, so the inner `InnerMultiComboBoxList` can realize
                // and own them without problem.
                //
                // However, if we own concrete instances, we must send clones to the InnerMultiComboBoxList,
                // so we clone and bind them.
                if (item is MultiComboBoxItem containerItem)
                {
                    this.filteredItems.Add(new MultiComboBoxItem
                    {
                        [!ContentControl.ContentProperty] = containerItem[!ContentControl.ContentProperty],
                        [!ContentControl.ContentTemplateProperty] = containerItem[!ContentControl.ContentTemplateProperty],
                        [!IsEnabledProperty] = containerItem[!IsEnabledProperty],
                        IsSelected = containerItem.IsSelected,
                    });
                }
                else
                {
                    this.filteredItems.Add(item);
                }
            }
        }
    }

    private bool ItemMatchesFilter(object? item, string? filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            return true;
        }

        object? content = (item as MultiComboBoxItem)?.Content ?? item;
        return content?.ToString()?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void FixInitializationFromAxamlItems()
    {
        bool isCleared = false;

        foreach (object? item in this.Items)
        {
            if (item is MultiComboBoxItem multiComboBoxItem)
            {
                if (!isCleared)
                {
                    this.SelectedItems?.Clear();
                    isCleared = true;
                }

                multiComboBoxItem.DataContext = multiComboBoxItem.Content;
                if (multiComboBoxItem.IsSelected)
                {
                    this.SelectedItems?.Add(multiComboBoxItem.DataContext);
                }
            }
        }
    }

    private void OnBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        this.SetCurrentValue(IsDropDownOpenProperty, !this.IsDropDownOpen);
    }

    private void OnSelectedItemsChanged(AvaloniaPropertyChangedEventArgs<IList?> args)
    {
        if (args.OldValue.Value is INotifyCollectionChanged old)
        {
            old.CollectionChanged -= this.OnSelectedItemsCollectionChanged;
        }

        if (args.NewValue.Value is INotifyCollectionChanged @new)
        {
            @new.CollectionChanged += this.OnSelectedItemsCollectionChanged;
        }
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.ApplyCurrentSelectionState();
    }

    private void ApplyCurrentSelectionState()
    {
        if (this.updateInternal) return;
        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count is null or 0);

        // Containers are now managed by innerItemsList
        Controls? containers = this.innerList.Presenter?.Panel?.Children;
        if (containers is not null)
        {
            foreach (Control? container in containers)
            {
                if (container is MultiComboBoxItem i)
                {
                    i.UpdateSelection();
                }
            }
        }

        foreach (object? item in this.ItemsView)
        {
            if (item is MultiComboBoxItem i)
            {
                i.UpdateSelection();
            }
        }

        this.selectAllItem?.UpdateSelection();
        this.RaiseDirectPropertyChanged(IsAllSelectedProperty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        this.HookKeyboardScroll(e);
        this.HookOpenAction(e);
        this.HookCloseAction(e);
        this.HookSelectAllNavigation(e);
        this.HookItemSelection(e);
        this.HookItemsNavigation(e);

        base.OnKeyDown(e);

        // Forward all other keypresses to the textbox and focus it (except Tab for normal navigation)
        if (!e.Handled && this.filterTextbox is { IsFocused: false } filter && e.KeySymbol is not null && e.Key != Key.Tab)
        {
            filter.Focus();
        }
    }

    private void HookKeyboardScroll(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (this.OverflowMode == MultiComboBoxOverflowMode.Scroll && e.Key is Key.Left or Key.Right && this.selectionScrollViewer is ScrollViewer sv)
        {
            sv.ScrollHorizontally(e.Key == Key.Left ? HorizontalScrollDirection.Left : HorizontalScrollDirection.Right);
            e.Handled = true;
        }
    }

    private void HookOpenAction(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (!this.IsDropDownOpen && e.Key is Key.Space or Key.Enter or Key.Down)
        {
            this.IsDropDownOpen = true;
            this.FocusFirstItemOrSelectAll();
            e.Handled = true;
        }
    }

    private void HookCloseAction(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (e.Key is Key.Escape or Key.Tab && this.IsDropDownOpen)
        {
            this.IsDropDownOpen = false;
            e.Handled = e.Key == Key.Escape;
        }
    }

    private void HookSelectAllNavigation(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (!this.IsDropDownOpen) return;

        if (this.selectAllItem?.IsFocused == true && e.Key == Key.Down)
        {
            this.FocusFirstItem();
            e.Handled = true;
        }

        if (e.Key == Key.Up)
        {
            // Containers are now managed by innerItemsList
            Control? firstChild = this.innerList.Presenter?.Panel?.Children.FirstOrDefault(CanFocus);
            if (firstChild?.IsFocused == true)
            {
                this.FocusSelectAll();
                e.Handled = true;
            }
        }
    }

    private void HookItemSelection(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (this.IsDropDownOpen && !this.IsFilterFocused && e.Key is Key.Enter or Key.Space)
        {
            this.ToggleFocusedItem();
            e.Handled = true;
        }
    }

    private void HookItemsNavigation(KeyEventArgs e)
    {
        if (e.Handled) return;

        // TODO: Support XY focus;

        // This part of code is needed just to acquire initial focus, subsequent focus navigation will be done by ItemsControl.
        if (this.IsDropDownOpen && this.ItemCount > 0 && e.Key is Key.Up or Key.Down && (this.IsFocused || this.IsFilterFocused))
        {
            e.Handled = this.FocusFirstItemOrSelectAll();
        }
    }

    private bool FocusFirstItemOrSelectAll() => this.FocusSelectAll() || this.FocusFirstItem();

    private bool FocusSelectAll()
    {
        if (this.selectAllItem is not null && CanFocus(this.selectAllItem))
        {
            return this.selectAllItem.Focus();
        }

        return false;
    }

    private bool FocusFirstItem()
    {
        // Containers are now managed by innerItemsList
        Control? firstChild = this.innerList.Presenter?.Panel?.Children.FirstOrDefault(CanFocus);
        if (firstChild != null)
        {
            this.innerList.ScrollIntoView(firstChild.DataContext ?? firstChild);
            return firstChild.Focus(NavigationMethod.Directional);
        }

        return false;
    }

    private void ToggleFocusedItem()
    {
        if (this.selectAllItem?.IsFocused == true)
        {
            this.selectAllItem.IsSelected = this.selectAllItem.IsSelected is null or false;
            return;
        }

        // Containers are now managed by innerItemsList
        foreach (Control dropdownItem in this.innerList.GetRealizedContainers())
        {
            if (dropdownItem is MultiComboBoxItem { IsFocused: true } multiComboBoxItem)
            {
                multiComboBoxItem.IsSelected = !multiComboBoxItem.IsSelected;
                break;
            }
        }
    }

    private static bool CanFocus(Control control) => control is { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true };
}