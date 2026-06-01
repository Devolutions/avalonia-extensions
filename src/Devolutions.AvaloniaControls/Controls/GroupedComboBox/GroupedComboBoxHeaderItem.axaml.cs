namespace Devolutions.AvaloniaControls.Controls;

using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

/// <summary>
/// Non-selectable header container inserted into a <see cref="GroupedComboBox"/> popup.
/// </summary>
[RequiresUnreferencedCode("BindingEvaluator require preserved types")]
public class GroupedComboBoxHeaderItem : ComboBoxItem
{
    public GroupedComboBoxHeaderItem()
    {
        this.Focusable = false;
        this.IsEnabled = false;
    }

    protected override Type StyleKeyOverride => typeof(GroupedComboBoxHeaderItem);
}
