namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.LogicalTree;

// ListItemAutomationPeer reads ListBoxItem.IsSelected, a different property, so it reports every item as unselected.
internal sealed class MultiComboBoxItemAutomationPeer : ContentControlAutomationPeer, ISelectionItemProvider
{
    private readonly MultiComboBoxItem owner;

    public MultiComboBoxItemAutomationPeer(MultiComboBoxItem owner)
        : base(owner)
    {
        this.owner = owner;
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == MultiComboBoxItem.IsSelectedProperty)
            {
                this.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, e.OldValue, e.NewValue);
            }
        };
    }

    public bool IsSelected => this.owner.IsSelected;

    public ISelectionProvider? SelectionContainer =>
        this.owner.FindLogicalAncestorOfType<ItemsControl>() is { } list ? GetOrCreate(list).GetProvider<ISelectionProvider>() : null;

    public void Select()
    {
        this.EnsureEnabled();
        this.owner.FindLogicalAncestorOfType<MultiComboBox>()?.SelectedItems?.Clear();
        this.owner.IsSelected = true;
    }

    public void AddToSelection()
    {
        this.EnsureEnabled();
        this.owner.IsSelected = true;
    }

    public void RemoveFromSelection()
    {
        this.EnsureEnabled();
        this.owner.IsSelected = false;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    // ContentControlAutomationPeer leaves its control out of the tree; these are what the user interacts with.
    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override string? GetNameCore() => AccessibleContentText.Get(this.owner);
}
