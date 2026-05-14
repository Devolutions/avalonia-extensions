namespace Devolutions.AvaloniaControls.AttachedProperties;

using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;

public static class ComboBoxContent
{
    public static readonly AttachedProperty<IEnumerable> InnerLeftContentProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IEnumerable>("InnerLeftContent", typeof(ComboBoxContent));

    public static readonly AttachedProperty<IEnumerable> InnerLeftOfDropDownArrowContentProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IEnumerable>("InnerLeftOfDropDownArrowContent", typeof(ComboBoxContent));

    public static readonly AttachedProperty<IEnumerable> InnerRightContentProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IEnumerable>("InnerRightContent", typeof(ComboBoxContent));

    public static IEnumerable GetInnerLeftContent(ComboBox element) => GetOrCreate(element, InnerLeftContentProperty);

    public static void SetInnerLeftContent(ComboBox element, IEnumerable value) => element.SetValue(InnerLeftContentProperty, value);

    public static IEnumerable GetInnerLeftOfDropDownArrowContent(ComboBox element) =>
        GetOrCreate(element, InnerLeftOfDropDownArrowContentProperty);

    public static void SetInnerLeftOfDropDownArrowContent(ComboBox element, IEnumerable value) =>
        element.SetValue(InnerLeftOfDropDownArrowContentProperty, value);

    public static IEnumerable GetInnerRightContent(ComboBox element) => GetOrCreate(element, InnerRightContentProperty);

    public static void SetInnerRightContent(ComboBox element, IEnumerable value) => element.SetValue(InnerRightContentProperty, value);

    // Attached-property defaults are shared across all instances, which is not what we want for
    // mutable collections. Lazily create a per-instance AvaloniaList<Control> on first read so
    // XAML element-syntax (<ComboBoxContentSlots.InnerLeftContent>...</...>) can Add into it.
    private static IEnumerable GetOrCreate(ComboBox element, AttachedProperty<IEnumerable> property)
    {
        if (element.GetValue(property) is { } existing)
        {
            return existing;
        }

        // ReSharper disable once CollectionNeverUpdated.Local
        AvaloniaList<Control> list = [];
        element.SetValue(property, list);
        return list;
    }
}