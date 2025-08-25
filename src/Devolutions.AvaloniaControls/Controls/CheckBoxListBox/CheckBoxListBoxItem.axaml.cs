namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;
using Avalonia.Interactivity;

public class CheckBoxListBoxItem : CheckBox
{
    protected override void OnIsCheckedChanged(RoutedEventArgs e)
    {
        base.OnIsCheckedChanged(e);

        if (ItemsControl.ItemsControlFromItemContainer(this) is CheckBoxListBox.InnerCheckBoxListBox owner)
        {
            owner.UpdateSelectionFromItem(this);
        }
    }
}