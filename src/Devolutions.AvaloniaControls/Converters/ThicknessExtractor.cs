namespace Devolutions.AvaloniaControls.Converters;

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data.Converters;

public static partial class DevoConverters
{
  public static FuncValueConverter<Thickness, ThicknessSubset, Thickness> ThicknessExtractor { get; } =
    new(static (thickness, subset) => new Thickness(
      subset.HasFlag(ThicknessSubset.Left) ? thickness.Left : 0,
      subset.HasFlag(ThicknessSubset.Top) ? thickness.Top : 0,
      subset.HasFlag(ThicknessSubset.Right) ? thickness.Right : 0,
      subset.HasFlag(ThicknessSubset.Bottom) ? thickness.Bottom : 0));
}

[Flags]
[SuppressMessage("StyleCop.CSharp.SpacingRules",
  "SA1025:Code should not contain multiple whitespace in a row",
  Justification = "Clearer for bitflags")]
public enum ThicknessSubset
{
  Left = 0b_0001,
  Top = 0b_0010,
  Right = 0b_0100,
  Bottom = 0b_1000,

  AllButLeft = Bottom | Right | Top,
  AllButTop = Bottom | Right | Left,
  AllButRight = Bottom | Top | Left,
  AllButBottom = Right | Top | Left,
  
  Horizontal = Left | Right,
  Vertical = Top | Bottom,
  
  All = Left | Top | Right | Bottom,
}