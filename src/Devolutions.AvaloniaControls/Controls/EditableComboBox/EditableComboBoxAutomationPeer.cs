namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;

// The combo box that represents an EditableComboBox. Expand/collapse and selection stay ComboBox's; the value is the
// field's text, as for an editable ComboBox, so a typed entry is reported and can be set, not only a selected item.
internal sealed class EditableComboBoxAutomationPeer : ComboBoxAutomationPeer, IValueProvider
{
    private readonly TextBox textBox;

    private bool isReadOnly;

    public EditableComboBoxAutomationPeer(ComboBox owner, TextBox textBox)
        : base(owner)
    {
        this.textBox = textBox;
        this.isReadOnly = this.IsTextReadOnly();
        textBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                this.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
            }
            else if (e.Property == TextBox.IsReadOnlyProperty || e.Property == InputElement.IsEffectivelyEnabledProperty)
            {
                this.UpdateReadOnly();
            }
        };
    }

    bool IValueProvider.IsReadOnly => this.IsTextReadOnly();

    private bool IsTextReadOnly() => this.textBox.IsReadOnly || !this.textBox.IsEffectivelyEnabled;

    // Clients cache whether the value can be edited, so a change after the peer exists is announced.
    private void UpdateReadOnly()
    {
        bool wasReadOnly = this.isReadOnly;
        this.isReadOnly = this.IsTextReadOnly();
        if (wasReadOnly != this.isReadOnly)
        {
            this.RaisePropertyChangedEvent(ValuePatternIdentifiers.IsReadOnlyProperty, wasReadOnly, this.isReadOnly);
        }
    }

    // The combo box itself never takes focus; focusing the field means focusing its edit, as keyboard focus does.
    protected override bool IsKeyboardFocusableCore() => this.textBox.Focusable;

    protected override void SetFocusCore() => this.textBox.Focus();

    string? IValueProvider.Value => this.textBox.Text;

    // Through the text box, as typing does, so filtering and custom-value rules apply the same way.
    void IValueProvider.SetValue(string? value)
    {
        if (((IValueProvider)this).IsReadOnly)
        {
            throw new ElementNotEnabledException();
        }

        this.textBox.SetCurrentValue(TextBox.TextProperty, value);
    }
}
