namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Exposes a <see cref="GroupedTileListBox"/> to UI Automation as a list whose tiles are selectable list items, the
/// way a ListBox is: a screen reader selects a tile, then the host's default button acts on the selection.
/// </summary>
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator requires preserved types")]
public class GroupedTileListBoxAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
    public GroupedTileListBoxAutomationPeer(GroupedTileListBox owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == GroupedTileListBox.SelectedItemProperty)
            {
                this.RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
            }
        };
    }

    public new GroupedTileListBox Owner => (GroupedTileListBox)base.Owner;

    public bool CanSelectMultiple => this.Owner.SelectionMode.HasFlag(SelectionMode.Multiple);

    public bool IsSelectionRequired => this.Owner.SelectionMode.HasFlag(SelectionMode.AlwaysSelected);

    // Only realized tiles have peers; the list virtualizes the rest.
    public IReadOnlyList<AutomationPeer> GetSelection() =>
        this.Owner.GetVisualDescendants()
            .OfType<GroupedTileListBoxItem>()
            .Where(static item => item.IsSelected)
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .ToList();

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override bool IsControlElementCore() => true;
}

/// <summary>A <see cref="GroupedTileListBoxItem"/> as a selectable list item.</summary>
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator requires preserved types")]
public class GroupedTileListBoxItemAutomationPeer : ContentControlAutomationPeer, ISelectionItemProvider
{
    public GroupedTileListBoxItemAutomationPeer(GroupedTileListBoxItem owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == GroupedTileListBoxItem.IsSelectedProperty)
            {
                this.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, e.OldValue, e.NewValue);
            }
        };
    }

    public new GroupedTileListBoxItem Owner => (GroupedTileListBoxItem)base.Owner;

    public bool IsSelected => this.Owner.IsSelected;

    public ISelectionProvider? SelectionContainer =>
        this.ListBox is { } listBox ? ControlAutomationPeer.CreatePeerForElement(listBox) as ISelectionProvider : null;

    private GroupedTileListBox? ListBox => this.Owner.FindAncestorOfType<GroupedTileListBox>();

    public void Select()
    {
        if (this.Owner.Content is { } item)
        {
            this.ListBox?.SelectFromAutomation(item);
        }
    }

    public void AddToSelection()
    {
        if (this.Owner.Content is { } item)
        {
            this.ListBox?.SetSelectedFromAutomation(item, true);
        }
    }

    public void RemoveFromSelection()
    {
        if (this.Owner.Content is { } item)
        {
            this.ListBox?.SetSelectedFromAutomation(item, false);
        }
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override bool IsControlElementCore() => true;
}
