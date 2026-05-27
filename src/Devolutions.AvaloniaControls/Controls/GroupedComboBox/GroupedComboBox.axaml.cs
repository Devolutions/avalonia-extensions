namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;

using Devolutions.AvaloniaControls.Helpers;

/// <summary>
/// A grouped <see cref="ComboBox"/> that keeps the active theme's normal <see cref="ComboBox"/>
/// template and normal <see cref="ComboBoxItem"/> containers for selectable rows. Group headers are
/// inserted as separate non-selectable pseudo-items.
/// </summary>
[PseudoClasses(":empty")]
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
public class GroupedComboBox : ComboBox
{
    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedComboBox, Func<object, string>?>("GroupSelector");

    public static readonly StyledProperty<IBinding?> GroupBindingProperty =
        AvaloniaProperty.Register<GroupedComboBox, IBinding?>(nameof(GroupBinding));

    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<GroupedComboBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedComboBox, Func<string, int>?>("GroupOrderSelector");

    public static readonly StyledProperty<IBinding?> GroupOrderBindingProperty =
        AvaloniaProperty.Register<GroupedComboBox, IBinding?>(nameof(GroupOrderBinding));

    public static readonly StyledProperty<IBrush?> GroupForegroundProperty =
        AvaloniaProperty.Register<GroupedComboBox, IBrush?>(nameof(GroupForeground));

    public static readonly StyledProperty<Thickness> HeaderMarginProperty =
        GroupedComboBoxHeaderItem.HeaderMarginProperty.AddOwner<GroupedComboBox>();

    /// <summary>
    /// Display name to use for the empty/null group key. When <c>null</c>, items in the empty
    /// group render without a header.
    /// </summary>
    public static readonly StyledProperty<string?> EmptyGroupNameProperty =
        AvaloniaProperty.Register<GroupedComboBox, string?>(nameof(EmptyGroupName));

    protected static readonly object HeaderRecycleKey = typeof(GroupHeader);

    private readonly List<object> sourceItems = [];

    private BindingEvaluator? bindingEvaluator;

    private IEnumerable? externalItemsSource;

    private INotifyCollectionChanged? externalCollectionChanged;

    private bool applyingDisplayItems;

    private bool inlineItemsCaptured;

