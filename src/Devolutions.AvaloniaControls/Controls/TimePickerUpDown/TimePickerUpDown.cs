namespace Devolutions.AvaloniaControls.Controls;

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

[TemplatePart("PART_Spinner", typeof(Spinner))]
[TemplatePart("PART_HourTextBox", typeof(TextBox))]
[TemplatePart("PART_MinuteTextBox", typeof(TextBox))]
[TemplatePart("PART_SecondTextBox", typeof(TextBox))]
[TemplatePart("PART_PeriodTextBox", typeof(TextBox))]
[TemplatePart("PART_SecondSeparator", typeof(Control))]
[PseudoClasses(":hasnotime")]
public class TimePickerUpDown : TemplatedControl
{
    public static readonly StyledProperty<bool> AllowSpinProperty =
        ButtonSpinner.AllowSpinProperty.AddOwner<TimePickerUpDown>();

    public static readonly StyledProperty<Location> ButtonSpinnerLocationProperty =
        ButtonSpinner.ButtonSpinnerLocationProperty.AddOwner<TimePickerUpDown>();

    public static readonly StyledProperty<string> ClockIdentifierProperty =
        AvaloniaProperty.Register<TimePickerUpDown, string>(nameof(ClockIdentifier), "12HourClock");

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<TimePickerUpDown, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<int> MinuteIncrementProperty =
        AvaloniaProperty.Register<TimePickerUpDown, int>(nameof(MinuteIncrement), 1, coerce: CoercePositiveIncrement);

    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<TimePickerUpDown, TimeSpan?>(
            nameof(SelectedTime),
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public static readonly StyledProperty<int> SecondIncrementProperty =
        AvaloniaProperty.Register<TimePickerUpDown, int>(nameof(SecondIncrement), 1, coerce: CoercePositiveIncrement);

    public static readonly StyledProperty<bool> ShowButtonSpinnerProperty =
        ButtonSpinner.ShowButtonSpinnerProperty.AddOwner<TimePickerUpDown>();

    public static readonly StyledProperty<bool> UseSecondsProperty =
        AvaloniaProperty.Register<TimePickerUpDown, bool>(nameof(UseSeconds));

    private TextBox? _hourTextBox;
    private bool _isUpdatingText;
    private TextBox? _minuteTextBox;
    private TextBox? _periodTextBox;
    private TextBox? _secondTextBox;
    private Control? _secondSeparator;
    private Spinner? _spinner;
    private TimeSegment _activeSegment = TimeSegment.Hour;

    public TimePickerUpDown()
    {
        UpdatePseudoClasses();
    }

    public bool AllowSpin
    {
        get => GetValue(AllowSpinProperty);
        set => SetValue(AllowSpinProperty, value);
    }

    public Location ButtonSpinnerLocation
    {
        get => GetValue(ButtonSpinnerLocationProperty);
        set => SetValue(ButtonSpinnerLocationProperty, value);
    }

    public string ClockIdentifier
    {
        get => GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public int MinuteIncrement
    {
        get => GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    public TimeSpan? SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public int SecondIncrement
    {
        get => GetValue(SecondIncrementProperty);
        set => SetValue(SecondIncrementProperty, value);
    }

    public bool ShowButtonSpinner
    {
        get => GetValue(ShowButtonSpinnerProperty);
        set => SetValue(ShowButtonSpinnerProperty, value);
    }

    public bool UseSeconds
    {
        get => GetValue(UseSecondsProperty);
        set => SetValue(UseSecondsProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        UnhookTemplateParts();

        base.OnApplyTemplate(e);

        _spinner = e.NameScope.Find<Spinner>("PART_Spinner");
        _hourTextBox = e.NameScope.Find<TextBox>("PART_HourTextBox");
        _minuteTextBox = e.NameScope.Find<TextBox>("PART_MinuteTextBox");
        _secondTextBox = e.NameScope.Find<TextBox>("PART_SecondTextBox");
        _periodTextBox = e.NameScope.Find<TextBox>("PART_PeriodTextBox");
        _secondSeparator = e.NameScope.Find<Control>("PART_SecondSeparator");

        if (_spinner is not null)
        {
            _spinner.Spin += OnSpinnerSpin;
        }

        HookSegment(_hourTextBox);
        HookSegment(_minuteTextBox);
        HookSegment(_secondTextBox);
        HookSegment(_periodTextBox);

        UpdateSegmentVisibility();
        UpdateSegmentTexts();
        UpdateSpinner();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedTimeProperty)
        {
            UpdatePseudoClasses();
            UpdateSegmentTexts();
        }
        else if (change.Property == ClockIdentifierProperty)
        {
            UpdateSegmentVisibility();
            UpdateSegmentTexts();
        }
        else if (change.Property == UseSecondsProperty)
        {
            if (!UseSeconds && SelectedTime is { } selectedTime && selectedTime.Seconds != 0)
            {
                SetSelectedTimeValue(selectedTime);
            }

            UpdateSegmentVisibility();
            UpdateSegmentTexts();
        }
        else if (change.Property == AllowSpinProperty || change.Property == IsReadOnlyProperty)
        {
            UpdateSpinner();
        }
    }

    private static int CoercePositiveIncrement(AvaloniaObject sender, int value)
    {
        return value <= 0 ? 1 : value;
    }

    private static string GetAmDesignator()
    {
        return string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator)
            ? "AM"
            : CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
    }

    private static string GetPmDesignator()
    {
        return string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator)
            ? "PM"
            : CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
    }

    private static bool IsAllBlank(params string?[] values)
    {
        return values.All(string.IsNullOrWhiteSpace);
    }

    private static bool MatchesPeriod(string text, string designator)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return designator.Equals(text, StringComparison.CurrentCultureIgnoreCase)
            || designator.StartsWith(text, StringComparison.CurrentCultureIgnoreCase)
            || text.StartsWith(designator, StringComparison.CurrentCultureIgnoreCase);
    }

