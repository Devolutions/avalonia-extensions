using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Devolutions.AvaloniaTheme.WinUI;
using Devolutions.AvaloniaTheme.WinUI.Converters;
using Devolutions.AvaloniaTheme.WinUI.Internal;
using Xunit;
using Calendar = Avalonia.Controls.Calendar;

namespace Devolutions.AvaloniaControls.Tests;

public class WinUiCalendarDatePickerTests
{
    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Calendar_button_uses_static_glyph_and_roomier_horizontal_spacing(bool dark, bool disabled)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = true };
        theme.BeginInit();
        theme.EndInit();

        var picker = new CalendarDatePicker { IsEnabled = !disabled };
        var window = new Window
        {
            Content = picker,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(CalendarDatePicker), out var pickerTheme));
        picker.Theme = Assert.IsType<ControlTheme>(pickerTheme);
        window.Show();

        try
        {
            var button = Assert.Single(picker.GetVisualDescendants().OfType<Button>(),
                button => button.Name == "PART_Button");
            var glyph = Assert.Single(button.GetVisualDescendants().OfType<PathIcon>());
            Assert.Equal("CalendarGlyph", glyph.Name);
            Assert.Equal(14, glyph.Bounds.Width);
            Assert.Equal(14, glyph.Bounds.Height);
            Assert.Equal(32, button.Bounds.Width);
            Assert.False(button.Focusable);
            Assert.Empty(button.GetVisualDescendants().OfType<TextBlock>());
            Assert.Equal(new Thickness(12, 4, 8, 4), picker.Padding);
            Assert.NotNull(glyph.Data);
            Assert.Equal(button.Foreground, glyph.Foreground);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Popup_has_WinUi_surface_and_preserves_all_three_calendar_modes(bool dark, bool mica)
    {
        Windows11MicaDetector.SetTestOverride(mica);
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();

        var date = new DateTime(2026, 10, 14);
        var picker = new CalendarDatePicker
        {
            DisplayDate = date,
            SelectedDate = date,
            FirstDayOfWeek = DayOfWeek.Monday,
            IsTodayHighlighted = true
        };
        var window = new Window
        {
            Content = picker,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(CalendarDatePicker), out var pickerTheme));
        picker.Theme = Assert.IsType<ControlTheme>(pickerTheme);
        window.Show();

        try
        {
            picker.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            var popup = Assert.Single(picker.GetVisualDescendants().OfType<Popup>());
            var calendar = Assert.IsType<Calendar>(popup.Child);
            Assert.True(window.TryFindResource("WinUiCalendar", out var calendarTheme));
            Assert.Same(calendarTheme, calendar.Theme);
            Assert.Equal(DayOfWeek.Monday, calendar.FirstDayOfWeek);
            Assert.Equal(date, calendar.SelectedDate);

            var item = Assert.Single(calendar.GetVisualDescendants().OfType<CalendarItem>());
            Assert.Equal(new Size(300, 352), item.Bounds.Size);
            Assert.False(item.ClipToBounds);
            Assert.Equal(new Thickness(16, 12, 16, 20), item.Margin);
            Assert.Equal(new Size(332, 384), calendar.Bounds.Size);
            Assert.Equal(-16, popup.HorizontalOffset);
            Assert.Equal(-8, popup.VerticalOffset);
            var surface = Assert.Single(item.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "CalendarSurface");
            var surfaceOrigin = surface.TranslatePoint(default, calendar);
            Assert.NotNull(surfaceOrigin);
            var shadowBounds = surface.BoxShadow.TransformBounds(new Rect(surfaceOrigin.Value, surface.Bounds.Size));
            Assert.True(new Rect(calendar.Bounds.Size).Contains(shadowBounds),
                $"Shadow {shadowBounds} must fit inside the popup calendar {calendar.Bounds.Size}.");
            Assert.Equal(new CornerRadius(8), item.CornerRadius);
            Assert.True(calendar.TryFindResource("CalendarViewBackground", calendar.ActualThemeVariant, out var background));
            Assert.Equal(((ISolidColorBrush)background!).Color, ((ISolidColorBrush)item.Background!).Color);

            var days = item.GetVisualDescendants().OfType<CalendarDayButton>().ToArray();
            var monthGrid = Assert.Single(item.GetVisualDescendants().OfType<Grid>(), grid => grid.Name == "PART_MonthView");
            var labels = monthGrid.Children.OfType<TextBlock>().ToArray();
            Assert.Equal(7, labels.Length);
            Assert.All(labels, label => Assert.Equal(
                CalendarWeekDayConverter.Instance.Convert(
                    [Grid.GetColumn(label), DayOfWeek.Monday], typeof(string), null, CultureInfo.CurrentCulture),
                label.Text));
            Assert.Equal(42, days.Length);
            Assert.All(days, day =>
            {
                var circle = Assert.Single(day.GetVisualDescendants().OfType<Border>(), border => border.Name == "Root");
                Assert.Equal(new Size(40, 40), circle.Bounds.Size);
                Assert.Equal(new CornerRadius(100), circle.CornerRadius);
            });

            var selected = Assert.Single(days, day => day.DataContext is DateTime dayDate && dayDate == date);
            Assert.True(calendar.TryFindResource("CalendarViewSelectedBorderBrush", calendar.ActualThemeVariant, out var selectedBorder));
            Assert.Equal(((ISolidColorBrush)selectedBorder!).Color, ((ISolidColorBrush)selected.BorderBrush!).Color);
            Assert.True(calendar.Focus(NavigationMethod.Pointer));
            var selectionFocusBorder = Assert.Single(selected.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "FocusBorder");
            Assert.False(selectionFocusBorder.IsVisible);

            var header = Assert.Single(item.GetVisualDescendants().OfType<Button>(), button => button.Name == "PART_HeaderButton");
            header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(CalendarMode.Year, calendar.DisplayMode);
            Assert.False(monthGrid.IsVisible);
            Assert.All(item.GetVisualDescendants().OfType<CalendarButton>(),
                button => Assert.IsType<DateTime>(button.DataContext));
            Assert.True(picker.IsDropDownOpen);
            Assert.Equal(12, item.GetVisualDescendants().OfType<CalendarButton>().Count());

            header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(CalendarMode.Decade, calendar.DisplayMode);
            Assert.True(picker.IsDropDownOpen);

            var years = item.GetVisualDescendants().OfType<CalendarButton>().ToArray();
            var selectedYear = Assert.Single(years, button => button.DataContext is DateTime year && year.Year == date.Year);
            Assert.False(Assert.Single(selectedYear.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "FocusBorder").IsVisible);
            Click(selectedYear);
            Assert.Equal(CalendarMode.Year, calendar.DisplayMode);
            Assert.True(picker.IsDropDownOpen);

            var months = item.GetVisualDescendants().OfType<CalendarButton>().ToArray();
            var selectedMonth = Assert.Single(months, button => button.DataContext is DateTime month && month.Month == date.Month);
            Assert.False(Assert.Single(selectedMonth.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "FocusBorder").IsVisible);
            Click(selectedMonth);
            Assert.Equal(CalendarMode.Month, calendar.DisplayMode);
            Assert.Equal(date, picker.SelectedDate);
            Assert.True(picker.IsDropDownOpen);

            Assert.True(calendar.Focus());
            Assert.True(calendar.IsFocused);
            var popupRoot = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(calendar));
            popupRoot.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Assert.Equal(date.AddDays(1), picker.SelectedDate);
            Assert.True(picker.IsDropDownOpen);
            popupRoot.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Assert.False(picker.IsDropDownOpen);
        }
        finally
        {
            window.Close();
            Windows11MicaDetector.SetTestOverride(null);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Standalone_calendar_shares_styling_blackout_behavior_and_keyboard_only_focus_rings(bool dark)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();
        var date = new DateTime(2026, 10, 14);
        var calendar = new Calendar { DisplayDate = date, SelectedDate = date };
        var other = new Button { Content = "Other focus target" };
        var window = new Window
        {
            Width = 900,
            Height = 700,
            Content = new StackPanel { Children = { calendar, other } },
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(Calendar), out var calendarTheme));
        calendar.Theme = Assert.IsType<ControlTheme>(calendarTheme);
        Assert.True(window.TryFindResource("WinUiCalendar", out var sharedCalendarTheme));
        Assert.Same(sharedCalendarTheme, Assert.IsType<ControlTheme>(calendarTheme).BasedOn);
        window.Show();
        try
        {
            Assert.Equal(HorizontalAlignment.Left, calendar.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Center, calendar.VerticalAlignment);
            Assert.Equal(new Size(332, 384), calendar.Bounds.Size);
            var item = Assert.Single(calendar.GetVisualDescendants().OfType<CalendarItem>());
            Assert.Equal(new Size(300, 352), item.Bounds.Size);
            Assert.All(calendar.GetVisualDescendants().OfType<CalendarDayButton>(), day =>
                Assert.Equal(new Size(40, 40),
                    Assert.Single(day.GetVisualDescendants().OfType<Border>(), border => border.Name == "Root").Bounds.Size));
            var selected = Assert.Single(calendar.GetVisualDescendants().OfType<CalendarDayButton>(),
                day => day.DataContext is DateTime value && value == date);
            var focusBorder = Assert.Single(selected.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "FocusBorder");
            Assert.True(calendar.Focus(NavigationMethod.Pointer));
            Assert.False(focusBorder.IsVisible);

            foreach (var mode in new[] { CalendarMode.Year, CalendarMode.Decade })
            {
                calendar.DisplayMode = mode;
                window.UpdateLayout();
                var focused = Assert.Single(calendar.GetVisualDescendants().OfType<CalendarButton>(),
                    button => button.Classes.Contains(":btnfocused"));
                var monthYearFocusBorder = Assert.Single(focused.GetVisualDescendants().OfType<Border>(),
                    border => border.Name == "FocusBorder");
                Assert.False(monthYearFocusBorder.IsVisible);
                Assert.True(other.Focus(NavigationMethod.Tab));
                Assert.True(calendar.Focus(NavigationMethod.Tab));
                Assert.True(monthYearFocusBorder.IsVisible);
                Assert.True(other.Focus(NavigationMethod.Pointer));
                Assert.True(calendar.Focus(NavigationMethod.Pointer));
                Assert.False(monthYearFocusBorder.IsVisible);
            }

            calendar.DisplayMode = CalendarMode.Month;
            calendar.BlackoutDates.Add(new CalendarDateRange(date.AddDays(1)));
            var blackout = Assert.Single(calendar.GetVisualDescendants().OfType<CalendarDayButton>(),
                day => day.DataContext is DateTime value && value == date.AddDays(1));
            Assert.True(Assert.Single(blackout.GetVisualDescendants().OfType<Rectangle>()).IsVisible);
            Click(blackout);
            Assert.Equal(date, calendar.SelectedDate);
            Assert.True(other.Focus(NavigationMethod.Tab));
            Assert.True(calendar.Focus(NavigationMethod.Tab));
            Assert.Contains(":focus-visible", calendar.Classes);
            Assert.True(focusBorder.IsVisible);
            window.UpdateLayout();
            Assert.True(selected.Bounds.Width >= focusBorder.Bounds.Width);
            Assert.True(selected.Bounds.Height >= focusBorder.Bounds.Height);
            Assert.True(focusBorder.Bounds.X >= 0);
            Assert.True(focusBorder.Bounds.Y >= 0);
            Assert.True(other.Focus(NavigationMethod.Pointer));
            Assert.True(calendar.Focus(NavigationMethod.Pointer));
            Assert.False(focusBorder.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Popup_preserves_today_blackout_and_ranges(bool dark)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();
        var today = DateTime.Today;
        var picker = new CalendarDatePicker
        {
            DisplayDate = today,
            SelectedDate = today,
            DisplayDateStart = today.AddDays(-7),
            DisplayDateEnd = today.AddDays(7)
        };
        var window = new Window
        {
            Content = picker,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(typeof(CalendarDatePicker), out var pickerTheme));
        picker.Theme = Assert.IsType<ControlTheme>(pickerTheme);
        window.Show();
        try
        {
            picker.BlackoutDates!.Add(new CalendarDateRange(today.AddDays(1)));
            picker.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            var popup = Assert.Single(picker.GetVisualDescendants().OfType<Popup>());
            var calendar = Assert.IsType<Calendar>(popup.Child);
            Assert.True(picker.IsTodayHighlighted);
            Assert.True(calendar.IsTodayHighlighted);
            var days = calendar.GetVisualDescendants().OfType<CalendarDayButton>().ToArray();
            var todayButton = Assert.Single(days, button => button.DataContext is DateTime date && date == today);
            Assert.True(calendar.TryFindResource("CalendarViewTodayBackground", calendar.ActualThemeVariant, out var todayBrush));
            Assert.Equal(((ISolidColorBrush)todayBrush!).Color, ((ISolidColorBrush)todayButton.Background!).Color);
            Assert.True(Assert.Single(todayButton.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "TodaySelectedBorder").IsVisible);

            var popupRoot = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(todayButton));
            var todayCenter = todayButton.TranslatePoint(
                new Point(todayButton.Bounds.Width / 2, todayButton.Bounds.Height / 2), popupRoot);
            Assert.NotNull(todayCenter);
            popupRoot.MouseMove(todayCenter.Value);
            popupRoot.MouseDown(todayCenter.Value, MouseButton.Left);
            Assert.True(calendar.TryFindResource("CalendarViewTodayForeground", calendar.ActualThemeVariant, out var todayForeground));
            Assert.Equal(((ISolidColorBrush)todayForeground!).Color, ((ISolidColorBrush)todayButton.Foreground!).Color);
            popupRoot.MouseMove(new Point(-10, -10));
            popupRoot.MouseUp(new Point(-10, -10), MouseButton.Left);
            picker.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();

            var blackout = Assert.Single(days, button => button.DataContext is DateTime date && date == today.AddDays(1));
            Assert.True(Assert.Single(blackout.GetVisualDescendants().OfType<Rectangle>()).IsVisible);
            Click(blackout);
            Assert.Equal(today, picker.SelectedDate);
            Assert.True(picker.IsDropDownOpen);
            Assert.All(days.Where(button => button.DataContext is DateTime date &&
                                           (date < today.AddDays(-7) || date > today.AddDays(7))),
                button => Assert.False(button.IsEnabled));

            var otherDate = today.AddDays(today.Day > 2 ? -2 : 2);
            var otherDay = Assert.Single(days, button => button.DataContext is DateTime date && date == otherDate);
            Click(otherDay);
            Assert.Equal(otherDate, picker.SelectedDate);
            Assert.False(picker.IsDropDownOpen);

            picker.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(((ISolidColorBrush)todayBrush!).Color, ((ISolidColorBrush)todayButton.Background!).Color);
            Assert.False(Assert.Single(todayButton.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "TodaySelectedBorder").IsVisible);

            picker.IsTodayHighlighted = false;
            Dispatcher.UIThread.RunJobs();
            Assert.False(calendar.IsTodayHighlighted);
            Assert.True(calendar.TryFindResource("CalendarViewCalendarItemBackground", calendar.ActualThemeVariant, out var ordinaryBackground));
            Assert.Equal(((ISolidColorBrush)ordinaryBackground!).Color, ((ISolidColorBrush)todayButton.Background!).Color);
        }
        finally
        {
            window.Close();
        }
    }

    [Theory]
    [InlineData("en-US", DayOfWeek.Monday, 0, "Mo")]
    [InlineData("fr-FR", DayOfWeek.Monday, 0, "lu")]
    [InlineData("de-DE", DayOfWeek.Sunday, 4, "Do")]
    [InlineData("en-US", DayOfWeek.Saturday, 1, "Su")]
    public void Weekday_labels_follow_culture_and_first_day(string culture, DayOfWeek firstDay, int column, string expected)
    {
        Assert.Equal(expected, CalendarWeekDayConverter.Instance.Convert(
            [column, firstDay], typeof(string), null, CultureInfo.GetCultureInfo(culture)));
    }

    [Fact]
    public void Weekday_labels_fall_back_to_culture_shortest_names_when_prefixes_collide()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        var labels = Enumerable.Range(0, 7).Select(column => Assert.IsType<string>(
            CalendarWeekDayConverter.Instance.Convert(
                [column, DayOfWeek.Sunday], typeof(string), null, culture))).ToArray();
        Assert.Equal(culture.DateTimeFormat.ShortestDayNames, labels);
        Assert.True(labels.Distinct(StringComparer.Ordinal).Count() > 1);
        Assert.Equal(labels[1], CalendarWeekDayConverter.Instance.Convert(
            [0, DayOfWeek.Monday], typeof(string), null, culture));
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Cells_show_focus_indicator_when_focused_directly(bool monthYear, bool dark)
    {
        var theme = new DevolutionsWinUiTheme { GlobalStyles = false };
        theme.BeginInit();
        theme.EndInit();
        Button cell = monthYear ? new CalendarButton() : new CalendarDayButton();
        var other = new Button { Content = "Other focus target" };
        var window = new Window
        {
            Content = new StackPanel { Children = { cell, other } },
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light
        };
        window.Styles.Add(theme);
        Assert.True(window.TryFindResource(monthYear ? "WinUiCalendarMonthYearButton" : "WinUiCalendarDayButton", out var cellTheme));
        cell.Theme = Assert.IsType<ControlTheme>(cellTheme);
        window.Show();
        try
        {
            var focusBorder = Assert.Single(cell.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "FocusBorder");
            Assert.True(cell.Focus(NavigationMethod.Pointer));
            Assert.False(focusBorder.IsVisible);
            Assert.True(other.Focus(NavigationMethod.Tab));
            Assert.True(cell.Focus(NavigationMethod.Tab));
            Assert.Contains(":focus-visible", cell.Classes);
            Assert.True(focusBorder.IsVisible);
            Assert.True(other.Focus(NavigationMethod.Pointer));
            Assert.True(cell.Focus(NavigationMethod.Pointer));
            Assert.False(focusBorder.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Weekday_labels_preserve_combining_characters()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.AbbreviatedDayNames = ["A\u0301BCD", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
        Assert.Equal("A\u0301B", CalendarWeekDayConverter.Instance.Convert(
            [0, DayOfWeek.Sunday], typeof(string), null, culture));
    }

    private static void Click(Control control)
    {
        var root = Assert.IsAssignableFrom<TopLevel>(TopLevel.GetTopLevel(control));
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), root);
        Assert.NotNull(point);
        root.MouseDown(point.Value, MouseButton.Left);
        root.MouseUp(point.Value, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
