namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;

using Devolutions.AvaloniaControls.Helpers;

/// <summary>
/// A grouped <see cref="ListBox"/> rendered as a flat vertical list of <see cref="GroupedListBoxItem"/>
/// containers. The first container in each group renders an <c>Expander</c>-based header above its
/// content; when the user collapses that header, all containers belonging to the same group hide.
/// <para>
/// Containers are direct logical children of <see cref="GroupedListBox"/> (single
/// <see cref="ItemsControl"/>, no nested items hosts). This guarantees that
/// <see cref="GroupedListBoxItem"/> instances appear under the <see cref="GroupedListBox"/>
/// in the logical tree, which matches the convention used by built-in controls such as
/// <c>ListBox</c> and <c>TabControl</c>.
/// </para>
/// <para>
/// By default the user's item order is preserved verbatim (transparent passthrough of both
/// <see cref="ItemsControl.Items"/> set in XAML and <see cref="ItemsControl.ItemsSource"/> set
/// programmatically). When any of the group-ordering properties (<see cref="GroupOrderAlphabetical"/>,
/// <see cref="GroupOrderSelector"/>, <see cref="GroupOrderBinding"/>) is set, the underlying
/// items collection is rearranged in-place so containers appear grouped in the desired order.
/// </para>
/// </summary>
[PseudoClasses(":empty")]
public class GroupedListBox : ListBox
{
    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedListBox, Func<object, string>?>("GroupSelector");

    public static readonly StyledProperty<IBinding?> GroupBindingProperty =
        AvaloniaProperty.Register<GroupedListBox, IBinding?>(nameof(GroupBinding));

    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<GroupedListBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedListBox, Func<string, int>?>("GroupOrderSelector");

    public static readonly StyledProperty<IBinding?> GroupOrderBindingProperty =
        AvaloniaProperty.Register<GroupedListBox, IBinding?>(nameof(GroupOrderBinding));

    public static readonly StyledProperty<bool> GroupsExpandedByDefaultProperty =
        AvaloniaProperty.Register<GroupedListBox, bool>(nameof(GroupsExpandedByDefault), defaultValue: true);

    public static readonly StyledProperty<IBrush?> GroupForegroundProperty =
        AvaloniaProperty.Register<GroupedListBox, IBrush?>(nameof(GroupForeground));

    /// <summary>Per-group expansion state, keyed by group key. Persisted across container recycling.</summary>
    private readonly Dictionary<string, bool> expandedByKey = new(StringComparer.Ordinal);

    /// <summary>Cached per-item group-key map for the current items collection.</summary>
    private readonly Dictionary<int, string> groupKeyByIndex = [];

    /// <summary>Index of the leader container per group key (lowest item index for that key).</summary>
    private readonly Dictionary<string, int> leaderIndexByKey = new(StringComparer.Ordinal);

    private BindingEvaluator? groupEvaluator;
    private IBinding? groupEvaluatorBinding;
    private BindingEvaluator? groupOrderEvaluator;
    private IBinding? groupOrderEvaluatorBinding;

    private bool reordering;

