using Avalonia;

namespace Devolutions.AvaloniaControls.Behaviors;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    private static readonly AttachedProperty<SegmentedState?> stateProperty =
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
        if (picker.GetValue(stateProperty) != null) return;

        SegmentedState state = new(picker);
        picker.SetValue(stateProperty, state);
        state.Hook();
    }

    private static void Detach(CalendarDatePicker picker)
    {
        SegmentedState? state = picker.GetValue(stateProperty);
        if (state == null) return;

        state.Unhook();
        picker.SetValue(stateProperty, null);
    }

    // ------------------------------------------------------------------------------------------
    // Per-picker state
    // ------------------------------------------------------------------------------------------

    private sealed class SegmentedState
    {
        private readonly CalendarDatePicker picker;
        private TextBox? textBox;
        private List<Segment> segments = new();
        private int activeIndex = -1;
        private string editBuffer = string.Empty;
        private bool suspendSelectionSync;
        private bool suspendTextSync;
        private bool segmentable;
        private bool pointerDown;

        public SegmentedState(CalendarDatePicker picker)
        {
            this.picker = picker;
        }

        public void Hook()
        {
            this.picker.TemplateApplied += this.OnTemplateApplied;
            this.picker.PropertyChanged += this.OnPickerPropertyChanged;
            this.TryHookTextBox();
        }

        public void Unhook()
        {
            this.picker.TemplateApplied -= this.OnTemplateApplied;
            this.picker.PropertyChanged -= this.OnPickerPropertyChanged;
            this.UnhookTextBox();
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            this.UnhookTextBox();
            this.textBox = e.NameScope.Find<TextBox>("PART_TextBox");
            this.HookTextBox();
        }

        private void TryHookTextBox()
        {
            if (this.textBox != null) return;
            this.textBox = this.picker.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PART_TextBox")
                            ?? this.picker.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            this.HookTextBox();
        }

        private void HookTextBox()
        {
            if (this.textBox == null) return;
            this.textBox.AddHandler(InputElement.KeyDownEvent, this.OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            this.textBox.AddHandler(InputElement.TextInputEvent, this.OnTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
            this.textBox.AddHandler(InputElement.PointerPressedEvent, this.OnPointerPressed, RoutingStrategies.Tunnel);
            this.textBox.AddHandler(InputElement.PointerReleasedEvent, this.OnPointerReleased, RoutingStrategies.Tunnel);
            this.textBox.GotFocus += this.OnTextBoxGotFocus;
            this.textBox.LostFocus += this.OnTextBoxLostFocus;
            this.textBox.PropertyChanged += this.OnTextBoxPropertyChanged;
            this.RebuildSegments();
        }

        private void UnhookTextBox()
        {
            if (this.textBox == null) return;
            this.textBox.RemoveHandler(InputElement.KeyDownEvent, this.OnKeyDown);
            this.textBox.RemoveHandler(InputElement.TextInputEvent, this.OnTextInput);
            this.textBox.RemoveHandler(InputElement.PointerPressedEvent, this.OnPointerPressed);
            this.textBox.RemoveHandler(InputElement.PointerReleasedEvent, this.OnPointerReleased);
            this.textBox.GotFocus -= this.OnTextBoxGotFocus;
            this.textBox.LostFocus -= this.OnTextBoxLostFocus;
            this.textBox.PropertyChanged -= this.OnTextBoxPropertyChanged;
            this.textBox = null;
            this.segments = new();
            this.activeIndex = -1;
            this.editBuffer = string.Empty;
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
                Dispatcher.UIThread.Post(this.RebuildSegments, DispatcherPriority.Background);
            }
        }

        private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                if (this.suspendTextSync) return;
                this.RebuildSegments();
            }
            else if ((e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty) &&
                     !this.suspendSelectionSync && this.segmentable)
            {
                this.SnapSelectionToSegment();
            }
        }

        private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (!this.segmentable) return;
            // Run after base CalendarDatePicker.OnGotFocus, which selects all on Tab nav.
            Dispatcher.UIThread.Post(() =>
            {
                if (this.textBox == null || !this.textBox.IsFocused) return;
                this.editBuffer = string.Empty;
                this.SelectSegment(this.activeIndex >= 0 ? this.activeIndex : this.FirstEditableSegment());
            }, DispatcherPriority.Input);
        }

        private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            this.pointerDown = false;
            this.CommitBuffer();
            // Collapse the selection so the highlight doesn't persist while unfocused.
            if (this.textBox != null)
            {
                this.suspendSelectionSync = true;
                try {
                    this.textBox.SelectionStart = this.textBox.SelectionEnd = 0; }
                finally {
                    this.suspendSelectionSync = false; }
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this.pointerDown = true;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            this.pointerDown = false;
            // After a click or drag, snap the final selection to the appropriate segment(s).
            if (this.segmentable) this.SnapSelectionToSegment();
        }

        // --------------------------------------------------------------------------------------
        // Tokenization / rendering
        // --------------------------------------------------------------------------------------

        private void RebuildSegments()
        {
            if (this.textBox == null)
            {
                this.segmentable = false;
                return;
            }

            string pattern = this.ResolvePattern();
            List<Segment>? parsed = DateFormatTokenizer.Tokenize(pattern);
            if (parsed == null)
            {
                this.segmentable = false;
                this.segments = new();
                this.activeIndex = -1;
                return;
            }

            this.segmentable = true;

            // Render the segments against the current SelectedDate (or DisplayDate as fallback)
            // so we know the exact character offsets for selection-snapping.
            DateTime renderDate = this.picker.SelectedDate ?? this.picker.DisplayDate;
            this.segments = DateFormatTokenizer.RenderSegments(parsed, renderDate);

            // If picker has no SelectedDate, the textbox is empty (watermark shown).
            // Don't force-render; just keep segments in standby for first interaction.
            if (this.activeIndex < 0 || this.activeIndex >= this.segments.Count || this.segments[this.activeIndex].kind == SegmentKind.Literal)
            {
                this.activeIndex = this.FirstEditableSegment();
            }
        }

        private string ResolvePattern()
        {
            DateTimeFormatInfo dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
            return this.picker.SelectedDateFormat switch
            {
                CalendarDatePickerFormat.Long   => dtfi.LongDatePattern,
                CalendarDatePickerFormat.Custom => string.IsNullOrEmpty(this.picker.CustomDateFormatString)
                    ? dtfi.ShortDatePattern
                    : ExpandIfStandardSpecifier(this.picker.CustomDateFormatString, dtfi),
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
            for (int i = 0; i < this.segments.Count; i++)
            {
                if (this.segments[i].kind != SegmentKind.Literal) return i;
            }
            return -1;
        }

        // --------------------------------------------------------------------------------------
        // Selection snap
        // --------------------------------------------------------------------------------------

        private void SnapSelectionToSegment()
        {
            if (this.textBox == null || this.segments.Count == 0) return;
            if (string.IsNullOrEmpty(this.textBox.Text)) return;

            int selStart = this.textBox.SelectionStart;
            int selEnd   = this.textBox.SelectionEnd;

            // Normalize: Avalonia's SelectionStart/End are not guaranteed to be in order.
            int minSel = Math.Min(selStart, selEnd);
            int maxSel = Math.Max(selStart, selEnd);

            // Full-text selection (Ctrl+A / triple-click): leave it — Delete will clear the date.
            if (minSel == 0 && maxSel == this.textBox.Text!.Length) return;

            // During a pointer drag, don't fight Avalonia's live selection — just track which
            // segment the drag started in.  We snap on PointerReleased instead.
            if (this.pointerDown)
            {
                if (maxSel > minSel)
                {
                    int firstSeg = this.FindSegmentAt(minSel);
                    if (firstSeg >= 0 && firstSeg != this.activeIndex) {
                        this.CommitBuffer();
                        this.activeIndex = firstSeg; }
                }
                return;
            }

            // Multi-segment drag: if the selection spans more than one editable segment,
            // leave the selection as-is so the user sees what they've spanned.
            // Delete/Backspace will clear the date in this state.
            if (maxSel > minSel && this.SpansMultipleSegments(minSel, maxSel))
            {
                int firstSeg = this.FindSegmentAt(minSel);
                if (firstSeg >= 0 && firstSeg != this.activeIndex) {
                    this.CommitBuffer();
                    this.activeIndex = firstSeg; }
                return;
            }

            int seg = this.FindSegmentAt(minSel);
            if (seg < 0) return;

            if (seg != this.activeIndex)
            {
                this.CommitBuffer();
                this.activeIndex = seg;
            }

            this.SelectSegment(seg);
        }

        /// <summary>
        /// Returns true if the character range [<paramref name="minSel"/>, <paramref name="maxSel"/>)
        /// overlaps two or more distinct editable segments.
        /// </summary>
        private bool SpansMultipleSegments(int minSel, int maxSel)
        {
            int count = 0;
            foreach (Segment s in this.segments)
            {
                if (s.kind == SegmentKind.Literal) continue;
                // Standard half-open range overlap: segment [s.Start, s.Start+s.Length) ∩ [minSel, maxSel)
                if (s.start < maxSel && s.start + s.length > minSel)
                {
                    if (++count >= 2) return true;
                }
            }
            return false;
        }

        private int FindSegmentAt(int caret)
        {
            // Prefer the segment whose range contains the caret; if the caret is on a literal
            // boundary, prefer the next editable segment to the right, then to the left.
            for (int i = 0; i < this.segments.Count; i++)
            {
                Segment s = this.segments[i];
                if (s.kind == SegmentKind.Literal) continue;
                if (caret >= s.start && caret <= s.start + s.length) return i;
            }

            // Find the nearest editable segment.
            int best = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < this.segments.Count; i++)
            {
                Segment s = this.segments[i];
                if (s.kind == SegmentKind.Literal) continue;
                int dist = caret < s.start ? s.start - caret : caret - (s.start + s.length);
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
            if (this.textBox == null || index < 0 || index >= this.segments.Count) return;
            Segment s = this.segments[index];
            string? text = this.textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            int start = Math.Min(s.start, text.Length);
            int end = Math.Min(s.start + s.length, text.Length);

            this.suspendSelectionSync = true;
            try
            {
                this.textBox.SelectionStart = start;
                this.textBox.SelectionEnd = end;
            }
            finally
            {
                this.suspendSelectionSync = false;
            }
        }

        // --------------------------------------------------------------------------------------
        // Key handling
        // --------------------------------------------------------------------------------------

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!this.segmentable || this.segments.Count == 0) return;

            switch (e.Key)
            {
                case Key.Left:
                    this.MoveActiveSegment(-1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    this.MoveActiveSegment(+1);
                    e.Handled = true;
                    break;
                case Key.Up:
                    this.AdjustActiveSegment(+1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    this.AdjustActiveSegment(-1);
                    e.Handled = true;
                    break;
                case Key.Back:
                case Key.Delete:
                    // Full-text selected (Ctrl+A / triple-click) OR multi-segment drag → clear date.
                    if (this.textBox != null && !string.IsNullOrEmpty(this.textBox.Text)
                                              && this.IsMultiOrFullSelection())
                    {
                        this.ClearSelectedDate();
                    }
                    else
                    {
                        this.ResetActiveSegmentToMin();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            if (!this.segmentable || this.segments.Count == 0) return;
            if (string.IsNullOrEmpty(e.Text)) return;

            foreach (char c in e.Text)
            {
                if (char.IsDigit(c))
                {
                    this.HandleDigit(c);
                }
                else if (this.IsSeparatorChar(c))
                {
                    this.CommitBuffer();
                    this.MoveActiveSegment(+1);
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

            if (this.activeIndex < 0 || this.activeIndex >= this.segments.Count) return false;
            // Also accept any literal character that follows the active segment in the pattern
            // (e.g. exotic localized separators like '年' / '월').
            for (int i = this.activeIndex + 1; i < this.segments.Count; i++)
            {
                Segment s = this.segments[i];
                if (s.kind != SegmentKind.Literal) return false;
                if (!string.IsNullOrEmpty(s.literalText) && s.literalText!.IndexOf(c) >= 0) return true;
            }
            return false;
        }

        // --------------------------------------------------------------------------------------
        // Edits
        // --------------------------------------------------------------------------------------

        private void EnsureSelectedDate()
        {
            if (this.picker.SelectedDate.HasValue) return;
            DateTime seed = this.picker.DisplayDate;
            this.SetSelectedDate(seed);
        }

        private void MoveActiveSegment(int delta)
        {
            this.CommitBuffer();
            if (this.segments.Count == 0) return;
            int idx = this.activeIndex;
            int step = Math.Sign(delta);
            if (step == 0) return;

            for (int i = 0; i < this.segments.Count; i++)
            {
                idx += step;
                if (idx < 0 || idx >= this.segments.Count) return; // do not wrap
                if (this.segments[idx].kind != SegmentKind.Literal)
                {
                    this.activeIndex = idx;
                    int target = this.activeIndex;
                    // Defer so we run after the TextBox's own KeyDown handler has finished
                    // moving the caret in response to Left/Right.
                    Dispatcher.UIThread.Post(() => this.SelectSegment(target), DispatcherPriority.Background);
                    return;
                }
            }
        }

        private void AdjustActiveSegment(int delta)
        {
            this.CommitBuffer();
            this.EnsureSelectedDate();
            if (this.activeIndex < 0 || this.activeIndex >= this.segments.Count) return;
            DateTime current = this.picker.SelectedDate ?? this.picker.DisplayDate;

            DateTime? candidate = this.segments[this.activeIndex].kind switch
            {
                SegmentKind.Year  => SafeAddYears(current, delta),
                SegmentKind.Month => SafeAddMonths(current, delta),
                SegmentKind.Day   => SafeAddDays(current, delta),
                _ => null,
            };
            if (candidate == null) return;

            candidate = this.ClampToPickerRange(candidate.Value);
            this.SetSelectedDate(candidate.Value);
            // Re-select the same segment after the text re-renders.
            int target = this.activeIndex;
            Dispatcher.UIThread.Post(() => this.SelectSegment(target), DispatcherPriority.Background);
        }

        private void ResetActiveSegmentToMin()
        {
            this.CommitBuffer();
            // If no date is currently selected, there is nothing to reset.
            if (!this.picker.SelectedDate.HasValue) return;
            if (this.activeIndex < 0 || this.activeIndex >= this.segments.Count) return;
            DateTime current = this.picker.SelectedDate ?? this.picker.DisplayDate;

            DateTime? candidate = this.segments[this.activeIndex].kind switch
            {
                SegmentKind.Year  => SafeWithYear(current, 1),
                SegmentKind.Month => SafeWithMonth(current, 1),
                SegmentKind.Day   => SafeWithDay(current, 1),
                _ => null,
            };
            if (candidate == null) return;

            candidate = this.ClampToPickerRange(candidate.Value);
            this.SetSelectedDate(candidate.Value);
            int target = this.activeIndex;
            Dispatcher.UIThread.Post(() => this.SelectSegment(target), DispatcherPriority.Background);
        }

        private void HandleDigit(char digit)
        {
            if (this.activeIndex < 0 || this.activeIndex >= this.segments.Count) return;

            Segment seg = this.segments[this.activeIndex];
            int maxDigits = seg.kind switch
            {
                SegmentKind.Year  => seg.tokenLength > 0 && seg.tokenLength < 4 ? seg.tokenLength : 4,
                SegmentKind.Month => 2,
                SegmentKind.Day   => 2,
                _ => 0,
            };
            if (maxDigits == 0) return;

            string candidate = (this.editBuffer + digit);
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
            if (this.TryApplySegmentValue(seg.kind, value, seg.tokenLength, out DateTime applied))
            {
                this.editBuffer = candidate;
                this.SetSelectedDate(applied);
                int target = this.activeIndex;
                Dispatcher.UIThread.Post(() => this.SelectSegment(target), DispatcherPriority.Background);

                // If the buffer is now "full" and any further digit would force a restart,
                // auto-commit but stay on the segment (the next digit will then start over).
                if (candidate.Length >= maxDigits)
                {
                    this.editBuffer = string.Empty;
                }
            }
            else if (candidate.Length == 1)
            {
                // First digit doesn't yield a valid value (e.g. "0" for month). Keep the
                // buffer so the next digit can complete it ("0" + "1" → "01").
                this.editBuffer = candidate;
            }
            // else: invalid candidate, ignore.
        }

        private bool TryApplySegmentValue(SegmentKind kind, int value, int tokenLength, out DateTime result)
        {
            DateTime current = this.picker.SelectedDate ?? this.picker.DisplayDate;
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
                    result = this.ClampToPickerRange(y.Value);
                    return true;
                case SegmentKind.Month:
                    if (value < 1 || value > 12) { result = default; return false; }
                    DateTime? m = SafeWithMonth(current, value);
                    if (m == null) { result = default; return false; }
                    result = this.ClampToPickerRange(m.Value);
                    return true;
                case SegmentKind.Day:
                    if (value < 1) { result = default; return false; }
                    int daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
                    if (value > daysInMonth) { result = default; return false; }
                    DateTime? d = SafeWithDay(current, value);
                    if (d == null) { result = default; return false; }
                    result = this.ClampToPickerRange(d.Value);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        private void CommitBuffer() =>
            this.editBuffer = string.Empty;

        // --------------------------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------------------------

        private void SetSelectedDate(DateTime value)
        {
            this.suspendTextSync = true;
            try
            {
                this.picker.SetCurrentValue(CalendarDatePicker.SelectedDateProperty, value);
            }
            finally
            {
                this.suspendTextSync = false;
            }
            // The picker schedules the textbox update via Dispatcher.UIThread.InvokeAsync,
            // so we must defer our segment rebuild & re-snap.
            Dispatcher.UIThread.Post(this.RebuildSegments, DispatcherPriority.Background);
        }

        private bool IsMultiOrFullSelection()
        {
            if (this.textBox == null || string.IsNullOrEmpty(this.textBox.Text)) return false;
            int minSel = Math.Min(this.textBox.SelectionStart, this.textBox.SelectionEnd);
            int maxSel = Math.Max(this.textBox.SelectionStart, this.textBox.SelectionEnd);
            if (maxSel <= minSel) return false;
            if (minSel == 0 && maxSel == this.textBox.Text!.Length) return true;
            return this.SpansMultipleSegments(minSel, maxSel);
        }

        private void ClearSelectedDate()
        {
            this.editBuffer = string.Empty;
            this.suspendTextSync = true;
            try
            {
                this.picker.SetCurrentValue(CalendarDatePicker.SelectedDateProperty, null);
            }
            finally
            {
                this.suspendTextSync = false;
            }
            Dispatcher.UIThread.Post(this.RebuildSegments, DispatcherPriority.Background);
        }

        private DateTime ClampToPickerRange(DateTime date)
        {
            if (this.picker.DisplayDateStart is { } start && date < start) return start;
            if (this.picker.DisplayDateEnd is { } end && date > end) return end;
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
        public SegmentKind kind;
        public int start;       // character index in rendered text
        public int length;      // character length in rendered text
        public int tokenLength; // pattern token length (1, 2, 4 …); 0 for literals
        public string? literalText;
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
                            kind = SegmentKind.Literal,
                            literalText = literal.ToString(),
                            tokenLength = 0,
                        });
                        literal.Clear();
                    }

                    int runStart = i;
                    while (i < pattern.Length && pattern[i] == c)
                    {
                        i++;
                    }

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

                    segments.Add(new Segment { kind = kind, tokenLength = runLen });
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
                    kind = SegmentKind.Literal,
                    literalText = literal.ToString(),
                    tokenLength = 0,
                });
            }

            // No editable segments → not segmentable (e.g. literal-only patterns or
            // standard format specifiers like "D" that the caller hasn't expanded).
            if (segments.All(s => s.kind == SegmentKind.Literal))
            {
                return null;
            }

            return segments;
        }

        /// <summary>
        /// Renders the supplied date through the parsed segments and returns the
        /// concatenated text plus segments updated with <see cref="Segment.start"/>
        /// and <see cref="Segment.length"/> offsets.
        /// </summary>
        public static List<Segment> RenderSegments(List<Segment> tokens, DateTime date)
        {
            List<Segment> result = new(tokens.Count);

            int start = 0;
            foreach (Segment t in tokens)
            {
                string piece;
                switch (t.kind)
                {
                    case SegmentKind.Year:
                        piece = t.tokenLength switch
                        {
                            1 => date.Year.ToString(CultureInfo.InvariantCulture),
                            2 => (date.Year % 100).ToString("D2", CultureInfo.InvariantCulture),
                            _ => date.Year.ToString("D" + t.tokenLength, CultureInfo.InvariantCulture),
                        };
                        break;
                    case SegmentKind.Month:
                        piece = t.tokenLength == 1
                            ? date.Month.ToString(CultureInfo.InvariantCulture)
                            : date.Month.ToString("D2", CultureInfo.InvariantCulture);
                        break;
                    case SegmentKind.Day:
                        piece = t.tokenLength == 1
                            ? date.Day.ToString(CultureInfo.InvariantCulture)
                            : date.Day.ToString("D2", CultureInfo.InvariantCulture);
                        break;
                    default:
                        piece = t.literalText ?? string.Empty;
                        break;
                }

                start += piece.Length;

                result.Add(new Segment
                {
                    kind = t.kind,
                    start = start,
                    length = piece.Length,
                    tokenLength = t.tokenLength,
                    literalText = t.literalText,
                });
            }

            return result;
        }
    }
}
