namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;

public partial class MultiComboBox
{
    /// <summary>
    /// Internal ItemsControl used to display filtered items in the dropdown.
    /// This allows filtering without affecting the parent MultiComboBox's Items/ItemsSource.
    /// </summary>
    internal class InnerMultiComboBoxList : SelectingItemsControl
    {
        private readonly MultiComboBox parent;

        public InnerMultiComboBoxList(MultiComboBox parent)
        {
            this.parent = parent;
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
            new MultiComboBoxItem();

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            recycleKey = item;
            return item is not MultiComboBoxItem;
        }

        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            if (item is MultiComboBoxItem containerItem)
            {
                container.DataContext = containerItem.Content;
                containerItem.ContentTemplate ??= this.ItemTemplate;
            }

            base.PrepareContainerForItemOverride(container, item, index);

            // Sync selection state from parent
            if (container is MultiComboBoxItem multiComboBoxItem)
            {
                // Check if this item is selected in parent
                bool isSelected = this.parent.SelectedItems?.Contains(multiComboBoxItem.DataContext ?? item) ?? false;
                multiComboBoxItem.IsSelected = isSelected;
            }
        }
    }
}