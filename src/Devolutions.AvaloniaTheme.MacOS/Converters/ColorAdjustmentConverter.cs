using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Devolutions.AvaloniaTheme.MacOS.Converters;

public class ColorAdjustmentConverter : IValueConverter
{
  /// <summary>
  /// Percentage to add to the saturation (e.g. -20 to subtract 20%).
  /// </summary>
  public double SaturationAdjustment { get; set; }

  /// <summary>
  /// Percentage to add to the brightness/value (e.g. -33 to subtract 33%).
  /// </summary>
  public double BrightnessAdjustment { get; set; }

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is Color color)
    {
      (double h, double s, double v) = ColorToHsv(color);

      // Adjust Saturation
      s = Math.Clamp(s + this.SaturationAdjustment / 100.0, 0, 1);

      // Adjust Brightness (Value)
      v = Math.Clamp(v + this.BrightnessAdjustment / 100.0, 0, 1);

      Color newColor = HsvToColor(color.A, h, s, v);

      if (targetType == typeof(IBrush))
      {
        return new SolidColorBrush(newColor);
      }

      return newColor;
      // return newColor;
    }

    return value;
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotImplementedException();

  private static (double h, double s, double v) ColorToHsv(Color color)
  {
    double r = color.R / 255.0;
    double g = color.G / 255.0;
    double b = color.B / 255.0;

    double max = Math.Max(r, Math.Max(g, b));
    double min = Math.Min(r, Math.Min(g, b));
    double delta = max - min;

    double h = 0;
    if (delta > 0)
    {
      if (Math.Abs(max - r) < 1e-9)
      {
        h = 60 * ((g - b) / delta % 6);
      }
      else if (Math.Abs(max - g) < 1e-9)
      {
        h = 60 * ((b - r) / delta + 2);
      }
      else
      {
        h = 60 * ((r - g) / delta + 4);
      }
    }

    if (h < 0) h += 360;

    double s = max <= double.Epsilon ? 0 : delta / max;
    double v = max;

    return (h, s, v);
  }

  private static Color HsvToColor(byte a, double h, double s, double v)
  {
    double c = v * s;
    double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
    double m = v - c;

    var (r, g, b) = h switch
    {
      >= 0 and < 60 => (c, x, 0.0),
      >= 60 and < 120 => (x, c, 0.0),
      >= 120 and < 180 => (0.0, c, x),
      >= 180 and < 240 => (0.0, x, c),
      >= 240 and < 300 => (x, 0.0, c),
      _ => (c, 0.0, x)
    };

    return Color.FromArgb(
      a,
      (byte)((r + m) * 255),
      (byte)((g + m) * 255),
      (byte)((b + m) * 255));
  }
}