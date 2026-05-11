# TimePickerUpDown Production Hardening Plan

**Status:** In Progress - Prototype Complete, Phase 1 decisions in progress
**Created:** 2026-05-07
**Last Updated:** 2026-05-07

## Overview

This document tracks the work needed to move `TimePickerUpDown` from a working prototype to a production-ready control distributed in `Devolutions.AvaloniaControls`.

The control already has:
- A functional shared control implementation in `Devolutions.AvaloniaControls`
- A DevExpress theme override
- A dedicated SampleApp demo page
- A dedicated SampleApp view model so each demo scenario retains its own state

What remains is hardening the control contract, editing behavior, validation, localization, accessibility, tests, and documentation.

## Current State Summary

### Completed groundwork
- [x] Control renamed from `SegmentedTimePicker` to `TimePickerUpDown`
- [x] Shared control template added to `Devolutions.AvaloniaControls`
- [x] DevExpress theme override added
- [x] Dedicated SampleApp demo page added
- [x] Dedicated `TimePickerUpDownViewModel` added so the demo is independent from the built-in `TimePicker` page

### Current strengths
- Inline segmented editing works
- Spinner integration works
- 12-hour and 24-hour modes both exist
- Optional seconds segment exists
- Null `SelectedTime` is supported
- DevExpress layout is visually close to the intended design

### Current limitations
- Behavior is still prototype-level and not yet locked as a long-term contract
- Validation is permissive and mostly commit-time only
- Editing ergonomics are still incomplete
- Localization coverage is partial
- There are no control-specific automated tests yet
- Accessibility and automation behavior have not been verified
- Package documentation has not been written yet

## Confirmed Gaps

### 1. API contract is not yet hardened

The public surface exists, but some behaviors are still implicit or prototype-shaped.

Open items:
- `ClockIdentifier` is currently a string-based contract using magic values such as `12HourClock` and `24HourClock`
- Null-state behavior needs to be explicitly defined and documented
- The behavior when spinning from an empty value needs to be defined
- The exact meaning of `MinuteIncrement` and `SecondIncrement` needs to be locked down
- We should confirm whether `SelectedTime` remains the only value property or whether any formatted text API is needed

### 2. Validation behavior is too soft

The control currently parses on commit and redraws from the last valid value. That is acceptable for a prototype, but production behavior should be intentional and predictable.

Open items:
- Decide whether invalid text is blocked while typing, accepted temporarily, or accepted with visible invalid state
- Decide how validation errors should surface through `DataValidationErrors`
- Decide whether partial values such as `1:` or blank seconds are temporarily valid while editing
- Confirm how increments interact with typed values that are off-increment

### 3. Editing behavior is incomplete

The current implementation supports focus, blur, Up, Down, Enter, and Escape. That is enough to prove the control, but not enough to make it feel finished.

Open items:
- Left and Right navigation between segments
- Auto-advance after typing two digits
- Backspace behavior across segment boundaries
- Paste behavior for strings such as `13:45`, `1:45 PM`, or `13:45:06`
- Selection and caret behavior when clicking into segments
- Mouse wheel behavior policy
- Period segment typing rules for AM/PM

### 4. The culture story is only partial

The control already uses the current culture's AM/PM designators for parsing and display, but the full localization behavior is not yet defined.

Open items:
- Decide whether the control always follows `CultureInfo.CurrentCulture` or whether it should support an explicit culture override later
- Verify behavior for cultures with longer or unusual AM/PM designators
- Verify behavior for 24-hour-default locales
- Verify placeholder text and display rules outside English-centric assumptions
- Verify right-to-left layout and text flow
- Confirm whether culture affects only parsing/display or also segment order in the long term

### 5. Theme and visual state coverage is incomplete

The control currently has a shared template and a DevExpress override, but the supported state matrix has not been verified.

Open items:
- Disabled state
- Read-only state
- Focused and focus-within state
- Validation error state
- Empty/null state
- High-contrast legibility
- Fallback behavior in themes without a dedicated override

### 6. Accessibility and automation need a dedicated pass

The inner `TextBox` controls give some baseline keyboard and text behavior, but the control should be verified as a reusable product surface.

Open items:
- Tab order and focus predictability
- Screen reader naming and announcement quality
- Automation exposure as one logical control versus several text boxes
- Clear focus visuals for keyboard users
- Accessibility implications of the spinner buttons

### 7. Automated coverage is missing

There are currently no targeted tests for `TimePickerUpDown`.

Open items:
- Parsing tests for 12-hour and 24-hour input
- Normalization tests for edge cases like `12 AM` and `12 PM`
- Increment and spinner tests
- Null/blank-state tests
- Visibility/state tests for seconds and AM/PM segments
- Visual tests for the default template and DevExpress theme

### 8. Documentation and package readiness are incomplete

The control is not yet documented as a supported reusable control.

Open items:
- Add README entry in `Devolutions.AvaloniaControls`
- Add usage examples
- Document public properties and expected behaviors
- Document theming expectations for theme authors
- Document known limitations if any remain after hardening

## Hardening Phases

### Phase 1. Lock the behavior contract

