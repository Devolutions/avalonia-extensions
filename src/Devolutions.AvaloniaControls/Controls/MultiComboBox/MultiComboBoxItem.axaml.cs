namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

public class MultiComboBoxItem : Ursa.Controls.MultiComboBoxItem { }

public class MultiComboBoxSelectAllItem : ContentControl
{
    public static readonly StyledProperty<bool?> IsSelectedProperty = AvaloniaProperty.Register<MultiComboBoxItem, bool?>(nameof(IsSelected), false);

    private MultiComboBox? parent;

    private bool updateInternal;

    static MultiComboBoxSelectAllItem()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBoxSelectAllItem>(true);
        IsSelectedProperty.Changed.AddClassHandler<MultiComboBoxSelectAllItem, bool?>((item, args) => item.OnSelectionChanged(args));
    }

    public bool? IsSelected
    {
        get => this.GetValue(IsSelectedProperty);
        set => this.SetValue(IsSelectedProperty, value);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        this.parent = this.FindLogicalAncestorOfType<MultiComboBox>();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.UpdateSelection();
    }

    private void OnSelectionChanged(AvaloniaPropertyChangedEventArgs<bool?> args)
    {
        if (this.updateInternal) return;
        if (args.OldValue.Value == args.NewValue.Value) return;

        if (args.OldValue.Value is null && args.NewValue.Value != true)
        {
            this.IsSelected = true;
            return;
        }

        if (args.OldValue.Value != true)
        {
            this.parent?.SelectAll();
            this.IsSelected = true;
        }
        else
        {
            this.parent?.DeselectAll();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        this.IsSelected = this.IsSelected is null or false;
    }

    internal void UpdateSelection()
    {
        this.updateInternal = true;
        if (this.parent?.ItemsPanelRoot is VirtualizingPanel)
        {
            var selectCount = this.parent?.SelectedItems?.Count;
            var itemsCount = this.parent?.Items.Count;

            if (selectCount is null || itemsCount is null || itemsCount == 0 || selectCount == 0)
            {
                this.IsSelected = false;
            }
            else
            {
                this.IsSelected = selectCount == itemsCount ? true : null;
            }
        }

        this.updateInternal = false;
    }
}