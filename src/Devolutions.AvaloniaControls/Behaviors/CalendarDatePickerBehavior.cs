using Avalonia;

namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

public static class CalendarDatePickerBehavior
{
    public static readonly AttachedProperty<bool> OpenOnSelectedDateProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, bool>("OpenOnSelectedDate", typeof(CalendarDatePickerBehavior));

    static CalendarDatePickerBehavior()
    {
        OpenOnSelectedDateProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not CalendarDatePicker datePicker) return;

            if (args.NewValue.GetValueOrDefault<bool>())
            {
                datePicker.CalendarOpened += OnCalendarOpened;
                datePicker.CalendarClosed += OnCalendarClosed;
            }
            else
            {
                datePicker.CalendarOpened -= OnCalendarOpened;
                datePicker.CalendarClosed -= OnCalendarClosed;
            }
        });
    }

    private static void OnCalendarOpened(object? sender, EventArgs e)
    {
        if (sender is not CalendarDatePicker datePicker) return;

        Popup? popup = datePicker.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
        Calendar? calendar = popup?.Child?.FindDescendantOfType<Calendar>() ?? popup?.Child as Calendar;

        if (calendar == null) return;

        calendar.DisplayDate = datePicker.SelectedDate ?? DateTime.Now;

        // Move keyboard focus into the calendar when the popup opens. The calendar button now lives inside
        // PART_TextBox (to host inner-content slots), so opening no longer takes focus off the text box on
        // its own — focus would otherwise stay in the box and its segmented arrow-keys would look like
        // calendar input. Focusing the popup also lets Avalonia's light-dismiss close it on Tab again.
        // (PART_Calendar sets Focusable="True" in each theme so this Focus() takes effect.)
        Dispatcher.UIThread.Post(() => calendar.Focus(), DispatcherPriority.Input);
    }

    private static void OnCalendarClosed(object? sender, EventArgs e)
    {
        if (sender is not CalendarDatePicker datePicker) return;

        // Focus was moved into the popup calendar on open (see OnCalendarOpened). The calendar lives in the
        // popup's own tree, outside the window's tab order, so once the popup closes we return focus to the
        // picker's text box — restoring the original tab flow (Tab then continues from the picker).
        Dispatcher.UIThread.Post(() => ReturnFocusToPicker(datePicker), DispatcherPriority.Background);
    }

    // Returns focus to the picker's primary control. Prefers the text box, but on themes where it is hidden
    // and non-focusable (Yaru shows a button instead) falls back to the button.
    private static void ReturnFocusToPicker(CalendarDatePicker picker)
    {
        TextBox? textBox = picker.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PART_TextBox");
        if (textBox is { Focusable: true, IsVisible: true })
        {
            textBox.Focus();
            return;
        }

        Button? button = picker.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_Button");
        button?.Focus();
    }
}
