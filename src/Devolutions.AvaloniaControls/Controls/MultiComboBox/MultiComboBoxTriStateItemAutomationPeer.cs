namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

internal sealed class MultiComboBoxTriStateItemAutomationPeer : ContentControlAutomationPeer, IToggleProvider
{
    private readonly ContentControl owner;

    private readonly StyledProperty<bool?> isSelectedProperty;

    public MultiComboBoxTriStateItemAutomationPeer(ContentControl owner, StyledProperty<bool?> isSelectedProperty)
        : base(owner)
    {
        this.owner = owner;
        this.isSelectedProperty = isSelectedProperty;
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == isSelectedProperty)
            {
                this.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, ToState((bool?)e.OldValue), ToState((bool?)e.NewValue));
            }
        };
    }

    public ToggleState ToggleState => ToState(this.owner.GetValue(this.isSelectedProperty));

    // As a click: from unchecked or partly checked, all of them are selected.
    public void Toggle()
    {
        this.EnsureEnabled();
        this.owner.SetValue(this.isSelectedProperty, this.owner.GetValue(this.isSelectedProperty) is null or false);
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    // ContentControlAutomationPeer leaves its control out of the tree; these are what the user interacts with.
    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override string? GetNameCore() => AccessibleContentText.Get(this.owner);

    private static ToggleState ToState(bool? isSelected) => isSelected switch
    {
        true => ToggleState.On,
        false => ToggleState.Off,
        null => ToggleState.Indeterminate,
    };
}
