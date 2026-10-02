namespace Devolutions.AvaloniaControls.Converters;

using Avalonia.Data.Converters;

public static partial class DevoDoubleConverters
{
  public static readonly IValueConverter Negate =
    new FuncValueConverter<double, double>(static value => -value);

  public static readonly IMultiValueConverter Multiply =
    new FuncMultiValueConverter<double, double>(static e => e.Aggregate(1.0, static (x, y) => x * y));

  public static readonly IMultiValueConverter Add =
    new FuncMultiValueConverter<double, double>(static e => e.Aggregate(0.0, static (x, y) => x + y));
}