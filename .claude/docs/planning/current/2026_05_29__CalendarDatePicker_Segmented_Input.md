# Segmented date input for `CalendarDatePicker`

## Problem

`CalendarDatePicker` currently behaves as a plain `TextBox` for typed input:
clicking inside the field places a free caret, the user can type any string,
and validation only happens on lost focus / Enter (reverting to the previous
value on failure).

Native controls (DevExpress `DateEdit`, macOS `NSDatePicker`) instead treat
the field as a sequence of **segments** (year / month / day):

- Clicking selects the whole segment under the cursor.
- ↑ / ↓ increments / decrements the active segment.
- ← / → moves to the previous / next segment.
- Digits overwrite the active segment.
- The separator character (`-`, `/`, `.`, etc.) advances to the next segment.
- Backspace resets the segment to its minimum value (`0000`, `01`, `01`).
- Tab moves focus out of the control (does not navigate segments).

## Scope

**In scope (Phase 1)**
- Numeric date patterns only — `SelectedDateFormat="Short"` and `Custom`
  patterns made up of `y/yy/yyyy`, `M/MM`, `d/dd` plus literal separators.
- Enabled **by default** in the DevExpress, MacOS and Linux Devolutions
  themes by attaching a behavior on the `CalendarDatePicker` `ControlTheme`.

**Out of scope (deliberate, can revisit later)**
- `Long` format and any `Custom` format containing `MMM` / `MMMM` /
  `ddd` / `dddd` → behavior detects this and falls back to today's free-text
  TextBox interaction.
- Tab / Shift+Tab segment hopping (Tab behaves as normal focus traversal).
- Auto-advance after typing the maximum number of digits.
- Per-segment mouse-wheel (base control already wheels days).
- RTL / IME considerations.

## Approach

Add a new **`SegmentedDateInputBehavior`** in
`src/Devolutions.AvaloniaControls/Behaviors/` exposing an attached property
`SegmentedDateInputBehavior.IsEnabled` on `CalendarDatePicker`. The three
themes set this property to `True` via a `<Setter>` in the existing control
theme (same pattern as `CalendarDatePickerBehavior.OpenOnSelectedDate`).

### Behavior responsibilities

1. **Hook the template** — on attach, locate the `PART_TextBox` (subscribe to
   `TemplateAppliedEvent` on the picker, or watch
   `Visual.AttachedToVisualTree` then `FindDescendantOfType<TextBox>("PART_TextBox")`).
2. **Decide active vs. fallback mode**
   - Resolve the active format string from `SelectedDateFormat`,
     `CustomDateFormatString`, and `CultureInfo.CurrentCulture.DateTimeFormat`.
   - If the format contains any non-numeric date token, disable segmented
     handlers entirely (transparent fallback).
3. **Tokenizer** (pure logic, easy to unit-test) — converts a format pattern
   into an ordered list:
   ```csharp
   internal enum SegmentKind { Year, Month, Day, Literal }
   internal sealed record Segment(SegmentKind Kind, int Start, int Length, int TokenLength);
   ```
   `TokenLength` is the count of pattern chars (1 for `M`, 2 for `MM`, 4 for `yyyy`)
   and is used when re-rendering.
4. **Re-map segments after every text render** — `CalendarDatePicker` resets
   `TextBox.Text` via `Dispatcher.UIThread.InvokeAsync` whenever
   `SelectedDate` changes; subscribe to `_textBox.GetObservable(TextBox.TextProperty)`
   with a re-entrancy guard and rebuild the segment table by formatting the
   current `SelectedDate` against the active pattern.
5. **Click-to-snap** — subscribe to `_textBox.GetObservable(TextBox.SelectionStartProperty)`
   (and `SelectionEndProperty`); when the user-driven selection lands inside
   a segment, snap to `[seg.Start, seg.End]`. A `_isSnapping` flag prevents
   recursion. Also re-snap when the textbox receives focus.
6. **Key handling** — register a tunneling handler on the picker so we run
   before the base `CalendarDatePicker.TextBox_KeyDown`:
   - `Left` / `Right` → move active segment (skip `Literal`s), `e.Handled = true`.
   - `Up` / `Down` → compute new `DateTime?` using `DateTime.AddYears` /
     `AddMonths` / `AddDays`, clamp to `DisplayDateStart` / `DisplayDateEnd`
     (ignore `BlackoutDates` for now — typed input bypasses calendar), call
     `SetCurrentValue(SelectedDateProperty, newDate)`. Re-snap once the new
     text lands.
   - `Back` → reset active segment to min value (`0000` / `01` / `01`).
     Update via `SetCurrentValue` if the resulting date is valid; otherwise
     stage as a pending edit.
   - Other keys: leave to base handlers (preserves Enter / Esc behavior).
7. **TextInput handler** (tunneling) — handles raw text:
   - Digits → maintain a per-segment string buffer, validate as a prefix of
     a legal value for the segment (1–12, 1–31, 0–9999), commit when full or
     on the next separator/arrow. Always `e.Handled = true` so the underlying
     TextBox doesn't see the keystroke.
   - Separator character (anything matching a `Literal` segment that follows
     the active one) → commit current segment, advance.
   - Anything else → `e.Handled = true` (no free text in segmented mode).
8. **Null state seeding** — first ↑/↓/digit on a picker with no selection
   seeds `SelectedDate` from `DisplayDate ?? DateTime.Today`.
9. **Commit on lost focus** — base class still calls `SetSelectedDate()` on
   lost focus, which acts as our safety net. If we have an uncommitted
   partial buffer, flush it first.

### Theme integration

`Devolutions.AvaloniaTheme.{DevExpress,MacOS,Linux}/Controls/CalendarDatePicker.axaml`:
```xml
<Setter Property="behaviors:SegmentedDateInputBehavior.IsEnabled" Value="True" />
```
Add the `xmlns:behaviors` import to each file (most already reference
`Devolutions.AvaloniaControls.Behaviors`).

### Sample updates

`samples/SampleApp/DemoPages/CalendarDatePickerDemo.axaml` — annotate the
existing `Short` examples noting the new typing behavior; ensure the
existing `Long` example continues to demonstrate free-text fallback. Add a
`Custom` `yyyy/MM/dd` example to exercise non-default separators.

### Tests

- New unit tests under `tests/Devolutions.AvaloniaControls.Tests/` (or the
  closest existing project) covering the tokenizer and a few interaction
  scenarios driven through the behavior.
- Update visual baselines if the sample page changes layout (`UPDATE_BASELINES=true dotnet test`).

## Risks / things to verify

- Avalonia's `TextBox` doesn't expose a clean "user vs. programmatic
  selection" signal; the re-entrancy flag plus deferred re-snap should be
  sufficient, but worth testing with mouse drag-selection.
- Base class' `OnGotFocus` selects the entire text on Tab-in; segmented
  re-snap must run *after* that.
- Setting `SelectedDate` re-renders via `InvokeAsync`; arrow-key handlers
  must not assume synchronous text update before re-snapping.

## Todos

Tracked in SQL (`todos` table).
