namespace Devolutions.AvaloniaTheme.WinUI.Converters;

using System.Globalization;
using Avalonia.Data.Converters;

public class CharToWinUiPasswordCharConverter : IValueConverter
{
  public static readonly CharToWinUiPasswordCharConverter Instance = new();

  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    const char emptyCharacter = '\0';
    const char passwordBullet = '\u2022';

    return value is char and not emptyCharacter ? passwordBullet : emptyCharacter;
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotImplementedException();
}
