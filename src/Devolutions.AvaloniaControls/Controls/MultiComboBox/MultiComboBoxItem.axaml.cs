namespace Devolutions.AvaloniaControls.Controls;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Extensions;
using Irihi.Avalonia.Shared.Common;
using Irihi.Avalonia.Shared.Helpers;

[PseudoClasses(PC_Grouped)]
public class MultiComboBoxItem : ContentControl
{
    /// <summary>Set on drop-down items while grouping is active, so themes can indent them under the group header.</summary>
    public const string PC_Grouped = ":grouped";

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<MultiComboBoxItem, bool>(
        nameof(IsSelected));

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Referencing MultiComboBox.FilterValueProperty merely triggers MultiComboBox's static constructor, which does not use dynamic code; the BindingEvaluator usage is confined to MultiComboBox's lazily-invoked grouping methods.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "See IL2026 justification: referencing MultiComboBox.FilterValueProperty does not itself require dynamic code.")]
    public static readonly StyledProperty<string?> FilterValueProperty =
        MultiComboBox.FilterValueProperty.AddOwner<MultiComboBoxItem>();

    private static readonly Point SInvalidPoint = new(double.NaN, double.NaN);
    private MultiComboBox? parent;
    private Point pointerDownPoint = SInvalidPoint;
    private bool updateInternal;

    static MultiComboBoxItem()
    {
        IsSelectedProperty.AffectsPseudoClass<MultiComboBoxItem>(PseudoClassName.PC_Selected);
        PressedMixin.Attach<MultiComboBoxItem>();
        FocusableProperty.OverrideDefaultValue<MultiComboBoxItem>(true);
        IsSelectedProperty.Changed.AddClassHandler<MultiComboBoxItem, bool>((item, args) => item.OnSelectionChanged(args));
    }

    public bool IsSelected
    {
        get => this.GetValue(IsSelectedProperty);
        set => this.SetValueIfChanged(IsSelectedProperty, value);
    }

    public string? FilterValue
    {
        get => this.GetValue(FilterValueProperty);
        set => this.SetValue(FilterValueProperty, value);
    }

    public override string ToString() => $"{nameof(MultiComboBoxItem)} ({this.Content})";

    internal void Select()
    {
        this.SetValue(IsSelectedProperty, true);
    }

    internal void SetGrouped(bool grouped)
    {
        this.PseudoClasses.Set(PC_Grouped, grouped);
    }

    internal void BeginUpdate()
    {
        this.updateInternal = true;
    }

    internal void EndUpdate()
    {
        this.updateInternal = false;
    }

    private void OnSelectionChanged(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        if (this.updateInternal) return;
        if (args.NewValue.Value)
        {
            if (this.parent?.SelectedItems?.Contains(this.DataContext) != true)
            {
                this.parent?.SelectedItems?.Add(this.DataContext);
            }
        }
        else
        {
            if (this.parent?.SelectedItems?.Contains(this.DataContext) == true)
            {
                this.parent?.SelectedItems?.Remove(this.DataContext);
            }
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        this.parent = this.FindLogicalAncestorOfType<MultiComboBox>();
        if (this.IsSelected && this.parent?.SelectedItems?.Contains(this.DataContext) == false)
        {
            this.parent?.SelectedItems?.Add(this.DataContext);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        this.pointerDownPoint = e.GetPosition(this);
        if (e.Handled)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            PointerPoint p = e.GetCurrentPoint(this);
            if (p.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed
                or PointerUpdateKind.RightButtonPressed)
            {
                if (p.Pointer.Type == PointerType.Mouse)
                {
                    this.IsSelected = !this.IsSelected;
                    e.Handled = true;
                }
                else
                {
                    this.pointerDownPoint = p.Position;
                }
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!e.Handled && !double.IsNaN(this.pointerDownPoint.X) &&
            e.InitialPressMouseButton is MouseButton.Left or MouseButton.Right)
        {
            PointerPoint point = e.GetCurrentPoint(this);
            if (new Rect(this.Bounds.Size).ContainsExclusive(point.Position) && e.Pointer.Type == PointerType.Touch)
            {
                this.IsSelected = !this.IsSelected;
                e.Handled = true;
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.UpdateSelection();
    }

    internal void UpdateSelection()
    {
        if (this.updateInternal) return;
        this.updateInternal = true;

        bool newIsSelected = this.parent?.SelectedItems?.Contains(this.DataContext) ?? false;
        if (newIsSelected != this.IsSelected)
        {
            this.IsSelected = newIsSelected;
        }

        this.updateInternal = false;
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new ListItemAutomationPeer(this);
}

public class MultiComboBoxSelectAllItem : ContentControl
{
    public static readonly StyledProperty<bool?> IsSelectedProperty = AvaloniaProperty.Register<MultiComboBoxSelectAllItem, bool?>(nameof(IsSelected), false);

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
        set => this.SetValueIfChanged(IsSelectedProperty, value);
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

    internal void BeginUpdate()
    {
        this.updateInternal = true;
    }

    internal void EndUpdate()
    {
        this.updateInternal = false;
    }

    internal void UpdateSelection()
    {
        this.updateInternal = true;

        int? selectCount = this.parent?.SelectedItems?.Count;
        int? itemsCount = this.parent?.Items.Count;

        if (selectCount is null || itemsCount is null || itemsCount == 0 || selectCount == 0)
        {
            this.IsSelected = false;
        }
        else
        {
            this.IsSelected = selectCount == itemsCount ? true : null;
        }

        this.updateInternal = false;
    }
}

/// <summary>
/// Non-selectable group header row for a grouped <see cref="MultiComboBox"/>. Its leading tri-state
/// check box selects/deselects every item of the group and shows an indeterminate (mixed) state when
/// only some of the group's items are selected. Modeled on <see cref="MultiComboBoxSelectAllItem"/>,
/// but scoped to a single group's items rather than the whole list.
/// </summary>
public class MultiComboBoxGroupHeaderItem : ContentControl
{
    public static readonly StyledProperty<bool?> IsSelectedProperty =
        AvaloniaProperty.Register<MultiComboBoxGroupHeaderItem, bool?>(nameof(IsSelected), false);

    private MultiComboBox? parent;

    private bool updateInternal;

    static MultiComboBoxGroupHeaderItem()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBoxGroupHeaderItem>(true);
        IsSelectedProperty.Changed.AddClassHandler<MultiComboBoxGroupHeaderItem, bool?>((item, args) => item.OnSelectionChanged(args));
    }

    /// <summary>The full (unfiltered) items of the group this header represents.</summary>
    internal IReadOnlyList<object>? GroupItems { get; set; }

    public bool? IsSelected
    {
        get => this.GetValue(IsSelectedProperty);
        set => this.SetValueIfChanged(IsSelectedProperty, value);
    }

    internal void BeginUpdate()
    {
        this.updateInternal = true;
    }

    internal void EndUpdate()
    {
        this.updateInternal = false;
    }

    internal void UpdateSelection()
    {
        this.updateInternal = true;

        IReadOnlyList<object>? items = this.GroupItems;
        IList? selected = this.parent?.SelectedItems;

        if (items is null || items.Count == 0 || selected is null || selected.Count == 0)
        {
            this.IsSelected = false;
        }
        else
        {
            int selectedCount = 0;
            foreach (object item in items)
            {
                if (selected.Contains(item))
                {
                    selectedCount++;
                }
            }

            this.IsSelected = selectedCount == 0 ? false : selectedCount == items.Count ? true : null;
        }

        this.updateInternal = false;
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

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        this.IsSelected = this.IsSelected is null or false;
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new ListItemAutomationPeer(this);

    private void OnSelectionChanged(AvaloniaPropertyChangedEventArgs<bool?> args)
    {
        if (this.updateInternal) return;
        if (args.OldValue.Value == args.NewValue.Value) return;

        // The indeterminate state is never a user-selectable target: coming from it always means "select all".
        if (args.OldValue.Value is null && args.NewValue.Value != true)
        {
            this.IsSelected = true;
            return;
        }

        if (this.GroupItems is not { } items)
        {
            return;
        }

        if (args.OldValue.Value != true)
        {
            this.parent?.SelectGroup(items);
            this.IsSelected = true;
        }
        else
        {
            this.parent?.DeselectGroup(items);
        }
    }
}