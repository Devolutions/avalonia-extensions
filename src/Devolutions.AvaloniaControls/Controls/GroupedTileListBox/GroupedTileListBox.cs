using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Devolutions.AvaloniaControls.Controls;

/// <summary>
/// A virtualized tile-based list box that supports grouping, selection, and keyboard navigation.
/// Items are displayed in a wrapping grid with uniform tile sizes.
/// </summary>
public class GroupedTileListBox : TemplatedControl
{
    private ItemsRepeater? _itemsRepeater;
    private ScrollViewer? _scrollViewer;
    private object? _selectedItem;
    private int _selectedIndex = -1;
    private int _cachedItemsPerRow = -1;
    private double _lastKnownWidth = -1;
    private bool _useGrouping = false;
    private List<object>? _visualToSourceMap;
    private Dictionary<object, int>? _sourceToVisualMap;
    private bool _indexMappingsDirty = true;
    private INotifyCollectionChanged? _collectionChangedSource;
    private double? _cachedHeaderHeight = null;

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="ItemsSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<GroupedTileListBox, IEnumerable?>(
            nameof(ItemsSource));

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<GroupedTileListBox, IDataTemplate?>(
            nameof(ItemTemplate));

    /// <summary>
    /// Defines the <see cref="ItemWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<GroupedTileListBox, double>(
            nameof(ItemWidth), 150.0);

    /// <summary>
    /// Defines the <see cref="ItemHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<GroupedTileListBox, double>(
            nameof(ItemHeight), 150.0);

    /// <summary>
    /// Defines the <see cref="ItemSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<GroupedTileListBox, double>(
            nameof(ItemSpacing), 8.0);

    /// <summary>
    /// Defines the <see cref="SelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<GroupedTileListBox, object?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="SelectedIndex"/> property.
    /// </summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GroupedTileListBox, int>(
            nameof(SelectedIndex),
            -1,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="GroupSelector"/> property.
    /// </summary>
    public static readonly StyledProperty<Func<object, object>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<object, object>?>(
            nameof(GroupSelector));

    /// <summary>
    /// Defines the <see cref="GroupOrderSelector"/> property.
    /// </summary>
    public static readonly StyledProperty<Func<object, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedTileListBox, Func<object, int>?>(
            nameof(GroupOrderSelector));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the collection of items to display.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to display each item.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of each item.
    /// </summary>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of each item.
    /// </summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between items.
    /// </summary>
    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of the currently selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the function used to determine the group for each item.
    /// Can be bound from a ViewModel for compile-time safety and MVVM support.
    /// </summary>
    public Func<object, object>? GroupSelector
    {
        get => GetValue(GroupSelectorProperty);
        set => SetValue(GroupSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the function used to determine the sort order for each group.
    /// Lower numbers appear first. If null, groups are sorted alphabetically by key.
    /// Can be bound from a ViewModel for compile-time safety and MVVM support.
    /// </summary>
    public Func<object, int>? GroupOrderSelector
    {
        get => GetValue(GroupOrderSelectorProperty);
        set => SetValue(GroupOrderSelectorProperty, value);
    }

    #endregion

    static GroupedTileListBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnItemsSourceChanged(e));

        ItemWidthProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnLayoutPropertyChanged(e));

        ItemHeightProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnLayoutPropertyChanged(e));

        ItemSpacingProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnLayoutPropertyChanged(e));

        SelectedItemProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnSelectedItemChanged(e));

        SelectedIndexProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnSelectedIndexChanged(e));

        GroupSelectorProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnGroupingPropertyChanged(e));

        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnGroupingPropertyChanged(e));

        PaddingProperty.Changed.AddClassHandler<GroupedTileListBox>(
            (x, e) => x.OnPaddingChanged(e));
    }

    public GroupedTileListBox()
    {
        Focusable = true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

        // Always create ItemsRepeater(s) dynamically in UpdateItemsRepeater
        UpdateItemsRepeater();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Re-subscribe to collection changes when control is re-attached
        // (in case ItemsSource was set while detached, or control was moved)
        if (ItemsSource is INotifyCollectionChanged notifyCollection && _collectionChangedSource == null)
        {
            _collectionChangedSource = notifyCollection;
            _collectionChangedSource.CollectionChanged += OnCollectionChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Unsubscribe from collection changes to prevent memory leaks
        if (_collectionChangedSource != null)
        {
            _collectionChangedSource.CollectionChanged -= OnCollectionChanged;
            _collectionChangedSource = null;
        }
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Unsubscribe from old collection
        if (_collectionChangedSource != null)
        {
            _collectionChangedSource.CollectionChanged -= OnCollectionChanged;
            _collectionChangedSource = null;
        }

        InvalidateIndexMappings();

        // Subscribe to new collection if it supports change notifications
        if (ItemsSource is INotifyCollectionChanged notifyCollection)
        {
            _collectionChangedSource = notifyCollection;
            _collectionChangedSource.CollectionChanged += OnCollectionChanged;
        }

        if (_itemsRepeater != null)
        {
            UpdateItemsRepeater();
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
        InvalidateIndexMappings();

        // Preserve selection by item reference if possible
        object? previouslySelectedItem = _selectedItem;

        // Rebuild UI (grouped mode needs full rebuild, non-grouped mode ItemsRepeater handles it automatically)
        if (_useGrouping)
        {
            UpdateItemsRepeater();
        }

        // Restore selection if item still exists in the collection
        if (previouslySelectedItem != null && ItemsSource != null)
        {
            bool itemStillExists = ItemsSource.Cast<object>().Contains(previouslySelectedItem);
            if (itemStillExists)
            {
                SelectItem(previouslySelectedItem);
            }
            else
            {
                // Selected item was removed
                SelectedItem = null;
                SelectedIndex = -1;
            }
        }
    }

    private void OnLayoutPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_itemsRepeater?.Layout is WrapLayout layout)
        {
            layout.HorizontalSpacing = ItemSpacing;
            layout.VerticalSpacing = ItemSpacing;
        }

        // Invalidate layout to recalculate with new dimensions
        _itemsRepeater?.InvalidateArrange();
    }

    private void OnPaddingChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Update margin on the ScrollViewer content to match the new Padding value
        if (_scrollViewer?.Content is Control content)
        {
            content.Margin = Padding;
        }
    }

    /// <summary>
    /// Orders groups according to GroupOrderSelector (if provided), otherwise alphabetically.
    /// </summary>
    private IEnumerable<IGrouping<object, object>> GetOrderedGroups(IEnumerable<IGrouping<object, object>> groups)
    {
        if (GroupOrderSelector != null)
        {
            // Primary sort by order value, secondary sort alphabetically
            return groups
                .OrderBy(g => GroupOrderSelector(g.Key ?? string.Empty))
                .ThenBy(g => g.Key?.ToString() ?? string.Empty);
        }

        return groups.OrderBy(g => g.Key?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Represents an item with its original index and group for navigation calculations.
    /// </summary>
    private sealed record IndexedItem(object Item, int Index, object Group);

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
        if (_scrollViewer == null)
            return;

        // Determine if grouping is enabled
        _useGrouping = GroupSelector != null;

        // Unhook old ItemsRepeater if it exists
        if (_itemsRepeater != null)
        {
            _itemsRepeater.ElementPrepared -= OnElementPrepared;
            _itemsRepeater = null;
        }

        if (_useGrouping)
        {
            // Create StackPanel to hold groups
            var stackPanel = new StackPanel
            {
                Spacing = ItemSpacing,
                Margin = Padding
            };

            if (ItemsSource != null && GroupSelector != null && _scrollViewer != null)
            {
                // Group the items and apply ordering
                var groups = ItemsSource.Cast<object>().GroupBy(GroupSelector);
                var orderedGroups = GetOrderedGroups(groups);

                foreach (var group in orderedGroups)
                {
                    // Create a container for each group
                    var groupContainer = new StackPanel
                    {
                        Spacing = ItemSpacing,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                    };

                    if (group.Key is string groupName && !string.IsNullOrEmpty(groupName))
                    {
                        // Add group header (not virtualized)
                        var headerControl = new GroupedTileListBoxGroupHeader
                        {
                            Text = groupName
                        };

                        if (this._cachedHeaderHeight is null)
                        {
                            EventHandler? layoutHandler = null;
                            layoutHandler = (sender, args) =>
                                {
                                    if (sender is Control control && control.Bounds.Height > 0)
                                    {
                                        _cachedHeaderHeight = control.Bounds.Height;
                                        // Unsubscribe to avoid memory leak
                                        control.LayoutUpdated -= layoutHandler;
                                    }
                                };
                            headerControl.LayoutUpdated += layoutHandler;
                        }

                        groupContainer.Children.Add(headerControl);
                    }

                    // Add ItemsRepeater for group items (virtualized)
                    var groupRepeater = new ItemsRepeater
                    {
                        ItemsSource = group.ToList(),
                        Margin = new Thickness(0, 0, 0, ItemSpacing),
                        Layout = new WrapLayout
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalSpacing = ItemSpacing,
                            VerticalSpacing = ItemSpacing
                        }
                    };

                    // Create item template that wraps in container with fixed size
                    groupRepeater.ItemTemplate = new FuncDataTemplate<object>((item, scope) =>
                    {
                        var content = ItemTemplate?.Build(item);
                        if (content != null)
                        {
                            content.DataContext = item;
                            return new GroupedTileListBoxItem
                            {
                                Content = content,
                                Width = ItemWidth,
                                Height = ItemHeight
                            };
                        }
                        return new GroupedTileListBoxItem
                        {
                            Width = ItemWidth,
                            Height = ItemHeight
                        };
                    });

                    groupRepeater.ElementPrepared += OnElementPrepared;

                    groupContainer.Children.Add(groupRepeater);
                    stackPanel.Children.Add(groupContainer);
                }
            }

            if (_scrollViewer != null)
            {
                _scrollViewer.Content = stackPanel;
            }
        }
        else if (_scrollViewer != null)
        {
            // No grouping - create single ItemsRepeater dynamically

            // Create new ItemsRepeater for non-grouped mode
            _itemsRepeater = new ItemsRepeater
            {
                ItemsSource = ItemsSource,
                Margin = Padding,
                Layout = new WrapLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalSpacing = ItemSpacing,
                    VerticalSpacing = ItemSpacing
                }
            };

            // Wrap ItemTemplate in container with fixed size (matches grouped mode pattern)
            _itemsRepeater.ItemTemplate = new FuncDataTemplate<object>((item, scope) =>
            {
                var content = ItemTemplate?.Build(item);
                if (content != null)
                {
                    content.DataContext = item;
                    return new GroupedTileListBoxItem
                    {
                        Content = content,
                        Width = ItemWidth,
                        Height = ItemHeight
                    };
                }
                return new GroupedTileListBoxItem
                {
                    Width = ItemWidth,
                    Height = ItemHeight
                };
            });

            _itemsRepeater.ElementPrepared += OnElementPrepared;

            _scrollViewer.Content = _itemsRepeater;
        }
    }

    private void OnGroupingPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        InvalidateIndexMappings();
        UpdateItemsRepeater();
    }

    private void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is not GroupedTileListBoxItem container || sender is not ItemsRepeater repeater)
            return;

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
        container.IsSelected = IsItemSelected(item);
        container.ItemIndex = e.Index;

        // Wire up pointer pressed event
        container.PointerPressed -= OnContainerPointerPressed;
        container.PointerPressed += OnContainerPointerPressed;
    }

    private void OnContainerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not GroupedTileListBoxItem container)
            return;

        // Get the actual data item from the content's DataContext
        var item = (container.Content as Control)?.DataContext;
        if (item != null)
        {
            SelectItem(item);
        }
    }

    private void OnSelectionChanged(object? newItem, int newIndex)
    {
        // Update Avalonia property bindings
        this.SelectedItem = newItem;
        this.SelectedIndex = newIndex;

        // Update visual state of realized containers
        UpdateRealizedContainerSelection();
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // External change to SelectedItem property
        if (!IsItemSelected(e.NewValue))
        {
            SelectItem(e.NewValue);
        }
    }

    private void OnSelectedIndexChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // External change to SelectedIndex property
        var newIndex = (int)e.NewValue!;
        if (!IsIndexSelected(newIndex))
        {
            SelectItemAtIndex(newIndex);
        }
    }

    private void UpdateRealizedContainerSelection()
    {
        if (_scrollViewer?.Content == null)
            return;

        if (_useGrouping && _scrollViewer.Content is StackPanel stackPanel)
        {
            // Update selection in all group ItemsRepeaters
            foreach (var repeater in GetAllItemsRepeaters(stackPanel))
            {
                UpdateRepeaterSelection(repeater);
            }
        }
        else if (_itemsRepeater != null)
        {
            // Update selection in single ItemsRepeater (non-grouped)
            UpdateRepeaterSelection(_itemsRepeater);
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
            return;

        int count = itemsSource is IList list
            ? list.Count
            : itemsSource.Cast<object>().Count();

        // Iterate through all ItemsSource indices
        // TryGetElement returns null for non-realized (virtualized) items
        for (int i = 0; i < count; i++)
        {
            var element = repeater.TryGetElement(i);
            if (element == null)
                continue;  // Item not realized (not in viewport), skip

            if (element is GroupedTileListBoxItem container)
            {
                // Get the actual data item from the content's DataContext
                var item = (container.Content as Control)?.DataContext;
                container.IsSelected = IsItemSelected(item);
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // Invalidate cached items per row when width changes
        if (Math.Abs(e.NewSize.Width - _lastKnownWidth) > 0.1)
        {
            _cachedItemsPerRow = -1;
            _lastKnownWidth = e.NewSize.Width;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || ItemsSource == null)
            return;

        var currentIndex = _selectedIndex;
        var itemCount = GetItemCount();

        if (itemCount == 0)
            return;

        int newIndex = currentIndex;

        // For Left/Right, use visual order navigation (respects grouping)
        if (e.Key == Key.Left || e.Key == Key.Right)
        {
            var currentItem = _selectedItem;
            int currentVisualIndex = GetVisualIndexFromItem(currentItem);

            // If mapping failed, fall back to source index as visual index
            if (currentVisualIndex < 0)
                currentVisualIndex = currentIndex;

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
            var newItem = GetItemFromVisualIndex(newVisualIndex);
            if (newItem != null)
            {
                SelectItem(newItem);
                newIndex = _selectedIndex;
            }
        }
        else
        {
            // For Up/Down/Home/End, continue using source order
            switch (e.Key)
            {

                case Key.Up:
                    // Don't change selection if already at the top
                    if (IsInFirstRow(currentIndex))
                        return;
                    newIndex = CalculateUpIndex(currentIndex);
                    break;

                case Key.Down:
                    // Don't change selection if already at the bottom
                    if (IsInLastRow(currentIndex, itemCount))
                        return;
                    newIndex = CalculateDownIndex(currentIndex, itemCount);
                    break;

                case Key.Home:
                    // Select first item in visual order
                    {
                        var firstItem = GetItemFromVisualIndex(0);
                        if (firstItem != null)
                        {
                            SelectItem(firstItem);
                            newIndex = _selectedIndex;
                        }
                    }
                    break;

                case Key.End:
                    // Select last item in visual order
                    {
                        var lastItem = GetItemFromVisualIndex(itemCount - 1);
                        if (lastItem != null)
                        {
                            SelectItem(lastItem);
                            newIndex = _selectedIndex;
                        }
                    }
                    break;

                default:
                    return;
            }

            if (newIndex != currentIndex)
            {
                SelectItemAtIndex(newIndex);
            }
        }

        // Scroll to show the newly selected item
        if (newIndex != currentIndex)
        {
            ScrollIntoView(newIndex);
            e.Handled = true;
        }
    }

    private int CalculateItemsPerRow()
    {
        if (_cachedItemsPerRow > 0)
            return _cachedItemsPerRow;

        if (_scrollViewer == null)
            return 1;

        var availableWidth = _scrollViewer.Bounds.Width - _scrollViewer.Padding.Left - _scrollViewer.Padding.Right;

        if (availableWidth <= 0)
            return 1;

        // Calculate items per row: floor((width) / (itemWidth + spacing))
        var itemsPerRow = (int)Math.Floor(availableWidth / (ItemWidth + ItemSpacing));
        _cachedItemsPerRow = Math.Max(1, itemsPerRow);

        return _cachedItemsPerRow;
    }

    /// <summary>
    /// Calculates the row number (0-indexed) of the last row for a given item count and items per row.
    /// Uses integer division: (itemCount - 1) / itemsPerRow.
    /// The -1 ensures correct rounding for exact multiples (e.g., 20 items, 5 per row → row 3, not row 4).
    /// </summary>
    private static int CalculateLastRowNumber(int itemCount, int itemsPerRow)
    {
        return (itemCount - 1) / itemsPerRow;
    }

    /// <summary>
    /// Calculates the starting index of the last row for a given item count and items per row.
    /// </summary>
    private static int CalculateLastRowStart(int itemCount, int itemsPerRow)
    {
        return CalculateLastRowNumber(itemCount, itemsPerRow) * itemsPerRow;
    }

    /// <summary>
    /// Checks if the current index is in the first row.
    /// </summary>
    private bool IsInFirstRow(int currentIndex)
    {
        if (currentIndex < 0)
            return false;

        var itemsPerRow = CalculateItemsPerRow();

        if (!_useGrouping || ItemsSource == null)
        {
            // Simple case: first row is indices 0 to itemsPerRow-1
            return currentIndex < itemsPerRow;
        }

        // Grouped case: check if in first row of first group
        if (GroupSelector == null)
            return currentIndex < itemsPerRow;

        var allItems = ItemsSource.Cast<object>().ToList();
        if (currentIndex >= allItems.Count)
            return false;

        var groups = GetIndexedGroups(allItems, GroupSelector);

        // Find the first group
        var firstGroup = groups.FirstOrDefault();
        if (firstGroup == null)
            return false;

        var firstGroupItems = firstGroup.ToList();
        var indexInGroup = firstGroupItems.FindIndex(x => x.Index == currentIndex);

        if (indexInGroup < 0)
            return false; // Not in first group

        // Check if in first row of first group
        return indexInGroup < itemsPerRow;
    }

    /// <summary>
    /// Checks if the current index is in the last row.
    /// </summary>
    private bool IsInLastRow(int currentIndex, int itemCount)
    {
        if (currentIndex < 0 || currentIndex >= itemCount)
            return false;

        var itemsPerRow = CalculateItemsPerRow();

        if (!_useGrouping || ItemsSource == null || GroupSelector == null)
        {
            int lastRowStart = CalculateLastRowStart(itemCount, itemsPerRow);
            return currentIndex >= lastRowStart;
        }

        // Grouped case: check if in last row of last group
        var allItems = ItemsSource.Cast<object>().ToList();
        if (currentIndex >= allItems.Count)
            return false;

        var groups = GetIndexedGroups(allItems, this.GroupSelector);

        // Find the last group
        var lastGroup = groups.LastOrDefault();
        if (lastGroup == null)
            return false;

        var lastGroupItems = lastGroup.ToList();
        var indexInGroup = lastGroupItems.FindIndex(x => x.Index == currentIndex);

        if (indexInGroup < 0)
            return false; // Not in last group

        // Check if in last row of last group
        int lastGroupRowStart = CalculateLastRowStart(lastGroupItems.Count, itemsPerRow);
        return indexInGroup >= lastGroupRowStart;
    }

    /// <summary>
    /// Calculates the index when navigating vertically, accounting for group boundaries.
    /// </summary>
    private int CalculateVerticalNavigationIndex(int currentIndex, int itemCount, bool moveUp)
    {
        var itemsPerRow = CalculateItemsPerRow();
        var offset = moveUp ? -itemsPerRow : itemsPerRow;
        var fallback = moveUp
            ? Math.Max(0, currentIndex + offset)
            : Math.Min(itemCount - 1, currentIndex + offset);

        // If no grouping, use simple calculation
        if (!_useGrouping || ItemsSource == null)
        {
            return fallback;
        }

        // Get all items and group them
        var allItems = ItemsSource.Cast<object>().ToList();

        if (GroupSelector == null || currentIndex < 0 || currentIndex >= allItems.Count)
        {
            return fallback;
        }

        var groups = GetIndexedGroups(allItems, this.GroupSelector);

        // Find current item's group and position within group
        foreach (var group in groups)
        {
            var groupItems = group.ToList();
            var indexInGroup = groupItems.FindIndex(x => x.Index == currentIndex);

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
                        var groupIndex = groups.IndexOf(group);
                        if (groupIndex > 0)
                        {
                            var prevGroup = groups[groupIndex - 1].ToList();
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
                        var groupIndex = groups.IndexOf(group);
                        if (groupIndex < groups.Count - 1)
                        {
                            var nextGroup = groups[groupIndex + 1].ToList();
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
    private int CalculateUpIndex(int currentIndex)
    {
        return CalculateVerticalNavigationIndex(currentIndex, 0, moveUp: true);
    }

    /// <summary>
    /// Calculates the index when navigating down, accounting for group boundaries.
    /// </summary>
    private int CalculateDownIndex(int currentIndex, int itemCount)
    {
        return CalculateVerticalNavigationIndex(currentIndex, itemCount, moveUp: false);
    }

    private int GetItemCount()
    {
        return GetItemCount(ItemsSource);
    }

    private static int GetItemCount(IEnumerable? itemsSource)
    {
        if (itemsSource == null)
            return 0;

        if (itemsSource is IList list)
            return list.Count;

        return itemsSource.Cast<object?>().Count();
    }

    private void InvalidateIndexMappings()
    {
        _indexMappingsDirty = true;
        _visualToSourceMap = null;
        _sourceToVisualMap = null;
    }

    private void BuildIndexMappings()
    {
        if (!_indexMappingsDirty || ItemsSource == null)
            return;

        _visualToSourceMap = new List<object>();
        _sourceToVisualMap = new Dictionary<object, int>();

        if (_useGrouping && GroupSelector != null)
        {
            // Group the items and apply ordering
            var groups = ItemsSource.Cast<object>().GroupBy(GroupSelector);
            var orderedGroups = GetOrderedGroups(groups);

            // Add all items from ordered groups to List (this is the visual order)
            foreach (var group in orderedGroups)
            {
                foreach (var item in group)
                {
                    if (item != null)
                    {
                        _visualToSourceMap.Add(item);
                    }
                }
            }
        }
        else
        {
            // No grouping - add items in source order
            foreach (var item in ItemsSource)
            {
                if (item != null)
                {
                    _visualToSourceMap.Add(item);
                }
            }
        }

        // Build Dictionary from List (item → visual index)
        for (int i = 0; i < _visualToSourceMap.Count; i++)
        {
            object item = _visualToSourceMap[i];
            this._sourceToVisualMap.TryAdd(item, i);
        }

        _indexMappingsDirty = false;
    }

    private int GetVisualIndexFromItem(object? item)
    {
        if (item == null)
            return -1;

        BuildIndexMappings();

        return _sourceToVisualMap != null && _sourceToVisualMap.TryGetValue(item, out int index)
            ? index
            : -1;
    }

    private object? GetItemFromVisualIndex(int visualIndex)
    {
        BuildIndexMappings();

        return visualIndex >= 0 && visualIndex < (_visualToSourceMap?.Count ?? 0)
            ? _visualToSourceMap![visualIndex]
            : null;
    }

    private void ScrollIntoView(int sourceIndex)
    {
        if (_scrollViewer == null || ItemsSource == null)
            return;

        // Get the item at this source index
        var allItems = ItemsSource.Cast<object>().ToList();
        if (sourceIndex < 0 || sourceIndex >= allItems.Count)
            return;

        var item = allItems[sourceIndex];

        // Find the visual element for this item
        Control? targetElement = null;

        if (_useGrouping)
        {
            // In grouped mode, find the element in one of the group ItemsRepeaters
            targetElement = FindElementInGroupedLayout(sourceIndex);
        }
        else if (_itemsRepeater != null)
        {
            // In non-grouped mode, get element from main ItemsRepeater
            targetElement = _itemsRepeater.TryGetElement(sourceIndex) as Control;
        }

        if (targetElement != null)
        {
            // Check if element is already fully visible
            if (IsElementFullyVisible(targetElement))
            {
                return; // Already visible, no need to scroll
            }

            // Scroll to make the element visible with minimal movement
            ScrollToElement(targetElement);
        }
        else
        {
            // Element is not realized, calculate estimated scroll offset
            double targetY = CalculateScrollOffsetForItem(item);
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetY);
        }
    }

    private double CalculateScrollOffsetForItem(object item)
    {
        if (_scrollViewer == null)
            return 0;

        int visualIndex = GetVisualIndexFromItem(item);
        if (visualIndex < 0)
            return 0;

        var itemsPerRow = CalculateItemsPerRow();
        if (itemsPerRow <= 0)
            return 0;

        if (!_useGrouping)
        {
            // Simple calculation for non-grouped mode
            var row = visualIndex / itemsPerRow;
            return row * (ItemHeight + ItemSpacing);
        }

        // Grouped mode: account for group headers
        if (GroupSelector == null || ItemsSource == null)
            return 0;

        var groups = ItemsSource.Cast<object>().GroupBy(GroupSelector);
        var orderedGroups = GetOrderedGroups(groups);

        double offset = 0;
        int itemsSoFar = 0;

        foreach (var group in orderedGroups)
        {
            int groupItemCount = group.Count();
            bool hasHeader = group.Key is string groupName && !string.IsNullOrEmpty(groupName);

            // Add header height for this group (only if it has a visible header)
            if (hasHeader)
            {
                offset += (_cachedHeaderHeight ?? 0) + ItemSpacing;
            }

            if (itemsSoFar + groupItemCount > visualIndex)
            {
                // Target item is in this group - calculate position within this group
                int itemIndexInGroup = visualIndex - itemsSoFar;
                int rowInGroup = itemIndexInGroup / itemsPerRow;
                offset += rowInGroup * (ItemHeight + ItemSpacing);

                break;
            }

            // Add remaining group height (items + spacing)
            int rowsInGroup = (int)Math.Ceiling((double)groupItemCount / itemsPerRow);
            offset += rowsInGroup * (ItemHeight + ItemSpacing);
            offset += ItemSpacing; // Group spacing

            itemsSoFar += groupItemCount;
        }

        return offset;
    }

    /// <summary>
    /// Finds the visual element for an item in the grouped layout.
    /// </summary>
    private Control? FindElementInGroupedLayout(int index)
    {
        if (_scrollViewer?.Content is not StackPanel stackPanel)
            return null;

        var allItems = ItemsSource?.Cast<object>().ToList();
        if (allItems == null || index < 0 || index >= allItems.Count)
            return null;

        var targetItem = allItems[index];

        // Search through all group ItemsRepeaters
        foreach (var repeater in GetAllItemsRepeaters(stackPanel))
        {
            int count = GetItemCount(repeater.ItemsSource);
            if (count == 0)
                continue;

            // Search through all ItemsSource indices
            for (int i = 0; i < count; i++)
            {
                var element = repeater.TryGetElement(i);
                if (element == null)
                    continue;  // Item not realized (not in viewport), skip

                if (element is GroupedTileListBoxItem container)
                {
                    var itemDataContext = (container.Content as Control)?.DataContext;
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
        if (_scrollViewer == null)
            return null;

        var elementPosition = element.TranslatePoint(new Point(0, 0), _scrollViewer);
        if (elementPosition == null)
            return null;

        return new Rect(elementPosition.Value, element.Bounds.Size);
    }

    /// <summary>
    /// Checks if an element is fully visible within the scroll viewer's viewport.
    /// </summary>
    private bool IsElementFullyVisible(Control element)
    {
        if (_scrollViewer == null)
            return false;

        var elementRect = GetElementRectInScrollViewer(element);
        if (elementRect == null)
            return false;

        var viewportBounds = new Rect(0, 0, _scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height);

        // Check if element is fully contained within viewport
        return viewportBounds.Contains(elementRect.Value);
    }

    /// <summary>
    /// Scrolls to make an element visible with minimal movement.
    /// </summary>
    private void ScrollToElement(Control element)
    {
        if (_scrollViewer == null)
            return;

        var elementRect = GetElementRectInScrollViewer(element);
        if (elementRect == null)
            return;

        var currentOffset = _scrollViewer.Offset;
        var viewportHeight = _scrollViewer.Viewport.Height;

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
            _scrollViewer.Offset = new Vector(currentOffset.X, newY);
        }
    }

    #region Selection Management

    private void SelectItem(object? item)
    {
        if (item == null)
        {
            ClearSelection();
            return;
        }

        int index = GetIndexOfItem(item);
        if (index < 0)
        {
            ClearSelection();
            return;
        }

        _selectedItem = item;
        _selectedIndex = index;

        OnSelectionChanged(_selectedItem, _selectedIndex);
    }

    private void SelectItemAtIndex(int index)
    {
        if (index < 0)
        {
            ClearSelection();
            return;
        }

        object? item = GetItemAtIndex(index);
        if (item == null)
        {
            ClearSelection();
            return;
        }

        _selectedItem = item;
        _selectedIndex = index;

        OnSelectionChanged(_selectedItem, _selectedIndex);
    }

    private void ClearSelection()
    {
        if (_selectedItem == null)
            return;

        _selectedItem = null;
        _selectedIndex = -1;

        OnSelectionChanged(_selectedItem, _selectedIndex);
    }

    private bool IsItemSelected(object? item)
    {
        return item != null && ReferenceEquals(item, _selectedItem);
    }

    private bool IsIndexSelected(int index)
    {
        return index >= 0 && index == _selectedIndex;
    }

    private object? GetItemAtIndex(int index)
    {
        if (ItemsSource == null || index < 0)
            return null;

        if (ItemsSource is IList list)
        {
            if (index < list.Count)
                return list[index];
        }
        else
        {
            return ItemsSource.Cast<object?>().ElementAtOrDefault(index);
        }

        return null;
    }

    private int GetIndexOfItem(object? item)
    {
        if (ItemsSource == null || item == null)
            return -1;

        if (ItemsSource is IList list)
        {
            return list.IndexOf(item);
        }

        return ItemsSource.Cast<object?>()
            .Select((currentItem, index) => new { Item = currentItem, Index = index })
            .FirstOrDefault(x => ReferenceEquals(x.Item, item))
            ?.Index ?? -1;
    }

    #endregion
}
