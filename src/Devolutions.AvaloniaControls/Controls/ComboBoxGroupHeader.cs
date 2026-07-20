namespace Devolutions.AvaloniaControls.Controls;

/// <summary>
/// Non-selectable pseudo-item inserted into a combo box's grouped item list to represent a group header.
/// Shared by <see cref="GroupedComboBox"/> and <see cref="EditableComboBox"/>.
/// </summary>
internal readonly struct ComboBoxGroupHeader(string? text)
{
    public readonly string? text = text;

    public override string ToString() => this.text ?? string.Empty;
}
