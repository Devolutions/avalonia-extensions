namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;

/// <summary>
/// Non-selectable header container inserted into a <see cref="GroupedComboBox"/> popup.
/// </summary>
public class GroupedComboBoxItem : ComboBoxItem
{
    protected override Type StyleKeyOverride => typeof(ComboBoxItem);
}
