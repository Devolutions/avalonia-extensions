namespace Devolutions.AvaloniaControls.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

/// <summary>
///   Returns a choice based on whether any of the first N-2 values are not null.
/// </summary>
/// <param name="values">A list where the first N-2 values are checked for null, and the last two values are the true/false choices.</param>
/// <returns>The second-to-last value if any input is not null, otherwise the last value.</returns>
/// <remarks>
///   The last two values in the binding are treated as the "true" and "false" choices respectively.
///   All preceding values are checked - if any are not null and not UnsetValue, the "true" choice is returned.
/// </remarks>
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
