namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Metadata;
using Avalonia.Metadata;
using Avalonia.Threading;
using Devolutions.AvaloniaControls.Helpers;

/// <summary>
/// A virtualized tile-based list box that supports grouping, selection, and keyboard navigation.
/// Items are displayed in a wrapping grid with uniform tile sizes.
/// </summary>
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer), IsRequired = true)]
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
public class GroupedTileListBox : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<GroupedTileListBox, IEnumerable?>(
            nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<GroupedTileListBox, IDataTemplate?>(
            nameof(ItemTemplate));

    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<GroupedTileListBox, double>(
            nameof(ItemWidth),
            150.0);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<GroupedTileListBox, double>(
            nameof(ItemHeight),
            150.0);

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<GroupedTileListBox, double>(
            nameof(ItemSpacing),
            8.0);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<GroupedTileListBox, object?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GroupedTileListBox, int>(
            nameof(SelectedIndex),
            -1,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<GroupedTileListBox, SelectionMode>(
            nameof(SelectionMode),
            defaultValue: SelectionMode.Single);

    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<GroupedTileListBox, IList?>(
            nameof(SelectedItems),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> AutoScrollToSelectedItemProperty =
        AvaloniaProperty.Register<GroupedTileListBox, bool>(
            nameof(AutoScrollToSelectedItem),
            defaultValue: true);

    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<object, string>?>(
            "GroupSelector");

    public static readonly StyledProperty<BindingBase?> GroupBindingProperty =
        AvaloniaProperty.Register<GroupedTileListBox, BindingBase?>(
            nameof(GroupBinding));

    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<GroupedTileListBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<string, int>?>(
            "GroupOrderSelector");

    public static readonly StyledProperty<BindingBase?> GroupOrderBindingProperty =
        AvaloniaProperty.Register<GroupedTileListBox, BindingBase?>(
            nameof(GroupOrderBinding));

    private double? cachedHeaderHeight;
    
    private int cachedItemsPerRow = -1;
    private double lastKnownWidth = -1;
    
    private bool indexMappingsDirty = true;
    private Dictionary<object, int>? sourceToVisualMap;
    private List<object>? visualToSourceMap;
    
    private INotifyCollectionChanged? collectionChangedSource;
    
    private ItemsRepeater? itemsRepeater;
    private ScrollViewer? scrollViewer;
    private Control? content;
    
    private int selectedIndex = -1;
    private object? selectedItem;

    // The range-selection anchor item (Shift-based selection extends from here). Stored by reference
    // rather than as a visual index so it stays valid when grouping/ordering rebuilds the visual map.
    private object? anchorItem;

    // Guards against re-entrancy when internal selection updates mutate SelectedItem/SelectedIndex/SelectedItems.
    private bool suppressSelectionSync;

    private bool useGrouping;

    private BindingEvaluator? bindingEvaluator = null;

    static GroupedTileListBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnItemsSourceChanged(e));

        ItemWidthProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnLayoutPropertyChanged(e));

        ItemHeightProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnLayoutPropertyChanged(e));

        ItemSpacingProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnLayoutPropertyChanged(e));

        SelectedItemProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnSelectedItemChanged(e));

        SelectedIndexProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnSelectedIndexChanged(e));

        SelectionModeProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnSelectionModeChanged(e));

        SelectedItemsProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnSelectedItemsPropertyChanged(e));

        GroupSelectorProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnGroupingPropertyChanged(e));

        GroupBindingProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnGroupingPropertyChanged(e));

        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnGroupingPropertyChanged(e));

        GroupOrderBindingProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnGroupingPropertyChanged(e));
    }

    public GroupedTileListBox()
    {
        // Seed a control-owned collection so SelectedItems is never null and can be observed/bound.
        // A TwoWay binding replaces this instance with the consumer's collection.
        this.SetCurrentValue(SelectedItemsProperty, new AvaloniaList<object>());
    }

    /// <summary>
    /// Gets or sets the collection of items to display.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => this.GetValue(ItemsSourceProperty);
        set => this.SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to display each item.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => this.GetValue(ItemTemplateProperty);
        set => this.SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of each item.
    /// </summary>
    public double ItemWidth
    {
        get => this.GetValue(ItemWidthProperty);
        set => this.SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of each item.
    /// </summary>
    public double ItemHeight
    {
        get => this.GetValue(ItemHeightProperty);
        set => this.SetValue(ItemHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between items.
    /// </summary>
    public double ItemSpacing
    {
        get => this.GetValue(ItemSpacingProperty);
        set => this.SetValue(ItemSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => this.GetValue(SelectedItemProperty);
        set => this.SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of the currently selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => this.GetValue(SelectedIndexProperty);
        set => this.SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets how items may be selected. Defaults to <see cref="SelectionMode.Single"/>.
    /// Set to <see cref="SelectionMode.Multiple"/> (optionally combined with
    /// <see cref="SelectionMode.Toggle"/> / <see cref="SelectionMode.AlwaysSelected"/>) to allow
    /// selecting more than one item via pointer (Ctrl/Cmd+click, Shift+click) and keyboard
    /// (Shift+arrows, Ctrl/Cmd+Space).
    /// </summary>
    public SelectionMode SelectionMode
    {
        get => this.GetValue(SelectionModeProperty);
        set => this.SetValue(SelectionModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the collection of selected items. The control keeps this in sync in every
    /// selection mode (0 or 1 item in <see cref="SelectionMode.Single"/>). Bind a two-way
    /// collection to observe or drive the selection.
    /// </summary>
    public IList? SelectedItems
    {
        get => this.GetValue(SelectedItemsProperty);
        set => this.SetValue(SelectedItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the control automatically scrolls to bring the
    /// currently selected item into view when <see cref="SelectedItem"/> or
    /// <see cref="SelectedIndex"/> changes. Defaults to <c>true</c>, matching
    /// Avalonia's <c>SelectingItemsControl.AutoScrollToSelectedItem</c>.
    /// </summary>
    public bool AutoScrollToSelectedItem
    {
        get => this.GetValue(AutoScrollToSelectedItemProperty);
        set => this.SetValue(AutoScrollToSelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the function used to determine the group for each item.
    /// </summary>
    // TODO: Once [AssignBinding] and [InheritDataTypeFromItems] are properly supported by
    //       JetBrains Rider, we can uncomment the obsoletion below
    // [Obsolete("Use GroupBinding instead for XAML-friendly, type-checked group selection.")]
    public Func<object, string>? GroupSelector
    {
        get => this.GetValue(GroupSelectorProperty);
        set => this.SetValue(GroupSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the binding used to extract the group key from each item.
    /// When set, this takes precedence over <see cref="GroupSelector"/>.
    /// The binding's data type is inferred from <see cref="ItemsSource"/> for compile-time safety.
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? GroupBinding
    {
        get => this.GetValue(GroupBindingProperty);
        set => this.SetValue(GroupBindingProperty, value);
    }

    /// <summary>
    /// Gets or sets whether groups are sorted alphabetically by key. Applied as a secondary sort after the resolved group-order selector, if set.
    /// </summary>
    public bool GroupOrderAlphabetical
    {
        get => this.GetValue(GroupOrderAlphabeticalProperty);
        set => this.SetValue(GroupOrderAlphabeticalProperty, value);
    }

    /// <summary>
    /// Gets or sets the function used to determine the sort order for each group.
    /// Lower numbers appear first. If null, groups are sorted alphabetically by key.
    /// </summary>
    // TODO: Once [AssignBinding] and [InheritDataTypeFromItems] are properly supported by
    //       JetBrains Rider, we can uncomment the obsoletion below
    // [Obsolete("Use GroupOrderBinding instead for XAML-friendly group ordering.")]
    public Func<string, int>? GroupOrderSelector
    {
        get => this.GetValue(GroupOrderSelectorProperty);
        set => this.SetValue(GroupOrderSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the binding used to extract a sort order value (int) for each group key.
    /// When set, this takes precedence over <see cref="GroupOrderSelector"/>.
    /// The binding is evaluated against the group key (string) as DataContext.
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public BindingBase? GroupOrderBinding
    {
        get => this.GetValue(GroupOrderBindingProperty);
        set => this.SetValue(GroupOrderBindingProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.scrollViewer = e.NameScope.Get<ScrollViewer>("PART_ScrollViewer");

        // Always create ItemsRepeater(s) dynamically in UpdateItemsRepeater
        this.UpdateItemsRepeater();

        // If selection was set before the template was applied, scroll now.
        this.AutoScrollToSelectionIfEnabled();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Re-subscribe to collection changes when control is re-attached
        // (in case ItemsSource was set while detached, or control was moved)
        if (this.ItemsSource is INotifyCollectionChanged notifyCollection && this.collectionChangedSource is null)
        {
            this.collectionChangedSource = notifyCollection;
            this.collectionChangedSource.CollectionChanged += this.OnCollectionChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Unsubscribe from collection changes to prevent memory leaks
        if (this.collectionChangedSource is not null)
        {
            this.collectionChangedSource.CollectionChanged -= this.OnCollectionChanged;
            this.collectionChangedSource = null;
        }
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs _)
    {
        // Unsubscribe from old collection
        if (this.collectionChangedSource is not null)
        {
            this.collectionChangedSource.CollectionChanged -= this.OnCollectionChanged;
            this.collectionChangedSource = null;
        }

        this.InvalidateIndexMappings();

        // Subscribe to new collection if it supports change notifications
        if (this.ItemsSource is INotifyCollectionChanged notifyCollection)
        {
            this.collectionChangedSource = notifyCollection;
            this.collectionChangedSource.CollectionChanged += this.OnCollectionChanged;
        }

        this.UpdateItemsRepeater();
    }

    /// <summary>
    /// Handles collection change notifications from observable collections (e.g., AvaloniaList).
    /// Simple approach: invalidate and rebuild everything on any change.
    /// TODO: For better performance, handle specific change types (Add, Remove, etc.) and only update affected items.
    /// </summary>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Invalidate cached index mappings - they'll rebuild on next access
        this.InvalidateIndexMappings();

        // Rebuild UI (grouped mode needs full rebuild, non-grouped mode ItemsRepeater handles it automatically)
        if (this.useGrouping)
        {
            this.UpdateItemsRepeater();
        }

        // Reconcile the (possibly multiple) selection against the new contents.
        this.ReconcileSelectionWithItemsSource();
    }

    /// <summary>
    /// Reconciles the selection after <see cref="ItemsSource"/> content changes: drops selected items
    /// that are no longer present (by reference, preserving any that remain), enforces
    /// <see cref="SelectionMode.AlwaysSelected"/>, and re-derives the primary item.
    /// </summary>
    private void ReconcileSelectionWithItemsSource() => this.BeginSelectionUpdate(() =>
    {
        if (this.SelectedItems is not { } selectedItems)
        {
            return;
        }

        if (this.ItemsSource is null)
        {
            selectedItems.Clear();
        }
        else
        {
            HashSet<object> present = new(this.ItemsSource.Cast<object>(), ReferenceEqualityComparer.Instance);
            for (int i = selectedItems.Count - 1; i >= 0; i--)
            {
                if (selectedItems[i] is not { } selected || !present.Contains(selected))
                {
                    selectedItems.RemoveAt(i);
                }
            }
        }

        // AlwaysSelected: never leave the control empty while items exist.
        this.EnsureAlwaysSelectedInvariant();

        // Re-derive the primary (and anchor) from what survived.
        if (this.selectedItem is null || !this.IsItemSelected(this.selectedItem))
        {
            object? primary = this.GetLastSelectedItem();
            this.selectedItem = primary;
            this.selectedIndex = primary is null ? -1 : this.GetIndexOfItem(primary);
            this.anchorItem = primary;
        }
        else
        {
            // Primary survived but its source index may have shifted.
            this.selectedIndex = this.GetIndexOfItem(this.selectedItem);
        }

        this.SetCurrentValue(SelectedItemProperty, this.selectedItem);
        this.SetCurrentValue(SelectedIndexProperty, this.selectedIndex);

        this.UpdateRealizedContainerSelection();
    });

    private void OnLayoutPropertyChanged(AvaloniaPropertyChangedEventArgs _)
    {
        if (this.itemsRepeater?.Layout is WrapLayout layout)
        {
            layout.HorizontalSpacing = this.ItemSpacing;
            layout.VerticalSpacing = this.ItemSpacing;
        }
        
        this.cachedItemsPerRow = -1;

        // Invalidate layout to recalculate with new dimensions
        this.itemsRepeater?.InvalidateArrange();
    }

    /// <summary>
    /// Returns groups sorted by the resolved group-order selector first (if set), then alphabetically by key (if <see cref="GroupOrderAlphabetical"/> is enabled).
    /// </summary>
    private IEnumerable<IGrouping<string, T>> GetOrderedGroups<T>(IEnumerable<IGrouping<string, T>> groups)
    {
        if (this.ResolveGroupOrderSelector() is { } selector)
        {
            return this.GroupOrderAlphabetical
                ? groups.OrderBy(g => selector(g.Key)).ThenBy(g => g.Key)
                : groups.OrderBy(g => selector(g.Key));
        }

        return this.GroupOrderAlphabetical ? groups.OrderBy(g => g.Key) : groups;
    }

    /// <summary>
    /// Groups items with their original indices, ordered by group key.
    /// </summary>
    private IEnumerable<IGrouping<object, IndexedItem>> GetGroupedIndexedItems(
        IEnumerable<object> allItems,
        Func<object, string> groupSelector)
    {
        return this.GetOrderedGroups(allItems
            .Select((item, index) => new IndexedItem(item, index, groupSelector(item)))
            .GroupBy(x => x.Group));
    }

    private void UpdateItemsRepeater()
    {
        if (this.scrollViewer is null)
        {
            return;
        }

        // Determine if grouping is enabled
        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        this.useGrouping = groupSelector is not null;

        // Unhook old ItemsRepeater if it exists
        if (this.itemsRepeater is not null)
        {
            this.itemsRepeater.ElementPrepared -= this.OnElementPrepared;
            this.itemsRepeater = null;
        }

        this.content = null;

        if (this.useGrouping)
        {
            // Create StackPanel to hold groups
            StackPanel stackPanel = new()
            {
                Spacing = this.ItemSpacing,
                [!MarginProperty] = this[!PaddingProperty],
                [!MinWidthProperty] = this[!ItemWidthProperty],
                [!MinHeightProperty] = this[!ItemHeightProperty]
            };

            if (this.ItemsSource is not null && groupSelector is not null && this.scrollViewer is not null)
            {
                // Group the items and apply ordering
                IEnumerable<IGrouping<string, object>> groups = this.ItemsSource.Cast<object>().GroupBy(groupSelector);
                IEnumerable<IGrouping<string, object>> orderedGroups = this.GetOrderedGroups(groups);

                foreach (IGrouping<string, object> group in orderedGroups)
                {
                    // Create a container for each group
                    StackPanel groupContainer = new()
                    {
                        Spacing = this.ItemSpacing,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    if (!string.IsNullOrEmpty(group.Key))
                    {
                        // Add group header (not virtualized)
                        GroupedTileListBoxGroupHeader headerControl = new()
                        {
                            Text = group.Key
                        };

                        if (this.cachedHeaderHeight is null)
                        {
                            // Set the value so that the handler is only called for the first header
                            this.cachedHeaderHeight = 0;
                            EventHandler? layoutHandler = null;
                            layoutHandler = (sender, _) =>
                            {
                                if (sender is Control { Bounds.Height: > 0 } control)
                                {
                                    this.cachedHeaderHeight = control.Bounds.Height + control.Margin.Top + control.Margin.Bottom;
                                    // Unsubscribe to avoid memory leak
                                    control.LayoutUpdated -= layoutHandler;
                                }
                            };
                            headerControl.LayoutUpdated += layoutHandler;
                        }

                        groupContainer.Children.Add(headerControl);
                    }

                    // Add ItemsRepeater for group items (virtualized)
                    ItemsRepeater groupRepeater = new()
                    {
                        ItemsSource = group.ToList(),
                        Margin = new Thickness(0, 0, 0, this.ItemSpacing),
                        Layout = new WrapLayout
                        {
                            Orientation = Orientation.Horizontal,
                            [!WrapLayout.HorizontalSpacingProperty] = this[!ItemSpacingProperty],
                            [!WrapLayout.VerticalSpacingProperty] = this[!ItemSpacingProperty]
                        },
                        // Create item template that wraps in container with fixed size
                        ItemTemplate = new FuncDataTemplate<object>((item, _) =>
                        {
                            GroupedTileListBoxItem container = new()
                            {
                                Content = item,
                                [!WidthProperty] = this[!ItemWidthProperty],
                                [!HeightProperty] = this[!ItemHeightProperty]
                            };

                            if (this.ItemTemplate is not null)
                            {
                                container.ContentTemplate = this.ItemTemplate;
                            }

                            return container;
                        })
                    };

                    groupRepeater.ElementPrepared += this.OnElementPrepared;

                    groupContainer.Children.Add(groupRepeater);
                    stackPanel.Children.Add(groupContainer);
                }
            }

            this.content = stackPanel;
        }
        else if (this.scrollViewer is not null)
        {
            // No grouping - create single ItemsRepeater dynamically

            // Create new ItemsRepeater for non-grouped mode
            this.itemsRepeater = new ItemsRepeater
            {
                ItemsSource = this.ItemsSource,
                [!MarginProperty] = this[!PaddingProperty],
                Layout = new WrapLayout
                {
                    Orientation = Orientation.Horizontal,
                    [!WrapLayout.HorizontalSpacingProperty] = this[!ItemSpacingProperty],
                    [!WrapLayout.VerticalSpacingProperty] = this[!ItemSpacingProperty],
                    [!MinWidthProperty] = this[!ItemWidthProperty],
                    [!MinHeightProperty] = this[!ItemHeightProperty]
                },
                // Wrap ItemTemplate in container with fixed size (matches grouped mode pattern)
                ItemTemplate = new FuncDataTemplate<object>((item, _) =>
                {
                    GroupedTileListBoxItem container = new()
                    {
                        Content = item,
                        [!WidthProperty] = this[!ItemWidthProperty],
                        [!HeightProperty] = this[!ItemHeightProperty],
                    };

                    if (this.ItemTemplate is not null)
                    {
                        container.ContentTemplate = this.ItemTemplate;
                    }

                    return container;
                })
            };

            this.itemsRepeater.ElementPrepared += this.OnElementPrepared;

            this.content = this.itemsRepeater;
        }

        if (this.scrollViewer is not null)
        {
            this.scrollViewer.Content = this.content;
        }
    }

    private void OnGroupingPropertyChanged(AvaloniaPropertyChangedEventArgs _)
    {
        this.InvalidateIndexMappings();
        this.UpdateItemsRepeater();
    }

    private void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is not GroupedTileListBoxItem container || sender is not ItemsRepeater repeater)
        {
            return;
        }

        // Get the actual item from the repeater's ItemsSource at this index
        object? item = repeater.ItemsSource switch
        {
            IList list when e.Index >= 0 && e.Index < list.Count => list[e.Index],
            IEnumerable enumerable => enumerable.Cast<object>().ElementAtOrDefault(e.Index),
            _ => null
        };

        // Update selection state
        container.Content = item;
        container.IsSelected = this.IsItemSelected(item);
        container.ItemIndex = e.Index;

        // Wire up pointer pressed event
        container.PointerPressed -= this.OnContainerPointerPressed;
        container.PointerPressed += this.OnContainerPointerPressed;
    }

    private void OnContainerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not GroupedTileListBoxItem container)
        {
            return;
        }

        // Get the actual data item from the container's Content
        if (container.Content is not { } item)
        {
            return;
        }

        if (this.SelectionMode.HasFlag(SelectionMode.Multiple))
        {
            bool toggle = HasToggleModifier(e.KeyModifiers) || this.SelectionMode.HasFlag(SelectionMode.Toggle);
            bool range = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

            if (range)
            {
                // Shift+click extends the range from the anchor; Ctrl/Cmd+Shift adds the range to the selection.
                this.SelectRangeToVisual(this.GetVisualIndexFromItem(item), union: toggle);
            }
            else if (toggle)
            {
                this.ToggleItemSelection(item);
            }
            else
            {
                this.SelectItem(item);
            }
        }
        else
        {
            this.SelectItem(item);
        }

        e.Handled = true;
    }

    private void OnSelectionChanged(object? newItem, int newIndex)
    {
        // Update Avalonia property bindings
        this.SelectedItem = newItem;
        this.SelectedIndex = newIndex;

        // Update visual state of realized containers
        this.UpdateRealizedContainerSelection();

        // Single scroll trigger for every selection change (internal or external).
        this.AutoScrollToSelectionIfEnabled();
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Ignore property writes that originate from our own selection updates.
        if (this.suppressSelectionSync)
        {
            return;
        }

        // External change to SelectedItem property.
        object? newValue = e.NewValue;

        // Already the primary: nothing to reconcile.
        if (ReferenceEquals(newValue, this.selectedItem))
        {
            return;
        }

        if (newValue is not null && this.IsItemSelected(newValue))
        {
            // Already part of a multi-selection: promote it to primary without clearing the rest.
            this.PromotePrimary(newValue);
        }
        else
        {
            // Not selected: replace the selection with this single item.
            this.SelectItem(newValue);
        }
    }

    private void OnSelectedIndexChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Ignore property writes that originate from our own selection updates.
        if (this.suppressSelectionSync)
        {
            return;
        }

        // External change to SelectedIndex property.
        int newIndex = (int)e.NewValue!;

        // Already the primary index: nothing to reconcile.
        if (newIndex == this.selectedIndex)
        {
            return;
        }

        object? item = this.GetItemAtIndex(newIndex);
        if (item is not null && this.IsItemSelected(item))
        {
            // Already selected: promote to primary without disturbing the rest of the selection.
            this.PromotePrimary(item);
        }
        else
        {
            this.SelectItemAtIndex(newIndex);
        }
    }

    /// <summary>
    /// Makes an already-selected item the primary (keyboard/scroll focus and anchor) without changing
    /// which items are selected. Used when SelectedItem/SelectedIndex is set externally to an item that
    /// is already part of a multiple selection.
    /// </summary>
    private void PromotePrimary(object item) => this.BeginSelectionUpdate(() =>
    {
        this.selectedItem = item;
        this.selectedIndex = this.GetIndexOfItem(item);
        this.anchorItem = item;

        this.SetCurrentValue(SelectedItemProperty, this.selectedItem);
        this.SetCurrentValue(SelectedIndexProperty, this.selectedIndex);

        this.AutoScrollToSelectionIfEnabled();
    });

    private void OnSelectionModeChanged(AvaloniaPropertyChangedEventArgs _) => this.BeginSelectionUpdate(() =>
    {
        // Leaving multiple selection: collapse down to the primary item only.
        if (!this.SelectionMode.HasFlag(SelectionMode.Multiple) && (this.SelectedItems?.Count ?? 0) > 1)
        {
            if (this.selectedItem is { } primary)
            {
                this.SetSelectedItemsTo(primary);
            }
            else
            {
                this.SelectedItems?.Clear();
            }
        }

        // AlwaysSelected may have just been enabled on an empty selection.
        this.EnsureAlwaysSelectedInvariant();

        // Re-derive the primary in case the collapse/enforcement changed the selection.
        if (this.selectedItem is null || !this.IsItemSelected(this.selectedItem))
        {
            object? newPrimary = this.GetLastSelectedItem();
            this.selectedItem = newPrimary;
            this.selectedIndex = newPrimary is null ? -1 : this.GetIndexOfItem(newPrimary);
            this.anchorItem = newPrimary;
            this.SetCurrentValue(SelectedItemProperty, this.selectedItem);
            this.SetCurrentValue(SelectedIndexProperty, this.selectedIndex);
        }

        this.UpdateRealizedContainerSelection();
    });

    private void OnSelectedItemsPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Re-hook collection-change notifications to the newly assigned collection.
        if (e.OldValue is INotifyCollectionChanged oldNotify)
        {
            oldNotify.CollectionChanged -= this.OnSelectedItemsCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newNotify)
        {
            newNotify.CollectionChanged += this.OnSelectedItemsCollectionChanged;
        }

        if (this.suppressSelectionSync)
        {
            return;
        }

        this.SyncFromSelectedItems();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only react to external mutations; our own updates are applied directly.
        if (this.suppressSelectionSync)
        {
            return;
        }

        this.SyncFromSelectedItems();
    }

    private void AutoScrollToSelectionIfEnabled()
    {
        if (!this.AutoScrollToSelectedItem || this.selectedIndex < 0)
        {
            return;
        }

        if (this.scrollViewer is null || !this.IsArrangeValid)
        {
            int targetIndex = this.selectedIndex;
            Dispatcher.UIThread.Post(() => this.ScrollIntoView(targetIndex), DispatcherPriority.Loaded);
        }
        else
        {
            this.ScrollIntoView(this.selectedIndex);
        }
    }

    private void UpdateRealizedContainerSelection()
    {
        if (this.content is null)
        {
            return;
        }

        if (this.useGrouping && this.content is StackPanel stackPanel)
        {
            // Update selection in all group ItemsRepeaters
            foreach (ItemsRepeater repeater in GetAllItemsRepeaters(stackPanel))
            {
                this.UpdateRepeaterSelection(repeater);
            }
        }
        else if (this.itemsRepeater is not null)
        {
            // Update selection in single ItemsRepeater (non-grouped)
            this.UpdateRepeaterSelection(this.itemsRepeater);
        }
    }

    /// <summary>
    /// Gets all ItemsRepeaters from the grouped layout structure.
    /// </summary>
    private static IEnumerable<ItemsRepeater> GetAllItemsRepeaters(StackPanel stackPanel)
    {
        return stackPanel.Children
            .OfType<StackPanel>()
            .SelectMany(static groupStack => groupStack.Children.OfType<ItemsRepeater>());
    }

    private void UpdateRepeaterSelection(ItemsRepeater repeater)
    {
        // Get the total count of items in the ItemsSource
        if (repeater.ItemsSource is not IEnumerable itemsSource)
        {
            return;
        }

        int count = itemsSource is IList list
            ? list.Count
            : itemsSource.Cast<object>().Count();

        // Iterate through all ItemsSource indices
        // TryGetElement returns null for non-realized (virtualized) items
        for (int i = 0; i < count; i++)
        {
            if (repeater.TryGetElement(i) is GroupedTileListBoxItem container)
            {
                // Get the actual data item from the container's Content
                object? item = container.Content;
                container.IsSelected = this.IsItemSelected(item);
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // Invalidate cached items per row when width changes
        if (Math.Abs(e.NewSize.Width - this.lastKnownWidth) > 0.1)
        {
            this.cachedItemsPerRow = -1;
            this.lastKnownWidth = e.NewSize.Width;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || this.ItemsSource is null)
        {
            return;
        }

        int currentIndex = this.selectedIndex;
        int itemCount = this.GetItemCount();

        if (itemCount == 0)
        {
            return;
        }

        bool multiple = this.SelectionMode.HasFlag(SelectionMode.Multiple);

        // Ctrl/Cmd+Space (or Toggle mode + Space) toggles the current item's selection.
        if (multiple && e.Key == Key.Space
            && (HasToggleModifier(e.KeyModifiers) || this.SelectionMode.HasFlag(SelectionMode.Toggle)))
        {
            if (this.selectedItem is not null)
            {
                this.ToggleItemSelection(this.selectedItem);
                e.Handled = true;
            }

            return;
        }

        // Ctrl/Cmd+A selects every item.
        if (multiple && e.Key == Key.A && HasToggleModifier(e.KeyModifiers))
        {
            this.SelectAll();
            e.Handled = true;
            return;
        }

        // In multiple mode, holding Shift extends the selection range instead of replacing it.
        bool extend = multiple && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        int newIndex = currentIndex;

        // For Left/Right, use visual order navigation (respects grouping)
        if (e.Key is Key.Left or Key.Right)
        {
            object? currentItem = this.selectedItem;
            int currentVisualIndex = this.GetVisualIndexFromItem(currentItem);

            // If mapping failed, fall back to source index as visual index
            if (currentVisualIndex < 0)
            {
                return;
            }

            int newVisualIndex;
            if (e.Key == Key.Left)
            {
                // Navigate left in visual order (with wrapping)
                newVisualIndex = (itemCount + currentVisualIndex - 1) % itemCount;
            }
            else // Key.Right
            {
                // Navigate right in visual order (with wrapping)
                newVisualIndex = (currentVisualIndex + 1) % itemCount;
            }

            // Convert visual index back to source item
            object? newItem = this.GetItemFromVisualIndex(newVisualIndex);
            if (newItem is not null)
            {
                if (extend)
                {
                    this.SelectRangeToVisual(newVisualIndex, union: false);
                }
                else
                {
                    this.SelectItem(newItem);
                }

                newIndex = this.selectedIndex;
            }
        }
        else
        {
            // For Up/Down/Home/End, continue using source order
            switch (e.Key)
            {
                case Key.Up:
                    // Don't change selection if already at the top
                    if (this.IsInFirstRow(currentIndex))
                    {
                        return;
                    }

                    newIndex = this.CalculateUpIndex(currentIndex);
                    this.SelectByKeyboard(newIndex, extend);
                    break;

                case Key.Down:
                    // Don't change selection if already at the bottom
                    if (this.IsInLastRow(currentIndex, itemCount))
                    {
                        return;
                    }

                    newIndex = this.CalculateDownIndex(currentIndex, itemCount);
                    this.SelectByKeyboard(newIndex, extend);
                    break;

                case Key.Home:
                    // Select first item in visual order
                    {
                        object? firstItem = this.GetItemFromVisualIndex(0);
                        if (firstItem is not null)
                        {
                            if (extend)
                            {
                                this.SelectRangeToVisual(0, union: false);
                            }
                            else
                            {
                                this.SelectItem(firstItem);
                            }

                            newIndex = this.selectedIndex;
                        }
                    }
                    break;

                case Key.End:
                    // Select last item in visual order
                    {
                        object? lastItem = this.GetItemFromVisualIndex(itemCount - 1);
                        if (lastItem is not null)
                        {
                            if (extend)
                            {
                                this.SelectRangeToVisual(itemCount - 1, union: false);
                            }
                            else
                            {
                                this.SelectItem(lastItem);
                            }

                            newIndex = this.selectedIndex;
                        }
                    }
                    break;

                default:
                    return;
            }
        }

        if (newIndex != currentIndex)
        {
            // OnSelectionChanged already scrolled via AutoScrollToSelectionIfEnabled
            // when the property is on. When it's off, still scroll here because
            // explicit user keyboard navigation should always follow the selection
            // (matches Avalonia ListBox, where arrow keys scroll even with
            // AutoScrollToSelectedItem=false).
            if (!this.AutoScrollToSelectedItem)
            {
                this.ScrollIntoView(newIndex);
            }

            e.Handled = true;
        }
    }

    private int CalculateItemsPerRow()
    {
        if (this.cachedItemsPerRow > 0)
        {
            return this.cachedItemsPerRow;
        }

        if (this.content is null)
        {
            return 1;
        }
        
        // Bounds already exclude margins, so no need to subtract those
        double availableWidth = this.content.Bounds.Width;

        if (availableWidth <= 0)
        {
            return 1;
        }

        // Calculate items per row: N * ItemWidth + (N-1) * ItemSpacing <= availableWidth
        // Solving for N: N <= (availableWidth + ItemSpacing) / (ItemWidth + ItemSpacing)
        // The last item doesn't have trailing spacing, so we add ItemSpacing to the numerator
        int itemsPerRow = (int)Math.Floor((availableWidth + this.ItemSpacing) / (this.ItemWidth + this.ItemSpacing));
        this.cachedItemsPerRow = Math.Max(1, itemsPerRow);

        return this.cachedItemsPerRow;
    }

    /// <summary>
    /// Calculates the row number (0-indexed) of the last row for a given item count and items per row.
    /// Uses integer division: (itemCount - 1) / itemsPerRow.
    /// The -1 ensures correct rounding for exact multiples (e.g., 20 items, 5 per row → row 3, not row 4).
    /// </summary>
    private static int CalculateLastRowNumber(int itemCount, int itemsPerRow) =>
        (itemCount - 1) / itemsPerRow;

    /// <summary>
    /// Calculates the starting index of the last row for a given item count and items per row.
    /// </summary>
    private static int CalculateLastRowStart(int itemCount, int itemsPerRow) =>
        CalculateLastRowNumber(itemCount, itemsPerRow) * itemsPerRow;
    
    /// <summary>
    /// Checks if the current index is in the first row.
    /// </summary>
    private bool IsInFirstRow(int currentIndex)
    {
        if (currentIndex < 0 || this.ItemsSource is null)
        {
            return false;
        }

        int itemsPerRow = this.CalculateItemsPerRow();

        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        if (!this.useGrouping || groupSelector is null)
        {
            return currentIndex < itemsPerRow;
        }

        // Find the first group
        IGrouping<object, IndexedItem>? firstGroup = this.GetGroupedIndexedItems(this.ItemsSource.Cast<object>(), groupSelector).FirstOrDefault();
        if (firstGroup is null)
        {
            return false;
        }

        (int indexInGroup, IndexedItem? item) = firstGroup.Index().FirstOrDefault(indexedItem => indexedItem.Item.Index == currentIndex);

        if (item is null)
        {
            return false; // Not in first group
        }

        // Check if in first row of first group
        return indexInGroup < itemsPerRow;
    }

    /// <summary>
    /// Checks if the current index is in the last row.
    /// </summary>
    private bool IsInLastRow(int currentIndex, int itemCount)
    {
        if (currentIndex < 0 || currentIndex >= itemCount || this.ItemsSource is null)
        {
            return false;
        }

        int itemsPerRow = this.CalculateItemsPerRow();

        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        if (!this.useGrouping || groupSelector is null)
        {
            int lastRowStart = CalculateLastRowStart(itemCount, itemsPerRow);
            return currentIndex >= lastRowStart;
        }

        // Find the last group
        IGrouping<object, IndexedItem>? lastGroup = this.GetGroupedIndexedItems(this.ItemsSource.Cast<object>(), groupSelector).LastOrDefault();
        if (lastGroup == null)
        {
            return false;
        }

        List<IndexedItem> lastGroupItems = lastGroup.ToList();
        int indexInGroup = lastGroupItems.FindIndex(x => x.Index == currentIndex);

        if (indexInGroup < 0)
        {
            return false; // Not in last group
        }

        // Check if in last row of last group
        int lastGroupRowStart = CalculateLastRowStart(lastGroupItems.Count, itemsPerRow);
        return indexInGroup >= lastGroupRowStart;
    }

    /// <summary>
    /// Calculates the index when navigating vertically, accounting for group boundaries.
    /// </summary>
    private int CalculateVerticalNavigationIndex(int currentIndex, int itemCount, bool moveUp)
    {
        if (this.ItemsSource is null)
        {
            return -1;
        }
        
        int itemsPerRow = this.CalculateItemsPerRow();
        int offset = moveUp ? -itemsPerRow : itemsPerRow;
        int fallback = moveUp
            ? Math.Max(0, currentIndex + offset)
            : Math.Min(itemCount - 1, currentIndex + offset);

        // If no grouping, use simple calculation
        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        if (!this.useGrouping || groupSelector is null)
        {
            return fallback;
        }

        List<IGrouping<object, IndexedItem>> groups = this.GetGroupedIndexedItems(this.ItemsSource.Cast<object>(), groupSelector).ToList();

        // Find current item's group and position within group
        foreach (IGrouping<object, IndexedItem> group in groups)
        {
            List<IndexedItem> groupItems = group.ToList();
            int indexInGroup = groupItems.FindIndex(x => x.Index == currentIndex);

            if (indexInGroup >= 0)
            {
                // Calculate row and column within this group
                int row = indexInGroup / itemsPerRow;
                int col = indexInGroup % itemsPerRow;

                if (moveUp)
                {
                    if (row > 0)
                    {
                        // Move up within the same group
                        int targetIndexInGroup = (row - 1) * itemsPerRow + col;
                        if (targetIndexInGroup < groupItems.Count)
                        {
                            return groupItems[targetIndexInGroup].Index;
                        }
                    }
                    else
                    {
                        // Move to previous group's last row
                        int groupIndex = groups.IndexOf(group);
                        if (groupIndex > 0)
                        {
                            List<IndexedItem> prevGroup = groups[groupIndex - 1].ToList();
                            if (prevGroup.Count > 0)
                            {
                                int prevGroupLastRow = CalculateLastRowNumber(prevGroup.Count, itemsPerRow);
                                int targetIndexInPrevGroup = Math.Min(prevGroupLastRow * itemsPerRow + col, prevGroup.Count - 1);
                                return prevGroup[targetIndexInPrevGroup].Index;
                            }
                        }
                    }
                }
                else // moveDown
                {
                    int lastRow = CalculateLastRowNumber(groupItems.Count, itemsPerRow);
                    if (row < lastRow)
                    {
                        // Move down within the same group
                        int targetIndexInGroup = (row + 1) * itemsPerRow + col;
                        return targetIndexInGroup < groupItems.Count
                            ? groupItems[targetIndexInGroup].Index
                            : groupItems[^1].Index;
                    }
                    else
                    {
                        // Move to next group's first row
                        int groupIndex = groups.IndexOf(group);
                        if (groupIndex < groups.Count - 1)
                        {
                            List<IndexedItem> nextGroup = groups[groupIndex + 1].ToList();
                            if (nextGroup.Count > 0)
                            {
                                int targetIndexInNextGroup = Math.Min(col, nextGroup.Count - 1);
                                return nextGroup[targetIndexInNextGroup].Index;
                            }
                        }
                    }
                }

                break;
            }
        }

        return fallback;
    }

    /// <summary>
    /// Calculates the index when navigating up, accounting for group boundaries.
    /// </summary>
    private int CalculateUpIndex(int currentIndex) =>
        this.CalculateVerticalNavigationIndex(currentIndex, 0, true);

    /// <summary>
    /// Calculates the index when navigating down, accounting for group boundaries.
    /// </summary>
    private int CalculateDownIndex(int currentIndex, int itemCount) =>
        this.CalculateVerticalNavigationIndex(currentIndex, itemCount, false);

    private int GetItemCount() =>
        GetItemCount(this.ItemsSource);

    private static int GetItemCount(IEnumerable? itemsSource)
    {
        if (itemsSource is null)
        {
            return 0;
        }

        IEnumerable<object> items = itemsSource.Cast<object>();
        return items.TryGetNonEnumeratedCount(out int count) ? count : items.Count();
    }

    private void InvalidateIndexMappings()
    {
        this.indexMappingsDirty = true;
        this.visualToSourceMap = null;
        this.sourceToVisualMap = null;
    }

    private void BuildIndexMappings()
    {
        if (!this.indexMappingsDirty || this.ItemsSource is null)
        {
            return;
        }

        IEnumerable<object> items = this.ItemsSource.Cast<object>();

        _ = items.TryGetNonEnumeratedCount(out int estimatedCount);
        this.visualToSourceMap = new List<object>(estimatedCount);

        // Reference-equality keys so value-equal items (e.g. records) map to distinct visual indices
        // instead of collapsing onto the first occurrence via TryAdd.
        this.sourceToVisualMap = new Dictionary<object, int>(estimatedCount, ReferenceEqualityComparer.Instance);

        if (this.useGrouping && this.ResolveGroupSelector() is { } groupSelector)
        {
            // Group the items and apply ordering
            IEnumerable<IGrouping<string, object>> orderedGroups = this.GetOrderedGroups(items.GroupBy(groupSelector));

            // Add all items from ordered groups to List (this is the visual order)
            foreach (IGrouping<string, object> group in orderedGroups)
            {
                this.visualToSourceMap.AddRange(group);
            }
        }
        else
        {
            // No grouping - add items in source order
            this.visualToSourceMap.AddRange(items);
        }

        // Build Dictionary from List (item → visual index)
        for (int i = 0; i < this.visualToSourceMap.Count; i++)
        {
            object item = this.visualToSourceMap[i];
            this.sourceToVisualMap.TryAdd(item, i);
        }

        this.indexMappingsDirty = false;
    }

    private int GetVisualIndexFromItem(object? item)
    {
        if (item is null)
        {
            return -1;
        }

        this.BuildIndexMappings();

        return this.sourceToVisualMap is not null && this.sourceToVisualMap.TryGetValue(item, out int index)
            ? index
            : -1;
    }

    private object? GetItemFromVisualIndex(int visualIndex)
    {
        this.BuildIndexMappings();

        return visualIndex >= 0 && visualIndex < (this.visualToSourceMap?.Count ?? 0)
            ? this.visualToSourceMap![visualIndex]
            : null;
    }

    private void ScrollIntoView(int sourceIndex)
    {
        if (this.scrollViewer is null || this.ItemsSource is null)
        {
            return;
        }

        // Get the item at this source index
        if (sourceIndex < 0)
        {
            return;
        }

        object? item = this.ItemsSource.Cast<object>().ElementAtOrDefault(sourceIndex);
        if (item is null)
        {
            return;
        }

        // Find the visual element for this item
        Control? targetElement = null;

        if (this.useGrouping)
        {
            // In grouped mode, find the element in one of the group ItemsRepeaters
            targetElement = this.FindElementInGroupedLayout(sourceIndex);
        }
        else if (this.itemsRepeater is not null)
        {
            // In non-grouped mode, get element from main ItemsRepeater
            targetElement = this.itemsRepeater.TryGetElement(sourceIndex);
        }

        if (targetElement is not null)
        {
            // Check if element is already fully visible
            if (this.IsElementFullyVisible(targetElement))
            {
                return; // Already visible, no need to scroll
            }

            // Scroll to make the element visible with minimal movement
            this.ScrollToElement(targetElement);
        }
        else
        {
            // Element is not realized, calculate estimated scroll offset
            double targetY = this.CalculateScrollOffsetForItem(item);

            // Scroll down
            if (targetY > this.scrollViewer.Offset.Y)
            {
                // Remove the ViewPort height (minus ItemHeight and ItemSpacing) to make the selected item at the bottom the ViewPort
                targetY -= this.scrollViewer.Viewport.Height - this.ItemHeight - this.ItemSpacing;
            }
            
            this.scrollViewer.Offset = new Vector(this.scrollViewer.Offset.X, targetY);
        }
    }

    private double CalculateScrollOffsetForItem(object item)
    {
        if (this.scrollViewer is null || this.ItemsSource is null)
        {
            return 0;
        }

        int visualIndex = this.GetVisualIndexFromItem(item);
        if (visualIndex < 0)
        {
            return 0;
        }

        int itemsPerRow = this.CalculateItemsPerRow();
        if (itemsPerRow <= 0)
        {
            return 0;
        }

        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        if (!this.useGrouping || groupSelector is null)
        {
            // Simple calculation for non-grouped mode
            int row = visualIndex / itemsPerRow;
            return row * (this.ItemHeight + this.ItemSpacing);
        }

        IEnumerable<IGrouping<string, object>> orderedGroups = this.GetOrderedGroups(this.ItemsSource.Cast<object>().GroupBy(groupSelector));

        double offset = 0;
        int itemsSoFar = 0;

        foreach (IGrouping<string, object> group in orderedGroups)
        {
            int groupItemCount = group.Count();
            bool hasHeader = group.Key is string groupName && !string.IsNullOrEmpty(groupName);

            // Add header height for this group (only if it has a visible header)
            if (hasHeader)
            {
                offset += (this.cachedHeaderHeight ?? 0) + this.ItemSpacing;
            }

            if (itemsSoFar + groupItemCount > visualIndex)
            {
                // Target item is in this group - calculate position within this group
                int itemIndexInGroup = visualIndex - itemsSoFar;
                int rowInGroup = itemIndexInGroup / itemsPerRow;
                offset += rowInGroup * (this.ItemHeight + this.ItemSpacing);

                break;
            }

            // Add remaining group height (items + spacing)
            int rowsInGroup = (int)Math.Ceiling((double)groupItemCount / itemsPerRow);
            offset += rowsInGroup * (this.ItemHeight + this.ItemSpacing);
            offset += this.ItemSpacing; // Group spacing

            itemsSoFar += groupItemCount;
        }

        return offset;
    }

    /// <summary>
    /// Finds the visual element for an item in the grouped layout.
    /// </summary>
    private Control? FindElementInGroupedLayout(int index)
    {
        if (this.content is not StackPanel stackPanel || !this.useGrouping || this.ResolveGroupSelector() is null)
        {
            return null;
        }

        object? targetItem = this.ItemsSource?.Cast<object>().ElementAtOrDefault(index);
        if (targetItem is null)
        {
            return null;
        }

        // Search through all group ItemsRepeaters
        foreach (ItemsRepeater repeater in GetAllItemsRepeaters(stackPanel))
        {
            int count = GetItemCount(repeater.ItemsSource);
            if (count == 0)
            {
                continue;
            }

            // Search through all ItemsSource indices
            for (int i = 0; i < count; ++i)
            {
                Control? element = repeater.TryGetElement(i);

                if (element is GroupedTileListBoxItem container)
                {
                    object? item = container.Content;
                    if (ReferenceEquals(item, targetItem))
                    {
                        return container;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the element's rectangle in scroll viewer coordinates.
    /// Returns null if the position cannot be determined.
    /// </summary>
    private Rect? GetElementRectInScrollViewer(Control element)
    {
        if (this.scrollViewer is null)
        {
            return null;
        }

        Point? elementPosition = element.TranslatePoint(new Point(0, 0), this.scrollViewer);
        if (elementPosition is null)
        {
            return null;
        }

        return new Rect(elementPosition.Value, element.Bounds.Size);
    }

    /// <summary>
    /// Checks if an element is fully visible within the scroll viewer's viewport.
    /// </summary>
    private bool IsElementFullyVisible(Control element)
    {
        if (this.scrollViewer is null)
        {
            return false;
        }

        Rect? elementRect = this.GetElementRectInScrollViewer(element);
        if (elementRect is null)
        {
            return false;
        }

        Rect viewportBounds = new(0, 0, this.scrollViewer.Viewport.Width, this.scrollViewer.Viewport.Height);

        // Check if element is fully contained within viewport
        return viewportBounds.Contains(elementRect.Value);
    }

    /// <summary>
    /// Scrolls to make an element visible with minimal movement.
    /// </summary>
    private void ScrollToElement(Control element)
    {
        if (this.scrollViewer is null)
        {
            return;
        }

        Rect? elementRect = this.GetElementRectInScrollViewer(element);
        if (elementRect is null)
        {
            return;
        }

        Vector currentOffset = this.scrollViewer.Offset;
        double viewportHeight = this.scrollViewer.Viewport.Height;

        double newY = currentOffset.Y;

        // Vertical scrolling - only scroll if element is not fully visible vertically
        if (elementRect.Value.Y < 0)
        {
            // Element is above viewport, scroll up to show it
            newY = currentOffset.Y + elementRect.Value.Y;
        }
        else if (elementRect.Value.Y + elementRect.Value.Height > viewportHeight)
        {
            // Element is below viewport, scroll down to show it
            newY = currentOffset.Y + (elementRect.Value.Y + elementRect.Value.Height - viewportHeight);
        }

        // Only update offset if it changed
        if (Math.Abs(newY - currentOffset.Y) > 0.1)
        {
            this.scrollViewer.Offset = new Vector(currentOffset.X, newY);
        }
    }

    private void SelectItem(object? item) => this.BeginSelectionUpdate(() =>
    {
        if (item is null)
        {
            this.ClearSelection();
            return;
        }

        int index = this.GetIndexOfItem(item);
        if (index < 0)
        {
            this.ClearSelection();
            return;
        }

        this.SetSelectedItemsTo(item);

        this.selectedItem = item;
        this.selectedIndex = index;
        this.anchorItem = item;

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    });

    private void SelectItemAtIndex(int index) => this.BeginSelectionUpdate(() =>
    {
        if (index < 0)
        {
            this.ClearSelection();
            return;
        }

        object? item = this.GetItemAtIndex(index);
        if (item is null)
        {
            this.ClearSelection();
            return;
        }

        this.SetSelectedItemsTo(item);

        this.selectedItem = item;
        this.selectedIndex = index;
        this.anchorItem = item;

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    });

    private void ClearSelection()
    {
        if (this.selectedItem is null && (this.SelectedItems?.Count ?? 0) == 0)
        {
            return;
        }

        this.SelectedItems?.Clear();

        this.selectedItem = null;
        this.selectedIndex = -1;
        this.anchorItem = null;

        // AlwaysSelected: never leave the control empty while items exist.
        this.EnsureAlwaysSelectedInvariant();
        if (this.SelectedItems is { Count: > 0 })
        {
            object? primary = this.GetLastSelectedItem();
            this.selectedItem = primary;
            this.selectedIndex = primary is null ? -1 : this.GetIndexOfItem(primary);
            this.anchorItem = primary;
        }

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    }

    /// <summary>
    /// Selects every item. Invoked for Ctrl/Cmd+A while in <see cref="SelectionMode.Multiple"/>.
    /// </summary>
    private void SelectAll() => this.BeginSelectionUpdate(() =>
    {
        if (this.ItemsSource is null || this.SelectedItems is not { } selectedItems)
        {
            return;
        }

        selectedItems.Clear();
        foreach (object? item in this.ItemsSource)
        {
            if (item is not null)
            {
                selectedItems.Add(item);
            }
        }

        // Keep the current primary if it is still selected; otherwise adopt the last item.
        if (this.selectedItem is null || !this.IsItemSelected(this.selectedItem))
        {
            object? primary = this.GetLastSelectedItem();
            this.selectedItem = primary;
            this.selectedIndex = primary is null ? -1 : this.GetIndexOfItem(primary);
        }

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    });

    /// <summary>
    /// Toggles a single item's membership in the selection (Ctrl/Cmd+click, Ctrl/Cmd+Space).
    /// </summary>
    private void ToggleItemSelection(object? item) => this.BeginSelectionUpdate(() =>
    {
        if (item is null)
        {
            return;
        }

        int index = this.GetIndexOfItem(item);
        if (index < 0)
        {
            return;
        }

        this.anchorItem = item;

        if (this.IsItemSelected(item))
        {
            // AlwaysSelected keeps at least one item selected.
            if (this.SelectionMode.HasFlag(SelectionMode.AlwaysSelected) && (this.SelectedItems?.Count ?? 0) <= 1)
            {
                return;
            }

            this.RemoveFromSelection(item);

            // If we removed the primary item, promote the most recent remaining selection.
            if (ReferenceEquals(item, this.selectedItem))
            {
                object? newPrimary = this.GetLastSelectedItem();
                this.selectedItem = newPrimary;
                this.selectedIndex = newPrimary is null ? -1 : this.GetIndexOfItem(newPrimary);
            }
        }
        else
        {
            this.AddToSelection(item);
            this.selectedItem = item;
            this.selectedIndex = index;
        }

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    });

    /// <summary>
    /// Selects the visual-order range between the current anchor and <paramref name="targetVisualIndex"/>
    /// (Shift+click, Shift+arrows). When <paramref name="union"/> is false the range replaces the
    /// selection; when true it is added to the existing selection.
    /// </summary>
    private void SelectRangeToVisual(int targetVisualIndex, bool union) => this.BeginSelectionUpdate(() =>
    {
        if (targetVisualIndex < 0 || this.SelectedItems is not { } selectedItems)
        {
            return;
        }

        // Resolve the anchor's current visual index on demand (the map may have been rebuilt since
        // the anchor was set); fall back to the target so a lone Shift gesture selects just the target.
        int anchorVisualIndex = this.anchorItem is null ? -1 : this.GetVisualIndexFromItem(this.anchorItem);
        int anchor = anchorVisualIndex >= 0 ? anchorVisualIndex : targetVisualIndex;
        int lo = Math.Min(anchor, targetVisualIndex);
        int hi = Math.Max(anchor, targetVisualIndex);

        if (!union)
        {
            selectedItems.Clear();
        }

        // O(1) membership so a k-item range stays O(k) instead of O(k²).
        HashSet<object> seen = new(selectedItems.Cast<object>(), ReferenceEqualityComparer.Instance);

        object? targetItem = null;
        for (int visualIndex = lo; visualIndex <= hi; visualIndex++)
        {
            object? item = this.GetItemFromVisualIndex(visualIndex);
            if (item is null)
            {
                continue;
            }

            if (seen.Add(item))
            {
                selectedItems.Add(item);
            }

            if (visualIndex == targetVisualIndex)
            {
                targetItem = item;
            }
        }

        if (targetItem is not null)
        {
            // The anchor stays put so the range can be re-sized; the target becomes the primary item.
            this.selectedItem = targetItem;
            this.selectedIndex = this.GetIndexOfItem(targetItem);
            this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
        }
    });

    /// <summary>
    /// Applies keyboard navigation selection for source-order keys (Up/Down): replaces the selection,
    /// or extends the range from the anchor when <paramref name="extend"/> is set.
    /// </summary>
    private void SelectByKeyboard(int sourceIndex, bool extend)
    {
        if (!extend)
        {
            this.SelectItemAtIndex(sourceIndex);
            return;
        }

        object? item = this.GetItemAtIndex(sourceIndex);
        int visualIndex = item is null ? -1 : this.GetVisualIndexFromItem(item);
        if (visualIndex >= 0)
        {
            this.SelectRangeToVisual(visualIndex, union: false);
        }
        else
        {
            this.SelectItemAtIndex(sourceIndex);
        }
    }

    /// <summary>
    /// Reconciles internal state with an externally-driven change to <see cref="SelectedItems"/>
    /// (a rebind or a mutation of the bound collection): refreshes container visuals and re-derives
    /// the primary item.
    /// </summary>
    private void SyncFromSelectedItems() => this.BeginSelectionUpdate(() =>
    {
        // Enforce the Single-mode 0/1 invariant on an externally-provided selection: if more than one
        // item was supplied, keep the current primary (if still present) or the last item.
        if (!this.SelectionMode.HasFlag(SelectionMode.Multiple) && (this.SelectedItems?.Count ?? 0) > 1)
        {
            object? keep = this.selectedItem is not null && this.IsItemSelected(this.selectedItem)
                ? this.selectedItem
                : this.GetLastSelectedItem();

            if (keep is not null)
            {
                this.SetSelectedItemsTo(keep);
            }
        }

        // AlwaysSelected: never leave the control empty while items exist.
        this.EnsureAlwaysSelectedInvariant();

        // Re-derive the primary (and anchor) from the resulting selection.
        if (this.selectedItem is null || !this.IsItemSelected(this.selectedItem))
        {
            object? newPrimary = this.GetLastSelectedItem();
            this.selectedItem = newPrimary;
            this.selectedIndex = newPrimary is null ? -1 : this.GetIndexOfItem(newPrimary);
            this.anchorItem = newPrimary;
        }
        else
        {
            // Primary is still selected but its source index may have shifted.
            this.selectedIndex = this.GetIndexOfItem(this.selectedItem);
        }

        this.SetCurrentValue(SelectedItemProperty, this.selectedItem);
        this.SetCurrentValue(SelectedIndexProperty, this.selectedIndex);

        this.UpdateRealizedContainerSelection();
    });

    /// <summary>
    /// When <see cref="SelectionMode.AlwaysSelected"/> is set and the source is non-empty, guarantees at
    /// least one item stays selected by selecting the first item if the selection is otherwise empty.
    /// </summary>
    private void EnsureAlwaysSelectedInvariant()
    {
        if (!this.SelectionMode.HasFlag(SelectionMode.AlwaysSelected))
        {
            return;
        }

        if (this.SelectedItems is not { Count: 0 } || this.ItemsSource is null)
        {
            return;
        }

        if (this.GetItemAtIndex(0) is { } first)
        {
            this.AddToSelection(first);
        }
    }

    /// <summary>
    /// Runs a selection mutation with re-entrancy suppressed so the resulting property/collection
    /// writes don't loop back through the external-change handlers.
    /// </summary>
    private void BeginSelectionUpdate(Action update)
    {
        if (this.suppressSelectionSync)
        {
            update();
            return;
        }

        this.suppressSelectionSync = true;
        try
        {
            update();
        }
        finally
        {
            this.suppressSelectionSync = false;
        }
    }

    private void SetSelectedItemsTo(object item)
    {
        if (this.SelectedItems is not { } selectedItems)
        {
            return;
        }

        selectedItems.Clear();
        selectedItems.Add(item);
    }

    private void AddToSelection(object item)
    {
        if (this.SelectedItems is { } selectedItems && !ContainsByReference(selectedItems, item))
        {
            selectedItems.Add(item);
        }
    }

    private void RemoveFromSelection(object item)
    {
        if (this.SelectedItems is not { } selectedItems)
        {
            return;
        }

        for (int i = selectedItems.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(selectedItems[i], item))
            {
                selectedItems.RemoveAt(i);
            }
        }
    }

    private object? GetLastSelectedItem() =>
        this.SelectedItems is { Count: > 0 } selectedItems ? selectedItems[selectedItems.Count - 1] : null;

    private bool IsItemSelected(object? item) =>
        item is not null && this.SelectedItems is { } selectedItems && ContainsByReference(selectedItems, item);

    /// <summary>
    /// Reference-equality membership test. Matches the control's identity-based selection semantics
    /// and avoids false positives when items use value equality (e.g. records).
    /// </summary>
    private static bool ContainsByReference(IList list, object item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasToggleModifier(KeyModifiers modifiers) =>
        modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta);

    private object? GetItemAtIndex(int index)
    {
        if (this.ItemsSource is null || index < 0)
        {
            return null;
        }

        return this.ItemsSource.Cast<object?>().ElementAtOrDefault(index);
    }

    private int GetIndexOfItem(object? item)
    {
        if (this.ItemsSource is null || item is null)
        {
            return -1;
        }

        // Reference-based scan (not IList.IndexOf) so the identity-based selection semantics hold
        // even when items use value equality (e.g. records with equal values but distinct instances).
        int index = 0;
        foreach (object? candidate in this.ItemsSource)
        {
            if (ReferenceEquals(candidate, item))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Resolves the effective group selector: <see cref="GroupBinding"/> first, falling back to <see cref="GroupSelector"/>.
    /// </summary>
    private Func<object, string>? ResolveGroupSelector()
    {
        if (this.GetValue(GroupBindingProperty) is {} binding)
        {
            this.EnsureInitBindingEvaluator();
            return this.bindingEvaluator?.BuildFormattedGetter(binding);
        }

        return this.GetValue(GroupSelectorProperty);
    }

    /// <summary>
    /// Resolves the effective group-order selector: <see cref="GroupOrderBinding"/> first, falling back to <see cref="GroupOrderSelector"/>.
    /// The binding is evaluated against the group key (string) as DataContext.
    /// </summary>
    private Func<string, object?>? ResolveGroupOrderSelector()
    {
        if (this.GetValue(GroupOrderBindingProperty) is {} binding)
        {
            this.EnsureInitBindingEvaluator();
            return this.bindingEvaluator?.BuildFormattedGetter(binding);
        }

        if (this.GetValue(GroupOrderSelectorProperty) is {} orderFn)
        {
            return str => orderFn(str);
        }

        return null;
    }
    
    private void EnsureInitBindingEvaluator()
    {
        if (this.bindingEvaluator is null)
        {
            if (this.ItemsSource?.GetType() is {} itemsType
                && BindingEvaluator.GetTypeFromItemsSource(itemsType) is {} itemType)
            {
                this.bindingEvaluator = new BindingEvaluator(this, itemType);
            }
        }
    }

    /// <summary>
    /// Represents an item with its original index and group for navigation calculations.
    /// </summary>
    // ReSharper disable once NotAccessedPositionalProperty.Local
    private sealed record IndexedItem(object Item, int Index, string Group);
}