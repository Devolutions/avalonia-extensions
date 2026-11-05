namespace Devolutions.AvaloniaTheme.WinUI.Internal;

using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Layout;

public static class ListBoxConverters
{
    public static readonly FuncValueConverter<Orientation?, ScrollBarVisibility> HorizontalScrollBarVisibility =
        new(orientation => orientation == Orientation.Vertical ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled);

    public static readonly FuncValueConverter<Orientation?, ScrollBarVisibility> VerticalScrollBarVisibility =
        new(orientation => orientation == Orientation.Vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);

    public static readonly FuncValueConverter<double, double> SelectionIndicatorHeight =
        new(GetSelectionIndicatorHeight);

    public static readonly FuncValueConverter<double, double> SelectionIndicatorPressedHeight =
        new(height => Math.Max(0, GetSelectionIndicatorHeight(height) - 6));

    // WinUI ListViewBaseItemChrome keeps 20px above/below tall indicators, with a 16px minimum.
    private static double GetSelectionIndicatorHeight(double height) =>
        Math.Min(height, Math.Max(16, height - 40));
}
