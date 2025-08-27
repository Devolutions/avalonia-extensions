// ReSharper disable InconsistentNaming

namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Irihi.Avalonia.Shared.Helpers;
using static Extensions.AvaloniaExtensions;

public enum MultiComboBoxOverflowMode
{
    Scroll,
    Wrap,
}

[TemplatePart("PART_SelectionScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_SelectAllItem", typeof(MultiComboBoxSelectAllItem))]
[TemplatePart(PART_BackgroundBorder, typeof(Border))]
[PseudoClasses(PC_DropDownOpen, PC_Empty)]
public class MultiComboBox : SelectingItemsControl
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

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        ComboBox.IsDropDownOpenProperty.AddOwner<MultiComboBox>();

    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<MultiComboBox, double>(
            nameof(MaxDropDownHeight));

    public static readonly StyledProperty<double> MaxSelectionBoxHeightProperty =
        AvaloniaProperty.Register<MultiComboBox, double>(
            nameof(MaxSelectionBoxHeight));

    public new static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<MultiComboBox, IList?>(
            nameof(SelectedItems));

    public static readonly StyledProperty<object?> InnerLeftContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(InnerLeftContent));

    public static readonly StyledProperty<object?> InnerRightContentProperty =
        AvaloniaProperty.Register<MultiComboBox, object?>(
            nameof(InnerRightContent));

    public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
        AvaloniaProperty.Register<MultiComboBox, IDataTemplate?>(
            nameof(SelectedItemTemplate));

    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<MultiComboBox>();

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
            o => o.ScrollbarVisibility);

    public static readonly DirectProperty<MultiComboBox, ITemplate<Panel>> EffectiveSelectedItemsPanelProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ITemplate<Panel>>(nameof(EffectiveSelectedItemsPanel),
            o => o.EffectiveSelectedItemsPanel);

    public static readonly DirectProperty<MultiComboBox, IDataTemplate?> EffectiveSelectedItemTemplateProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, IDataTemplate?>(nameof(EffectiveSelectedItemTemplate),
            o => o.EffectiveSelectedItemTemplate);

    public static readonly DirectProperty<MultiComboBox, bool?> IsAllSelectedProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, bool?>(nameof(IsAllSelected),
            o => o.IsAllSelected);

    private static readonly ITemplate<Panel> DefaultSelectedItemsPanel =
        new FuncTemplate<Panel>(() => new VirtualizingStackPanel { Orientation = Orientation.Horizontal, Background = Brushes.Transparent });


    private static readonly ITemplate<Panel> DefaultSelectedItemsWrapPanel =
        new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal, Background = Brushes.Transparent });


    private static readonly ITemplate<Panel?> DefaultPanel =
        new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

    private Border? rootBorder;

    private MultiComboBoxSelectAllItem? selectAllItem;

    private ScrollViewer? selectionScrollViewer;

    private bool updateInternal;

    static MultiComboBox()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBox>(true);
        ItemsPanelProperty.OverrideDefaultValue<MultiComboBox>(DefaultPanel);
        IsDropDownOpenProperty.AffectsPseudoClass<MultiComboBox>(PC_DropDownOpen);
        SelectedItemsProperty.Changed.AddClassHandler<MultiComboBox, IList?>((box, args) => box.OnSelectedItemsChanged(args));
    }

    public MultiComboBox()
    {
        this.SelectedItems = new AvaloniaList<object>();

        this.GetObservable(OverflowModeProperty).Subscribe(_ =>
        {
            this.RaiseDirectPropertyChanged(ScrollbarVisibilityProperty);
            this.RaiseDirectPropertyChanged(EffectiveSelectedItemsPanelProperty);
        });
        this.GetObservable(SelectedItemTemplateProperty).Subscribe(_ => this.RaiseDirectPropertyChanged(EffectiveSelectedItemTemplateProperty));
        this.GetObservable(ItemTemplateProperty).Subscribe(_ => this.RaiseDirectPropertyChanged(EffectiveSelectedItemTemplateProperty));
    }

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

    public bool IsDropDownOpen
    {
        get => this.GetValue(IsDropDownOpenProperty);
        set => this.SetValue(IsDropDownOpenProperty, value);
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

    private void RaiseDirectPropertyChanged<T>(DirectPropertyBase<T> property)
    {
        T val = this.GetValue(property);
        this.RaisePropertyChanged(property, default!, val);
    }

    private MultiComboBoxItem? GetMultiComboBoxItemContainer(object? item)
    {
        if (item is null) return null;
        return item as MultiComboBoxItem ?? this.ContainerFromItem(item) as MultiComboBoxItem;
    }

    public void Remove(object? o)
    {
        if (o is StyledElement s)
        {
            var data = s.DataContext;
            this.SelectedItems?.Remove(data);
            var item = this.Items.FirstOrDefault(a => ReferenceEquals(a, data));
            if (item is not null)
            {
                var container = this.ContainerFromItem(item);
                if (container is MultiComboBoxItem t)
                {
                    t.IsSelected = false;
                }
            }
        }
    }

    public void SelectAll()
    {
        if (this.SelectedItems is null) return;

        this.updateInternal = true;
        this.selectAllItem?.BeginUpdate();

        this.SelectedItems.Clear();

        foreach (var item in this.Items)
        {
            this.GetMultiComboBoxItemContainer(item)?.BeginUpdate();
        }

        foreach (var item in this.Items)
        {
            if (this.GetMultiComboBoxItemContainer(item) is MultiComboBoxItem multiComboBoxItem)
            {
                this.SelectedItems.Add(multiComboBoxItem.DataContext);
                multiComboBoxItem.Select();
            }
            else
            {
                this.SelectedItems.Add(item);
            }
        }

        foreach (var item in this.Items)
        {
            this.GetMultiComboBoxItemContainer(item)?.EndUpdate();
        }

        this.selectAllItem?.EndUpdate();

        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count is null or 0);
        this.selectAllItem?.UpdateSelection();

        var containers = this.Presenter?.Panel?.Children;
        if (containers is not null)
        {
            foreach (var container in containers)
            {
                if (container is MultiComboBoxItem i)
                {
                    i.Select();
                }
            }
        }

        this.RaiseDirectPropertyChanged(IsAllSelectedProperty);

        this.updateInternal = false;
    }

    public void DeselectAll()
    {
        this.SelectedItems?.Clear();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.FixInitializationFromAxamlItems();
        this.selectAllItem?.UpdateSelection();
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.selectionScrollViewer = e.NameScope.Find<ScrollViewer>("PART_SelectionScrollViewer");
        this.selectAllItem = e.NameScope.Find<MultiComboBoxSelectAllItem>("PART_SelectAllItem");

        this.selectAllItem?.UpdateSelection();

        PointerPressedEvent.RemoveHandler(this.OnBackgroundPointerPressed, this.rootBorder);
        this.rootBorder = e.NameScope.Find<Border>(PART_BackgroundBorder);
        PointerPressedEvent.AddHandler(this.OnBackgroundPointerPressed, this.rootBorder);
        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count == 0);

        if (this.selectAllItem is { } sai)
        {
            sai.GetObservable(IsSelectedProperty).Subscribe(isSelected =>
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

    private void FixInitializationFromAxamlItems()
    {
        var isCleared = false;

        foreach (var item in this.Items)
        {
            if (item is MultiComboBoxItem multiComboBoxItem)
            {
                if (!isCleared)
                {
                    this.SelectedItems?.Clear();
                    isCleared = true;
                }

                multiComboBoxItem.DataContext = multiComboBoxItem.Content;
                if (multiComboBoxItem.IsSelected)
                {
                    this.SelectedItems?.Add(multiComboBoxItem.DataContext);
                }
            }
        }
    }

    private void OnBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        this.SetCurrentValue(IsDropDownOpenProperty, !this.IsDropDownOpen);
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
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.ApplyCurrentSelectionState();
    }

    private void ApplyCurrentSelectionState()
    {
        if (this.updateInternal) return;
        this.PseudoClasses.Set(PC_Empty, this.SelectedItems?.Count is null or 0);

        var containers = this.Presenter?.Panel?.Children;
        if (containers is not null)
        {
            foreach (var container in containers)
            {
                if (container is MultiComboBoxItem i)
                {
                    i.UpdateSelection();
                }
            }
        }

        this.selectAllItem?.UpdateSelection();
        this.RaiseDirectPropertyChanged(IsAllSelectedProperty);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = item;
        return item is not MultiComboBoxItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new MultiComboBoxItem();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (item is MultiComboBoxItem containerItem)
        {
            container.DataContext = containerItem.Content;
        }

        base.PrepareContainerForItemOverride(container, item, index);
        this.selectAllItem?.UpdateSelection();
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
        if (e.Key is Key.Space or Key.Enter or Key.Down && !this.IsDropDownOpen)
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
            var firstChild = this.Presenter?.Panel?.Children.FirstOrDefault(CanFocus);
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
        if (this.IsDropDownOpen && e.Key is Key.Enter or Key.Space)
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
        if (this.IsDropDownOpen && this.ItemCount > 0 && e.Key is Key.Up or Key.Down && this.IsFocused)
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
        var firstChild = this.Presenter?.Panel?.Children.FirstOrDefault(CanFocus);
        if (firstChild != null)
        {
            this.ScrollIntoView(firstChild.DataContext ?? firstChild);
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

        foreach (var dropdownItem in this.GetRealizedContainers())
        {
            if (dropdownItem is MultiComboBoxItem { IsFocused: true } multiComboBoxItem)
            {
                multiComboBoxItem.IsSelected = !multiComboBoxItem.IsSelected;
                break;
            }
        }
    }

    private static bool CanFocus(Control control) => control is { Focusable: true, IsEffectivelyEnabled: true };
}