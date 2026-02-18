using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Devolutions.AvaloniaControls.Controls;

/// <summary>
/// Container for items in the GroupedTileListBox.
/// Provides selection state and visual feedback.
/// </summary>
[PseudoClasses(":selected")]
public class GroupedTileListBoxItem : ContentControl
{
    /// <summary>
    /// Defines the <see cref="IsSelected"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<GroupedTileListBoxItem, bool>(
            nameof(IsSelected));

    /// <summary>
    /// Defines the <see cref="ItemIndex"/> property.
    /// </summary>
    public static readonly StyledProperty<int> ItemIndexProperty =
        AvaloniaProperty.Register<GroupedTileListBoxItem, int>(
            nameof(ItemIndex), -1);

    /// <summary>
    /// Gets or sets a value indicating whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of this item in the collection.
    /// </summary>
    public int ItemIndex
    {
        get => GetValue(ItemIndexProperty);
        set => SetValue(ItemIndexProperty, value);
    }

    static GroupedTileListBoxItem()
    {
        IsSelectedProperty.Changed.AddClassHandler<GroupedTileListBoxItem>(
            (x, e) => x.OnIsSelectedChanged(e));
    }

    private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        PseudoClasses.Set(":selected", IsSelected);
    }
}
