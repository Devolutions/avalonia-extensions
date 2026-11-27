namespace Devolutions.AvaloniaControls.Controls;

using System.Collections.Specialized;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

internal sealed class MultiComboBoxAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider, IValueProvider, ISelectionProvider
{
    private readonly MultiComboBox owner;

    private INotifyCollectionChanged? selectedItems;

    public MultiComboBoxAutomationPeer(MultiComboBox owner)
        : base(owner)
    {
        this.owner = owner;
        this.WatchSelectedItems();
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == MultiComboBox.IsDropDownOpenProperty)
            {
                this.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, ToState((bool)e.OldValue!), ToState((bool)e.NewValue!));
            }
            else if (e.Property == MultiComboBox.SelectedItemsProperty)
            {
                this.WatchSelectedItems();
                this.RaiseValueChanged();
            }
        };
    }

    public ExpandCollapseState ExpandCollapseState => ToState(this.owner.IsDropDownOpen);

    public bool ShowsMenu => false;

    public bool IsReadOnly => true;

    public string? Value => string.Join(", ", this.owner.GetSelectedItemNames());

    public bool CanSelectMultiple => true;

    public bool IsSelectionRequired => false;

    public void Expand()
    {
        this.EnsureEnabled();
        this.owner.SetCurrentValue(MultiComboBox.IsDropDownOpenProperty, true);
    }

    public void Collapse()
    {
        this.EnsureEnabled();
        this.owner.SetCurrentValue(MultiComboBox.IsDropDownOpenProperty, false);
    }

    public void SetValue(string? value) => throw new InvalidOperationException("The value of a MultiComboBox is its selection; select its items instead.");

    public IReadOnlyList<AutomationPeer> GetSelection() => this.owner.GetSelectedItemPeers();

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

    private void WatchSelectedItems()
    {
        if (this.selectedItems is not null)
        {
            this.selectedItems.CollectionChanged -= this.OnSelectedItemsChanged;
        }

        this.selectedItems = this.owner.SelectedItems as INotifyCollectionChanged;
        if (this.selectedItems is not null)
        {
            this.selectedItems.CollectionChanged += this.OnSelectedItemsChanged;
        }
    }

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.RaiseValueChanged();

    private void RaiseValueChanged() => this.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, null, this.Value);

    private static ExpandCollapseState ToState(bool isOpen) => isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
}
