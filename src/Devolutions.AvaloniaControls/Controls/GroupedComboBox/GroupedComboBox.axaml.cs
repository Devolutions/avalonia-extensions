// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ArrangeStaticMemberQualifier
// ReSharper disable MemberCanBePrivate.Local
namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Metadata;

using Converters;
using Helpers;

/// <summary>
/// A grouped <see cref="ComboBox"/> that keeps the active theme's normal <see cref="ComboBox"/>
/// template and normal <see cref="ComboBoxItem"/> containers for selectable rows. Group headers are
/// inserted as separate non-selectable pseudo-items.
/// </summary>
[PseudoClasses(":empty")]
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator require preserved types")]
public class GroupedComboBox : ComboBox
{
    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<GroupedComboBox, Func<object, string>?>("GroupSelector");

    public static readonly StyledProperty<BindingBase?> GroupBindingProperty =
        AvaloniaProperty.Register<GroupedComboBox, BindingBase?>(nameof(GroupBinding));

    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<GroupedComboBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<GroupedComboBox, Func<string, int>?>("GroupOrderSelector");

    public static readonly StyledProperty<BindingBase?> GroupOrderBindingProperty =
        AvaloniaProperty.Register<GroupedComboBox, BindingBase?>(nameof(GroupOrderBinding));
    
    public static readonly StyledProperty<IBrush?> HeaderForegroundProperty =
        AvaloniaProperty.Register<GroupedComboBox, IBrush?>(nameof(HeaderForeground));
    
    public static readonly StyledProperty<Thickness> HeaderMarginProperty =
        AvaloniaProperty.Register<GroupedComboBox, Thickness>(nameof(HeaderMargin), new Thickness(4, 6, 8, 4));

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<GroupedComboBox, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>
    /// Display name to use for the empty/null group key. When <c>null</c>, items in the empty
    /// group render without a header.
    /// </summary>
    public static readonly StyledProperty<string?> EmptyGroupNameProperty =
        AvaloniaProperty.Register<GroupedComboBox, string?>(nameof(EmptyGroupName));

    protected static readonly object HeaderRecycleKey = typeof(GroupHeader);

    private readonly List<object> sourceItems = [];

    private readonly List<INotifyPropertyChanged> trackedItems = [];

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
    public BindingBase? GroupBinding
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
    public BindingBase? GroupOrderBinding
    {
        get => this.GetValue(GroupOrderBindingProperty);
        set => this.SetValue(GroupOrderBindingProperty, value);
    }

    public IBrush? HeaderForeground
    {
        get => this.GetValue(HeaderForegroundProperty);
        set => this.SetValue(HeaderForegroundProperty, value);
    }

    public Thickness HeaderMargin
    {
        get => this.GetValue(HeaderMarginProperty);
        set => this.SetValue(HeaderMarginProperty, value);
    }

    public IDataTemplate? HeaderTemplate
    {
        get => this.GetValue(HeaderTemplateProperty);
        set => this.SetValue(HeaderTemplateProperty, value);
    }

    public string? EmptyGroupName
    {
        get => this.GetValue(EmptyGroupNameProperty);
        set => this.SetValue(EmptyGroupNameProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ComboBox);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.TryCaptureInlineItems();
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

        recycleKey = item is GroupHeader ? HeaderRecycleKey : DefaultRecycleKey;
        return true;
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is not GroupedComboBoxHeaderItem headerContainer) return;

        if (item is GroupHeader header)
        {
            headerContainer[!GroupedComboBoxHeaderItem.ForegroundProperty] = new MultiBinding()
            {
                Converter = new FirstNonNullValueMultiConverter(),
                Bindings = [
                    this[!HeaderForegroundProperty],
                    new DynamicResourceExtension("SystemControlForegroundAccentBrush"),
                ],
            };
            headerContainer[!GroupedComboBoxHeaderItem.MarginProperty] = this[!HeaderMarginProperty];
            headerContainer.Content = header;
        }
        
        headerContainer.ContentTemplate = this.HeaderTemplate;
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);

        if (container is GroupedComboBoxHeaderItem headerContainer)
        {
            headerContainer.ClearValue(GroupedComboBoxHeaderItem.ForegroundProperty);
            headerContainer.ClearValue(GroupedComboBoxHeaderItem.MarginProperty);
            headerContainer.ClearValue(GroupedComboBoxHeaderItem.ContentTemplateProperty);
            headerContainer.Content = null;
        }
    }
    
    private void TryCaptureInlineItems()
    {
        if (this.externalItemsSource is null && !this.inlineItemsCaptured && this.Items.Count > 0)
        {
            this.inlineItemsCaptured = true;
            this.CaptureSourceItems(this.Items, this.Items.Count);
            this.RebuildDisplayItems();
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
        // TODO: This could be more granular for performances / UX (preserving scroll)
        this.CaptureSourceItems(this.externalItemsSource);
        this.RebuildDisplayItems();
    }

    private void OnSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // TODO: This could be more granular for performances / UX (preserving scroll)
        this.RebuildDisplayItems();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (this.applyingDisplayItems || this.inlineItemsCaptured) return;

        this.CaptureSourceItems(this.Items, this.Items.Count);
    }

    private void CaptureSourceItems(IEnumerable? itemsSource, int count = 0)
    {
        this.sourceItems.Clear();
        this.UnsubscribeFromSourceItemChanges();
        if (itemsSource is null) return;

        if (count > 0)
        {
            this.sourceItems.EnsureCapacity(count);
            this.trackedItems.EnsureCapacity(count);
        }

        foreach (object? item in itemsSource)
        {
            if (item is not null)
            {
                this.sourceItems.Add(item);
                this.SubscribeToSourceItemChanges(item);
            }
        }
    }

    private void SubscribeToSourceItemChanges(object item)
    {
        if (item is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += this.OnSourceItemPropertyChanged;
            this.trackedItems.Add(notifyPropertyChanged);
        }
    }

    private void UnsubscribeFromSourceItemChanges()
    {
        foreach (INotifyPropertyChanged item in this.trackedItems)
        {
            item.PropertyChanged -= this.OnSourceItemPropertyChanged;
        }

        this.trackedItems.Clear();
    }

    private void RebuildDisplayItems()
    {
        Func<object, string>? selector = this.ResolveGroupSelector();
        if (selector is null) return;

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
        string? emptyName = this.EmptyGroupName;
        bool hasEmptyName = string.IsNullOrEmpty(emptyName);
        foreach (IGrouping<string, (object item, string groupName, int originalIndex)> group in grouped)
        {
            bool isEmptyHeaderless = group.Key.Length == 0 && hasEmptyName;
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
