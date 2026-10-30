namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Makes a <see cref="GroupedTileListBox"/> accessible to screen readers and other assistive technologies the way
/// Avalonia's ListBox is (<c>SelectingItemsControlAutomationPeer</c>, <c>ItemsControlAutomationPeer</c>): a List with
/// a selection, scrollable through its ScrollViewer, whose tiles are list items in a Group per group. Each tile needs
/// an accessible name: set <c>AutomationProperties.Name</c> on the tiles (a style on <see cref="GroupedTileListBoxItem"/>),
/// or Avalonia names a tile after its item's <c>ToString()</c>, as it does a ListBox item.
/// </summary>
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
[RequiresDynamicCode("BindingEvaluator requires preserved types")]
public class GroupedTileListBoxAutomationPeer : ControlAutomationPeer, ISelectionProvider, IScrollProvider
{
    public GroupedTileListBoxAutomationPeer(GroupedTileListBox owner)
        : base(owner)
    {
    }

    public new GroupedTileListBox Owner => (GroupedTileListBox)base.Owner;

    public bool CanSelectMultiple => this.Owner.SelectionMode.HasFlag(SelectionMode.Multiple);

    public bool IsSelectionRequired => this.Owner.SelectionMode.HasFlag(SelectionMode.AlwaysSelected);

    public bool HorizontallyScrollable => this.Scroller?.HorizontallyScrollable ?? false;

    public double HorizontalScrollPercent => this.Scroller?.HorizontalScrollPercent ?? -1;

    public double HorizontalViewSize => this.Scroller?.HorizontalViewSize ?? 0;

    public bool VerticallyScrollable => this.Scroller?.VerticallyScrollable ?? false;

    public double VerticalScrollPercent => this.Scroller?.VerticalScrollPercent ?? -1;

    public double VerticalViewSize => this.Scroller?.VerticalViewSize ?? 0;

    // As SelectingItemsControlAutomationPeer: the selected items whose tiles are realized (the list virtualizes).
    public IReadOnlyList<AutomationPeer> GetSelection() =>
        [.. this.Owner.GetRealizedSelectedContainers().Select(GetOrCreate)];

    public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) =>
        this.Scroller?.Scroll(horizontalAmount, verticalAmount);

    public void SetScrollPercent(double horizontalPercent, double verticalPercent) =>
        this.Scroller?.SetScrollPercent(horizontalPercent, verticalPercent);

    // The list calls this once its tiles reflect a selection change, as Avalonia's peer does on SelectionChanged.
    internal void RaiseSelectionChanged() => this.RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override bool IsControlElementCore() => true;

    // As ItemsControlAutomationPeer: scrolling goes through the ScrollViewer's own peer.
    private IScrollProvider? Scroller =>
        this.Owner.ScrollViewerForAutomation is { } scrollViewer ? GetOrCreate(scrollViewer).GetProvider<IScrollProvider>() : null;
}

/// <summary>A <see cref="GroupedTileListBoxItem"/> as a selectable list item, as Avalonia's <c>ListItemAutomationPeer</c>.</summary>
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
        this.ListBox is { } listBox ? GetOrCreate(listBox).GetProvider<ISelectionProvider>() : null;

    private GroupedTileListBox? ListBox => this.Owner.FindAncestorOfType<GroupedTileListBox>();

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

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
