namespace Devolutions.AvaloniaControls.Controls;

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
public class MultiComboBox : Ursa.Controls.MultiComboBox
{
    public static readonly StyledProperty<MultiComboBoxOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<MultiComboBox, MultiComboBoxOverflowMode>(nameof(OverflowMode));

    public static readonly DirectProperty<MultiComboBox, ScrollBarVisibility> ScrollbarVisibilityProperty =
        AvaloniaProperty.RegisterDirect<MultiComboBox, ScrollBarVisibility>(nameof(OverflowMode),
            o => o.ScrollbarVisibility,
            (o, v) => o.ScrollbarVisibility = v);

    private ScrollViewer? selectionScrollViewer;

    public MultiComboBox()
    {
        OverflowModeProperty.Changed.Subscribe(_ => this.ScrollbarVisibility = this.OverflowMode switch
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

    public ScrollBarVisibility ScrollbarVisibility { get; private set; } = ScrollBarVisibility.Auto;

    protected override void OnInitialized()
    {
        this.FixInitializationFromAxamlItems();
        base.OnInitialized();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        this.selectionScrollViewer = e.NameScope.Get<ScrollViewer>("PART_SelectionScrollViewer");
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
                    this.Selection.Clear();
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
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        this.HookKeyboardScroll(e);
        this.HookOpenAction(e);
        this.HookCloseAction(e);
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
            this.FocusFirstChild();
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
            e.Handled = this.FocusFirstChild();
        }
    }

    private bool FocusFirstChild()
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