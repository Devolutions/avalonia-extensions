namespace Devolutions.AvaloniaControls.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

/// <summary>
///   Returns the first non-null value in the multi-value binding, ignores the rest.
/// </summary>
public class FirstNonNullValueMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.FirstOrDefault(static value => value is not null && value is not UnsetValueType, null);
}