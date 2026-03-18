namespace Devolutions.AvaloniaControls.Controls;

public abstract class EnumPickerTextOverride<T> where T : struct, Enum
{
    public required T Enum { get; init; }
}

public class EnumPickerDirectTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
    public required string Text { get; init; }
}

public class EnumPickerFormattedTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
    public required T EnumOverride { get; init; }

    public string Format { get; init; } = EnumPicker.DefaultFormat;
}