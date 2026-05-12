namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

using Devolutions.AvaloniaControls.Helpers;

/// <summary>
/// A grouped list control rendered as a vertical stack of collapsible group sections, each
/// containing a standard <see cref="ListBox"/>. Cross-group single-selection works
/// automatically: each inner <see cref="ListBox"/>'s <c>SelectedItem</c> is two-way bound to
/// this control's <see cref="SelectingItemsControl.SelectedItem"/>, and because groups are
/// disjoint a ListBox whose <c>ItemsSource</c> does not contain the new value silently clears
/// its own selection.
/// </summary>
[TemplatePart(PART_ItemsHost, typeof(ItemsControl), IsRequired = true)]
[TemplatePart(PART_ScrollViewer, typeof(ScrollViewer), IsRequired = false)]
public class GroupedListBox : SelectingItemsControl
{
    private const string PART_ItemsHost = "PART_ItemsHost";
    private const string PART_ScrollViewer = "PART_ScrollViewer";

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

    private ItemsControl? itemsHost;
    private ScrollViewer? scrollViewer;

    private readonly ObservableCollection<GroupedListBoxGroupItem> groups = [];

    private BindingEvaluator? groupEvaluator;
    private IBinding? groupEvaluatorBinding;
    private BindingEvaluator? groupOrderEvaluator;
    private IBinding? groupOrderEvaluatorBinding;

    private INotifyCollectionChanged? collectionChangedSource;