    static GroupedListBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.OnItemsChanged());
        GroupSelectorProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.OnGroupingChanged());
        GroupBindingProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.OnGroupingChanged());
        GroupOrderAlphabeticalProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.OnOrderingChanged());
        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.OnOrderingChanged());
        GroupOrderBindingProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.OnOrderingChanged());
    }

    public GroupedListBox()
    {
        // Subscribe to the realized Items collection (used when items are declared inline in XAML).
        ((INotifyCollectionChanged)this.Items).CollectionChanged += this.OnItemsCollectionChanged;
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

    [Obsolete("Use GroupOrderBinding instead for XAML-friendly group ordering.")]
    public Func<string, int>? GroupOrderSelector
    {
        get => this.GetValue(GroupOrderSelectorProperty);
        set => this.SetValue(GroupOrderSelectorProperty, value);
    }

    [AssignBinding]
    public IBinding? GroupOrderBinding
    {
        get => this.GetValue(GroupOrderBindingProperty);
        set => this.SetValue(GroupOrderBindingProperty, value);
    }

    public bool GroupsExpandedByDefault
    {
        get => this.GetValue(GroupsExpandedByDefaultProperty);
        set => this.SetValue(GroupsExpandedByDefaultProperty, value);
    }

    public IBrush? GroupForeground
    {
        get => this.GetValue(GroupForegroundProperty);
        set => this.SetValue(GroupForegroundProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(GroupedListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new GroupedListBoxItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is GroupedListBoxItem)
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
        if (container is GroupedListBoxItem gli)
        {
            this.ApplyGroupingTo(gli, index);
        }
    }

    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        if (container is GroupedListBoxItem gli)
        {
            this.ApplyGroupingTo(gli, index);
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is GroupedListBoxItem gli)
        {
            gli.IsGroupLeader = false;
            gli.GroupKey = null;
            gli.GroupHeaderText = null;
            gli.SetGroupExpandedSilently(true);
            gli.GroupForeground = null;
            gli.SetGroupExpansionCallback(null);
        }
    }

    /// <summary>
    /// Called by a leader <see cref="GroupedListBoxItem"/> when its expander toggles. Persists the
    /// new state and propagates visibility to every realized container in the same group.
    /// </summary>
    internal void OnGroupExpansionToggled(string key, bool isExpanded)
    {
        this.expandedByKey[key] = isExpanded;
        this.PropagateGroupExpansion(key, isExpanded);
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

        int i = 0;
        foreach (object? item in this.Items)
        {
            if (item is null) { i++; continue; }
            string key = selector(item) ?? string.Empty;
            this.groupKeyByIndex[i] = key;
            if (!this.leaderIndexByKey.ContainsKey(key))
            {
                this.leaderIndexByKey[key] = i;
                if (!this.expandedByKey.ContainsKey(key))
                    this.expandedByKey[key] = this.GroupsExpandedByDefault;
            }
            i++;
        }
    }

    private void RefreshAllContainers()
    {
        for (int i = 0; i < this.ItemCount; i++)
        {
            if (this.ContainerFromIndex(i) is GroupedListBoxItem gli)
                this.ApplyGroupingTo(gli, i);
        }
    }

    private void ApplyGroupingTo(GroupedListBoxItem container, int index)
    {
        if (!this.groupKeyByIndex.TryGetValue(index, out string? key))
        {
            // No grouping configured.
            container.IsGroupLeader = false;
            container.GroupKey = null;
            container.GroupHeaderText = null;
            container.SetGroupExpandedSilently(true);
            container.GroupForeground = this.GroupForeground;
            container.SetGroupExpansionCallback(null);
            container.IsVisible = true;
            return;
        }

        bool isLeader = this.leaderIndexByKey.TryGetValue(key, out int leaderIdx) && leaderIdx == index;
        bool isExpanded = this.expandedByKey.TryGetValue(key, out bool exp) ? exp : this.GroupsExpandedByDefault;

        container.GroupKey = key;
        container.GroupHeaderText = key;
        container.IsGroupLeader = isLeader;
        container.SetGroupExpandedSilently(isExpanded);
        container.GroupForeground = this.GroupForeground;
        container.SetGroupExpansionCallback(isLeader ? this.OnGroupExpansionToggled : null);
        // Leader stays visible (its header lets the user re-expand). Non-leaders disappear when collapsed.
        container.IsVisible = isLeader || isExpanded;
    }

    private void PropagateGroupExpansion(string key, bool isExpanded)
    {
        for (int i = 0; i < this.ItemCount; i++)
        {
            if (this.ContainerFromIndex(i) is GroupedListBoxItem gli && gli.GroupKey == key)
            {
                gli.SetGroupExpandedSilently(isExpanded);
                gli.IsVisible = gli.IsGroupLeader || isExpanded;
            }
        }
    }

    private void ReorderItemsInPlace()
    {
        Func<object, string>? selector = this.ResolveGroupSelector();
        if (selector is null) return;

        // Snapshot current items as a list with their group keys.
        List<(object item, string key, int originalIndex)> snapshot = [];
        int idx = 0;
        foreach (object? it in this.Items)
        {
            if (it is null) { idx++; continue; }
            snapshot.Add((it, selector(it) ?? string.Empty, idx));
            idx++;
        }

        // Compute desired group order.
        Func<string, int>? orderFn = this.ResolveGroupOrderSelector();
        IOrderedEnumerable<IGrouping<string, (object item, string key, int originalIndex)>> grouped =
            orderFn is { } fn
                ? snapshot.GroupBy(t => t.key).OrderBy(g => fn(g.Key))
                : snapshot.GroupBy(t => t.key).OrderBy(g => g.Key, StringComparer.Ordinal);

        if (this.GroupOrderAlphabetical && orderFn is not null)
        {
            grouped = snapshot.GroupBy(t => t.key).OrderBy(g => orderFn(g.Key)).ThenBy(g => g.Key, StringComparer.Ordinal);
        }
        else if (this.GroupOrderAlphabetical)
        {
            grouped = snapshot.GroupBy(t => t.key).OrderBy(g => g.Key, StringComparer.Ordinal);
        }

        List<object> sorted = [];
        foreach (IGrouping<string, (object item, string key, int originalIndex)> g in grouped)
        {
            // Within a group, preserve original order.
            foreach ((object item, _, _) in g.OrderBy(t => t.originalIndex))
                sorted.Add(item);
        }

        // Detect no-op.
        bool changed = sorted.Count != snapshot.Count;
        if (!changed)
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                if (!ReferenceEquals(sorted[i], snapshot[i].item)) { changed = true; break; }
            }
        }
        if (!changed) return;

        // Apply: rebuild Items in place. If ItemsSource is set, replace with sorted enumerable;
        // else mutate Items (XAML inline mode).
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
                foreach (object o in sorted) this.Items.Add(o);
            }
        }
        finally
        {
            this.reordering = false;
        }
    }

    private Func<object, string>? ResolveGroupSelector()
    {
        IBinding? binding = this.GetValue(GroupBindingProperty);
        if (binding is not null)
        {
            if (this.groupEvaluator is null || !ReferenceEquals(this.groupEvaluatorBinding, binding))
            {
                this.groupEvaluator = new BindingEvaluator(binding, this);
                this.groupEvaluatorBinding = binding;
            }
            BindingEvaluator e = this.groupEvaluator;
            return item => e.EvaluateAsString(item);
        }

        this.groupEvaluator = null;
        this.groupEvaluatorBinding = null;
#pragma warning disable CS0618
        return this.GetValue(GroupSelectorProperty);
#pragma warning restore CS0618
    }

    private Func<string, int>? ResolveGroupOrderSelector()
    {
        IBinding? binding = this.GetValue(GroupOrderBindingProperty);
        if (binding is not null)
        {
            if (this.groupOrderEvaluator is null || !ReferenceEquals(this.groupOrderEvaluatorBinding, binding))
            {
                this.groupOrderEvaluator = new BindingEvaluator(binding, this);
                this.groupOrderEvaluatorBinding = binding;
            }
            BindingEvaluator e = this.groupOrderEvaluator;
            return key => e.EvaluateAs<int>(key);
        }

        this.groupOrderEvaluator = null;
        this.groupOrderEvaluatorBinding = null;
#pragma warning disable CS0618
        return this.GetValue(GroupOrderSelectorProperty);
#pragma warning restore CS0618
    }
}
