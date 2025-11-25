using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Devolutions.AvaloniaControls.Converters;

/// <summary>
/// Converter that returns true if the value matches any of the comma-separated options in the parameter.
/// </summary>
public class IsOneOfConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter is not string paramString)
            return false;

        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString))
            return false;

        var options = paramString
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));

        return options.Contains(valueString, StringComparer.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("IsOneOfConverter does not support two-way binding.");
    }
}