    private static TimeSpan Normalize(TimeSpan value)
    {
        long ticks = value.Ticks % TimeSpan.TicksPerDay;
        if (ticks < 0)
        {
            ticks += TimeSpan.TicksPerDay;
        }

        return TimeSpan.FromTicks(ticks);
    }

    private static bool TryParseNumber(string? text, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private void CommitSegments()
    {
        if (_isUpdatingText)
        {
            return;
        }

        if (TryBuildTimeFromSegments(out TimeSpan? time))
        {
            SetSelectedTimeValue(time);
        }

        UpdateSegmentTexts();
    }

    private string FormatHourText(TimeSpan selectedTime)
    {
        if (Is24HourClock())
        {
            return selectedTime.Hours.ToString("00", CultureInfo.InvariantCulture);
        }

        int displayHour = selectedTime.Hours % 12;
        if (displayHour == 0)
        {
            displayHour = 12;
        }

        return displayHour.ToString("00", CultureInfo.InvariantCulture);
    }

    private TimeSpan GetEffectiveTime()
    {
        return Normalize(SelectedTime ?? DateTime.Now.TimeOfDay);
    }

    private TimeSegment GetSegment(TextBox? textBox)
    {
        if (ReferenceEquals(textBox, _minuteTextBox))
        {
            return TimeSegment.Minute;
        }

        if (ReferenceEquals(textBox, _secondTextBox))
        {
            return TimeSegment.Second;
        }

        if (ReferenceEquals(textBox, _periodTextBox))
        {
            return TimeSegment.Period;
        }

        return TimeSegment.Hour;
    }

    private void HookSegment(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.GotFocus += OnSegmentGotFocus;
        textBox.LostFocus += OnSegmentLostFocus;
        textBox.KeyDown += OnSegmentKeyDown;
    }

    private bool Is24HourClock()
    {
        return string.Equals(ClockIdentifier, "24HourClock", StringComparison.Ordinal);
    }

    private bool IsSegmentAvailable(TimeSegment segment)
    {
        return segment switch
        {
            TimeSegment.Second => UseSeconds,
            TimeSegment.Period => !Is24HourClock(),
            _ => true,
        };
    }

    private void OnSegmentGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _activeSegment = GetSegment(textBox);
            textBox.SelectAll();
        }
    }

    private void OnSegmentKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        _activeSegment = GetSegment(textBox);

        switch (e.Key)
        {
            case Key.Up:
                SpinActiveSegment(SpinDirection.Increase);
                e.Handled = true;
                break;
            case Key.Down:
                SpinActiveSegment(SpinDirection.Decrease);
                e.Handled = true;
                break;
            case Key.Enter:
                CommitSegments();
                e.Handled = true;
                break;
            case Key.Escape:
                UpdateSegmentTexts();
                e.Handled = true;
                break;
        }
    }

    private void OnSegmentLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitSegments();
    }

    private void OnSpinnerSpin(object? sender, SpinEventArgs e)
    {
        if (!AllowSpin || IsReadOnly)
        {
            return;
        }

        SpinActiveSegment(e.Direction);
        e.Handled = true;
    }

    private void SetSelectedTimeValue(TimeSpan? value)
    {
        if (value is null)
        {
            SetCurrentValue(SelectedTimeProperty, null);
            return;
        }

        var normalized = Normalize(value.Value);
        if (!UseSeconds)
        {
            normalized = new TimeSpan(normalized.Hours, normalized.Minutes, 0);
        }

        SetCurrentValue(SelectedTimeProperty, normalized);
    }

    private void SpinActiveSegment(SpinDirection direction)
    {
        if (!IsSegmentAvailable(_activeSegment))
        {
            _activeSegment = TimeSegment.Hour;
        }

        double amount = _activeSegment switch
        {
            TimeSegment.Hour => 3600,
            TimeSegment.Minute => 60 * MinuteIncrement,
            TimeSegment.Second => SecondIncrement,
            TimeSegment.Period => 43200,
            _ => 0,
        };

        if (amount == 0)
        {
            return;
        }

        if (direction == SpinDirection.Decrease)
        {
            amount = -amount;
        }

        SetSelectedTimeValue(GetEffectiveTime().Add(TimeSpan.FromSeconds(amount)));
    }

    private bool TryBuildTimeFromSegments(out TimeSpan? time)
    {
        string hourText = _hourTextBox?.Text?.Trim() ?? string.Empty;
        string minuteText = _minuteTextBox?.Text?.Trim() ?? string.Empty;
        string secondText = _secondTextBox?.Text?.Trim() ?? string.Empty;
        string periodText = _periodTextBox?.Text?.Trim() ?? string.Empty;

        if (IsAllBlank(hourText, minuteText, UseSeconds ? secondText : null, Is24HourClock() ? null : periodText))
        {
            time = null;
            return true;
        }

        if (!TryParseNumber(hourText, out int hour) || !TryParseNumber(minuteText, out int minute))
        {
            time = null;
            return false;
        }

        int second = 0;
        if (UseSeconds)
        {
            if (!TryParseNumber(secondText, out second))
            {
                time = null;
                return false;
            }
        }

        if (minute is < 0 or > 59 || second is < 0 or > 59)
        {
            time = null;
            return false;
        }

        if (Is24HourClock())
        {
            if (hour is < 0 or > 23)
            {
                time = null;
                return false;
            }
        }
        else
        {
            if (hour is < 1 or > 12 || !TryParsePeriod(periodText, out bool isPm))
            {
                time = null;
                return false;
            }

            if (isPm && hour != 12)
            {
                hour += 12;
            }
            else if (!isPm && hour == 12)
            {
                hour = 0;
            }
        }

        time = new TimeSpan(hour, minute, UseSeconds ? second : 0);
        return true;
    }

    private bool TryParsePeriod(string? value, out bool isPm)
    {
        string text = value?.Trim() ?? string.Empty;
        string am = GetAmDesignator();
        string pm = GetPmDesignator();

        if (MatchesPeriod(text, pm))
        {
            isPm = true;
            return true;
        }

        if (MatchesPeriod(text, am))
        {
            isPm = false;
            return true;
        }

        isPm = false;
        return false;
    }

    private void UnhookSegment(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.GotFocus -= OnSegmentGotFocus;
        textBox.LostFocus -= OnSegmentLostFocus;
        textBox.KeyDown -= OnSegmentKeyDown;
    }

    private void UnhookTemplateParts()
    {
        if (_spinner is not null)
        {
            _spinner.Spin -= OnSpinnerSpin;
        }

        UnhookSegment(_hourTextBox);
        UnhookSegment(_minuteTextBox);
        UnhookSegment(_secondTextBox);
        UnhookSegment(_periodTextBox);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":hasnotime", !SelectedTime.HasValue);
    }

    private void UpdateSegmentTexts()
    {
        if (_hourTextBox is null || _minuteTextBox is null || _periodTextBox is null)
        {
            return;
        }

        _isUpdatingText = true;

        try
        {
            if (SelectedTime is { } selectedTime)
            {
                _hourTextBox.Text = FormatHourText(selectedTime);
                _minuteTextBox.Text = selectedTime.Minutes.ToString("00", CultureInfo.InvariantCulture);

                if (_secondTextBox is not null)
                {
                    _secondTextBox.Text = selectedTime.Seconds.ToString("00", CultureInfo.InvariantCulture);
                }

                _periodTextBox.Text = Is24HourClock()
                    ? string.Empty
                    : selectedTime.Hours >= 12 ? GetPmDesignator() : GetAmDesignator();
            }
            else
            {
                _hourTextBox.Text = string.Empty;
                _minuteTextBox.Text = string.Empty;

                if (_secondTextBox is not null)
                {
                    _secondTextBox.Text = string.Empty;
                }

                _periodTextBox.Text = string.Empty;
            }
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    private void UpdateSegmentVisibility()
    {
        if (_secondTextBox is not null)
        {
            _secondTextBox.IsVisible = UseSeconds;
        }

        if (_secondSeparator is not null)
        {
            _secondSeparator.IsVisible = UseSeconds;
        }

        if (_periodTextBox is not null)
        {
            _periodTextBox.IsVisible = !Is24HourClock();
        }

        if (!IsSegmentAvailable(_activeSegment))
        {
            _activeSegment = TimeSegment.Hour;
        }
    }

    private void UpdateSpinner()
    {
        if (_spinner is not null)
        {
            _spinner.ValidSpinDirection = AllowSpin && !IsReadOnly
                ? ValidSpinDirections.Increase | ValidSpinDirections.Decrease
                : ValidSpinDirections.None;
        }
    }

    private enum TimeSegment
    {
        Hour,
        Minute,
        Second,
        Period,
    }
}