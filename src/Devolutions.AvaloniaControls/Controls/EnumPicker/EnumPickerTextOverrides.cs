namespace Devolutions.AvaloniaControls.Controls;

public abstract class EnumPickerTextOverride<T> where T : struct, Enum
{
    public required T Enum { get; init; }
}

public class EnumPickerDirectTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
    public required string Text { get; init; }
}

public class EnumPickerProxiedTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
    public required T EnumProxy { get; init; }

    public string Format { get; init; } = EnumPicker.DefaultFormat;
}

public class EnumPickerTextOverrides<T> : List<EnumPickerTextOverride<T>> where T : struct, Enum
{
}