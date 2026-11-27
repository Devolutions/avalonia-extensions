namespace Devolutions.AvaloniaControls.Controls;

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Threading;

internal sealed class MultiComboBoxAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider, IValueProvider, ISelectionProvider
{
    private readonly MultiComboBox owner;

    private readonly Dictionary<object, SelectedItemPeer> unrealizedSelection = [];

    private INotifyCollectionChanged? selectedItems;

    private string? announcedValue;

    public MultiComboBoxAutomationPeer(MultiComboBox owner)
        : base(owner)
    {
        this.owner = owner;
        this.WatchSelectedItems();
        this.announcedValue = this.Value;
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == MultiComboBox.IsDropDownOpenProperty)
            {
                this.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, ToState((bool)e.OldValue!), ToState((bool)e.NewValue!));
            }
            else if (e.Property == MultiComboBox.SelectedItemsProperty)
            {
                this.WatchSelectedItems();
                this.RaiseValueChangedAfterLayout();
            }
        };
    }

    public ExpandCollapseState ExpandCollapseState => ToState(this.owner.IsDropDownOpen);

    public bool ShowsMenu => false;

    public bool IsReadOnly => true;

    public string? Value => string.Join(", ", this.SelectedItems().Select(this.owner.GetItemText).OfType<string>());

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

    // Every selected item, whether or not the drop-down is open or its row is realized.
    public IReadOnlyList<AutomationPeer> GetSelection()
    {
        object[] selected = [.. this.SelectedItems()];
        foreach (object gone in this.unrealizedSelection.Keys.Except(selected).ToList())
        {
            this.unrealizedSelection.Remove(gone);
        }

        return selected
            .Select(item => this.owner.GetRealizedItemContainer(item) is { } row
                ? CreatePeerForElement(row)
                : this.UnrealizedPeer(item))
            .ToList();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

    private IEnumerable<object> SelectedItems() => this.owner.SelectedItems?.Cast<object>() ?? [];

    private AutomationPeer UnrealizedPeer(object item)
    {
        if (!this.unrealizedSelection.TryGetValue(item, out SelectedItemPeer? peer))
        {
            peer = new SelectedItemPeer(this, item);
            this.unrealizedSelection[item] = peer;
        }

        return peer;
    }

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

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.RaiseValueChangedAfterLayout();

    // The chips update after SelectedItems does, and they supply the shown text.
    private void RaiseValueChangedAfterLayout() => Dispatcher.UIThread.Post(
        () =>
        {
            string? value = this.Value;
            if (value != this.announcedValue)
            {
                this.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, this.announcedValue, value);
                this.announcedValue = value;
            }
        },
        DispatcherPriority.Loaded);

    private static ExpandCollapseState ToState(bool isOpen) => isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    // A selected item without a realized row, as Avalonia's ComboBox reports its closed selection.
    private sealed class SelectedItemPeer(MultiComboBoxAutomationPeer combo, object item) : UnrealizedElementAutomationPeer, ISelectionItemProvider
    {
        public bool IsSelected => combo.owner.SelectedItems?.Contains(item) == true;

        public ISelectionProvider SelectionContainer => combo;

        public void Select()
        {
            combo.EnsureEnabled();
            combo.owner.SelectedItems?.Clear();
            combo.owner.SelectedItems?.Add(item);
        }

        public void AddToSelection()
        {
            combo.EnsureEnabled();
            if (!this.IsSelected)
            {
                combo.owner.SelectedItems?.Add(item);
            }
        }

        public void RemoveFromSelection()
        {
            combo.EnsureEnabled();
            combo.owner.SelectedItems?.Remove(item);
        }

        protected override string? GetAcceleratorKeyCore() => null;

        protected override string? GetAccessKeyCore() => null;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

        protected override string? GetAutomationIdCore() => null;

        protected override string GetClassNameCore() => nameof(MultiComboBoxItem);

        protected override AutomationPeer? GetLabeledByCore() => null;

        protected override string? GetNameCore() => combo.owner.GetItemText(item);

        protected override AutomationPeer? GetParentCore() => combo;
    }
}
