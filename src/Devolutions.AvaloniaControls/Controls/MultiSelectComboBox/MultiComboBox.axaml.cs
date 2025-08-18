namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;

public class MultiComboBox : Ursa.Controls.MultiComboBox
{
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = item;
        return item is not MultiComboBoxItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new MultiComboBoxItem();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (item is MultiComboBoxItem containerItem)
        {
            container.DataContext = containerItem.Content;
            return;
        }

        base.PrepareContainerForItemOverride(container, item, index);
    }
}