namespace Devolutions.AvaloniaControls.Converters;

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Layout;

public static partial class DevoConverters
{
    public static readonly IMultiValueConverter IsVerticalScrollBarVisibleConverter =
        new FuncMultiValueConverter<object, bool>(static values => IsScrollBarVisible(values, static size => size.Height));

    public static readonly IMultiValueConverter IsHorizontalScrollBarVisibleConverter =
        new FuncMultiValueConverter<object, bool>(static values => IsScrollBarVisible(values, static size => size.Width));

    public static bool IsScrollBarVisible(ScrollBarVisibility visibility, Size extent, Size viewport, Orientation orientation) =>
        IsScrollBarVisible(visibility, extent, viewport,
            orientation == Orientation.Vertical ? static size => size.Height : static size => size.Width);

    private static bool IsScrollBarVisible(IEnumerable<object?> values, Func<Size, double> getLength)
    {
        object?[] valueArray = values.ToArray();
        if (valueArray is not [ScrollBarVisibility visibility, Size extent, Size viewport]) return false;

        return IsScrollBarVisible(visibility, extent, viewport, getLength);
    }

    private static bool IsScrollBarVisible(ScrollBarVisibility visibility, Size extent, Size viewport, Func<Size, double> getLength)
    {
        return visibility switch
        {
            ScrollBarVisibility.Visible => true,
            ScrollBarVisibility.Auto => getLength(extent) > getLength(viewport),
            _ => false
        };
    }
}
