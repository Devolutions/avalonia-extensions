namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

public enum MultiComboBoxOverflowMode
{
    Scroll,
    Wrap,
}

[TemplatePart("PART_SelectionScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_SelectAllItem", typeof(MultiComboBoxSelectAllItem))]
public class MultiComboBox : Ursa.Controls.MultiComboBox
{
    public static readonly StyledProperty<MultiComboBoxOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<MultiComboBox, MultiComboBoxOverflowMode>(nameof(OverflowMode));

    public static readonly StyledProperty<string?> SelectAllLabelProperty =
        AvaloniaProperty.Register<MultiComboBox, string?>(nameof(SelectAllLabel));

    public static readonly DirectProperty<MultiComboBox, ScrollBarVisibility> ScrollbarVisibilityProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ScrollBarVisibility>(nameof(OverflowMode),
            o => o.ScrollbarVisibility,
            (o, v) => o.ScrollbarVisibility = v);

    private MultiComboBoxSelectAllItem? selectAllItem;

    private ScrollViewer? selectionScrollViewer;

    static MultiComboBox()
    {
        SelectedItemsProperty.Changed.AddClassHandler<MultiComboBox, IList?>((box, args) => box.OnSelectedItemsChanged(args));
    }

    public MultiComboBox()
    {
        this.GetObservable(OverflowModeProperty).Subscribe(visibility => this.ScrollbarVisibility = visibility switch
        {
            MultiComboBoxOverflowMode.Scroll => ScrollBarVisibility.Auto,
            MultiComboBoxOverflowMode.Wrap => ScrollBarVisibility.Disabled,
            _ => ScrollBarVisibility.Auto,
        });
    }

    public MultiComboBoxOverflowMode OverflowMode
    {
        get => this.GetValue(OverflowModeProperty);
        set => this.SetValue(OverflowModeProperty, value);
    }

    public string? SelectAllLabel
    {
        get => this.GetValue(SelectAllLabelProperty);
        set => this.SetValue(SelectAllLabelProperty, value);
    }

    public ScrollBarVisibility ScrollbarVisibility { get; private set; } = ScrollBarVisibility.Auto;

    public void SelectAll()
    {
        if (this.SelectedItems is null) return;

        this.SelectedItems.Clear();
        foreach (var item in this.Items)
        {
            if (item is MultiComboBoxItem multiComboBoxItem)
            {
                this.SelectedItems.Add(multiComboBoxItem.DataContext);
                multiComboBoxItem.IsSelected = true;
            }
            else
            {
                this.SelectedItems.Add(item);
            }
        }
    }

    public void DeselectAll()
    {
        this.SelectedItems?.Clear();
    }

    protected override void OnInitialized()
    {
        if (this.SelectedItems is INotifyCollectionChanged c) c.CollectionChanged += this.OnSelectedItemsCollectionChanged;
        this.FixInitializationFromAxamlItems();
        this.selectAllItem?.UpdateSelection();
        base.OnInitialized();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.selectionScrollViewer = e.NameScope.Find<ScrollViewer>("PART_SelectionScrollViewer");
        this.selectAllItem = e.NameScope.Find<MultiComboBoxSelectAllItem>("PART_SelectAllItem");

        this.selectAllItem?.GetObservable(IsSelectedProperty).Subscribe(isSelected =>
        {
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
        this.selectAllItem?.UpdateSelection();
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
            return;
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
        this.HookTabItemSelection(e);
        this.HookItemsNavigation(e);

        base.OnKeyDown(e);
    }

    private void HookKeyboardScroll(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (this.OverflowMode == MultiComboBoxOverflowMode.Scroll && e.Key is Key.Left or Key.Right && this.selectionScrollViewer is ScrollViewer sv)
        {
            var dir = e.Key == Key.Left ? -1 : 1;
            var step = 48 * dir;
            var maxX = Math.Max(0, sv.Extent.Width - sv.Viewport.Width);
            var newX = Math.Clamp(sv.Offset.X + step, 0, maxX);
            sv.Offset = new Vector(newX, sv.Offset.Y);

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
            var firstChild = this.Presenter?.Panel?.Children.FirstOrDefault(this.CanFocus);
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
            this.SelectFocusedItem();
            e.Handled = true;
        }
    }

    private void HookTabItemSelection(KeyEventArgs e)
    {
        if (e.Handled) return;
        if (this.IsDropDownOpen && e.Key == Key.Tab)
        {
            this.SelectFocusedItem();
            this.IsDropDownOpen = false;
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
        if (this.selectAllItem is not null && this.CanFocus(this.selectAllItem))
        {
            return this.selectAllItem.Focus();
        }

        return false;
    }

    private bool FocusFirstItem()
    {
        var firstChild = this.Presenter?.Panel?.Children.FirstOrDefault(this.CanFocus);
        if (firstChild != null)
        {
            return firstChild.Focus(NavigationMethod.Directional);
        }

        return false;
    }

    private void SelectFocusedItem()
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

    private bool CanFocus(Control control) => control is { Focusable: true, IsEffectivelyEnabled: true, IsVisible: true };
}