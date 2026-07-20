// ReSharper disable InconsistentNaming

namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
using Avalonia.VisualTree;
using Helpers;
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
[TemplatePart("PART_NoResultsText", typeof(TextBlock), IsRequired = false)]
[TemplatePart("PART_Popup", typeof(Popup), IsRequired = true)]
[TemplatePart("PART_ItemsListPresenter", typeof(ContentPresenter), IsRequired = true)]
[TemplatePart("PART_SelectedItemsList", typeof(ItemsControl), IsRequired = true)]
[TemplatePart(PART_BackgroundBorder, typeof(Border))]
[PseudoClasses(PC_DropDownOpen, PC_Empty)]
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator require preserved types")]
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

    public static readonly StyledProperty<string> NoResultsTextProperty =
        AvaloniaProperty.Register<MultiComboBox, string>(nameof(NoResultsText), "No results found");

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
            nameof(SelectedItems),
            enableDataValidation: true,
            defaultBindingMode: BindingMode.TwoWay,
            coerce: CoerceSelectedItems);

    private static IList? CoerceSelectedItems(AvaloniaObject instance, IList? value)
    {
        // Coercion callback that just returns the value unchanged
        // This is needed so that CoerceValue() will trigger binding re-evaluation
        return value;
    }

    public static readonly StyledProperty<object?> InnerLeftContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(InnerLeftContent));

    public static readonly StyledProperty<object?> InnerRightContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(InnerRightContent));

    public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
        AvaloniaProperty.Register<MultiComboBox, IDataTemplate?>(
            nameof(SelectedItemTemplate));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        TextBox.PlaceholderTextProperty.AddOwner<MultiComboBox>();

    [Obsolete("Use PlaceholderTextProperty instead.")]
    public static readonly StyledProperty<string?> WatermarkProperty = AvaloniaProperty.Register<MultiComboBox, string?>(nameof(Watermark));

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
            static o => o.ScrollbarVisibility);

    public static readonly DirectProperty<MultiComboBox, ITemplate<Panel>> EffectiveSelectedItemsPanelProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ITemplate<Panel>>(nameof(EffectiveSelectedItemsPanel),
            static o => o.EffectiveSelectedItemsPanel);

    public static readonly DirectProperty<MultiComboBox, IDataTemplate?> EffectiveSelectedItemTemplateProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, IDataTemplate?>(nameof(EffectiveSelectedItemTemplate),
            static o => o.EffectiveSelectedItemTemplate);

    public static readonly DirectProperty<MultiComboBox, bool?> IsAllSelectedProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, bool?>(nameof(IsAllSelected),
            static o => o.IsAllSelected);

    public static readonly StyledProperty<Func<object, string>?> GroupSelectorProperty =
        AvaloniaProperty.Register<MultiComboBox, Func<object, string>?>("GroupSelector");

    public static readonly StyledProperty<BindingBase?> GroupBindingProperty =
        AvaloniaProperty.Register<MultiComboBox, BindingBase?>(nameof(GroupBinding));

    public static readonly StyledProperty<bool> GroupOrderAlphabeticalProperty =
        AvaloniaProperty.Register<MultiComboBox, bool>(nameof(GroupOrderAlphabetical));

    public static readonly StyledProperty<Func<string, int>?> GroupOrderSelectorProperty =
        AvaloniaProperty.Register<MultiComboBox, Func<string, int>?>("GroupOrderSelector");

    public static readonly StyledProperty<BindingBase?> GroupOrderBindingProperty =
        AvaloniaProperty.Register<MultiComboBox, BindingBase?>(nameof(GroupOrderBinding));

    public static readonly StyledProperty<IBrush?> HeaderForegroundProperty =
        AvaloniaProperty.Register<MultiComboBox, IBrush?>(nameof(HeaderForeground));

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<MultiComboBox, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>
    /// Display name to use for the empty/null group key. When <c>null</c>, items in the empty
    /// group render without a header (e.g. an "uncategorized" bucket shown at the top of the drop-down).
    /// </summary>
    public static readonly StyledProperty<string?> EmptyGroupNameProperty =
        AvaloniaProperty.Register<MultiComboBox, string?>(nameof(EmptyGroupName));

    private static readonly ITemplate<Panel> DefaultSelectedItemsPanel =
        new FuncTemplate<Panel>(static () => new VirtualizingStackPanel { Orientation = Orientation.Horizontal, Background = Brushes.Transparent });


    private static readonly ITemplate<Panel> DefaultSelectedItemsWrapPanel =
        new FuncTemplate<Panel>(static () => new WrapPanel { Orientation = Orientation.Horizontal, Background = Brushes.Transparent });


    // Default panel for the inner control (forwarded via property binding)
    private static readonly ITemplate<Panel?> DefaultPanel =
        new FuncTemplate<Panel?>(static () => new VirtualizingStackPanel());

    private readonly InnerMultiComboBoxList innerList;

    private readonly AvaloniaList<object?> displaySelectedItems = [];

    private TextBox? filterTextbox;

    private ContentPresenter? itemsListPresenter;

    private MultiComboBoxSelectAllItem? selectAllItem;

    private ItemsControl? selectedItemsList;

    private ScrollViewer? selectionScrollViewer;

    private bool updateInternal;

    private bool isGrouped;

    private BindingEvaluator? bindingEvaluator;

    private System.ComponentModel.INotifyDataErrorInfo? boundDataErrorInfo;
    
    private string? boundPropertyName;

    static MultiComboBox()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBox>(true);
        ItemsPanelProperty.OverrideDefaultValue<MultiComboBox>(DefaultPanel);
        AvaloniaPropertyExtension.AffectsPseudoClass<MultiComboBox>(IsDropDownOpenProperty, PC_DropDownOpen);
        SelectedItemsProperty.Changed.AddClassHandler<MultiComboBox, IList?>(static (box, args) => box.OnSelectedItemsChanged(args));

        GroupSelectorProperty.Changed.AddClassHandler<MultiComboBox>((x, _) => x.ApplyFilter(null));
        GroupBindingProperty.Changed.AddClassHandler<MultiComboBox>((x, _) => x.ApplyFilter(null));
        GroupOrderAlphabeticalProperty.Changed.AddClassHandler<MultiComboBox>((x, _) => x.ApplyFilter(null));
        GroupOrderSelectorProperty.Changed.AddClassHandler<MultiComboBox>((x, _) => x.ApplyFilter(null));
        GroupOrderBindingProperty.Changed.AddClassHandler<MultiComboBox>((x, _) => x.ApplyFilter(null));
        EmptyGroupNameProperty.Changed.AddClassHandler<MultiComboBox>((x, _) => x.ApplyFilter(null));
    }

    public MultiComboBox()
    {
        this.SelectedItems = new AvaloniaList<object>();

        // Create inner control for filtered display
        // Forward parent's ItemsPanel property to inner control - parent never uses it directly
        this.innerList = new InnerMultiComboBoxList(this)
        {
            Name = "PART_ItemsList",
            ItemsSource = this.FilteredItems,
            [!ItemsPanelProperty] = this[!ItemsPanelProperty],
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

#pragma warning disable CS0618 // Watermark is kept for back-compat; forward to PlaceholderText
        this.GetObservable(WatermarkProperty).Subscribe(value => this.PlaceholderText = value);
#pragma warning restore CS0618
    }

    public AvaloniaList<object?> FilteredItems { get; } = [];

    /// <summary>True when a group selector is configured, i.e. the drop-down is showing grouped items with headers.</summary>
    internal bool IsGrouped => this.isGrouped;

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

    public string NoResultsText
    {
        get => this.GetValue(NoResultsTextProperty);
        set => this.SetValue(NoResultsTextProperty, value);
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

    public string? PlaceholderText
    {
        get => this.GetValue(PlaceholderTextProperty);
        set => this.SetValue(PlaceholderTextProperty, value);
    }

    [Obsolete("Use PlaceholderText instead.")]
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

    /// <summary>
    /// Binding used to order the groups. It is evaluated against a single representative item of each group,
    /// so the bound value must be consistent for every item in the same group — i.e. a function of the same
    /// grouping defined by <see cref="GroupBinding"/>/<see cref="GroupSelector"/> (for example, bind to a
    /// property of the shared group object). The raw value is used for comparison, so a numeric property
    /// sorts numerically.
    /// </summary>
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
        this.FilteredItems.Clear();
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
        return this.innerList.ContainerFromItem(item) as MultiComboBoxItem;
    }

    public void Remove(object? o)
    {
        if (o is not Control chip || this.selectedItemsList is null) return;

        int index = this.selectedItemsList.IndexFromContainer(chip);
        if (index >= 0 && index < (this.SelectedItems?.Count ?? 0))
        {
            this.SelectedItems?.RemoveAt(index);
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
            this.SelectedItems.Add(item);
        }

        foreach (object? item in this.Items)
        {
            this.GetMultiComboBoxItemContainer(item)?.EndUpdate();
        }

        this.selectAllItem?.EndUpdate();

        this.SyncDisplaySelectedItems();
        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count is null or 0);
        this.selectAllItem?.UpdateSelection();

        // Update realized containers — unrealized ones will be synced in PrepareContainerForItemOverride.
        Controls? containers = this.innerList.Presenter?.Panel?.Children;
        if (containers is not null)
        {
            foreach (Control? container in containers)
            {
                if (container is MultiComboBoxItem i)
                {
                    i.Select();
                }
                else if (container is MultiComboBoxGroupHeaderItem h)
                {
                    h.UpdateSelection();
                }
            }
        }

        this.RaiseDirectPropertyChanged(IsAllSelectedProperty);

        this.updateInternal = false;
    }

    public void Clear() => this.DeselectAll();
    
    public void DeselectAll()
    {
        this.SelectedItems?.Clear();
        
        if (this.SelectedItems is null) return;
        
        bool notifiesChanges = this.SelectedItems is INotifyCollectionChanged;
        
        this.SelectedItems.Clear();
        
        if (!notifiesChanges)
        {
            // If SelectedItems is not INotifyCollectionChanged, clearing it
            // won't fire our `OnSelectedItemsCollectionChanged` event
            this.DoOnSelectedItemsCollectionChanged();
        }
    }

    /// <summary>
    /// Adds every item of a group to <see cref="SelectedItems"/> (skipping already-selected ones), then
    /// refreshes the drop-down once. Batched under <see cref="updateInternal"/> so the per-add collection
    /// notifications don't re-enter <see cref="ApplyCurrentSelectionState"/> for each item. Used by a group
    /// header's leading check box; operates on the group's full membership regardless of any active filter.
    /// </summary>
    public void SelectGroup(IReadOnlyList<object> items)
    {
        if (this.SelectedItems is null || items.Count == 0) return;

        this.updateInternal = true;

        foreach (object item in items)
        {
            this.GetMultiComboBoxItemContainer(item)?.BeginUpdate();
        }

        foreach (object item in items)
        {
            if (!this.SelectedItems.Contains(item))
            {
                this.SelectedItems.Add(item);
            }
        }

        foreach (object item in items)
        {
            this.GetMultiComboBoxItemContainer(item)?.EndUpdate();
        }

        this.updateInternal = false;

        this.SyncDisplaySelectedItems();
        this.ApplyCurrentSelectionState();
    }

    /// <summary>
    /// Removes every item of a group from <see cref="SelectedItems"/>, then refreshes the drop-down once.
    /// Counterpart to <see cref="SelectGroup"/>.
    /// </summary>
    public void DeselectGroup(IReadOnlyList<object> items)
    {
        if (this.SelectedItems is null || items.Count == 0) return;

        this.updateInternal = true;

        foreach (object item in items)
        {
            this.GetMultiComboBoxItemContainer(item)?.BeginUpdate();
        }

        foreach (object item in items)
        {
            while (this.SelectedItems.Contains(item))
            {
                this.SelectedItems.Remove(item);
            }
        }

        foreach (object item in items)
        {
            this.GetMultiComboBoxItemContainer(item)?.EndUpdate();
        }

        this.updateInternal = false;

        this.SyncDisplaySelectedItems();
        this.ApplyCurrentSelectionState();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.FixInitializationFromAxamlItems();
        this.selectAllItem?.UpdateSelection();
    }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);

        if (property == SelectedItemsProperty)
        {
            // When the binding is established, subscribe to the DataContext's INotifyDataErrorInfo
            // to monitor validation state changes
            this.SubscribeToDataContextValidation();
            
            DataValidationErrors.SetError(this, error);
        }
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        // Re-subscribe when DataContext changes
        this.SubscribeToDataContextValidation();
    }
    
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "DataContext types are preserved via assembly-level ILLink descriptor (preserve=\"all\"). GetProperties() iterates public properties to find the one matching SelectedItems by reference equality.")]
    private void SubscribeToDataContextValidation()
    {
        // Unsubscribe from previous DataContext
        if (this.boundDataErrorInfo != null)
        {
            this.boundDataErrorInfo.ErrorsChanged -= this.OnBoundPropertyErrorsChanged;
            this.boundDataErrorInfo = null;
            this.boundPropertyName = null;
        }
        
        // Subscribe to new DataContext if it implements INotifyDataErrorInfo
        if (this.DataContext is System.ComponentModel.INotifyDataErrorInfo ndei && this.SelectedItems != null)
        {
            // Find the property name in the DataContext that matches our SelectedItems collection
            Type dataContextType = this.DataContext.GetType();
            foreach (PropertyInfo prop in dataContextType.GetProperties())
            {
                if (prop.CanRead)
                {
                    object? propValue = prop.GetValue(this.DataContext);
                    bool matches = ReferenceEquals(propValue, this.SelectedItems);
                    
                    if (matches)
                    {
                        this.boundDataErrorInfo = ndei;
                        this.boundPropertyName = prop.Name;
                        this.boundDataErrorInfo.ErrorsChanged += this.OnBoundPropertyErrorsChanged;
                        
                        // Trigger initial validation check
                        this.UpdateValidationStateFromDataContext();
                        break;
                    }
                }
            }
        }
    }
    
    private void OnBoundPropertyErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        // Only handle errors for our bound property
        if (e.PropertyName == this.boundPropertyName || string.IsNullOrEmpty(e.PropertyName))
        {
            this.UpdateValidationStateFromDataContext();
        }
    }
    
    private void UpdateValidationStateFromDataContext()
    {
        if (this.boundDataErrorInfo != null && this.boundPropertyName != null)
        {
            Exception? error = null;
            
            IEnumerable? errors = this.boundDataErrorInfo.GetErrors(this.boundPropertyName);
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            object? errorObj = errors?.Cast<object>().FirstOrDefault();
            if (errorObj is not null)
            {
                // Convert first error to an exception
                string errorMessage = errorObj.ToString() ?? "Validation error";
                error = new DataValidationException(errorMessage);
            }
            
            DataValidationErrors.SetError(this, error);
        }
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Store reference to ContentPresenter and set inner control
        this.itemsListPresenter = e.NameScope.Get<ContentPresenter>("PART_ItemsListPresenter");
        this.itemsListPresenter.Content = this.innerList;


        this.selectedItemsList = e.NameScope.Get<ItemsControl>("PART_SelectedItemsList");
        this.selectedItemsList.ItemsSource = this.displaySelectedItems;

        this.selectionScrollViewer = e.NameScope.Find<ScrollViewer>("PART_SelectionScrollViewer");
        this.selectAllItem = e.NameScope.Find<MultiComboBoxSelectAllItem>("PART_SelectAllItem");
        this.filterTextbox = e.NameScope.Find<TextBox>("PART_FilterTextBox");

        if (this.filterTextbox is { } partFilterTextBox)
        {
            partFilterTextBox[!!TextBox.TextProperty] = this[!!FilterValueProperty];
            partFilterTextBox.GetObservable(TextBox.TextProperty).Subscribe(this.ApplyFilter);
        }

        this.selectAllItem?.UpdateSelection();

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
        this.FilteredItems.Clear();

        Func<object, string>? groupSelector = this.ResolveGroupSelector();
        this.isGrouped = groupSelector is not null;
        if (groupSelector is null)
        {
            // Un-grouped: preserve the original behavior exactly.
            this.FilteredItems.AddRange(hasFilterText
                ? this.ItemsView.Where(item => this.ItemMatchesFilter(item, filterText))
                : this.ItemsView);
            return;
        }

        // Grouped: headers carry each group's full membership so a group's leading check box operates on
        // the whole group; the itemFilter still hides non-matching rows and drops fully-filtered groups.
        this.FilteredItems.AddRange(
            ComboBoxGroupedItemsBuilder.Build(
                this.ItemsView,
                groupSelector,
                this.ResolveGroupOrderSelector(groupSelector),
                this.GroupOrderAlphabetical,
                this.EmptyGroupName,
                headerFactory: static (name, items) => new MultiComboBoxGroupHeader(name, items),
                itemFilter: hasFilterText ? item => this.ItemMatchesFilter(item, filterText) : null));
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

    private Func<object, object?>? ResolveGroupOrderSelector(Func<object, string>? groupSelector)
    {
        // GroupOrderBinding reads the sort order off each item (like GroupBinding reads the group key),
        // keeping the raw value so numeric orders sort numerically (10 after 2, not lexically).
        if (this.GetValue(GroupOrderBindingProperty) is { } binding)
        {
            this.bindingEvaluator ??= BindingEvaluator.FromItemsControl(this);
            return this.bindingEvaluator?.BuildRawGetter(binding);
        }

        // GroupOrderSelector maps a group key to an order; adapt it to operate on an item.
        if (this.GetValue(GroupOrderSelectorProperty) is { } orderFn && groupSelector is not null)
        {
            return item => orderFn(groupSelector(item));
        }

        return null;
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

                if (multiComboBoxItem.IsSelected)
                {
                    this.SelectedItems?.Add(multiComboBoxItem);
                }
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled || !this.ShouldToggleDropDown(e))
        {
            return;
        }

        this.SetCurrentValue(IsDropDownOpenProperty, !this.IsDropDownOpen);
        e.Handled = true;
    }

    private bool ShouldToggleDropDown(PointerPressedEventArgs e)
    {
        if (!this.IsEffectivelyEnabled)
        {
            return false;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return false;
        }

        if (e.Source is not Visual source)
        {
            return false;
        }

        if (TopLevel.GetTopLevel(source) is PopupRoot)
        {
            return false;
        }

        return source.FindAncestorOfType<Button>(includeSelf: true) is null;
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

        this.SyncDisplaySelectedItems();

        // Re-subscribe to validation when SelectedItems changes
        // This ensures we track the correct property in the DataContext
        this.SubscribeToDataContextValidation();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => this.DoOnSelectedItemsCollectionChanged();
    
    private void DoOnSelectedItemsCollectionChanged()
    {
        if (!this.updateInternal)
        {
            this.SyncDisplaySelectedItems();
        }

        this.ApplyCurrentSelectionState();

        // Trigger validation automatically when collection mutates
        // This works with any INotifyDataErrorInfo implementation
        this.TriggerCollectionValidation();
    }
    
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "DataContext types are preserved via assembly-level ILLink descriptor (preserve=\"all\"). Accesses known property by name and searches for ValidateProperty method in the type hierarchy.")]
    private void TriggerCollectionValidation()
    {
        // If we already know the bound property name from subscription, use it directly
        if (this.boundPropertyName != null && this.DataContext != null)
        {
            Type dataContextType = this.DataContext.GetType();
            
            // Get the actual collection value
            PropertyInfo? property = dataContextType.GetProperty(this.boundPropertyName);
            object? collectionValue = property?.GetValue(this.DataContext);
            
            // Try to call the protected ValidateProperty method from ObservableValidator base class
            // We need to search in base types as well
            MethodInfo? validateMethod = null;
            Type? currentType = dataContextType;
            while (currentType != null && validateMethod == null)
            {
                validateMethod = currentType.GetMethod("ValidateProperty",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    [typeof(object), typeof(string)],
                    null);
                currentType = currentType.BaseType;
            }
            
            if (validateMethod != null)
            {
                try
                {
                    validateMethod.Invoke(this.DataContext, [collectionValue, this.boundPropertyName]);
                }
                catch
                {
                    // Silently ignore validation errors
                }
            }
        }
    }

    private void ApplyCurrentSelectionState()
    {
        if (this.updateInternal) return;
        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count is null or 0);

        // Update only realized containers — inline data items in ItemsView are not displayed
        // and their DataContext may not match SelectedItems (it inherits from the parent control).
        Controls? containers = this.innerList.Presenter?.Panel?.Children;
        if (containers is not null)
        {
            foreach (Control? container in containers)
            {
                if (container is MultiComboBoxItem i)
                {
                    i.UpdateSelection();
                }
                else if (container is MultiComboBoxGroupHeaderItem h)
                {
                    h.UpdateSelection();
                }
            }
        }

        this.selectAllItem?.UpdateSelection();
        this.RaiseDirectPropertyChanged(IsAllSelectedProperty);
    }

    private void SyncDisplaySelectedItems()
    {
        this.displaySelectedItems.Clear();
        if (this.SelectedItems is null) return;

        this.displaySelectedItems.AddRange(this.SelectedItems.OfType<object?>()
            .Select(item => item is MultiComboBoxItem comboBoxItem ? comboBoxItem.Content : item));
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

            if (dropdownItem is MultiComboBoxGroupHeaderItem { IsFocused: true } headerItem)
            {
                headerItem.IsSelected = headerItem.IsSelected is null or false;
                break;
            }
        }
    }

    private static bool CanFocus(Control control) => control is { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true };
}