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
            // Always generate containers, even for inline MultiComboBoxItem data.
            // This enables VSP virtualization for all item types.
            recycleKey = DefaultRecycleKey;
            return true;
        }

        protected override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);
            if (element is MultiComboBoxItem multiComboBoxItem)
            {
                // Suppress OnSelectionChanged — container lifecycle must never mutate SelectedItems.
                multiComboBoxItem.BeginUpdate();
                multiComboBoxItem.ClearValue(MultiComboBoxItem.IsSelectedProperty);
                multiComboBoxItem.EndUpdate();
                multiComboBoxItem.ClearValue(ContentControl.ContentTemplateProperty);
                multiComboBoxItem.ClearValue(IsEnabledProperty);
            }
        }

        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            base.PrepareContainerForItemOverride(container, item, index);

            if (container is not MultiComboBoxItem multiComboBoxItem)
            {
                return;
            }

            // For inline MultiComboBoxItem data, unwrap Content for display but keep DataContext
            // pointing to the MultiComboBoxItem itself — matching what goes into SelectedItems
            // (consistent with how ComboBox returns ComboBoxItem for inline items).
            if (item is MultiComboBoxItem sourceItem)
            {
                multiComboBoxItem.Content = sourceItem.Content;
                multiComboBoxItem.DataContext = sourceItem;
                multiComboBoxItem.ContentTemplate ??= sourceItem.ContentTemplate ?? this.ItemTemplate;
                multiComboBoxItem.IsEnabled = sourceItem.IsEnabled;
            }

            // Suppress OnSelectionChanged — this is a programmatic sync, not user interaction.
            multiComboBoxItem.BeginUpdate();
            bool isSelected = this.parent.SelectedItems?.Contains(multiComboBoxItem.DataContext ?? item) ?? false;
            multiComboBoxItem.IsSelected = isSelected;
            multiComboBoxItem.EndUpdate();
        }
    }
}