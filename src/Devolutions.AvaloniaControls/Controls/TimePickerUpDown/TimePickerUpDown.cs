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
    AvaloniaProperty.Register<TimePickerUpDown, string>(nameof(ClockIdentifier), "12HourClock", coerce: CoerceClockIdentifier);

  public static readonly StyledProperty<bool> IsReadOnlyProperty =
    AvaloniaProperty.Register<TimePickerUpDown, bool>(nameof(IsReadOnly));

  public static readonly StyledProperty<int> MinuteIncrementProperty =
    AvaloniaProperty.Register<TimePickerUpDown, int>(nameof(MinuteIncrement), 1, coerce: CoerceIncrement);

  public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
    AvaloniaProperty.Register<TimePickerUpDown, TimeSpan?>(
      nameof(SelectedTime),
      defaultBindingMode: BindingMode.TwoWay,
      enableDataValidation: true);

  public static readonly StyledProperty<int> SecondIncrementProperty =
    AvaloniaProperty.Register<TimePickerUpDown, int>(nameof(SecondIncrement), 1, coerce: CoerceIncrement);

  public static readonly StyledProperty<bool> ShowButtonSpinnerProperty =
    ButtonSpinner.ShowButtonSpinnerProperty.AddOwner<TimePickerUpDown>();

  public static readonly StyledProperty<bool> UseSecondsProperty =
    AvaloniaProperty.Register<TimePickerUpDown, bool>(nameof(UseSeconds));

  private TextBox? _hourTextBox;
  private bool _isUpdatingText;
  private TextBox? _minuteTextBox;
  private TextBox? _periodTextBox;
  private TimeSegment? _returnToPreviousSegmentOnBackspace;
  private TextBox? _secondTextBox;
  private Control? _secondSeparator;
  private Spinner? _spinner;
  private TimeSegment _activeSegment = TimeSegment.Hour;

  public TimePickerUpDown()
  {
    string timePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
    if (timePattern.IndexOf("H", StringComparison.Ordinal) != -1)
    {
      this.SetCurrentValue(ClockIdentifierProperty, "24HourClock");
    }

    this.UpdatePseudoClasses();
  }

  public bool AllowSpin
  {
    get => this.GetValue(AllowSpinProperty);
    set => this.SetValue(AllowSpinProperty, value);
  }

  public Location ButtonSpinnerLocation
  {
    get => this.GetValue(ButtonSpinnerLocationProperty);
    set => this.SetValue(ButtonSpinnerLocationProperty, value);
  }

  public string ClockIdentifier
  {
    get => this.GetValue(ClockIdentifierProperty);
    set => this.SetValue(ClockIdentifierProperty, value);
  }

  public bool IsReadOnly
  {
    get => this.GetValue(IsReadOnlyProperty);
    set => this.SetValue(IsReadOnlyProperty, value);
  }

  public int MinuteIncrement
  {
    get => this.GetValue(MinuteIncrementProperty);
    set => this.SetValue(MinuteIncrementProperty, value);
  }

  public TimeSpan? SelectedTime
  {
    get => this.GetValue(SelectedTimeProperty);
    set => this.SetValue(SelectedTimeProperty, value);
  }

  public int SecondIncrement
  {
    get => this.GetValue(SecondIncrementProperty);
    set => this.SetValue(SecondIncrementProperty, value);
  }

  public bool ShowButtonSpinner
  {
    get => this.GetValue(ShowButtonSpinnerProperty);
    set => this.SetValue(ShowButtonSpinnerProperty, value);
  }

  public bool UseSeconds
  {
    get => this.GetValue(UseSecondsProperty);
    set => this.SetValue(UseSecondsProperty, value);
  }

  protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
  {
    this.UnhookTemplateParts();

    base.OnApplyTemplate(e);

    this._spinner = e.NameScope.Find<Spinner>("PART_Spinner");
    this._hourTextBox = e.NameScope.Find<TextBox>("PART_HourTextBox");
    this._minuteTextBox = e.NameScope.Find<TextBox>("PART_MinuteTextBox");
    this._secondTextBox = e.NameScope.Find<TextBox>("PART_SecondTextBox");
    this._periodTextBox = e.NameScope.Find<TextBox>("PART_PeriodTextBox");
    this._secondSeparator = e.NameScope.Find<Control>("PART_SecondSeparator");

    if (this._spinner is not null)
    {
      this._spinner.Spin += this.OnSpinnerSpin;
    }

    this.HookSegment(this._hourTextBox);
    this.HookSegment(this._minuteTextBox);
    this.HookSegment(this._secondTextBox);
    this.HookSegment(this._periodTextBox);

    this.UpdateSegmentVisibility();
    this.UpdateSegmentTexts();
    this.UpdateSpinner();
  }

  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == SelectedTimeProperty)
    {
      this.UpdatePseudoClasses();
      this.UpdateSegmentTexts();
    }
    else if (change.Property == ClockIdentifierProperty)
    {
      this.UpdateSegmentVisibility();
      this.UpdateSegmentTexts();
    }
    else if (change.Property == UseSecondsProperty)
    {
      if (!this.UseSeconds && this.SelectedTime is { } selectedTime && selectedTime.Seconds != 0)
      {
        this.SetSelectedTimeValue(selectedTime);
      }

      this.UpdateSegmentVisibility();
      this.UpdateSegmentTexts();
    }
    else if (change.Property == AllowSpinProperty || change.Property == IsReadOnlyProperty)
    {
      this.UpdateSpinner();
    }
  }

  private static string CoerceClockIdentifier(AvaloniaObject sender, string value)
  {
    if (!string.IsNullOrEmpty(value) && value != "12HourClock" && value != "24HourClock")
    {
      throw new ArgumentException("Invalid ClockIdentifier", nameof(value));
    }

    return value;
  }

  private static int CoerceIncrement(AvaloniaObject sender, int value)
  {
    if (value < 1 || value > 59)
    {
      throw new ArgumentOutOfRangeException(nameof(value), "increment must be between 1 and 59");
    }

    return value;
  }

  private static string GetAmDesignator() =>
    string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator)
      ? "AM"
      : CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;

  private static string GetPmDesignator() =>
    string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator)
      ? "PM"
      : CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;

  private static bool IsAllBlank(params string?[] values) =>
    values.All(string.IsNullOrWhiteSpace);

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

  private static bool TryParseNumber(string? text, out int value) =>
    int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

  private void CommitSegments()
  {
    if (this._isUpdatingText)
    {
      return;
    }

    if (this.TryBuildTimeFromSegments(out TimeSpan? time))
    {
      this.SetSelectedTimeValue(time);
    }

    this.UpdateSegmentTexts();
  }

  private string FormatHourText(TimeSpan selectedTime)
  {
    if (this.Is24HourClock())
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

  private TimeSpan GetEffectiveTime() =>
    Normalize(this.SelectedTime ?? DateTime.Now.TimeOfDay);

  private TimeSegment GetSegment(TextBox? textBox)
  {
    if (ReferenceEquals(textBox, this._minuteTextBox))
    {
      return TimeSegment.Minute;
    }

    if (ReferenceEquals(textBox, this._secondTextBox))
    {
      return TimeSegment.Second;
    }

    if (ReferenceEquals(textBox, this._periodTextBox))
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

    textBox.GotFocus += this.OnSegmentGotFocus;
    textBox.LostFocus += this.OnSegmentLostFocus;
    textBox.AddHandler(KeyDownEvent, this.OnSegmentKeyDown, RoutingStrategies.Tunnel, true);
    textBox.AddHandler(PointerPressedEvent, this.OnSegmentPointerPressed, RoutingStrategies.Tunnel, true);
    textBox.AddHandler(TextInputEvent, this.OnSegmentTextInput, RoutingStrategies.Tunnel, true);
    textBox.PropertyChanged += this.OnSegmentPropertyChanged;
  }

  private bool Is24HourClock() =>
    string.Equals(this.ClockIdentifier, "24HourClock", StringComparison.Ordinal);

  private bool IsSegmentAvailable(TimeSegment segment)
  {
    return segment switch
    {
      TimeSegment.Second => this.UseSeconds,
      TimeSegment.Period => !this.Is24HourClock(),
      _ => true
    };
  }

  private TextBox? GetTextBox(TimeSegment segment)
  {
    return segment switch
    {
      TimeSegment.Hour => this._hourTextBox,
      TimeSegment.Minute => this._minuteTextBox,
      TimeSegment.Second => this._secondTextBox,
      TimeSegment.Period => this._periodTextBox,
      _ => null
    };
  }

  private string GetDefaultSegmentText(TimeSegment segment)
  {
    return segment switch
    {
      TimeSegment.Hour => this.Is24HourClock() ? "00" : "01",
      TimeSegment.Minute => "00",
      TimeSegment.Second => "00",
      TimeSegment.Period => GetAmDesignator(),
      _ => string.Empty
    };
  }

  private bool IsCurrentSegmentValueValid(TimeSegment segment)
  {
    TextBox? textBox = this.GetTextBox(segment);
    string text = textBox?.Text?.Trim() ?? string.Empty;

    if (string.IsNullOrEmpty(text))
    {
      return true;
    }

    return segment switch
    {
      TimeSegment.Hour => TryParseNumber(text, out int hour) && (this.Is24HourClock() ? hour is >= 0 and <= 23 : hour is >= 1 and <= 12),
      TimeSegment.Minute => TryParseNumber(text, out int minute) && minute is >= 0 and <= 59,
      TimeSegment.Second => TryParseNumber(text, out int second) && second is >= 0 and <= 59,
      TimeSegment.Period => this.TryParsePeriod(text, out _),
      _ => true
    };
  }

  private bool TryGetImmediateSegmentValue(TimeSegment segment, TextBox textBox, string inputText, out string normalizedText)
  {
    normalizedText = string.Empty;

    if (inputText.Length != 1 || !char.IsDigit(inputText[0]))
    {
      return false;
    }

    int selectionStart = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
    int selectionEnd = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
    bool replacingWholeSegment = selectionStart == 0 && selectionEnd == (textBox.Text?.Length ?? 0);
    bool segmentIsEmpty = string.IsNullOrEmpty(textBox.Text);
    if (!replacingWholeSegment && !segmentIsEmpty)
    {
      return false;
    }

    int value = inputText[0] - '0';
    bool shouldNormalizeImmediately = segment switch
    {
      TimeSegment.Hour when this.Is24HourClock() => value >= 3,
      TimeSegment.Hour => value >= 2,
      TimeSegment.Minute => value >= 6,
      TimeSegment.Second => value >= 6,
      _ => false
    };

    if (!shouldNormalizeImmediately)
    {
      return false;
    }

    normalizedText = $"0{value}";
    return true;
  }

  private bool MoveFocusToAdjacentSegment(TimeSegment currentSegment, bool forward, bool selectAll = true,
    bool returnToPreviousOnBackspace = false)
  {
    TimeSegment[] orderedSegments =
    [
      TimeSegment.Hour,
      TimeSegment.Minute,
      TimeSegment.Second,
      TimeSegment.Period
    ];

    int currentIndex = Array.IndexOf(orderedSegments, currentSegment);
    if (currentIndex < 0)
    {
      return false;
    }

    int step = forward ? 1 : -1;
    for (int index = currentIndex + step; index >= 0 && index < orderedSegments.Length; index += step)
    {
      TimeSegment nextSegment = orderedSegments[index];
      if (!this.IsSegmentAvailable(nextSegment))
      {
        continue;
      }

      TextBox? nextTextBox = this.GetTextBox(nextSegment);
      if (nextTextBox is null || !nextTextBox.IsVisible || !nextTextBox.IsEnabled)
      {
        continue;
      }

      this._activeSegment = nextSegment;
      this._returnToPreviousSegmentOnBackspace = returnToPreviousOnBackspace ? nextSegment : null;
      nextTextBox.Focus(NavigationMethod.Tab);
      if (selectAll)
      {
        nextTextBox.SelectAll();
      }
      else
      {
        int textLength = nextTextBox.Text?.Length ?? 0;
        nextTextBox.SelectionStart = textLength;
        nextTextBox.SelectionEnd = textLength;
      }

      return true;
    }

    return false;
  }

  private void SelectSegment(TimeSegment segment, NavigationMethod navigationMethod = NavigationMethod.Tab)
  {
    TextBox? textBox = this.GetTextBox(segment);
    if (textBox is null || !textBox.IsVisible || !textBox.IsEnabled)
    {
      return;
    }

    this._activeSegment = segment;
    this._returnToPreviousSegmentOnBackspace = null;
    textBox.Focus(navigationMethod);
    textBox.SelectAll();
  }

  private static bool IsSegmentFullySelected(TextBox textBox)
  {
    int textLength = textBox.Text?.Length ?? 0;
    int selectionStart = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
    int selectionEnd = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
    return textLength > 0 && selectionStart == 0 && selectionEnd == textLength;
  }

  private static bool IsCaretAtSegmentStart(TextBox textBox)
  {
    if (string.IsNullOrEmpty(textBox.Text))
    {
      return true;
    }

    int selectionStart = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
    int selectionEnd = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
    return selectionStart == 0 && selectionEnd == 0;
  }

  private void ResetCurrentSegment()
  {
    TextBox? textBox = this.GetTextBox(this._activeSegment);
    if (textBox is null)
    {
      return;
    }

    this._isUpdatingText = true;
    try
    {
      textBox.Text = this.GetDefaultSegmentText(this._activeSegment);
    }
    finally
    {
      this._isUpdatingText = false;
    }

    this.CommitSegments();
    this.SelectSegment(this._activeSegment);
  }

  private void OnSegmentGotFocus(object? sender, GotFocusEventArgs e)
  {
    if (sender is TextBox textBox)
    {
      this._activeSegment = this.GetSegment(textBox);
      if (this._returnToPreviousSegmentOnBackspace != this._activeSegment)
      {
        this._returnToPreviousSegmentOnBackspace = null;
      }

      textBox.SelectAll();
    }
  }

  private void OnSegmentKeyDown(object? sender, KeyEventArgs e)
  {
    if (sender is not TextBox textBox)
    {
      return;
    }

    this._activeSegment = this.GetSegment(textBox);

    switch (e.Key)
    {
      case Key.Up:
        this.SpinActiveSegment(SpinDirection.Increase);
        e.Handled = true;
        break;
      case Key.Down:
        this.SpinActiveSegment(SpinDirection.Decrease);
        e.Handled = true;
        break;
      case Key.Delete:
        this.ResetCurrentSegment();
        e.Handled = true;
        break;
      case Key.Back:
        if (this._returnToPreviousSegmentOnBackspace == this._activeSegment && IsSegmentFullySelected(textBox))
        {
          this._returnToPreviousSegmentOnBackspace = null;
          e.Handled = this.MoveFocusToAdjacentSegment(this._activeSegment, false, false);
        }
        else if (IsCaretAtSegmentStart(textBox))
        {
          this._returnToPreviousSegmentOnBackspace = null;
          e.Handled = this.MoveFocusToAdjacentSegment(this._activeSegment, false, false);
        }
        else if (IsSegmentFullySelected(textBox))
        {
          this._returnToPreviousSegmentOnBackspace = null;
          this.ResetCurrentSegment();
          e.Handled = true;
        }

        break;
      case Key.Left:
        this._returnToPreviousSegmentOnBackspace = null;
        this.MoveFocusToAdjacentSegment(this._activeSegment, false);
        e.Handled = true;
        break;
      case Key.Right:
        this._returnToPreviousSegmentOnBackspace = null;
        this.MoveFocusToAdjacentSegment(this._activeSegment, true);
        e.Handled = true;
        break;
      case Key.Enter:
        this.CommitSegments();
        e.Handled = true;
        break;
      case Key.Escape:
        this.UpdateSegmentTexts();
        e.Handled = true;
        break;
    }
  }

  private void OnSegmentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
  {
    if (this._isUpdatingText || e.Property != TextBox.TextProperty || sender is not TextBox textBox)
    {
      return;
    }

    TimeSegment segment = this.GetSegment(textBox);
    if (segment == TimeSegment.Period)
    {
      return;
    }

    string text = textBox.Text ?? string.Empty;
    if (textBox.MaxLength > 0 && text.Length >= textBox.MaxLength)
    {
      if (!this.IsCurrentSegmentValueValid(segment))
      {
        this.UpdateSegmentTexts();
        textBox.Focus(NavigationMethod.Tab);
        textBox.SelectAll();
        this._returnToPreviousSegmentOnBackspace = null;
        return;
      }

      this.MoveFocusToAdjacentSegment(segment, true, returnToPreviousOnBackspace: true);
    }
  }

  private void OnSegmentPointerPressed(object? sender, PointerPressedEventArgs e)
  {
    if (this._isUpdatingText || sender is not TextBox textBox || !textBox.IsEnabled)
    {
      return;
    }

    if (!e.GetCurrentPoint(textBox).Properties.IsLeftButtonPressed)
    {
      return;
    }

    this._activeSegment = this.GetSegment(textBox);
    this._returnToPreviousSegmentOnBackspace = null;
    textBox.Focus(NavigationMethod.Pointer);
    textBox.SelectAll();
    e.Handled = true;
  }

  private void OnSegmentTextInput(object? sender, TextInputEventArgs e)
  {
    if (this._isUpdatingText || this.IsReadOnly || sender is not TextBox textBox)
    {
      return;
    }

    TimeSegment segment = this.GetSegment(textBox);
    if (segment == TimeSegment.Period || string.IsNullOrEmpty(e.Text))
    {
      return;
    }

    if (e.Text.Any(character => !char.IsDigit(character)))
    {
      e.Handled = true;
      return;
    }

    if (this.TryGetImmediateSegmentValue(segment, textBox, e.Text, out string normalizedText))
    {
      this._isUpdatingText = true;
      try
      {
        textBox.Text = normalizedText;
      }
      finally
      {
        this._isUpdatingText = false;
      }

      e.Handled = true;
      this.MoveFocusToAdjacentSegment(segment, true, returnToPreviousOnBackspace: true);
    }
  }

  private void OnSegmentLostFocus(object? sender, RoutedEventArgs e)
  {
    this.CommitSegments();
  }

  private void OnSpinnerSpin(object? sender, SpinEventArgs e)
  {
    if (!this.AllowSpin || this.IsReadOnly)
    {
      return;
    }

    this.SpinActiveSegment(e.Direction);
    e.Handled = true;
  }

  private void SetSelectedTimeValue(TimeSpan? value)
  {
    if (value is null)
    {
      this.SetCurrentValue(SelectedTimeProperty, null);
      return;
    }

    TimeSpan normalized = Normalize(value.Value);
    if (!this.UseSeconds)
    {
      normalized = new TimeSpan(normalized.Hours, normalized.Minutes, 0);
    }

    this.SetCurrentValue(SelectedTimeProperty, normalized);
  }

  private void SpinActiveSegment(SpinDirection direction)
  {
    if (!this.IsSegmentAvailable(this._activeSegment))
    {
      this._activeSegment = TimeSegment.Hour;
    }

    double amount = this._activeSegment switch
    {
      TimeSegment.Hour => 3600,
      TimeSegment.Minute => 60 * this.MinuteIncrement,
      TimeSegment.Second => this.SecondIncrement,
      TimeSegment.Period => 43200,
      _ => 0
    };

    if (amount == 0)
    {
      return;
    }

    if (direction == SpinDirection.Decrease)
    {
      amount = -amount;
    }

    this.SetSelectedTimeValue(this.GetEffectiveTime().Add(TimeSpan.FromSeconds(amount)));
    this.SelectSegment(this._activeSegment, NavigationMethod.Unspecified);
  }

  private bool TryBuildTimeFromSegments(out TimeSpan? time)
  {
    string hourText = this._hourTextBox?.Text?.Trim() ?? string.Empty;
    string minuteText = this._minuteTextBox?.Text?.Trim() ?? string.Empty;
    string secondText = this._secondTextBox?.Text?.Trim() ?? string.Empty;
    string periodText = this._periodTextBox?.Text?.Trim() ?? string.Empty;

    if (IsAllBlank(hourText, minuteText, this.UseSeconds ? secondText : null, this.Is24HourClock() ? null : periodText))
    {
      time = null;
      return true;
    }

    if (!TryParseNumber(hourText, out int hour) || !TryParseNumber(minuteText, out int minute))
    {
      time = null;
      return false;
    }

    var second = 0;
    if (this.UseSeconds)
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

    if (this.Is24HourClock())
    {
      if (hour is < 0 or > 23)
      {
        time = null;
        return false;
      }
    }
    else
    {
      if (hour is < 1 or > 12 || !this.TryParsePeriod(periodText, out bool isPm))
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

    time = new TimeSpan(hour, minute, this.UseSeconds ? second : 0);
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

    textBox.GotFocus -= this.OnSegmentGotFocus;
    textBox.LostFocus -= this.OnSegmentLostFocus;
    textBox.RemoveHandler(KeyDownEvent, this.OnSegmentKeyDown);
    textBox.RemoveHandler(PointerPressedEvent, this.OnSegmentPointerPressed);
    textBox.RemoveHandler(TextInputEvent, this.OnSegmentTextInput);
    textBox.PropertyChanged -= this.OnSegmentPropertyChanged;
  }

  private void UnhookTemplateParts()
  {
    if (this._spinner is not null)
    {
      this._spinner.Spin -= this.OnSpinnerSpin;
    }

    this.UnhookSegment(this._hourTextBox);
    this.UnhookSegment(this._minuteTextBox);
    this.UnhookSegment(this._secondTextBox);
    this.UnhookSegment(this._periodTextBox);
  }

  private void UpdatePseudoClasses()
  {
    this.PseudoClasses.Set(":hasnotime", !this.SelectedTime.HasValue);
  }

  private void UpdateSegmentTexts()
  {
    if (this._hourTextBox is null || this._minuteTextBox is null || this._periodTextBox is null)
    {
      return;
    }

    this._isUpdatingText = true;

    try
    {
      if (this.SelectedTime is { } selectedTime)
      {
        this._hourTextBox.Text = this.FormatHourText(selectedTime);
        this._minuteTextBox.Text = selectedTime.Minutes.ToString("00", CultureInfo.InvariantCulture);

        if (this._secondTextBox is not null)
        {
          this._secondTextBox.Text = selectedTime.Seconds.ToString("00", CultureInfo.InvariantCulture);
        }

        this._periodTextBox.Text = this.Is24HourClock()
          ? string.Empty
          : selectedTime.Hours >= 12
            ? GetPmDesignator()
            : GetAmDesignator();
      }
      else
      {
        this._hourTextBox.Text = string.Empty;
        this._minuteTextBox.Text = string.Empty;

        if (this._secondTextBox is not null)
        {
          this._secondTextBox.Text = string.Empty;
        }

        this._periodTextBox.Text = string.Empty;
      }
    }
    finally
    {
      this._isUpdatingText = false;
    }
  }

  private void UpdateSegmentVisibility()
  {
    if (this._secondTextBox is not null)
    {
      this._secondTextBox.IsVisible = this.UseSeconds;
    }

    if (this._secondSeparator is not null)
    {
      this._secondSeparator.IsVisible = this.UseSeconds;
    }

    if (this._periodTextBox is not null)
    {
      this._periodTextBox.IsVisible = !this.Is24HourClock();
    }

    if (!this.IsSegmentAvailable(this._activeSegment))
    {
      this._activeSegment = TimeSegment.Hour;
    }
  }

  private void UpdateSpinner()
  {
    if (this._spinner is not null)
    {
      this._spinner.ValidSpinDirection = this.AllowSpin && !this.IsReadOnly
        ? ValidSpinDirections.Increase | ValidSpinDirections.Decrease
        : ValidSpinDirections.None;
    }
  }

  private enum TimeSegment
  {
    Hour,
    Minute,
    Second,
    Period
  }
}