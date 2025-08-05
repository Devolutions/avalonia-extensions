namespace Devolutions.AvaloniaControls.Converters;

using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
///     Returns the first non-empty string in the multi-value binding, ignores the rest.
/// </summary>
public class FirstNonEmptyStringMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.FirstOrDefault(static value => value is string s && !string.IsNullOrEmpty(s), null);
    }
}