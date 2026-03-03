namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Metadata;

/// <summary>
/// A virtualized tile-based list box that supports grouping, selection, and keyboard navigation.
/// Items are displayed in a wrapping grid with uniform tile sizes.
/// </summary>
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer), IsRequired = true)]
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

    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<object, string>?>(
            nameof(GroupSelector));
    
    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<GroupedTileListBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<string, int>?>(
            nameof(GroupOrderSelector));

    private double? cachedHeaderHeight = null;
    
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
    
    private bool useGrouping = false;

    static GroupedTileListBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnItemsSourceChanged(e));

        ItemWidthProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnLayoutPropertyChanged(e));

        ItemHeightProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnLayoutPropertyChanged(e));

        ItemSpacingProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnLayoutPropertyChanged(e));

        SelectedItemProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnSelectedItemChanged(e));

        SelectedIndexProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnSelectedIndexChanged(e));

        GroupSelectorProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnGroupingPropertyChanged(e));

        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnGroupingPropertyChanged(e));
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
    /// Gets or sets the function used to determine the group for each item.
    /// Can be bound from a ViewModel for compile-time safety and MVVM support.
    /// </summary>
    public Func<object, string>? GroupSelector
    {
        get => this.GetValue(GroupSelectorProperty);
        set => this.SetValue(GroupSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether groups are sorted alphabetically by key. Applied as a secondary sort after <see cref="GroupOrderSelector"/>, if set.
    /// </summary>
    public bool GroupOrderAlphabetical
    {
        get => this.GetValue(GroupOrderAlphabeticalProperty);
        set => this.SetValue(GroupOrderAlphabeticalProperty, value);
    }
    
    /// <summary>
    /// Gets or sets the function used to determine the sort order for each group.
    /// Lower numbers appear first. If null, groups are sorted alphabetically by key.
    /// Can be bound from a ViewModel for compile-time safety and MVVM support.
    /// </summary>
    public Func<string, int>? GroupOrderSelector
    {
        get => this.GetValue(GroupOrderSelectorProperty);
        set => this.SetValue(GroupOrderSelectorProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.scrollViewer = e.NameScope.Get<ScrollViewer>("PART_ScrollViewer");

        // Always create ItemsRepeater(s) dynamically in UpdateItemsRepeater
        this.UpdateItemsRepeater();
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

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
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

        // Preserve selection by item reference if possible
        object? previouslySelectedItem = this.selectedItem;

        // Rebuild UI (grouped mode needs full rebuild, non-grouped mode ItemsRepeater handles it automatically)
        if (this.useGrouping)
        {
            this.UpdateItemsRepeater();
        }

        // Restore selection if item still exists in the collection
        if (previouslySelectedItem is not null && this.ItemsSource is not null)
        {
            bool itemStillExists = this.ItemsSource.Cast<object>().Contains(previouslySelectedItem);
            if (itemStillExists)
            {
                this.SelectItem(previouslySelectedItem);
            }
            else
            {
                // Selected item was removed
                // Setting SelectedItem to null will update SelectedIndex to -1 as well
                this.SelectedItem = null;
            }
        }
    }

    private void OnLayoutPropertyChanged(AvaloniaPropertyChangedEventArgs e)
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
    /// Returns groups sorted by <see cref="GroupOrderSelector"/> first (if set), then alphabetically by key (if <see cref="GroupOrderAlphabetical"/> is enabled).
    /// </summary>
    private IEnumerable<IGrouping<string, T>> GetOrderedGroups<T>(IEnumerable<IGrouping<string, T>> groups)
    {
        if (this.GroupOrderSelector is { } selector)
            return this.GroupOrderAlphabetical
                ? groups.OrderBy(g => selector(g.Key)).ThenBy(g => g.Key)
                : groups.OrderBy(g => selector(g.Key));

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
        this.useGrouping = this.GroupSelector is not null;

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

            if (this.ItemsSource is not null && this.GroupSelector is not null && this.scrollViewer is not null)
            {
                // Group the items and apply ordering
                IEnumerable<IGrouping<string, object>> groups = this.ItemsSource.Cast<object>().GroupBy(this.GroupSelector);
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
                        [!HeightProperty] = this[!ItemHeightProperty]
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

    private void OnGroupingPropertyChanged(AvaloniaPropertyChangedEventArgs e)
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
        if (container.Content is not null)
        {
            this.SelectItem(container.Content);
        }
    }

    private void OnSelectionChanged(object? newItem, int newIndex)
    {
        // Update Avalonia property bindings
        this.SelectedItem = newItem;
        this.SelectedIndex = newIndex;

        // Update visual state of realized containers
        this.UpdateRealizedContainerSelection();
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // External change to SelectedItem property
        if (!this.IsItemSelected(e.NewValue))
        {
            this.SelectItem(e.NewValue);
        }
    }

    private void OnSelectedIndexChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // External change to SelectedIndex property
        int newIndex = (int)e.NewValue!;
        if (!this.IsIndexSelected(newIndex))
        {
            this.SelectItemAtIndex(newIndex);
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
                this.SelectItem(newItem);
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
                    this.SelectItemAtIndex(newIndex);
                    break;

                case Key.Down:
                    // Don't change selection if already at the bottom
                    if (this.IsInLastRow(currentIndex, itemCount))
                    {
                        return;
                    }

                    newIndex = this.CalculateDownIndex(currentIndex, itemCount);
                    this.SelectItemAtIndex(newIndex);
                    break;

                case Key.Home:
                    // Select first item in visual order
                    {
                        object? firstItem = this.GetItemFromVisualIndex(0);
                        if (firstItem is not null)
                        {
                            this.SelectItem(firstItem);
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
                            this.SelectItem(lastItem);
                            newIndex = this.selectedIndex;
                        }
                    }
                    break;

                default:
                    return;
            }
        }

        // Scroll to show the newly selected item
        if (newIndex != currentIndex)
        {
            this.ScrollIntoView(newIndex);
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

        if (!this.useGrouping || this.GroupSelector is null)
        {
            return currentIndex < itemsPerRow;
        }

        // Find the first group
        IGrouping<object, IndexedItem>? firstGroup = this.GetGroupedIndexedItems(this.ItemsSource.Cast<object>(), this.GroupSelector).FirstOrDefault();
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

        if (!this.useGrouping || this.GroupSelector is null)
        {
            int lastRowStart = CalculateLastRowStart(itemCount, itemsPerRow);
            return currentIndex >= lastRowStart;
        }

        // Find the last group
        IGrouping<object, IndexedItem>? lastGroup = this.GetGroupedIndexedItems(this.ItemsSource.Cast<object>(), this.GroupSelector).LastOrDefault();
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
        if (!this.useGrouping || this.GroupSelector is null)
        {
            return fallback;
        }

        List<IGrouping<object, IndexedItem>> groups = this.GetGroupedIndexedItems(this.ItemsSource.Cast<object>(), this.GroupSelector).ToList();

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
        this.sourceToVisualMap = new Dictionary<object, int>(estimatedCount);

        if (this.useGrouping && this.GroupSelector is not null)
        {
            // Group the items and apply ordering
            IEnumerable<IGrouping<string, object>> orderedGroups = this.GetOrderedGroups(items.GroupBy(this.GroupSelector));

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

        if (!this.useGrouping || this.GroupSelector is null)
        {
            // Simple calculation for non-grouped mode
            int row = visualIndex / itemsPerRow;
            return row * (this.ItemHeight + this.ItemSpacing);
        }

        IEnumerable<IGrouping<string, object>> orderedGroups = this.GetOrderedGroups(this.ItemsSource.Cast<object>().GroupBy(this.GroupSelector));

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
        if (this.content is not StackPanel stackPanel || !this.useGrouping || this.GroupSelector is null)
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

    private void SelectItem(object? item)
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

        this.selectedItem = item;
        this.selectedIndex = index;

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    }

    private void SelectItemAtIndex(int index)
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

        this.selectedItem = item;
        this.selectedIndex = index;

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    }

    private void ClearSelection()
    {
        if (this.selectedItem is null)
        {
            return;
        }

        this.selectedItem = null;
        this.selectedIndex = -1;

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    }

    private bool IsItemSelected(object? item) =>
        item is not null && ReferenceEquals(item, this.selectedItem);

    private bool IsIndexSelected(int index) =>
        index >= 0 && index == this.selectedIndex;

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

        if (this.ItemsSource is IList list)
        {
            return list.IndexOf(item);
        }

        (int index, object? indexedItem) = this.ItemsSource.Cast<object?>()
            .Index()
            .FirstOrDefault(x => ReferenceEquals(x.Item, item));
        return indexedItem is null ? -1 : index;
    }

    /// <summary>
    /// Represents an item with its original index and group for navigation calculations.
    /// </summary>
    private sealed record IndexedItem(object Item, int Index, string Group);
}