namespace Devolutions.AvaloniaTheme.MacOS.Converters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

public class AndToOpacityConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.All(x => x is true))
        {
            return 1.0;
        }

        return 0.0;
    }
}
