using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Devolutions.AvaloniaTheme.MacOS.Converters;

public class OklchAdjustmentConverter : IValueConverter
{
    /// <summary>
    /// Delta to add to the Lightness (0-1 scale).
    /// </summary>
    public double LightnessAdjustment { get; set; }

    /// <summary>
    /// Delta to add to the Chroma.
    /// </summary>
    public double ChromaAdjustment { get; set; }

    /// <summary>
    /// Degrees to offset Hue.
    /// </summary>
    public double HueAdjustment { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            // Normalize RGB to 0-1
            (double l, double c, double h) = RgbToOklch(color.R / 255.0, color.G / 255.0, color.B / 255.0);

            // Adjust
            l = Math.Clamp(l + this.LightnessAdjustment, 0, 1);
            c = Math.Max(0, c + this.ChromaAdjustment);
            h = (h + this.HueAdjustment) % 360.0;
            if (h < 0) h += 360.0;

            (double r, double g, double b) = OklchToRgb(l, c, h);

            // Clamp RGB to 0-1
            r = Math.Clamp(r, 0, 1);
            g = Math.Clamp(g, 0, 1);
            b = Math.Clamp(b, 0, 1);

            Color newColor = new Color(color.A, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));

            System.Console.WriteLine($"[OklchConverter] In: {color} | LCH: {l:F4}, {c:F4}, {h:F2} | Out: {newColor}");

            if (targetType == typeof(IBrush))
            {
                return new SolidColorBrush(newColor);
            }

            return newColor;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    // --- Math Conversions ---

    private static (double l, double c, double h) RgbToOklch(double r, double g, double b)
    {
        // 1. sRGB to Linear RGB
        double lr = SrgbToLinear(r);
        double lg = SrgbToLinear(g);
        double lb = SrgbToLinear(b);

        // 2. Linear RGB to LMS
        double l1 = 0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb;
        double m1 = 0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb;
        double s1 = 0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb;

        // 3. Cube root (non-linearity)
        double l_ = Math.Cbrt(l1);
        double m_ = Math.Cbrt(m1);
        double s_ = Math.Cbrt(s1);

        // 4. LMS to Oklab
        double L = 0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_;
        double a = 1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_;
        double b_ = 0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_;

        // 5. Oklab to Oklch
        double C = Math.Sqrt(a * a + b_ * b_);
        double H = Math.Atan2(b_, a) * 180.0 / Math.PI;
        if (H < 0) H += 360.0;

        return (L, C, H);
    }

    private static (double r, double g, double b) OklchToRgb(double l, double c, double h)
    {
        // 1. Oklch to Oklab
        double hRad = h * Math.PI / 180.0;
        double L = l;
        double a = c * Math.Cos(hRad);
        double b_ = c * Math.Sin(hRad);

        // 2. Oklab to LMS
        double l_ = L + 0.3963377774 * a + 0.2158037573 * b_;
        double m_ = L - 0.1055613458 * a - 0.0638541728 * b_;
        double s_ = L - 0.0894841775 * a - 1.2914855480 * b_;

        // 3. Cube (linearity)
        double l1 = l_ * l_ * l_;
        double m1 = m_ * m_ * m_;
        double s1 = s_ * s_ * s_;

        // 4. LMS to Linear RGB
        double lr = +4.0767416621 * l1 - 3.3077115913 * m1 + 0.2309699292 * s1;
        double lg = -1.2684380046 * l1 + 2.6097574011 * m1 - 0.3413193965 * s1;
        double lb = -0.0041960863 * l1 - 0.7034186147 * m1 + 1.7076147010 * s1;

        // 5. Linear RGB to sRGB
        return (LinearToSrgb(lr), LinearToSrgb(lg), LinearToSrgb(lb));
    }

    private static double SrgbToLinear(double c)
    {
        return (c >= 0.04045) 
            ? Math.Pow((c + 0.055) / 1.055, 2.4) 
            : c / 12.92;
    }

    private static double LinearToSrgb(double c)
    {
        return (c >= 0.0031308) 
            ? 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055 
            : 12.92 * c;
    }
}
