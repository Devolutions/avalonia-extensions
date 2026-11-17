namespace Devolutions.AvaloniaControls.Converters;

using System.Globalization;
using Avalonia.Data.Converters;

public class SelectedIndexToPopupOffsetConverter : IMultiValueConverter
{
  public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
  {
    if (values.Count is not (5 or 6)
        || values[0] is not int index
        || values[1] is not int rowHeight
        || values[2] is not int initialFirstItemDistance
        || values[3] is not double maxDropDownHeight
        || values[4] is not int popupTrimHeight
        || index < 0
        || values.Count == 6 && values[5] is true)
    {
      return 0d;
    }

    double effectivePopupHeight = maxDropDownHeight - popupTrimHeight;
    double offset = (index + 1) * -rowHeight - initialFirstItemDistance;
    return -effectivePopupHeight < offset ? offset : -effectivePopupHeight;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotImplementedException();
}