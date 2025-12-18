# Focus Adorner Refactoring Plan

**Status:** In Progress
**Date:** 2025-12-17

## Goal
Refactor controls in MacOS and Linux themes to use the built-in `FocusAdorner` mechanism instead of manual focus borders that reserve layout space. This eliminates layout shifts and standardizes focus visualization.

## Strategy
**Update (2025-12-17):** The original plan to use `FocusAdorner` was abandoned due to clipping issues with the AdornerLayer. The new strategy is to use a manual "Overlay Border" within the ControlTemplate.

1.  Remove `ReservedSpaceForFocus` panels and margins that cause layout issues in cross-platform scenarions.
2.  Ensure the root container of the template has `ClipToBounds="False"`.
3.  Add a `FocusBorderElement` (Border) inside the template, positioned to overlay the control.
4.  Use negative margins (e.g., `Margin="-3"`) on this border to draw the focus ring *outside* the control's logical bounds.
5.  Bind the visibility/color of this border to the focus state using styles.

## Controls to Refactor

### MacOS Theme
- [x] **TextBox**
- [ ] **Button**
- [x] **CheckBox**
- [ ] **ComboBox**
- [ ] **Expander**
- [ ] **MultiComboBox**
- [ ] **RadioButton**
- [ ] **TabControl** (and `TabItem`)
- [ ] **TagInput**

### Linux Theme
- [ ] **ComboBox**
- [ ] **TagInput**
- [ ] **Button** (Consistency check)
- [ ] **NumericUpDown** (Consistency check)

## Workflow
For each control:
1.  Switch to the relevant theme and tab using `/workon`.
2.  Apply the refactoring changes.
3.  Run `dotnet test` to ensure no regressions.
4.  **Wait for user visual verification.**
5.  Mark as completed only after user approval.
