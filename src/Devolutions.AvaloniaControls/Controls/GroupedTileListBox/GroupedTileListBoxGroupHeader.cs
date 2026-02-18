using Avalonia;
using Avalonia.Controls.Primitives;

namespace Devolutions.AvaloniaControls.Controls;

/// <summary>
/// Header control for groups in the GroupedTileListBox.
/// Displays the group name/key as text.
/// </summary>
public class GroupedTileListBoxGroupHeader : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<GroupedTileListBoxGroupHeader, string?>(
            nameof(Text));

    /// <summary>
    /// Gets or sets the header text (group name/key).
    /// </summary>
    public string? Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }
}