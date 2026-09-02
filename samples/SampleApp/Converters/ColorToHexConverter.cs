namespace SampleApp.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

/// <summary>
///   Formats a <see cref="Color" /> as an sRGB hex string for on-screen readout.
/// </summary>
/// <remarks>
///   Exists so the accent swatches can be read as exact values instead of sampled with an
///   eyedropper. The colour macOS reports to an app is not the swatch drawn in System Settings, and
///   it differs per appearance, so deriving a selection colour from a sampled swatch fits the model
///   against the wrong input. This prints what Avalonia actually resolved, which is what the theme's
///   converters consume.
/// </remarks>
public class ColorToHexConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    Color? color = value switch
    {
      Color c => c,
      ISolidColorBrush brush => brush.Color,
      _ => null,
    };

    if (color is not { } resolved) return "<unresolved>";

    // Alpha first matches Avalonia's own Color.ToString(); the trailing RGB hex is what a colour
    // meter reports, so both forms are shown rather than making the reader convert.
    return resolved.A == 255
      ? $"#{resolved.R:X2}{resolved.G:X2}{resolved.B:X2}"
      : $"#{resolved.R:X2}{resolved.G:X2}{resolved.B:X2} (alpha {resolved.A})";
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
