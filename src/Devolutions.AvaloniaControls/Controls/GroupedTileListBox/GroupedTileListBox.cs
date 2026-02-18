using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using System.Collections;
using System.Collections.Specialized;

namespace Devolutions.AvaloniaControls.Controls;

/// <summary>
/// A virtualized tile-based list box that supports grouping, selection, and keyboard navigation.
/// Items are displayed in a wrapping grid with uniform tile sizes.
/// </summary>
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

    public static readonly StyledProperty<Func<object, object>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<object, object>?>(
            nameof(GroupSelector));

    public static readonly StyledProperty<Func<object, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<object, int>?>(
            nameof(GroupOrderSelector));

    private double? cachedHeaderHeight = null;
    private int cachedItemsPerRow = -1;
    private INotifyCollectionChanged? collectionChangedSource;
    private bool indexMappingsDirty = true;
    private ItemsRepeater? itemsRepeater;
    private double lastKnownWidth = -1;
    private ScrollViewer? scrollViewer;
    private int selectedIndex = -1;
    private object? selectedItem;
    private Dictionary<object, int>? sourceToVisualMap;
    private bool useGrouping = false;
    private List<object>? visualToSourceMap;

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

        PaddingProperty.Changed.AddClassHandler<GroupedTileListBox>((x, e) => x.OnPaddingChanged(e));
    }

    public GroupedTileListBox()
    {
        this.Focusable = true;
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
    public Func<object, object>? GroupSelector
    {
        get => this.GetValue(GroupSelectorProperty);
        set => this.SetValue(GroupSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the function used to determine the sort order for each group.
    /// Lower numbers appear first. If null, groups are sorted alphabetically by key.
    /// Can be bound from a ViewModel for compile-time safety and MVVM support.
    /// </summary>
    public Func<object, int>? GroupOrderSelector
    {
        get => this.GetValue(GroupOrderSelectorProperty);
        set => this.SetValue(GroupOrderSelectorProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

        // Always create ItemsRepeater(s) dynamically in UpdateItemsRepeater
        this.UpdateItemsRepeater();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Re-subscribe to collection changes when control is re-attached
        // (in case ItemsSource was set while detached, or control was moved)
        if (this.ItemsSource is INotifyCollectionChanged notifyCollection && this.collectionChangedSource == null)
        {
            this.collectionChangedSource = notifyCollection;
            this.collectionChangedSource.CollectionChanged += this.OnCollectionChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Unsubscribe from collection changes to prevent memory leaks
        if (this.collectionChangedSource != null)
        {
            this.collectionChangedSource.CollectionChanged -= this.OnCollectionChanged;
            this.collectionChangedSource = null;
        }
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Unsubscribe from old collection
        if (this.collectionChangedSource != null)
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

        if (this.itemsRepeater != null)
        {
            this.UpdateItemsRepeater();
        }
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
        if (previouslySelectedItem != null && this.ItemsSource != null)
        {
            bool itemStillExists = this.ItemsSource.Cast<object>().Contains(previouslySelectedItem);
            if (itemStillExists)
            {
                this.SelectItem(previouslySelectedItem);
            }
            else
            {
                // Selected item was removed
                this.SelectedItem = null;
                this.SelectedIndex = -1;
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

        // Invalidate layout to recalculate with new dimensions
        this.itemsRepeater?.InvalidateArrange();
    }

    private void OnPaddingChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Update margin on the ScrollViewer content to match the new Padding value
        if (this.scrollViewer?.Content is Control content)
        {
            content.Margin = this.Padding;
        }
    }

    /// <summary>
    /// Orders groups according to GroupOrderSelector (if provided), otherwise alphabetically.
    /// </summary>
    private IEnumerable<IGrouping<object, object>> GetOrderedGroups(IEnumerable<IGrouping<object, object>> groups)
    {
        if (this.GroupOrderSelector != null)
        {
            // Primary sort by order value, secondary sort alphabetically
            return groups
                .OrderBy(g => this.GroupOrderSelector(g.Key ?? string.Empty))
                .ThenBy(g => g.Key?.ToString() ?? string.Empty);
        }

        return groups.OrderBy(g => g.Key?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Groups items with their original indices, ordered by group key.
    /// </summary>
    private static List<IGrouping<object, IndexedItem>> GetIndexedGroups(
        List<object> allItems,
        Func<object, object> groupSelector)
    {
        return allItems
            .Select((item, index) => new IndexedItem(item, index, groupSelector(item)))
            .GroupBy(x => x.Group)
            .OrderBy(g => g.Key?.ToString() ?? string.Empty)
            .ToList();
    }

    private void UpdateItemsRepeater()
    {
        if (this.scrollViewer == null)
        {
            return;
        }

        // Determine if grouping is enabled
        this.useGrouping = this.GroupSelector != null;

        // Unhook old ItemsRepeater if it exists
        if (this.itemsRepeater != null)
        {
            this.itemsRepeater.ElementPrepared -= this.OnElementPrepared;
            this.itemsRepeater = null;
        }

        if (this.useGrouping)
        {
            // Create StackPanel to hold groups
            StackPanel stackPanel = new()
            {
                Spacing = this.ItemSpacing,
                Margin = this.Padding
            };

            if (this.ItemsSource != null && this.GroupSelector != null && this.scrollViewer != null)
            {
                // Group the items and apply ordering
                IEnumerable<IGrouping<object, object>> groups = this.ItemsSource.Cast<object>().GroupBy(this.GroupSelector);
                IEnumerable<IGrouping<object, object>> orderedGroups = this.GetOrderedGroups(groups);

                foreach (IGrouping<object, object> group in orderedGroups)
                {
                    // Create a container for each group
                    StackPanel groupContainer = new()
                    {
                        Spacing = this.ItemSpacing,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    if (group.Key is string groupName && !string.IsNullOrEmpty(groupName))
                    {
                        // Add group header (not virtualized)
                        GroupedTileListBoxGroupHeader headerControl = new()
                        {
                            Text = groupName
                        };

                        if (this.cachedHeaderHeight is null)
                        {
                            EventHandler? layoutHandler = null;
                            layoutHandler = (sender, args) =>
                            {
                                if (sender is Control control && control.Bounds.Height > 0)
                                {
                                    this.cachedHeaderHeight = control.Bounds.Height;
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
                            HorizontalSpacing = this.ItemSpacing,
                            VerticalSpacing = this.ItemSpacing
                        }
                    };

                    // Create item template that wraps in container with fixed size
                    groupRepeater.ItemTemplate = new FuncDataTemplate<object>((item, scope) =>
                    {
                        Control? content = this.ItemTemplate?.Build(item);
                        if (content != null)
                        {
                            content.DataContext = item;
                            return new GroupedTileListBoxItem
                            {
                                Content = content,
                                Width = this.ItemWidth,
                                Height = this.ItemHeight
                            };
                        }

                        return new GroupedTileListBoxItem
                        {
                            Width = this.ItemWidth,
                            Height = this.ItemHeight
                        };
                    });

                    groupRepeater.ElementPrepared += this.OnElementPrepared;

                    groupContainer.Children.Add(groupRepeater);
                    stackPanel.Children.Add(groupContainer);
                }
            }

            if (this.scrollViewer != null)
            {
                this.scrollViewer.Content = stackPanel;
            }
        }
        else if (this.scrollViewer != null)
        {
            // No grouping - create single ItemsRepeater dynamically

            // Create new ItemsRepeater for non-grouped mode
            this.itemsRepeater = new ItemsRepeater
            {
                ItemsSource = this.ItemsSource,
                Margin = this.Padding,
                Layout = new WrapLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalSpacing = this.ItemSpacing,
                    VerticalSpacing = this.ItemSpacing
                }
            };

            // Wrap ItemTemplate in container with fixed size (matches grouped mode pattern)
            this.itemsRepeater.ItemTemplate = new FuncDataTemplate<object>((item, scope) =>
            {
                Control? content = this.ItemTemplate?.Build(item);
                if (content != null)
                {
                    content.DataContext = item;
                    return new GroupedTileListBoxItem
                    {
                        Content = content,
                        Width = this.ItemWidth,
                        Height = this.ItemHeight
                    };
                }

                return new GroupedTileListBoxItem
                {
                    Width = this.ItemWidth,
                    Height = this.ItemHeight
                };
            });

            this.itemsRepeater.ElementPrepared += this.OnElementPrepared;

            this.scrollViewer.Content = this.itemsRepeater;
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

        // Update the container's content DataContext for recycled elements
        if (container.Content is Control contentControl && item != null)
        {
            contentControl.DataContext = item;
        }

        // Update selection state
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

        // Get the actual data item from the content's DataContext
        object? item = (container.Content as Control)?.DataContext;
        if (item != null)
        {
            this.SelectItem(item);
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
        if (this.scrollViewer?.Content == null)
        {
            return;
        }

        if (this.useGrouping && this.scrollViewer.Content is StackPanel stackPanel)
        {
            // Update selection in all group ItemsRepeaters
            foreach (ItemsRepeater repeater in GetAllItemsRepeaters(stackPanel))
            {
                this.UpdateRepeaterSelection(repeater);
            }
        }
        else if (this.itemsRepeater != null)
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
            .SelectMany(groupStack => groupStack.Children.OfType<ItemsRepeater>());
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
            Control? element = repeater.TryGetElement(i);
            if (element == null)
            {
                continue; // Item not realized (not in viewport), skip
            }

            if (element is GroupedTileListBoxItem container)
            {
                // Get the actual data item from the content's DataContext
                object? item = (container.Content as Control)?.DataContext;
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

        if (e.Handled || this.ItemsSource == null)
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
        if (e.Key == Key.Left || e.Key == Key.Right)
        {
            object? currentItem = this.selectedItem;
            int currentVisualIndex = this.GetVisualIndexFromItem(currentItem);

            // If mapping failed, fall back to source index as visual index
            if (currentVisualIndex < 0)
            {
                currentVisualIndex = currentIndex;
            }

            int newVisualIndex;
            if (e.Key == Key.Left)
            {
                // Navigate left in visual order (with wrapping)
                newVisualIndex = currentVisualIndex == 0
                    ? itemCount - 1
                    : currentVisualIndex - 1;
            }
            else // Key.Right
            {
                // Navigate right in visual order (with wrapping)
                newVisualIndex = currentVisualIndex == itemCount - 1
                    ? 0
                    : currentVisualIndex + 1;
            }

            // Convert visual index back to source item
            object? newItem = this.GetItemFromVisualIndex(newVisualIndex);
            if (newItem != null)
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
                    break;

                case Key.Down:
                    // Don't change selection if already at the bottom
                    if (this.IsInLastRow(currentIndex, itemCount))
                    {
                        return;
                    }

                    newIndex = this.CalculateDownIndex(currentIndex, itemCount);
                    break;

                case Key.Home:
                    // Select first item in visual order
                {
                    object? firstItem = this.GetItemFromVisualIndex(0);
                    if (firstItem != null)
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
                    if (lastItem != null)
                    {
                        this.SelectItem(lastItem);
                        newIndex = this.selectedIndex;
                    }
                }
                    break;

                default:
                    return;
            }

            if (newIndex != currentIndex)
            {
                this.SelectItemAtIndex(newIndex);
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

        if (this.scrollViewer == null)
        {
            return 1;
        }

        double availableWidth = this.scrollViewer.Bounds.Width - this.Padding.Left - this.Padding.Right;

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
        if (currentIndex < 0)
        {
            return false;
        }

        int itemsPerRow = this.CalculateItemsPerRow();

        if (!this.useGrouping || this.ItemsSource == null)
        {
            // Simple case: first row is indices 0 to itemsPerRow-1
            return currentIndex < itemsPerRow;
        }

        // Grouped case: check if in first row of first group
        if (this.GroupSelector == null)
        {
            return currentIndex < itemsPerRow;
        }

        List<object> allItems = this.ItemsSource.Cast<object>().ToList();
        if (currentIndex >= allItems.Count)
        {
            return false;
        }

        List<IGrouping<object, IndexedItem>> groups = GetIndexedGroups(allItems, this.GroupSelector);

        // Find the first group
        IGrouping<object, IndexedItem>? firstGroup = groups.FirstOrDefault();
        if (firstGroup == null)
        {
            return false;
        }

        List<IndexedItem> firstGroupItems = firstGroup.ToList();
        int indexInGroup = firstGroupItems.FindIndex(x => x.Index == currentIndex);

        if (indexInGroup < 0)
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
        if (currentIndex < 0 || currentIndex >= itemCount)
        {
            return false;
        }

        int itemsPerRow = this.CalculateItemsPerRow();

        if (!this.useGrouping || this.ItemsSource == null || this.GroupSelector == null)
        {
            int lastRowStart = CalculateLastRowStart(itemCount, itemsPerRow);
            return currentIndex >= lastRowStart;
        }

        // Grouped case: check if in last row of last group
        List<object> allItems = this.ItemsSource.Cast<object>().ToList();
        if (currentIndex >= allItems.Count)
        {
            return false;
        }

        List<IGrouping<object, IndexedItem>> groups = GetIndexedGroups(allItems, this.GroupSelector);

        // Find the last group
        IGrouping<object, IndexedItem>? lastGroup = groups.LastOrDefault();
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
        int itemsPerRow = this.CalculateItemsPerRow();
        int offset = moveUp ? -itemsPerRow : itemsPerRow;
        int fallback = moveUp
            ? Math.Max(0, currentIndex + offset)
            : Math.Min(itemCount - 1, currentIndex + offset);

        // If no grouping, use simple calculation
        if (!this.useGrouping || this.ItemsSource == null)
        {
            return fallback;
        }

        // Get all items and group them
        List<object> allItems = this.ItemsSource.Cast<object>().ToList();

        if (this.GroupSelector == null || currentIndex < 0 || currentIndex >= allItems.Count)
        {
            return fallback;
        }

        List<IGrouping<object, IndexedItem>> groups = GetIndexedGroups(allItems, this.GroupSelector);

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
                            : groupItems[groupItems.Count - 1].Index;
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
        if (itemsSource == null)
        {
            return 0;
        }

        if (itemsSource is IList list)
        {
            return list.Count;
        }

        return itemsSource.Cast<object?>().Count();
    }

    private void InvalidateIndexMappings()
    {
        this.indexMappingsDirty = true;
        this.visualToSourceMap = null;
        this.sourceToVisualMap = null;
    }

    private void BuildIndexMappings()
    {
        if (!this.indexMappingsDirty || this.ItemsSource == null)
        {
            return;
        }

        this.visualToSourceMap = new List<object>();
        this.sourceToVisualMap = new Dictionary<object, int>();

        if (this.useGrouping && this.GroupSelector != null)
        {
            // Group the items and apply ordering
            IEnumerable<IGrouping<object, object>> groups = this.ItemsSource.Cast<object>().GroupBy(this.GroupSelector);
            IEnumerable<IGrouping<object, object>> orderedGroups = this.GetOrderedGroups(groups);

            // Add all items from ordered groups to List (this is the visual order)
            foreach (IGrouping<object, object> group in orderedGroups)
            {
                foreach (var item in group)
                {
                    if (item != null)
                    {
                        this.visualToSourceMap.Add(item);
                    }
                }
            }
        }
        else
        {
            // No grouping - add items in source order
            foreach (object? item in this.ItemsSource)
            {
                if (item != null)
                {
                    this.visualToSourceMap.Add(item);
                }
            }
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
        if (item == null)
        {
            return -1;
        }

        this.BuildIndexMappings();

        return this.sourceToVisualMap != null && this.sourceToVisualMap.TryGetValue(item, out int index)
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
        if (this.scrollViewer == null || this.ItemsSource == null)
        {
            return;
        }

        // Get the item at this source index
        List<object> allItems = this.ItemsSource.Cast<object>().ToList();
        if (sourceIndex < 0 || sourceIndex >= allItems.Count)
        {
            return;
        }

        object item = allItems[sourceIndex];

        // Find the visual element for this item
        Control? targetElement = null;

        if (this.useGrouping)
        {
            // In grouped mode, find the element in one of the group ItemsRepeaters
            targetElement = this.FindElementInGroupedLayout(sourceIndex);
        }
        else if (this.itemsRepeater != null)
        {
            // In non-grouped mode, get element from main ItemsRepeater
            targetElement = this.itemsRepeater.TryGetElement(sourceIndex) as Control;
        }

        if (targetElement != null)
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
        if (this.scrollViewer == null)
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

        if (!this.useGrouping)
        {
            // Simple calculation for non-grouped mode
            int row = visualIndex / itemsPerRow;
            return row * (this.ItemHeight + this.ItemSpacing);
        }

        // Grouped mode: account for group headers
        if (this.GroupSelector == null || this.ItemsSource == null)
        {
            return 0;
        }

        IEnumerable<IGrouping<object, object>> groups = this.ItemsSource.Cast<object>().GroupBy(this.GroupSelector);
        IEnumerable<IGrouping<object, object>> orderedGroups = this.GetOrderedGroups(groups);

        double offset = 0;
        int itemsSoFar = 0;

        foreach (IGrouping<object, object> group in orderedGroups)
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
        if (this.scrollViewer?.Content is not StackPanel stackPanel)
        {
            return null;
        }

        List<object>? allItems = this.ItemsSource?.Cast<object>().ToList();
        if (allItems == null || index < 0 || index >= allItems.Count)
        {
            return null;
        }

        object targetItem = allItems[index];

        // Search through all group ItemsRepeaters
        foreach (ItemsRepeater repeater in GetAllItemsRepeaters(stackPanel))
        {
            int count = GetItemCount(repeater.ItemsSource);
            if (count == 0)
            {
                continue;
            }

            // Search through all ItemsSource indices
            for (int i = 0; i < count; i++)
            {
                Control? element = repeater.TryGetElement(i);
                if (element == null)
                {
                    continue; // Item not realized (not in viewport), skip
                }

                if (element is GroupedTileListBoxItem container)
                {
                    object? itemDataContext = (container.Content as Control)?.DataContext;
                    if (ReferenceEquals(itemDataContext, targetItem))
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
        if (this.scrollViewer == null)
        {
            return null;
        }

        Point? elementPosition = element.TranslatePoint(new Point(0, 0), this.scrollViewer);
        if (elementPosition == null)
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
        if (this.scrollViewer == null)
        {
            return false;
        }

        Rect? elementRect = this.GetElementRectInScrollViewer(element);
        if (elementRect == null)
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
        if (this.scrollViewer == null)
        {
            return;
        }

        Rect? elementRect = this.GetElementRectInScrollViewer(element);
        if (elementRect == null)
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
        if (item == null)
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
        if (item == null)
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
        if (this.selectedItem == null)
        {
            return;
        }

        this.selectedItem = null;
        this.selectedIndex = -1;

        this.OnSelectionChanged(this.selectedItem, this.selectedIndex);
    }

    private bool IsItemSelected(object? item) =>
        item != null && ReferenceEquals(item, this.selectedItem);

    private bool IsIndexSelected(int index) =>
        index >= 0 && index == this.selectedIndex;

    private object? GetItemAtIndex(int index)
    {
        if (this.ItemsSource == null || index < 0)
        {
            return null;
        }

        if (this.ItemsSource is IList list)
        {
            if (index < list.Count)
            {
                return list[index];
            }
        }
        else
        {
            return this.ItemsSource.Cast<object?>().ElementAtOrDefault(index);
        }

        return null;
    }

    private int GetIndexOfItem(object? item)
    {
        if (this.ItemsSource == null || item == null)
        {
            return -1;
        }

        if (this.ItemsSource is IList list)
        {
            return list.IndexOf(item);
        }

        return this.ItemsSource.Cast<object?>()
            .Select((currentItem, index) => new { Item = currentItem, Index = index })
            .FirstOrDefault(x => ReferenceEquals(x.Item, item))
            ?.Index ?? -1;
    }

    /// <summary>
    /// Represents an item with its original index and group for navigation calculations.
    /// </summary>
    private sealed record IndexedItem(object Item, int Index, object Group);
}