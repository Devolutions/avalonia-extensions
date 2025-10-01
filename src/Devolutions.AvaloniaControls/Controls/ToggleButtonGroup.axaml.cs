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
}