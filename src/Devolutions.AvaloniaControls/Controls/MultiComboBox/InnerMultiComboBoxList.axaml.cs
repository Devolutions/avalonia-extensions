namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;

using Converters;

public partial class MultiComboBox
{
    /// <summary>
    /// Internal ItemsControl used to display filtered items in the dropdown.
    /// This allows filtering without affecting the parent MultiComboBox's Items/ItemsSource.
    /// </summary>
    internal class InnerMultiComboBoxList : SelectingItemsControl
    {
        private static readonly object HeaderRecycleKey = typeof(MultiComboBoxGroupHeader);

        private readonly MultiComboBox parent;

        public InnerMultiComboBoxList(MultiComboBox parent)
        {
            this.parent = parent;
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
            recycleKey == HeaderRecycleKey
                ? new MultiComboBoxGroupHeaderItem()
                : new MultiComboBoxItem();

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            recycleKey = item is MultiComboBoxGroupHeader ? HeaderRecycleKey : DefaultRecycleKey;
            return true;
        }

        protected override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);
            if (element is MultiComboBoxGroupHeaderItem headerContainer)
            {
                headerContainer.ClearValue(MultiComboBoxGroupHeaderItem.ForegroundProperty);
                headerContainer.ClearValue(ContentControl.ContentTemplateProperty);
                headerContainer.Content = null;
                headerContainer.GroupItems = null;
                headerContainer.BeginUpdate();
                headerContainer.ClearValue(MultiComboBoxGroupHeaderItem.IsSelectedProperty);
                headerContainer.EndUpdate();
            }
            else if (element is MultiComboBoxItem multiComboBoxItem)
            {
                multiComboBoxItem.DataContext = null;
                multiComboBoxItem.ClearValue(ContentControl.ContentProperty);
                multiComboBoxItem.ClearValue(ContentControl.ContentTemplateProperty);
                multiComboBoxItem.ClearValue(IsEnabledProperty);
                multiComboBoxItem.BeginUpdate();
                multiComboBoxItem.ClearValue(MultiComboBoxItem.IsSelectedProperty);
                multiComboBoxItem.EndUpdate();
            }
        }

        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            base.PrepareContainerForItemOverride(container, item, index);

            if (container is MultiComboBoxGroupHeaderItem headerContainer)
            {
                if (item is MultiComboBoxGroupHeader header)
                {
                    headerContainer[!MultiComboBoxGroupHeaderItem.ForegroundProperty] = new MultiBinding
                    {
                        Converter = new FirstNonNullValueMultiConverter(),
                        Bindings =
                        [
                            // This method itself uses no dynamic code. It references MultiComboBox.HeaderForegroundProperty
                            // (a StyledProperty field), which merely triggers MultiComboBox's static constructor — that ctor
                            // only registers properties/handlers and is trim/AOT-safe. The warning is inherited from
                            // MultiComboBox's class-level annotation (added for its lazily-invoked BindingEvaluator usage).
#pragma warning disable IL2026
#pragma warning disable IL3050
                            this.parent[!MultiComboBox.HeaderForegroundProperty],
#pragma warning restore IL3050
#pragma warning restore IL2026
                            new DynamicResourceExtension("SystemControlForegroundAccentBrush"),
                        ],
                    };
                    headerContainer.Content = header;
                    headerContainer.GroupItems = header.Items;
                }

                headerContainer.ContentTemplate = this.parent.HeaderTemplate;
                headerContainer.UpdateSelection();
                return;
            }

            if (container is not MultiComboBoxItem multiComboBoxItem)
            {
                return;
            }

            // For inline MultiComboBoxItem data, unwrap Content for display but keep DataContext
            // pointing to the MultiComboBoxItem itself — matching what goes into SelectedItems
            // (consistent with how ComboBox returns ComboBoxItem for inline items).
            if (item is MultiComboBoxItem sourceItem)
            {
                multiComboBoxItem.DataContext = sourceItem;
                multiComboBoxItem[!ContentControl.ContentProperty] = sourceItem[!ContentControl.ContentProperty];
                multiComboBoxItem.ContentTemplate = sourceItem.ContentTemplate ?? this.ItemTemplate;
                multiComboBoxItem[!IsEnabledProperty] = sourceItem[!IsEnabledProperty];
            }

            bool isSelected = this.parent.SelectedItems?.Contains(multiComboBoxItem.DataContext ?? item) ?? false;
            multiComboBoxItem.BeginUpdate();
            multiComboBoxItem.IsSelected = isSelected;
            multiComboBoxItem.EndUpdate();

            // In grouped mode, indent items so they read as children of the group header
            // (mirrors the checkmark-column reservation that ComboBox/GroupedComboBox items have).
            multiComboBoxItem.SetGrouped(this.parent.IsGrouped);
        }
    }
}