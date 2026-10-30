namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using System.Diagnostics.CodeAnalysis;

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

        // Keyboard focus moves onto the tiles, as screen readers announce the focused element. The list makes one
        // tile at a time the Tab stop, so Tab enters and leaves the list in one step.
        FocusableProperty.OverrideDefaultValue<GroupedTileListBoxItem>(true);
        IsTabStopProperty.OverrideDefaultValue<GroupedTileListBoxItem>(false);
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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only GroupedTileListBox, which carries the same requirement, creates its items.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only GroupedTileListBox, which carries the same requirement, creates its items.")]
    protected override AutomationPeer OnCreateAutomationPeer() => new GroupedTileListBoxItemAutomationPeer(this);

    private void OnIsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        this.PseudoClasses.Set(":selected", this.IsSelected);
    }
}