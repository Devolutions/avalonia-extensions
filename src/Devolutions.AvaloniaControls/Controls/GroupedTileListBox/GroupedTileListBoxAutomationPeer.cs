namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Exposes a <see cref="GroupedTileListBox"/> to UI Automation as a selectable, scrollable List. Set
/// <c>AutomationProperties.Name</c> on tiles with a style on <see cref="GroupedTileListBoxItem"/>; otherwise a tile is
/// named after its item's <c>ToString()</c>.
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

    public IReadOnlyList<AutomationPeer> GetSelection() =>
        [.. this.Owner.GetRealizedSelectedContainers().Select(GetOrCreate)];

    public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) =>
        this.Scroller?.Scroll(horizontalAmount, verticalAmount);

    public void SetScrollPercent(double horizontalPercent, double verticalPercent) =>
        this.Scroller?.SetScrollPercent(horizontalPercent, verticalPercent);

    internal void RaiseSelectionChanged() => this.RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override bool IsControlElementCore() => true;

    private IScrollProvider? Scroller =>
        this.Owner.ScrollViewerForAutomation is { } scrollViewer ? GetOrCreate(scrollViewer).GetProvider<IScrollProvider>() : null;
}

/// <summary>Exposes a <see cref="GroupedTileListBoxItem"/> to UI Automation as a selectable ListItem.</summary>
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
