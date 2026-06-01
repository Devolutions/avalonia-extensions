using Avalonia;

namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

/// <summary>
/// Attached behavior that turns the <see cref="CalendarDatePicker"/>'s text box into a
/// segmented date input (year / month / day), mimicking native DevExpress and macOS
/// date editors:
/// <list type="bullet">
///   <item>Clicking inside the text selects the whole segment under the cursor.</item>
///   <item>Left/Right move between segments.</item>
///   <item>Up/Down increment / decrement the active segment.</item>
///   <item>Digits overwrite the active segment.</item>
///   <item>The separator character commits the current segment and advances.</item>
///   <item>Backspace resets the active segment to its minimum value.</item>
/// </list>
/// <para>
/// The behavior is a no-op for non-numeric date formats (anything containing
/// <c>MMM</c>, <c>MMMM</c>, <c>ddd</c>, or <c>dddd</c>) — the
/// <see cref="CalendarDatePicker.SelectedDateFormatProperty"/> set to
/// <see cref="CalendarDatePickerFormat.Long"/> in particular keeps its default
/// free-text editing behavior.
/// </para>
/// </summary>
public static class SegmentedDateInputBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, bool>("IsEnabled", typeof(SegmentedDateInputBehavior));

    private static readonly AttachedProperty<SegmentedState?> StateProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, SegmentedState?>("State", typeof(SegmentedDateInputBehavior));

    static SegmentedDateInputBehavior()
    {
        IsEnabledProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not CalendarDatePicker picker) return;

            if (args.NewValue.GetValueOrDefault<bool>())
            {
                Attach(picker);
            }
            else
            {
                Detach(picker);
            }
        });
    }

    public static void SetIsEnabled(CalendarDatePicker element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(CalendarDatePicker element) => element.GetValue(IsEnabledProperty);

    private static void Attach(CalendarDatePicker picker)
    {
        if (picker.GetValue(StateProperty) != null) return;

        SegmentedState state = new(picker);
        picker.SetValue(StateProperty, state);
        state.Hook();
    }

    private static void Detach(CalendarDatePicker picker)
    {
        SegmentedState? state = picker.GetValue(StateProperty);
        if (state == null) return;

        state.Unhook();
        picker.SetValue(StateProperty, null);
    }

    // ------------------------------------------------------------------------------------------
    // Per-picker state
    // ------------------------------------------------------------------------------------------

    private sealed class SegmentedState
    {
        private readonly CalendarDatePicker _picker;
        private TextBox? _textBox;
        private List<Segment> _segments = new();
        private int _activeIndex = -1;
        private string _editBuffer = string.Empty;
        private bool _suspendSelectionSync;
        private bool _suspendTextSync;
        private bool _segmentable;

        public SegmentedState(CalendarDatePicker picker)
        {
            _picker = picker;
        }

        public void Hook()
        {
            _picker.TemplateApplied += OnTemplateApplied;
            _picker.PropertyChanged += OnPickerPropertyChanged;
            TryHookTextBox();
        }

        public void Unhook()
        {
            _picker.TemplateApplied -= OnTemplateApplied;
            _picker.PropertyChanged -= OnPickerPropertyChanged;
            UnhookTextBox();
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            UnhookTextBox();
            _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
            HookTextBox();
        }

        private void TryHookTextBox()
        {
            if (_textBox != null) return;
            _textBox = _picker.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PART_TextBox")
                       ?? _picker.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            HookTextBox();
        }

        private void HookTextBox()
        {
            if (_textBox == null) return;
            _textBox.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            _textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
            _textBox.GotFocus += OnTextBoxGotFocus;
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.PropertyChanged += OnTextBoxPropertyChanged;
            RebuildSegments();
        }

        private void UnhookTextBox()
        {
            if (_textBox == null) return;
            _textBox.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            _textBox.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
            _textBox.GotFocus -= OnTextBoxGotFocus;
            _textBox.LostFocus -= OnTextBoxLostFocus;
            _textBox.PropertyChanged -= OnTextBoxPropertyChanged;
            _textBox = null;
            _segments = new();
            _activeIndex = -1;
            _editBuffer = string.Empty;
        }

        // --------------------------------------------------------------------------------------
        // Reactions
        // --------------------------------------------------------------------------------------

        private void OnPickerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CalendarDatePicker.SelectedDateFormatProperty ||
                e.Property == CalendarDatePicker.CustomDateFormatStringProperty)
            {
                // Format change → defer until the picker re-renders the text.
                Dispatcher.UIThread.Post(RebuildSegments, DispatcherPriority.Background);
            }
        }

        private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                if (_suspendTextSync) return;
                RebuildSegments();
            }
            else if ((e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty) &&
                     !_suspendSelectionSync && _segmentable)
            {
                SnapSelectionToSegment();
            }
        }

        private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (!_segmentable) return;
            // Run after base CalendarDatePicker.OnGotFocus, which selects all on Tab nav.
            Dispatcher.UIThread.Post(() =>
            {
                if (_textBox == null || !_textBox.IsFocused) return;
                _editBuffer = string.Empty;
                SelectSegment(_activeIndex >= 0 ? _activeIndex : FirstEditableSegment());
            }, DispatcherPriority.Input);
        }

        private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            CommitBuffer();
        }

        // --------------------------------------------------------------------------------------
        // Tokenization / rendering
        // --------------------------------------------------------------------------------------

        private void RebuildSegments()
        {
            if (_textBox == null)
            {
                _segmentable = false;
                return;
            }

            string pattern = ResolvePattern();
            List<Segment>? parsed = DateFormatTokenizer.Tokenize(pattern);
            if (parsed == null)
            {
                _segmentable = false;
                _segments = new();
                _activeIndex = -1;
                return;
            }

            _segmentable = true;

            // Render the segments against the current SelectedDate (or DisplayDate as fallback)
            // so we know the exact character offsets for selection-snapping.
            DateTime renderDate = _picker.SelectedDate ?? _picker.DisplayDate;
            (_, _segments) = DateFormatTokenizer.RenderSegments(parsed, renderDate);

            // If picker has no SelectedDate, the textbox is empty (watermark shown).
            // Don't force-render; just keep segments in standby for first interaction.
            if (_activeIndex < 0 || _activeIndex >= _segments.Count || _segments[_activeIndex].Kind == SegmentKind.Literal)
            {
                _activeIndex = FirstEditableSegment();
            }
        }

        private string ResolvePattern()
        {
            DateTimeFormatInfo dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
            return _picker.SelectedDateFormat switch
            {
                CalendarDatePickerFormat.Long   => dtfi.LongDatePattern,
                CalendarDatePickerFormat.Custom => string.IsNullOrEmpty(_picker.CustomDateFormatString)
                    ? dtfi.ShortDatePattern
                    : ExpandIfStandardSpecifier(_picker.CustomDateFormatString!, dtfi),
                _ => dtfi.ShortDatePattern,
            };
        }

        /// <summary>
        /// Expands a single-character .NET standard date/time format specifier (e.g. "d", "D")
        /// to its full pattern so the tokenizer sees the real token structure.
        /// Single-character custom tokens ("%d", "%M", …) are left for the tokenizer's "%" handling.
        /// </summary>
        private static string ExpandIfStandardSpecifier(string pattern, DateTimeFormatInfo dtfi)
        {
            if (pattern.Length != 1) return pattern;
            return pattern[0] switch
            {
                'd' => dtfi.ShortDatePattern,
                'D' => dtfi.LongDatePattern,
                'f' => $"{dtfi.LongDatePattern} {dtfi.ShortTimePattern}",
                'F' => dtfi.FullDateTimePattern,
                'g' => $"{dtfi.ShortDatePattern} {dtfi.ShortTimePattern}",
                'G' => $"{dtfi.ShortDatePattern} {dtfi.LongTimePattern}",
                'm' or 'M' => dtfi.MonthDayPattern,
                'r' or 'R' => dtfi.RFC1123Pattern,
                's' => dtfi.SortableDateTimePattern,
                't' => dtfi.ShortTimePattern,
                'T' => dtfi.LongTimePattern,
                'u' => dtfi.UniversalSortableDateTimePattern,
                'U' => dtfi.FullDateTimePattern,
                'y' or 'Y' => dtfi.YearMonthPattern,
                _ => pattern,
            };
        }

        private int FirstEditableSegment()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].Kind != SegmentKind.Literal) return i;
            }
            return -1;
        }

        // --------------------------------------------------------------------------------------
        // Selection snap
        // --------------------------------------------------------------------------------------

        private void SnapSelectionToSegment()
        {
            if (_textBox == null || _segments.Count == 0) return;
            if (string.IsNullOrEmpty(_textBox.Text)) return;

            // Allow a full-text selection (e.g. Ctrl+A) to persist unsnapped so the user
            // can then press Delete/Backspace to clear the date.
            if (_textBox.SelectionStart == 0 && _textBox.SelectionEnd == _textBox.Text!.Length)
                return;

            int caret = _textBox.SelectionStart;
            int seg = FindSegmentAt(caret);
            if (seg < 0) return;

            if (seg != _activeIndex)
            {
                CommitBuffer();
                _activeIndex = seg;
            }

            SelectSegment(seg);
        }

        private int FindSegmentAt(int caret)
        {
            // Prefer the segment whose range contains the caret; if the caret is on a literal
            // boundary, prefer the next editable segment to the right, then to the left.
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment s = _segments[i];
                if (s.Kind == SegmentKind.Literal) continue;
                if (caret >= s.Start && caret <= s.Start + s.Length) return i;
            }

            // Find the nearest editable segment.
            int best = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment s = _segments[i];
                if (s.Kind == SegmentKind.Literal) continue;
                int dist = caret < s.Start ? s.Start - caret : caret - (s.Start + s.Length);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }

        private void SelectSegment(int index)
        {
            if (_textBox == null || index < 0 || index >= _segments.Count) return;
            Segment s = _segments[index];
            string? text = _textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            int start = Math.Min(s.Start, text.Length);
            int end = Math.Min(s.Start + s.Length, text.Length);

            _suspendSelectionSync = true;
            try
            {
                _textBox.SelectionStart = start;
                _textBox.SelectionEnd = end;
            }
            finally
            {
                _suspendSelectionSync = false;
            }
        }

        // --------------------------------------------------------------------------------------
        // Key handling
        // --------------------------------------------------------------------------------------

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_segmentable || _segments.Count == 0) return;

            switch (e.Key)
            {
                case Key.Left:
                    MoveActiveSegment(-1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveActiveSegment(+1);
                    e.Handled = true;
                    break;
                case Key.Up:
                    AdjustActiveSegment(+1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    AdjustActiveSegment(-1);
                    e.Handled = true;
                    break;
                case Key.Back:
                case Key.Delete:
                    // Full-text selected (e.g. after Ctrl+A) → clear the date entirely.
                    if (_textBox != null && !string.IsNullOrEmpty(_textBox.Text)
                        && _textBox.SelectionStart == 0 && _textBox.SelectionEnd == _textBox.Text!.Length)
                    {
                        ClearSelectedDate();
                    }
                    else
                    {
                        ResetActiveSegmentToMin();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            if (!_segmentable || _segments.Count == 0) return;
            if (string.IsNullOrEmpty(e.Text)) return;

            foreach (char c in e.Text)
            {
                if (char.IsDigit(c))
                {
                    HandleDigit(c);
                }
                else if (IsSeparatorChar(c))
                {
                    CommitBuffer();
                    MoveActiveSegment(+1);
                }
                // Silently swallow everything else (letters, punctuation, etc.).
            }

            e.Handled = true;
        }

        private static bool IsCommonDateSeparator(char c) =>
            c is '/' or '-' or '.' or ',' or ':' or ' ';

        private bool IsSeparatorChar(char c)
        {
            // Accept any common date separator universally so users aren't forced to match
            // the configured format's specific separator.
            if (IsCommonDateSeparator(c)) return true;

            if (_activeIndex < 0 || _activeIndex >= _segments.Count) return false;
            // Also accept any literal character that follows the active segment in the pattern
            // (e.g. exotic localized separators like '年' / '월').
            for (int i = _activeIndex + 1; i < _segments.Count; i++)
            {
                Segment s = _segments[i];
                if (s.Kind != SegmentKind.Literal) return false;
                if (!string.IsNullOrEmpty(s.LiteralText) && s.LiteralText!.IndexOf(c) >= 0) return true;
            }
            return false;
        }

        // --------------------------------------------------------------------------------------
        // Edits
        // --------------------------------------------------------------------------------------

        private void EnsureSelectedDate()
        {
            if (_picker.SelectedDate.HasValue) return;
            DateTime seed = _picker.DisplayDate;
            SetSelectedDate(seed);
        }

        private void MoveActiveSegment(int delta)
        {
            CommitBuffer();
            if (_segments.Count == 0) return;
            int idx = _activeIndex;
            int step = Math.Sign(delta);
            if (step == 0) return;

            for (int i = 0; i < _segments.Count; i++)
            {
                idx += step;
                if (idx < 0 || idx >= _segments.Count) return; // do not wrap
                if (_segments[idx].Kind != SegmentKind.Literal)
                {
                    _activeIndex = idx;
                    int target = _activeIndex;
                    // Defer so we run after the TextBox's own KeyDown handler has finished
                    // moving the caret in response to Left/Right.
                    Dispatcher.UIThread.Post(() => SelectSegment(target), DispatcherPriority.Background);
                    return;
                }
            }
        }

        private void AdjustActiveSegment(int delta)
        {
            CommitBuffer();
            EnsureSelectedDate();
            if (_activeIndex < 0 || _activeIndex >= _segments.Count) return;
            DateTime current = _picker.SelectedDate ?? _picker.DisplayDate;

            DateTime? candidate = _segments[_activeIndex].Kind switch
            {
                SegmentKind.Year  => SafeAddYears(current, delta),
                SegmentKind.Month => SafeAddMonths(current, delta),
                SegmentKind.Day   => SafeAddDays(current, delta),
                _ => null,
            };
            if (candidate == null) return;

            candidate = ClampToPickerRange(candidate.Value);
            SetSelectedDate(candidate.Value);
            // Re-select the same segment after the text re-renders.
            int target = _activeIndex;
            Dispatcher.UIThread.Post(() => SelectSegment(target), DispatcherPriority.Background);
        }

        private void ResetActiveSegmentToMin()
        {
            CommitBuffer();
            // If no date is currently selected, there is nothing to reset.
            if (!_picker.SelectedDate.HasValue) return;
            if (_activeIndex < 0 || _activeIndex >= _segments.Count) return;
            DateTime current = _picker.SelectedDate ?? _picker.DisplayDate;

            DateTime? candidate = _segments[_activeIndex].Kind switch
            {
                SegmentKind.Year  => SafeWithYear(current, 1),
                SegmentKind.Month => SafeWithMonth(current, 1),
                SegmentKind.Day   => SafeWithDay(current, 1),
                _ => null,
            };
            if (candidate == null) return;

            candidate = ClampToPickerRange(candidate.Value);
            SetSelectedDate(candidate.Value);
            int target = _activeIndex;
            Dispatcher.UIThread.Post(() => SelectSegment(target), DispatcherPriority.Background);
        }

        private void HandleDigit(char digit)
        {
            if (_activeIndex < 0 || _activeIndex >= _segments.Count) return;

            Segment seg = _segments[_activeIndex];
            int maxDigits = seg.Kind switch
            {
                SegmentKind.Year  => seg.TokenLength > 0 && seg.TokenLength < 4 ? seg.TokenLength : 4,
                SegmentKind.Month => 2,
                SegmentKind.Day   => 2,
                _ => 0,
            };
            if (maxDigits == 0) return;

            string candidate = (_editBuffer + digit);
            if (candidate.Length > maxDigits)
            {
                candidate = digit.ToString();
            }

            if (!int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return;
            }

            // For Month/Day, "0" is a valid prefix (continues a "01"/"02" entry) but
            // we don't apply it yet — only persist the buffer until a valid date emerges.
            if (TryApplySegmentValue(seg.Kind, value, seg.TokenLength, out DateTime applied))
            {
                _editBuffer = candidate;
                SetSelectedDate(applied);
                int target = _activeIndex;
                Dispatcher.UIThread.Post(() => SelectSegment(target), DispatcherPriority.Background);

                // If the buffer is now "full" and any further digit would force a restart,
                // auto-commit but stay on the segment (the next digit will then start over).
                if (candidate.Length >= maxDigits)
                {
                    _editBuffer = string.Empty;
                }
            }
            else if (candidate.Length == 1)
            {
                // First digit doesn't yield a valid value (e.g. "0" for month). Keep the
                // buffer so the next digit can complete it ("0" + "1" → "01").
                _editBuffer = candidate;
            }
            // else: invalid candidate, ignore.
        }

        private bool TryApplySegmentValue(SegmentKind kind, int value, int tokenLength, out DateTime result)
        {
            DateTime current = _picker.SelectedDate ?? _picker.DisplayDate;
            switch (kind)
            {
                case SegmentKind.Year:
                    // For short year tokens (y, yy, yyy), convert via the culture's two-digit year
                    // window so that typing "26" with a "yy" pattern gives 2026, not 0026.
                    int fullYear = (tokenLength > 0 && tokenLength < 4)
                        ? CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(value)
                        : value;
                    if (fullYear < 1 || fullYear > 9999) { result = default; return false; }
                    DateTime? y = SafeWithYear(current, fullYear);
                    if (y == null) { result = default; return false; }
                    result = ClampToPickerRange(y.Value);
                    return true;
                case SegmentKind.Month:
                    if (value < 1 || value > 12) { result = default; return false; }
                    DateTime? m = SafeWithMonth(current, value);
                    if (m == null) { result = default; return false; }
                    result = ClampToPickerRange(m.Value);
                    return true;
                case SegmentKind.Day:
                    if (value < 1) { result = default; return false; }
                    int daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
                    if (value > daysInMonth) { result = default; return false; }
                    DateTime? d = SafeWithDay(current, value);
                    if (d == null) { result = default; return false; }
                    result = ClampToPickerRange(d.Value);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        private void CommitBuffer() => _editBuffer = string.Empty;

        // --------------------------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------------------------

        private void SetSelectedDate(DateTime value)
        {
            _suspendTextSync = true;
            try
            {
                _picker.SetCurrentValue(CalendarDatePicker.SelectedDateProperty, value);
            }
            finally
            {
                _suspendTextSync = false;
            }
            // The picker schedules the textbox update via Dispatcher.UIThread.InvokeAsync,
            // so we must defer our segment rebuild & re-snap.
            Dispatcher.UIThread.Post(RebuildSegments, DispatcherPriority.Background);
        }

        private void ClearSelectedDate()
        {
            _editBuffer = string.Empty;
            _suspendTextSync = true;
            try
            {
                _picker.SetCurrentValue(CalendarDatePicker.SelectedDateProperty, (DateTime?)null);
            }
            finally
            {
                _suspendTextSync = false;
            }
            Dispatcher.UIThread.Post(RebuildSegments, DispatcherPriority.Background);
        }

        private DateTime ClampToPickerRange(DateTime date)
        {
            if (_picker.DisplayDateStart is { } start && date < start) return start;
            if (_picker.DisplayDateEnd is { } end && date > end) return end;
            return date;
        }

        private static DateTime? SafeAddYears(DateTime d, int delta)
        {
            try { return d.AddYears(delta); } catch { return null; }
        }

        private static DateTime? SafeAddMonths(DateTime d, int delta)
        {
            try { return d.AddMonths(delta); } catch { return null; }
        }

        private static DateTime? SafeAddDays(DateTime d, int delta)
        {
            try { return d.AddDays(delta); } catch { return null; }
        }

        private static DateTime? SafeWithYear(DateTime d, int year)
        {
            if (year < 1 || year > 9999) return null;
            int day = Math.Min(d.Day, DateTime.DaysInMonth(year, d.Month));
            try { return new DateTime(year, d.Month, day, d.Hour, d.Minute, d.Second, d.Kind); }
            catch { return null; }
        }

        private static DateTime? SafeWithMonth(DateTime d, int month)
        {
            if (month < 1 || month > 12) return null;
            int day = Math.Min(d.Day, DateTime.DaysInMonth(d.Year, month));
            try { return new DateTime(d.Year, month, day, d.Hour, d.Minute, d.Second, d.Kind); }
            catch { return null; }
        }

        private static DateTime? SafeWithDay(DateTime d, int day)
        {
            if (day < 1) return null;
            int max = DateTime.DaysInMonth(d.Year, d.Month);
            if (day > max) return null;
            try { return new DateTime(d.Year, d.Month, day, d.Hour, d.Minute, d.Second, d.Kind); }
            catch { return null; }
        }
    }

    // ------------------------------------------------------------------------------------------
    // Tokenizer
    // ------------------------------------------------------------------------------------------

    internal enum SegmentKind { Year, Month, Day, Literal }

    internal sealed class Segment
    {
        public SegmentKind Kind;
        public int Start;       // character index in rendered text
        public int Length;      // character length in rendered text
        public int TokenLength; // pattern token length (1, 2, 4 …); 0 for literals
        public string? LiteralText;
    }

    internal static class DateFormatTokenizer
    {
        /// <summary>
        /// Parses a .NET date format pattern into an ordered list of segments.
        /// Returns <c>null</c> if the pattern contains any non-numeric date token
        /// (<c>MMM</c>, <c>MMMM</c>, <c>ddd</c>, <c>dddd</c>) or any time tokens
        /// (<c>h</c>, <c>H</c>, <c>m</c>, <c>s</c>, …), in which case segmented
        /// editing should be disabled.
        /// </summary>
        public static List<Segment>? Tokenize(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return null;

            List<Segment> segments = new();
            StringBuilder literal = new();
            int i = 0;

            while (i < pattern.Length)
            {
                char c = pattern[i];

                // Quoted literal: '...'
                if (c == '\'' || c == '"')
                {
                    char quote = c;
                    i++;
                    while (i < pattern.Length && pattern[i] != quote)
                    {
                        literal.Append(pattern[i]);
                        i++;
                    }
                    if (i < pattern.Length) i++; // skip closing quote
                    continue;
                }

                // Escape: \X
                if (c == '\\' && i + 1 < pattern.Length)
                {
                    literal.Append(pattern[i + 1]);
                    i += 2;
                    continue;
                }

                // % modifier: single-char custom format disambiguator (e.g. "%d" means just the day).
                // Strip it and let the next character be processed normally.
                if (c == '%' && i + 1 < pattern.Length)
                {
                    i++;
                    continue;
                }

                if (c == 'y' || c == 'M' || c == 'd')
                {
                    // Flush pending literal
                    if (literal.Length > 0)
                    {
                        segments.Add(new Segment
                        {
                            Kind = SegmentKind.Literal,
                            LiteralText = literal.ToString(),
                            TokenLength = 0,
                        });
                        literal.Clear();
                    }

                    int runStart = i;
                    while (i < pattern.Length && pattern[i] == c) i++;
                    int runLen = i - runStart;

                    SegmentKind kind;
                    switch (c)
                    {
                        case 'y':
                            kind = SegmentKind.Year;
                            break;
                        case 'M':
                            if (runLen >= 3) return null; // MMM / MMMM → not segmentable
                            kind = SegmentKind.Month;
                            break;
                        case 'd':
                            if (runLen >= 3) return null; // ddd / dddd → not segmentable
                            kind = SegmentKind.Day;
                            break;
                        default:
                            return null;
                    }

                    segments.Add(new Segment { Kind = kind, TokenLength = runLen });
                    continue;
                }

                // Unsupported tokens → bail
                if (c == 'h' || c == 'H' || c == 'm' || c == 's' || c == 'f' || c == 'F' ||
                    c == 't' || c == 'z' || c == 'g' || c == 'K')
                {
                    return null;
                }

                literal.Append(c);
                i++;
            }

            if (literal.Length > 0)
            {
                segments.Add(new Segment
                {
                    Kind = SegmentKind.Literal,
                    LiteralText = literal.ToString(),
                    TokenLength = 0,
                });
            }

            // No editable segments → not segmentable (e.g. literal-only patterns or
            // standard format specifiers like "D" that the caller hasn't expanded).
            if (!segments.Any(s => s.Kind != SegmentKind.Literal))
            {
                return null;
            }

            return segments;
        }

        /// <summary>
        /// Renders the supplied date through the parsed segments and returns the
        /// concatenated text plus segments updated with <see cref="Segment.Start"/>
        /// and <see cref="Segment.Length"/> offsets.
        /// </summary>
        public static (string Text, List<Segment> Segments) RenderSegments(List<Segment> tokens, DateTime date)
        {
            StringBuilder sb = new();
            List<Segment> result = new(tokens.Count);

            foreach (Segment t in tokens)
            {
                int start = sb.Length;
                string piece;
                switch (t.Kind)
                {
                    case SegmentKind.Year:
                        piece = t.TokenLength switch
                        {
                            1 => date.Year.ToString(CultureInfo.InvariantCulture),
                            2 => (date.Year % 100).ToString("D2", CultureInfo.InvariantCulture),
                            _ => date.Year.ToString("D" + t.TokenLength, CultureInfo.InvariantCulture),
                        };
                        break;
                    case SegmentKind.Month:
                        piece = t.TokenLength == 1
                            ? date.Month.ToString(CultureInfo.InvariantCulture)
                            : date.Month.ToString("D2", CultureInfo.InvariantCulture);
                        break;
                    case SegmentKind.Day:
                        piece = t.TokenLength == 1
                            ? date.Day.ToString(CultureInfo.InvariantCulture)
                            : date.Day.ToString("D2", CultureInfo.InvariantCulture);
                        break;
                    default:
                        piece = t.LiteralText ?? string.Empty;
                        break;
                }
                sb.Append(piece);

                result.Add(new Segment
                {
                    Kind = t.Kind,
                    Start = start,
                    Length = piece.Length,
                    TokenLength = t.TokenLength,
                    LiteralText = t.LiteralText,
                });
            }

            return (sb.ToString(), result);
        }
    }
}
