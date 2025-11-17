namespace Devolutions.AvaloniaControls.Controls;

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;

public partial class MultiComboBox
{
    /// <summary>
    /// Internal ItemsControl used to display filtered items in the dropdown.
    /// This allows filtering without affecting the parent MultiComboBox's Items/ItemsSource.
    /// </summary>
    internal class MultiComboBoxItemsList : SelectingItemsControl
    {
        private readonly MultiComboBox parent;

        public MultiComboBoxItemsList(MultiComboBox parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Resets the scroll state to clear any stale VirtualizingStackPanel anchor references.
        /// Called when the dropdown reopens to prevent anchor registration errors.
        /// </summary>
        public void ResetScrollState()
        {
            // Ensure template is applied
            this.ApplyTemplate();

            // Get ScrollViewer from template by name
            var scrollViewer = this.FindNameScope()?.Find<ScrollViewer>("PART_ScrollViewer");
            if (scrollViewer is not null)
            {
                // Reset scroll position
                scrollViewer.Offset = new Vector(0, 0);
            }

            // Force layout invalidation on the panel to clear stale state
            if (this.Presenter?.Panel is Panel panel)
            {
                panel.InvalidateMeasure();
                panel.InvalidateArrange();
            }
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
// ReSharper enable MemberHidesStaticFromOuterClass