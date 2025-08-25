namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;

public partial class CheckBoxListBox
{
    public class InnerCheckBoxListBox : ListBox
    {
        static InnerCheckBoxListBox()
        {
            SelectionModeProperty.OverrideDefaultValue<InnerCheckBoxListBox>(SelectionMode.Multiple | SelectionMode.Toggle);
        }

        public InnerCheckBoxListBox()
        {
            this.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
            new CheckBoxListBoxItem();

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
            this.NeedsContainer<CheckBoxListBoxItem>(item, out recycleKey);

        internal void UpdateSelectionFromItem(CheckBoxListBoxItem item)
        {
            int index = this.IndexFromContainer(item);
            if (index < 0) return;

            if (item.IsChecked ?? false)
            {
                this.Selection.Select(index);
            }
            else
            {
                this.Selection.Deselect(index);
            }
        }
    }
}