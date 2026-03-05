namespace Devolutions.AvaloniaControls.Converters;

using Avalonia;
using Avalonia.Data.Converters;

public static partial class DevoConverters
{
    public static FuncValueConverter<Thickness, ThicknessSide, double> ThicknessNumericExtractor { get; } = new(
        static (thickness, side) => side switch {
            ThicknessSide.Left => thickness.Left,
            ThicknessSide.Top => thickness.Top,
            ThicknessSide.Right => thickness.Right,
            ThicknessSide.Bottom => thickness.Bottom,
            _ => 0,
        });
}

[Flags]
public enum ThicknessSide
{
    Left,
    Top,
    Right,
    Bottom,
}