namespace Devolutions.AvaloniaTheme.WinUI.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

public sealed class CalendarWeekDayConverter : IMultiValueConverter
{
    public static readonly CalendarWeekDayConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Any(value => value == AvaloniaProperty.UnsetValue))
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (values.Count != 2 || values[0] is not int column || column is < 0 or > 6 ||
            values[1] is not DayOfWeek firstDay || !Enum.IsDefined(firstDay))
        {
            return new BindingNotification(
                new ArgumentException("Calendar weekday labels require a column from 0 to 6 and a valid first day of the week."),
                BindingErrorType.Error);
        }

        var day = (DayOfWeek)((column + (int)firstDay) % 7);
        var labels = culture.DateTimeFormat.AbbreviatedDayNames.Select(label =>
        {
            var textElements = new StringInfo(label);
            return textElements.SubstringByTextElements(0, Math.Min(2, textElements.LengthInTextElements));
        }).ToArray();
        return labels.Distinct(StringComparer.Ordinal).Count() == 7
            ? labels[(int)day]
            : culture.DateTimeFormat.GetShortestDayName(day);
    }
}
