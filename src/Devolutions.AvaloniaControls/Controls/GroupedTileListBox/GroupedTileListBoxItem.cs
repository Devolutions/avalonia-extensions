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
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<GroupedTileListBoxItem, bool>(
            nameof(IsSelected));

    public static readonly StyledProperty<int> ItemIndexProperty =
        AvaloniaProperty.Register<GroupedTileListBoxItem, int>(
            nameof(ItemIndex),
            -1);

    static GroupedTileListBoxItem()
    {
        IsSelectedProperty.Changed.AddClassHandler<GroupedTileListBoxItem>((x, e) => x.OnIsSelectedChanged(e));
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => this.GetValue(IsSelectedProperty);
        set => this.SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of this item in the collection.
    /// </summary>
    public int ItemIndex
    {
        get => this.GetValue(ItemIndexProperty);
        set => this.SetValue(ItemIndexProperty, value);
    }

    private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        this.PseudoClasses.Set(":selected", this.IsSelected);
    }
}