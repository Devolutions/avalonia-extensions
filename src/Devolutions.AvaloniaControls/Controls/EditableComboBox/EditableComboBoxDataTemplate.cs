namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;

public class EditableComboBoxDataTemplate : DataTemplate
{
    /// <summary>
    /// Binding evaluated against each item to produce the text-box display string.
    /// This is usually the textual representation of the selected item.
    /// Example: ItemStringValue="{Binding Name}"
    /// </summary>
    public IBinding? SelectedItemValue { get; set; }
}
