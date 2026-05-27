namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;

using Devolutions.AvaloniaControls.Helpers;

/// <summary>
/// A grouped <see cref="ComboBox"/> that keeps the active theme's normal <see cref="ComboBox"/>
/// template and only decorates popup item containers with group headers.
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
        AvaloniaProperty.Register<GroupedComboBox, Thickness>(nameof(HeaderMargin), new Thickness(4, 6, 8, 4));

    /// <summary>
    /// Display name to use for the empty/null group key. When <c>null</c>, items in the empty
    /// group render without a header.
    /// </summary>
    public static readonly StyledProperty<string?> EmptyGroupNameProperty =
        AvaloniaProperty.Register<GroupedComboBox, string?>(nameof(EmptyGroupName));

    private readonly Dictionary<int, string> groupKeyByIndex = [];

    private readonly Dictionary<string, int> leaderIndexByKey = new(StringComparer.Ordinal);

    private BindingEvaluator? bindingEvaluator;

    private bool reordering;

    static GroupedComboBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.OnItemsChanged());
        GroupSelectorProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.OnGroupingChanged());
        GroupBindingProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.OnGroupingChanged());
        GroupOrderAlphabeticalProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.OnOrderingChanged());
        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.OnOrderingChanged());
        GroupOrderBindingProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.OnOrderingChanged());
        EmptyGroupNameProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RefreshAllContainers());
        GroupForegroundProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RefreshAllContainers());
        HeaderMarginProperty.Changed.AddClassHandler<GroupedComboBox>((x, _) => x.RefreshAllContainers());
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

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new GroupedComboBoxItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is ComboBoxItem)
        {
            recycleKey = null;
            return false;
        }

        recycleKey = DefaultRecycleKey;
        return true;
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is GroupedComboBoxItem groupedContainer)
        {
            this.ApplyGroupingTo(groupedContainer, item, index);
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is GroupedComboBoxItem groupedContainer)
        {
            groupedContainer.IsGroupLeader = false;
            groupedContainer.GroupKey = null;
            groupedContainer.GroupHeaderText = null;
            groupedContainer.GroupForeground = null;
            groupedContainer.ClearValue(GroupedComboBoxItem.HeaderMarginProperty);
        }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (this.reordering) return;
        this.OnItemsChanged();
    }

    private void OnItemsChanged()
    {
        this.RecomputeGroupMaps();
        if (this.HasOrderingConfigured()) this.ReorderItemsInPlace();
        this.RefreshAllContainers();
    }

    private void OnGroupingChanged()
    {
        this.RecomputeGroupMaps();
        if (this.HasOrderingConfigured()) this.ReorderItemsInPlace();
        this.RefreshAllContainers();
    }

    private void OnOrderingChanged()
    {
        if (this.HasOrderingConfigured()) this.ReorderItemsInPlace();
        this.RecomputeGroupMaps();
        this.RefreshAllContainers();
    }

    private bool HasOrderingConfigured() =>
        this.GroupOrderAlphabetical
        || this.GetValue(GroupOrderBindingProperty) is not null
#pragma warning disable CS0618
        || this.GetValue(GroupOrderSelectorProperty) is not null;
#pragma warning restore CS0618

    private void RecomputeGroupMaps()
    {
        this.groupKeyByIndex.Clear();
        this.leaderIndexByKey.Clear();

        Func<object, string>? selector = this.ResolveGroupSelector();
        if (selector is null) return;

        for (int i = 0; i < this.Items.Count; ++i)
        {
            object? item = this.Items[i];
            if (item is null) continue;

            string key = selector(item) ?? string.Empty;
            this.groupKeyByIndex[i] = key;
            this.leaderIndexByKey.TryAdd(key, i);
        }
    }

    private void RefreshAllContainers()
    {
        for (int i = 0; i < this.ItemCount; ++i)
        {
            if (this.ContainerFromIndex(i) is GroupedComboBoxItem groupedContainer)
            {
                object? item = i >= 0 && i < this.Items.Count ? this.Items[i] : null;
                this.ApplyGroupingTo(groupedContainer, item, i);
            }
        }
    }

    private void ApplyGroupingTo(GroupedComboBoxItem container, object? item, int index)
    {
        if (!this.groupKeyByIndex.TryGetValue(index, out string? groupName))
        {
#pragma warning disable CS0618
            throw new InvalidOperationException(
                $"{nameof(GroupedComboBox)} requires either {nameof(this.GroupBinding)} or {nameof(this.GroupSelector)} to be set. " +
                "Grouping is not optional - use a regular ComboBox for an ungrouped list.");
#pragma warning restore CS0618
        }

        bool isLeader = this.leaderIndexByKey.TryGetValue(groupName, out int leaderIdx) && leaderIdx == index;
        string? emptyName = this.EmptyGroupName;
        bool isEmptyHeaderless = groupName.Length == 0 && string.IsNullOrEmpty(emptyName);

        container.GroupKey = groupName;
        container.GroupHeaderText = groupName.Length == 0 ? emptyName : groupName;
        container.IsGroupLeader = isLeader && !isEmptyHeaderless;
        container.HeaderMargin = this.HeaderMargin;
        if (this.GroupForeground is { } groupForeground)
        {
            container.GroupForeground = groupForeground;
        }
        else
        {
            container.ClearValue(GroupedComboBoxItem.GroupForegroundProperty);
        }
    }

    private void ReorderItemsInPlace()
    {
        Func<object, string>? selector = this.ResolveGroupSelector();
        if (selector is null) return;

        List<(object item, string groupName, int originalIndex)> snapshot = new(this.Items.Count);
        for (int idx = 0; idx < this.Items.Count; ++idx)
        {
            object? it = this.Items[idx];
            if (it is not null)
            {
                snapshot.Add((it, selector(it) ?? string.Empty, idx));
            }
        }

        Func<string, object?>? orderFn = this.ResolveGroupOrderSelector();
        IEnumerable<IGrouping<string, (object item, string groupName, int originalIndex)>> grouped =
            snapshot.GroupBy(t => t.groupName);

        grouped = (orderFn, this.GroupOrderAlphabetical) switch
        {
            ({ } fn, true) => grouped
                .OrderBy(g => fn(g.Key))
                .ThenBy(static g => g.Key, StringComparer.Ordinal),
            ({ } fn, false) => grouped.OrderBy(g => fn(g.Key)),
            (null, true) => grouped.OrderBy(g => g.Key, StringComparer.Ordinal),
            (null, false) => grouped,
        };

        List<object> sorted = new(this.Items.Count);
        foreach (IGrouping<string, (object item, string groupName, int originalIndex)> g in grouped)
        {
            foreach ((object item, _, _) in g.OrderBy(t => t.originalIndex))
            {
                sorted.Add(item);
            }
        }

        bool changed = sorted.Count != snapshot.Count;
        if (!changed)
        {
            for (int i = 0; i < sorted.Count; ++i)
            {
                if (!ReferenceEquals(sorted[i], snapshot[i].item)) { changed = true; break; }
            }
        }
        if (!changed) return;

        this.reordering = true;
        try
        {
            if (this.ItemsSource is not null)
            {
                this.ItemsSource = sorted;
            }
            else
            {
                this.Items.Clear();
                foreach (object o in sorted)
                {
                    this.Items.Add(o);
                }
            }
        }
        finally
        {
            this.reordering = false;
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
}