    static GroupedComboBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedComboBox>((x, e) => x.OnItemsSourceChanged(e.NewValue as IEnumerable));
        GroupSelectorProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RebuildDisplayItems());
        GroupBindingProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RebuildDisplayItems());
        GroupOrderAlphabeticalProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RebuildDisplayItems());
        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RebuildDisplayItems());
        GroupOrderBindingProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RebuildDisplayItems());
        EmptyGroupNameProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RebuildDisplayItems());
        GroupForegroundProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RefreshHeaderContainers());
    }

    public GroupedComboBox()
    {
        this.Items.CollectionChanged += this.OnItemsCollectionChanged;
    }

    [Obsolete("Use GroupBinding instead for XAML-friendly, type-checked group selection.")]
    public Func<object, string>? GroupSelector
    {
        get => this.GetValue(GroupSelectorProperty);
        set => this.SetValue(GroupSelectorProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IBinding? GroupBinding
    {
        get => this.GetValue(GroupBindingProperty);
        set => this.SetValue(GroupBindingProperty, value);
    }

    public bool GroupOrderAlphabetical
    {
        get => this.GetValue(GroupOrderAlphabeticalProperty);
        set => this.SetValue(GroupOrderAlphabeticalProperty, value);
    }

    public Func<string, int>? GroupOrderSelector
    {
        get => this.GetValue(GroupOrderSelectorProperty);
        set => this.SetValue(GroupOrderSelectorProperty, value);
    }

    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IBinding? GroupOrderBinding
    {
        get => this.GetValue(GroupOrderBindingProperty);
        set => this.SetValue(GroupOrderBindingProperty, value);
    }

    public IBrush? GroupForeground
    {
        get => this.GetValue(GroupForegroundProperty);
        set => this.SetValue(GroupForegroundProperty, value);
    }

    public Thickness HeaderMargin
    {
        get => this.GetValue(HeaderMarginProperty);
        set => this.SetValue(HeaderMarginProperty, value);
    }

    public string? EmptyGroupName
    {
        get => this.GetValue(EmptyGroupNameProperty);
        set => this.SetValue(EmptyGroupNameProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ComboBox);

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (this.externalItemsSource is null && !this.inlineItemsCaptured && this.Items.Count > 0)
        {
            this.inlineItemsCaptured = true;
            this.sourceItems.Clear();
            foreach (object? item in this.Items)
            {
                if (item is not null)
                {
                    this.sourceItems.Add(item);
                }
            }

            this.RebuildDisplayItems();
        }
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        recycleKey == HeaderRecycleKey ? new GroupedComboBoxHeaderItem() : new GroupedComboBoxItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is ComboBoxItem)
        {
            recycleKey = null;
            return false;
        }

        recycleKey = item is GroupHeader ? typeof(GroupHeader) : DefaultRecycleKey;
        return true;
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is GroupedComboBoxHeaderItem headerContainer && item is GroupHeader header)
        {
            headerContainer.HeaderText = header.text;
            if (this.GroupForeground is { } groupForeground)
            {
                headerContainer.HeaderForeground = groupForeground;
            }
            else
            {
                headerContainer.ClearValue(GroupedComboBoxHeaderItem.HeaderForegroundProperty);
            }
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);

        if (container is GroupedComboBoxHeaderItem headerContainer)
        {
            headerContainer.HeaderText = null;
            headerContainer.ClearValue(GroupedComboBoxHeaderItem.HeaderForegroundProperty);
        }
    }

    private void OnItemsSourceChanged(IEnumerable? itemsSource)
    {
        if (this.applyingDisplayItems) return;

        if (ReferenceEquals(itemsSource, this.externalItemsSource)) return;

        if (this.externalCollectionChanged is not null)
        {
            this.externalCollectionChanged.CollectionChanged -= this.OnExternalCollectionChanged;
        }

        // ReSharper disable once PossibleMultipleEnumeration
        this.externalItemsSource = itemsSource;
        this.externalCollectionChanged = itemsSource as INotifyCollectionChanged;
        if (this.externalCollectionChanged is not null)
        {
            this.externalCollectionChanged.CollectionChanged += this.OnExternalCollectionChanged;
        }

        // ReSharper disable once PossibleMultipleEnumeration
        this.CaptureSourceItems(itemsSource);
        this.RebuildDisplayItems();
    }

    private void OnExternalCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.CaptureSourceItems(this.externalItemsSource);
        this.RebuildDisplayItems();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (this.applyingDisplayItems || this.externalItemsSource is not null || this.inlineItemsCaptured) return;

        this.sourceItems.Clear();
        foreach (object? item in this.Items)
        {
            if (item is not null)
            {
                this.sourceItems.Add(item);
            }
        }
    }

    private void CaptureSourceItems(IEnumerable? itemsSource)
    {
        this.sourceItems.Clear();
        if (itemsSource is null) return;

        foreach (object? item in itemsSource)
        {
            if (item is not null)
            {
                this.sourceItems.Add(item);
            }
        }
    }

    private void RebuildDisplayItems()
    {
        Func<object, string>? selector = this.ResolveGroupSelector();
        if (selector is null)
        {
            return;
        }

        List<(object item, string groupName, int originalIndex)> source = new(this.sourceItems.Count);
        for (int i = 0; i < this.sourceItems.Count; ++i)
        {
            object item = this.sourceItems[i];
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            source.Add((item, selector(item) ?? string.Empty, i));
        }

        IEnumerable<IGrouping<string, (object item, string groupName, int originalIndex)>> grouped = source.GroupBy(static x => x.groupName);
        Func<string, object?>? orderFn = this.ResolveGroupOrderSelector();
        grouped = (orderFn, this.GroupOrderAlphabetical) switch
        {
            ({ } fn, true) => grouped.OrderBy(g => fn(g.Key)).ThenBy(static g => g.Key, StringComparer.Ordinal),
            ({ } fn, false) => grouped.OrderBy(g => fn(g.Key)),
            (null, true) => grouped.OrderBy(static g => g.Key, StringComparer.Ordinal),
            (null, false) => grouped,
        };

        grouped.TryGetNonEnumeratedCount(out var groupedCount);
        List<object> displayItems = new(source.Count + groupedCount);
        foreach (IGrouping<string, (object item, string groupName, int originalIndex)> group in grouped)
        {
            string? emptyName = this.EmptyGroupName;
            bool isEmptyHeaderless = group.Key.Length == 0 && string.IsNullOrEmpty(emptyName);
            if (!isEmptyHeaderless)
            {
                displayItems.Add(new GroupHeader(group.Key.Length == 0 ? emptyName : group.Key));
            }

            foreach ((object item, _, _) in group.OrderBy(static x => x.originalIndex))
            {
                displayItems.Add(item);
            }
        }

        this.applyingDisplayItems = true;
        try
        {
            this.SetCurrentValue(ItemsSourceProperty, displayItems);
        }
        finally
        {
            this.applyingDisplayItems = false;
        }
    }

    private void RefreshHeaderContainers()
    {
        for (int i = 0; i < this.ItemCount; ++i)
        {
            if (this.ContainerFromIndex(i) is GroupedComboBoxHeaderItem headerContainer && this.Items[i] is GroupHeader header)
            {
                headerContainer.HeaderText = header.text;
                if (this.GroupForeground is { } groupForeground)
                {
                    headerContainer.HeaderForeground = groupForeground;
                }
                else
                {
                    headerContainer.ClearValue(GroupedComboBoxHeaderItem.HeaderForegroundProperty);
                }
            }
        }
    }

    private Func<object, string>? ResolveGroupSelector()
    {
        if (this.GetValue(GroupBindingProperty) is { } binding)
        {
            this.bindingEvaluator ??= BindingEvaluator.FromItemsControl(this);
            return this.bindingEvaluator?.BuildFormattedGetter(binding);
        }

        return this.GetValue(GroupSelectorProperty);
    }

    private Func<string, object?>? ResolveGroupOrderSelector()
    {
        if (this.GetValue(GroupOrderBindingProperty) is { } binding)
        {
            this.bindingEvaluator ??= BindingEvaluator.FromItemsControl(this);
            return this.bindingEvaluator?.BuildFormattedGetter(binding);
        }

        if (this.GetValue(GroupOrderSelectorProperty) is { } orderFn)
        {
            return str => orderFn(str);
        }

        return null;
    }

    private readonly struct GroupHeader(string? text)
    {
        public readonly string? text = text;
        public override string ToString() => this.text ?? string.Empty;
    }
}
