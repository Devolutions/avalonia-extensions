namespace Devolutions.AvaloniaControls.Converters;

using Avalonia.Data.Converters;

public static partial class DevoConverters
{
    public static FuncValueConverter<object?, bool> IsOddConverter { get; } =
        new(static value => value is int i && i % 2 == 1);
}