namespace Devolutions.AvaloniaControls.Converters;

public static class DevoMultiConverters
{
    public static readonly BooleanToChoiceConverter BooleanToChoiceConverter = new();
    public static readonly OptionsToChoiceConverter OptionsToChoiceConverter = new();
    public static readonly ClassToChoiceConverter ClassToChoiceConverter = new();
    public static readonly IsExplicitlyTrueConverter IsExplicitlyTrueConverter = new();
    public static readonly IsUnsetConverter IsUnsetConverter = new();
    public static readonly RevealPasswordToFontSizeConverter RevealPasswordToFontSizeConverter = new();
    public static readonly SelectedIndexToPopupOffsetConverter SelectedIndexToPopupOffsetConverter = new();
    public static readonly TotalWidthConverter TotalWidthConverter = new();
    public static readonly TabBorderVisibilityConverter TabBorderVisibilityConverter = new();
    public static readonly FirstNonNullValueMultiConverter FirstNonNullValueMultiConverter = new();
    public static readonly FirstNonEmptyStringMultiConverter FirstNonEmptyStringMultiConverter = new();
    public static readonly AnyNotNullToChoiceConverter AnyNotNullToChoiceConverter = new();
}