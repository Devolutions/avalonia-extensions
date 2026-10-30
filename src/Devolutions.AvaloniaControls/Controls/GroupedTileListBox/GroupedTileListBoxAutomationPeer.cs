namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Makes a <see cref="GroupedTileListBox"/> accessible to screen readers and other assistive technologies: a list
/// whose tiles are selectable list items, the way a ListBox is, under headings for their groups. Each tile needs an
/// accessible name: set <c>AutomationProperties.Name</c> on the tiles (a style on <see cref="GroupedTileListBoxItem"/>).
/// </summary>
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator requires preserved types")]
public class GroupedTileListBoxAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
    public GroupedTileListBoxAutomationPeer(GroupedTileListBox owner)
        : base(owner)
    {
    }

    public new GroupedTileListBox Owner => (GroupedTileListBox)base.Owner;

    public bool CanSelectMultiple => this.Owner.SelectionMode.HasFlag(SelectionMode.Multiple);

    public bool IsSelectionRequired => this.Owner.SelectionMode.HasFlag(SelectionMode.AlwaysSelected);

    // Only realized tiles have peers; the list virtualizes the rest. A list nested in a tile's template owns its own.
    public IReadOnlyList<AutomationPeer> GetSelection() =>
        this.Owner.GetVisualDescendants()
            .OfType<GroupedTileListBoxItem>()
            .Where(item => item.IsSelected && item.FindAncestorOfType<GroupedTileListBox>() == this.Owner)
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .ToList();

    // The box calls this once its containers reflect a selection change, including one that keeps SelectedItem.
    internal void RaiseSelectionChanged() => this.RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);

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

    // EnsureEnabled (Avalonia's, as ListBox items use): a disabled list refuses selection requests.
    public void Select()
    {
        this.EnsureEnabled();
        if (this.Owner.Content is { } item)
        {
            this.ListBox?.SelectFromAssistiveTechnology(item);
        }
    }

    public void AddToSelection()
    {
        this.EnsureEnabled();
        if (this.Owner.Content is { } item)
        {
            this.ListBox?.SetSelectedFromAssistiveTechnology(item, true);
        }
    }

    public void RemoveFromSelection()
    {
        this.EnsureEnabled();
        if (this.Owner.Content is { } item)
        {
            this.ListBox?.SetSelectedFromAssistiveTechnology(item, false);
        }
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override bool IsControlElementCore() => true;
}

/// <summary>
/// A group's header as a heading named after the group, so screen readers announce the group and can move between
/// groups. Its level comes from <c>AutomationProperties.HeadingLevel</c>, 2 unless a style sets another.
/// </summary>
public class GroupedTileListBoxGroupHeaderAutomationPeer : ControlAutomationPeer
{
    public GroupedTileListBoxGroupHeaderAutomationPeer(GroupedTileListBoxGroupHeader owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            // The text is the name only while the host sets none; a host-set name's changes are raised by the base peer.
            if (e.Property == GroupedTileListBoxGroupHeader.TextProperty && string.IsNullOrEmpty(AutomationProperties.GetName(owner)))
            {
                this.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, e.OldValue, e.NewValue);
            }
        };
    }

    public new GroupedTileListBoxGroupHeader Owner => (GroupedTileListBoxGroupHeader)base.Owner;

    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : this.Owner.Text;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    // The header's own text block would read the name a second time.
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

    protected override bool IsControlElementCore() => true;
}
