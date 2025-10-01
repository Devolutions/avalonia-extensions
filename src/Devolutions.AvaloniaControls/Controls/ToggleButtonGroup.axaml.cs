namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;
using Avalonia.Controls;

public class ToggleButtonGroup : ListBox
{
    public static readonly StyledProperty<double?> ItemsMinWidthProperty =
        AvaloniaProperty.Register<ToggleButtonGroup, double?>(nameof(ItemsMinWidth));

    static ToggleButtonGroup()
    {
        SelectionModeProperty.OverrideDefaultValue<ToggleButtonGroup>(SelectionMode.Single | SelectionMode.AlwaysSelected);
    }

    public double? ItemsMinWidth
    {
        get => this.GetValue(ItemsMinWidthProperty);
        set => this.SetValue(ItemsMinWidthProperty, value);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new ToggleButtonGroupItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        this.NeedsContainer<ToggleButtonGroupItem>(item, out recycleKey);
}