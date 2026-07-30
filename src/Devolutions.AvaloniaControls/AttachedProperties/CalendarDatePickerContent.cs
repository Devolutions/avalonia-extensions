namespace Devolutions.AvaloniaControls.AttachedProperties;

using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;

public static class CalendarDatePickerContent
{
    public static readonly AttachedProperty<IEnumerable> InnerLeftContentProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, IEnumerable>("InnerLeftContent", typeof(CalendarDatePickerContent));

    public static readonly AttachedProperty<IEnumerable> InnerLeftOfCalendarButtonContentProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, IEnumerable>("InnerLeftOfCalendarButtonContent", typeof(CalendarDatePickerContent));

    public static readonly AttachedProperty<IEnumerable> InnerRightContentProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, IEnumerable>("InnerRightContent", typeof(CalendarDatePickerContent));

    public static IEnumerable GetInnerLeftContent(CalendarDatePicker element) => GetOrCreate(element, InnerLeftContentProperty);

    public static void SetInnerLeftContent(CalendarDatePicker element, IEnumerable value) => element.SetValue(InnerLeftContentProperty, value);

    public static IEnumerable GetInnerLeftOfCalendarButtonContent(CalendarDatePicker element) =>
        GetOrCreate(element, InnerLeftOfCalendarButtonContentProperty);

    public static void SetInnerLeftOfCalendarButtonContent(CalendarDatePicker element, IEnumerable value) =>
        element.SetValue(InnerLeftOfCalendarButtonContentProperty, value);

    public static IEnumerable GetInnerRightContent(CalendarDatePicker element) => GetOrCreate(element, InnerRightContentProperty);

    public static void SetInnerRightContent(CalendarDatePicker element, IEnumerable value) => element.SetValue(InnerRightContentProperty, value);

    // Attached-property defaults are shared across all instances, which is not what we want for
    // mutable collections. Lazily create a per-instance AvaloniaList<Control> on first read so
    // XAML element-syntax (<CalendarDatePickerContent.InnerLeftContent>...</...>) can Add into it.
    private static IEnumerable GetOrCreate(CalendarDatePicker element, AttachedProperty<IEnumerable> property)
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
