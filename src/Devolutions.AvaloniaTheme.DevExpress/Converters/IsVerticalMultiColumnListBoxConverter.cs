namespace Devolutions.AvaloniaTheme.DevExpress.Converters;

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;

public class IsVerticalMultiColumnListBoxConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ListBox { ItemsPanel : ItemsPanelTemplate template })
        {
            Panel? probe = template.Build(); // off-tree probe
            if (probe is WrapPanel wp) return wp.Orientation == Orientation.Vertical;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}