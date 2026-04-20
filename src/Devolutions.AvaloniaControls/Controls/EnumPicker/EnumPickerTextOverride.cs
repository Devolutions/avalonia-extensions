namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;

public abstract class EnumPickerTextOverride<T> : AvaloniaObject where T : struct, Enum
{
#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPickerTextOverride<T>, T> EnumProperty =
        AvaloniaProperty.RegisterDirect<EnumPickerTextOverride<T>, T>(
            nameof(Enum),
            o => o.Enum,
            (o, v) => o.Enum = v);
#pragma warning restore AVP1002

    public required T Enum
    {
        get;
        set => this.SetAndRaise(EnumProperty, ref field, value);
    }
}

public class EnumPickerDirectTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPickerDirectTextOverride<T>, string> TextProperty =
        AvaloniaProperty.RegisterDirect<EnumPickerDirectTextOverride<T>, string>(
            nameof(Text),
            o => o.Text,
            (o, v) => o.Text = v);
#pragma warning restore AVP1002

    public required string Text
    {
        get;
        set => this.SetAndRaise(TextProperty, ref field, value);
    } = string.Empty;
}

public class EnumPickerFormattedTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPickerFormattedTextOverride<T>, T> EnumOverrideProperty =
        AvaloniaProperty.RegisterDirect<EnumPickerFormattedTextOverride<T>, T>(
            nameof(EnumOverride),
            o => o.EnumOverride,
            (o, v) => o.EnumOverride = v);
    
    public static readonly DirectProperty<EnumPickerFormattedTextOverride<T>, string> FormatProperty =
        AvaloniaProperty.RegisterDirect<EnumPickerFormattedTextOverride<T>, string>(
            nameof(Format),
            o => o.Format,
            (o, v) => o.Format = v);
#pragma warning restore AVP1002

    public required T EnumOverride
    {
        get;
        set => this.SetAndRaise(EnumOverrideProperty, ref field, value);
    }

    public string Format
    {
        get;
        set => this.SetAndRaise(FormatProperty, ref field, value);
    } = EnumPicker.DefaultFormat;
}