    static GroupedListBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<GroupedListBox>((x, e) => x.OnItemsSourceChanged(e));
        ItemTemplateProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.RebuildGroups());

        GroupSelectorProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.RebuildGroups());
        GroupBindingProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.RebuildGroups());
        GroupOrderSelectorProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.RebuildGroups());
        GroupOrderBindingProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.RebuildGroups());
        GroupOrderAlphabeticalProperty.Changed.AddClassHandler<GroupedListBox>((x, _) => x.RebuildGroups());
    }

    /// <summary>
    /// Gets or sets the function used to determine the group for each item.
    /// </summary>
    [Obsolete("Use GroupBinding instead for XAML-friendly, type-checked group selection.")]
    public Func<object, string>? GroupSelector
    {
        get => this.GetValue(GroupSelectorProperty);
        set => this.SetValue(GroupSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the binding used to extract the group key from each item.
    /// When set, this takes precedence over <see cref="GroupSelector"/>.
    /// The binding's data type is inferred from <see cref="ItemsControl.ItemsSource"/> for compile-time safety.
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IBinding? GroupBinding
    {
        get => this.GetValue(GroupBindingProperty);
        set => this.SetValue(GroupBindingProperty, value);
    }

    /// <summary>
    /// Gets or sets whether groups are sorted alphabetically by key. Applied as a secondary
    /// sort after the resolved group-order selector, if set.
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
    [Obsolete("Use GroupOrderBinding instead for XAML-friendly group ordering.")]
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
    public IBinding? GroupOrderBinding
    {
        get => this.GetValue(GroupOrderBindingProperty);
        set => this.SetValue(GroupOrderBindingProperty, value);
    }

    /// <summary>
    /// Gets or sets whether new groups start in the expanded state. Defaults to <see langword="true"/>.
    /// </summary>
    public bool GroupsExpandedByDefault
    {
        get => this.GetValue(GroupsExpandedByDefaultProperty);
        set => this.SetValue(GroupsExpandedByDefaultProperty, value);
    }

    /// <summary>
    /// Exposes the grouped data for the templated <see cref="ItemsControl"/> to bind against.
    /// </summary>
    internal IReadOnlyList<GroupedListBoxGroupItem> Groups => this.groups;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.scrollViewer = e.NameScope.Find<ScrollViewer>(PART_ScrollViewer);
        this.itemsHost = e.NameScope.Find<ItemsControl>(PART_ItemsHost);

        if (this.itemsHost is not null)
        {
            this.itemsHost.ItemsSource = this.groups;
        }

        this.RebuildGroups();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (this.ItemsSource is INotifyCollectionChanged notify && this.collectionChangedSource is null)
        {
            this.collectionChangedSource = notify;
            this.collectionChangedSource.CollectionChanged += this.OnSourceCollectionChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (this.collectionChangedSource is not null)
        {
            this.collectionChangedSource.CollectionChanged -= this.OnSourceCollectionChanged;
            this.collectionChangedSource = null;
        }
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (this.collectionChangedSource is not null)
        {
            this.collectionChangedSource.CollectionChanged -= this.OnSourceCollectionChanged;
            this.collectionChangedSource = null;
        }

        if (this.ItemsSource is INotifyCollectionChanged notify)
        {
            this.collectionChangedSource = notify;
            this.collectionChangedSource.CollectionChanged += this.OnSourceCollectionChanged;
        }

        this.RebuildGroups();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        this.RebuildGroups();

    /// <summary>
    /// Resolves the effective group selector: <see cref="GroupBinding"/> first, falling back to <see cref="GroupSelector"/>.
    /// </summary>
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

            BindingEvaluator evaluator = this.groupEvaluator;
            return item => evaluator.EvaluateAsString(item);
        }

        this.groupEvaluator = null;
        this.groupEvaluatorBinding = null;

#pragma warning disable CS0618
        return this.GetValue(GroupSelectorProperty);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Resolves the effective group-order selector: <see cref="GroupOrderBinding"/> first, falling back to <see cref="GroupOrderSelector"/>.
    /// </summary>
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

            BindingEvaluator evaluator = this.groupOrderEvaluator;
            return key => evaluator.EvaluateAs<int>(key);
        }

        this.groupOrderEvaluator = null;
        this.groupOrderEvaluatorBinding = null;

#pragma warning disable CS0618
        return this.GetValue(GroupOrderSelectorProperty);
#pragma warning restore CS0618
    }

    private void RebuildGroups()
    {
        // Preserve previously expanded keys so a rebuild doesn't collapse open groups.
        HashSet<string> previouslyExpanded = new(StringComparer.Ordinal);
        foreach (GroupedListBoxGroupItem g in this.groups)
        {
            if (g.IsExpanded)
            {
                previouslyExpanded.Add(g.Key);
            }
        }

        this.groups.Clear();

        if (this.ItemsSource is null)
        {
            return;
        }

        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        IEnumerable<object> items = this.ItemsSource.Cast<object>();
        IDataTemplate? itemTemplate = this.ItemTemplate;

        if (groupSelector is null)
        {
            // No grouping: a single unnamed group containing all items.
            this.groups.Add(new GroupedListBoxGroupItem(
                key: string.Empty,
                hasHeader: false,
                isExpanded: true,
                items: items.ToList(),
                itemTemplate: itemTemplate));
            return;
        }

        IEnumerable<IGrouping<string, object>> grouped = items.GroupBy(groupSelector);
        IEnumerable<IGrouping<string, object>> ordered = this.OrderGroups(grouped);

        bool defaultExpanded = this.GroupsExpandedByDefault;

        foreach (IGrouping<string, object> g in ordered)
        {
            string key = g.Key ?? string.Empty;
            bool isExpanded = previouslyExpanded.Count > 0
                ? previouslyExpanded.Contains(key)
                : defaultExpanded;

            this.groups.Add(new GroupedListBoxGroupItem(
                key: key,
                hasHeader: !string.IsNullOrEmpty(key),
                isExpanded: isExpanded,
                items: g.ToList(),
                itemTemplate: itemTemplate));
        }
    }

    private IEnumerable<IGrouping<string, object>> OrderGroups(IEnumerable<IGrouping<string, object>> groups)
    {
        Func<string, int>? orderSelector = this.ResolveGroupOrderSelector();

        if (orderSelector is { } selector)
        {
            return this.GroupOrderAlphabetical
                ? groups.OrderBy(g => selector(g.Key)).ThenBy(g => g.Key)
                : groups.OrderBy(g => selector(g.Key));
        }

        return this.GroupOrderAlphabetical ? groups.OrderBy(g => g.Key) : groups;
    }
}
