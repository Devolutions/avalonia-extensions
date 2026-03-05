using Avalonia;

namespace Devolutions.AvaloniaControls.Behaviors;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

public static class CalendarDatePickerBehavior
{
    public static readonly AttachedProperty<bool> OpenOnSelectedDateProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, bool>("OpenOnSelectedDate", typeof(CalendarDatePickerBehavior));

    static CalendarDatePickerBehavior()
    {
        OpenOnSelectedDateProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is CalendarDatePicker datePicker)
            {
                if (args.NewValue.GetValueOrDefault<bool>())
                {
                    datePicker.CalendarOpened += OnCalendarOpen;
                }
                else
                {
                    datePicker.CalendarOpened -= OnCalendarOpen;
                }
            }
        });
    }

    private static void OnCalendarOpen(object? sender, EventArgs e)
    {
        if (sender is CalendarDatePicker datePicker)
        {
            Popup? popup = datePicker.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
            Calendar? calendar = popup?.Child?.FindDescendantOfType<Calendar>() ?? popup?.Child as Calendar;

            if (calendar == null) return;

            calendar.DisplayDate = datePicker.SelectedDate ?? DateTime.Now;
        }
    }
}