Goal: define the control's long-term rules before refining implementation details.

### Phase 1 recommendations based on Avalonia and current app locales

The current recommendation is to stay close to Avalonia's `TimePicker` where that helps interoperability and user expectations, while keeping the inline-edit model stable.

Recommended decisions:
- Keep `ClockIdentifier` string-based for now and accept only Avalonia-compatible values: `12HourClock` and `24HourClock`
- Infer the default clock mode from `CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern`, as Avalonia does
- Keep `SelectedTime` as the only value property for v1; do not add a formatted text API yet
- Keep null behavior as blank/unset display state
- Keep spinner-from-empty behavior seeded from current local time, matching the current implementation and Avalonia's general behavior
- Constrain `MinuteIncrement` and `SecondIncrement` to `1..59`, matching Avalonia
- Accept typed values exactly as entered as long as they are valid times, even when they do not match the configured increment
- Treat increments as spinner-step behavior first, not as a rule that snaps typed input on commit
- Let culture affect default 12/24-hour mode, AM/PM parsing and display, and RTL/layout verification
- Do not let culture reorder segments in v1; keep the control shape as `hour : minute [: second] [period]`
- Use English-style `hh`, `mm`, `ss`, and `AM` placeholders as generic editing hints for v1 unless we find a concrete localization issue that justifies localized placeholders

Conclusions from the supported locale list:
- Most of the listed non-US locales are likely to expect 24-hour time by default, so culture-based defaulting is worth doing now rather than later
- `en-US` remains an important 12-hour case, so AM/PM behavior needs to stay first-class rather than treated as edge behavior
- Some supported locales may have empty, shortened, or longer AM/PM designators, so the control should not assume two-character `AM` and `PM` strings everywhere
- For IT support software, expert users may tolerate invariant editing shapes, but they will still notice if the control defaults to the wrong clock mode for their locale

Explicit decisions still needed:
- Which optional parity APIs with Avalonia's `TimePicker` are actually needed by product teams in the first release?
- Whether any team needs text-format customization or culture override APIs immediately, or whether those stay out of scope for v1

Tasks:
- [ ] Confirm the public API that should ship
- [ ] Decide whether `ClockIdentifier` stays string-based for compatibility or moves to a stronger type
- [ ] Define null behavior and spinner-from-empty behavior
- [ ] Define increment semantics for typed input and spinner input
- [ ] Write down the control's culture story

Exit criteria:
- The intended behavior can be described clearly without referring to the current implementation

### Phase 2. Finish editing and validation behavior

Goal: make the control feel deliberate and predictable during typing.

Tasks:
- [ ] Add segment-to-segment navigation rules
- [ ] Add auto-advance and backspace behavior where appropriate
- [ ] Add paste handling for common time formats
- [ ] Decide and implement invalid-input behavior
- [ ] Ensure `DataValidationErrors` integration is intentional

Exit criteria:
- A user can type, correct, and navigate values without surprising resets or dead ends

### Phase 3. Accessibility and automation pass

Goal: verify that the control is usable beyond mouse-only visual inspection.

Tasks:
- [ ] Verify keyboard-only editing flow
- [ ] Review focus visuals
- [ ] Check screen-reader and automation behavior
- [ ] Decide whether any automation peer or accessibility metadata is needed

Exit criteria:
- The control has a defensible accessibility baseline for package distribution

### Phase 4. Theme and state verification

Goal: ensure the control behaves correctly across supported states and theme contexts.

Tasks:
- [ ] Verify all key visual states in DevExpress
- [ ] Verify fallback behavior in the shared template
- [ ] Confirm no layout regressions in the dedicated SampleApp demo page
- [ ] Decide whether Linux and MacOS need explicit overrides now or can rely on fallback initially

Exit criteria:
- The supported theme matrix is explicit and visually acceptable

### Phase 5. Add automated tests

Goal: stop relying only on manual sample inspection.

Tasks:
- [ ] Add logic-level tests for parsing, normalization, and increments
- [ ] Add tests for null/empty transitions
- [ ] Add tests for segment visibility rules
- [ ] Add visual regression coverage for representative states

Exit criteria:
- Core behavior has repeatable automated coverage

### Phase 6. Documentation and package readiness

Goal: make the control understandable and supportable for consumers.

Tasks:
- [ ] Add README documentation
- [ ] Add usage examples
- [ ] Document API expectations and supported scenarios
- [ ] Document theming hooks and any remaining limitations

Exit criteria:
- The control can be shipped and used without relying on sample code archaeology

## Immediate Next Actions

Recommended implementation order:
1. Finalize Phase 1 behavior decisions while the control surface is still small
2. Use the dedicated SampleApp page as the checklist surface for Phase 2 editing work
3. Add logic-level tests before broad visual/state refinement

## Notes

- The dedicated `TimePickerUpDown` SampleApp page is now the preferred manual validation surface for this effort.
- The original `TimePickerDemo` should remain independent unless and until the built-in `TimePicker` demo is intentionally removed.
- This document should move to `.claude/docs/planning/completed/` once the control is considered production-ready.