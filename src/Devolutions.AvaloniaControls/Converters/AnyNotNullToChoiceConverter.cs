using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Devolutions.AvaloniaControls.Converters;

public class AnyNotNullToChoiceConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var trueChoice = values[values.Count - 2];
        var falseChoice = values[values.Count - 1];

        for (int i = 0; i < values.Count - 2; i++)
        {
            var val = values[i];
            if (val is not null && val != AvaloniaProperty.UnsetValue)
            {
                return trueChoice;
            }
        }

        return falseChoice;
    }

    public object ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
