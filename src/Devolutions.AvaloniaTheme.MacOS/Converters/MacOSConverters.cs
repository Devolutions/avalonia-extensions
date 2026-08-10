namespace Devolutions.AvaloniaTheme.MacOS.Converters;

using Avalonia;
using Avalonia.Data.Converters;

public static partial class MacOSConverters
{
  public static readonly CharToMacOsPasswordCharConverter CharToMacOsPasswordCharConverter = new();
  public static readonly IsVerticalMultiColumnListBoxConverter IsVerticalMultiColumnListBoxConverter = new();
  public static readonly AndToOpacityConverter AndToOpacityConverter = new();

  /// <summary>
  /// Inflates a corner radius by an offset in px (the <c>ConverterParameter</c>), clamped to
  /// <c>&gt;= 0</c> on every corner. Bind a focus ring's radius to the control's own radius through
  /// this so the ring tracks it automatically instead of hard-coding a per-control ring radius:
  /// <code>
  /// CornerRadius="{TemplateBinding CornerRadius,
  ///                Converter={x:Static MacOSConverters.InflateCornerRadius},
  ///                ConverterParameter={StaticResource FocusRingRadiusOffset}}"
  /// </code>
  /// </summary>
  /// <remarks>
  /// The offset is an <em>optical</em> value, not a geometric one. A mathematically-parallel ring
  /// (offset = the ring's outward <c>Margin</c>) reads as too round against high-contrast fills
  /// (accent buttons, selected tabs); native macOS draws the ring a touch tighter, so the theme
  /// uses a slightly smaller offset.
  /// </remarks>
  public static FuncValueConverter<CornerRadius, double, CornerRadius> InflateCornerRadius { get; } =
      new(static (radius, offset) => new CornerRadius(
          Math.Max(0, radius.TopLeft + offset),
          Math.Max(0, radius.TopRight + offset),
          Math.Max(0, radius.BottomRight + offset),
          Math.Max(0, radius.BottomLeft + offset)));
}