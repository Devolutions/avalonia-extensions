namespace Devolutions.AvaloniaControls.Controls;

using Avalonia;

public abstract class EnumPickerTextOverride<T> : AvaloniaObject where T : struct, Enum
{
    private T enumValue;

#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPickerTextOverride<T>, T> EnumProperty =
        AvaloniaProperty.RegisterDirect<EnumPickerTextOverride<T>, T>(
            nameof(Enum),
            o => o.Enum,
            (o, v) => o.Enum = v);
#pragma warning restore AVP1002

    public required T Enum
    {
        get => this.enumValue;
        set => this.SetAndRaise(EnumProperty, ref this.enumValue, value);
    }
}

public class EnumPickerDirectTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
    private string text = string.Empty;
    
#pragma warning disable AVP1002
    public static readonly DirectProperty<EnumPickerDirectTextOverride<T>, string> TextProperty =
        AvaloniaProperty.RegisterDirect<EnumPickerDirectTextOverride<T>, string>(
            nameof(Text),
            o => o.Text,
            (o, v) => o.Text = v);
#pragma warning restore AVP1002

    public required string Text
    {
        get => this.text;
        set => this.SetAndRaise(TextProperty, ref this.text, value);
    }
}

public class EnumPickerFormattedTextOverride<T> : EnumPickerTextOverride<T> where T : struct, Enum
{
    private T enumOverride;
    private string format = EnumPicker.DefaultFormat;

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
        get => this.enumOverride;
        set => this.SetAndRaise(EnumOverrideProperty, ref this.enumOverride, value);
    }

    public string Format
    {
        get => this.format;
        set => this.SetAndRaise(FormatProperty, ref this.format, value);
    }
}