# Liquid Glass Resource Refactoring Guide

**Status:** Active
**Context:** Implementing "Liquid Glass" (LG) theme support for `Devolutions.AvaloniaTheme.MacOS`.

## Objective

To implement the Liquid Glass visual design by overriding resources, while simultaneously refactoring the existing "MacOS Classic" resources to use clean, semantic naming. The goal is to decouple specific control properties from generic or reused resources, allowing LG to override them individually without affecting other controls in the Classic theme.

## Workflow Overview

We will proceed control by control, property by property.

### Step 0: Analysis (Human)
*   Determine if a specific property needs to be visually different in Liquid Glass compared to the Classic theme.
*   If they are identical, no action is required for that property.
*   If they differ, proceed to Step 1.

### Step 1: Implementation

#### A. Determine Logical Name
*   Choose a specific, semantic name for the resource based on the control and property being styled.
*   **Format:** `[ControlName][Part][Property]Brush/Thickness/etc`
*   **Example:** `ButtonPointerOverBackgroundBrush` (instead of a generic `ControlBackgroundActiveHighBrush`).

#### B. Check Existing Usage
*   Search the codebase to see if this logical name is already in use.
*   *Note:* Linux and DevExpress themes are independent; focus on `Devolutions.AvaloniaTheme.MacOS`.

#### C. Refactor Existing Collisions (If necessary)
*   If the logical name is already used but points to a resource shared by multiple disparate controls (where changing it for one would incorrectly change it for others in LG):
    *   Rename the existing resource to something more specific for its *actual* current usage, or duplicate it if it was serving two unrelated purposes that now need to diverge.
    *   **Goal:** Free up the logical name so it can be used exclusively for the target property.
    *   **Constraint:** Ensure no visual changes occur in the Classic theme during this refactoring.

#### D. Define Resources
Define the new resource in both theme files.

**1. In `ThemeResources_LiquidGlass.axaml` (The New Look):**
*   Add the resource with the **new Liquid Glass value**.
*   Use the 3-Tier structure:
    *   **Tier 1 (Core):** `ThemeDictionaries` (Light/Dark).
    *   **Tier 2 (Primitives):** Defined at Root (e.g., `SurfaceT007`).
    *   **Tier 3 (Semantic):** Defined in `ThemeDictionaries` pointing to Tier 2.
    *   *Example:* `<StaticResource x:Key='ButtonPointerOverBackgroundBrush' ResourceKey='SurfaceT008' />`

**2. In `ThemeResources.axaml` (The Classic Look):**
*   Add the resource with the **Classic value**.
*   It should point to the resource that was previously used for this property (preserving the old look).
*   *Example:* `<StaticResource x:Key='ButtonPointerOverBackgroundBrush' ResourceKey='ControlBackgroundActiveHighBrush' />`
*   *Note:* If the value is the same for Light/Dark, it can be defined in the "Common Resources" section. If it differs, define it in the `Default` and `Dark` dictionaries.

#### E. Update Control Template
*   Open the control's `.axaml` file (e.g., `Button.axaml`).
*   Replace the old resource reference with the new logical key.
*   *Example:* Change `Value="{DynamicResource ControlBackgroundActiveHighBrush}"` to `Value="{DynamicResource ButtonPointerOverBackgroundBrush}"`.

### Step 2: Verification

#### A. Automated Tests
*   Run the visual regression tests for MacOS Classic to ensure no regressions.
*   Command: `dotnet test --filter "DisplayName~MacClassic"`

#### B. Visual Check
*   Launch the SampleApp.
*   Verify the control looks correct in **MacOS Classic** (should look unchanged).
*   Verify the control looks correct in **MacOS Liquid Glass** (should show new styling).
*   Toggle between Light and Dark modes to ensure dynamic switching works.

## Key Files

*   **New Resources:** `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml`
*   **Classic Resources:** `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml`
*   **Control Templates:** `src/Devolutions.AvaloniaTheme.MacOS/Controls/*.axaml`

## Refactoring Progress Checklist

- [ ] AutoCompleteBox
- [x] Button
- [ ] ButtonSpinner
- [ ] Calendar
- [ ] CalendarButton
- [ ] CalendarDatePicker
- [ ] CalendarDayButton
- [ ] CalendarItem
- [x] CheckBox
- [ ] ClosableTag
- [ ] ComboBox
- [ ] ComboBoxItem
- [ ] ContextMenu
- [ ] DataGrid
- [ ] EditableComboBox
- [ ] EmbeddableControlRoot
- [ ] Expander
- [ ] GridSplitter
- [ ] ListBox
- [ ] ListBoxItem
- [ ] Menu
- [ ] MenuFlyoutPresenter
- [ ] MenuItem
- [ ] MultiComboBox
- [ ] MultiComboBoxItem
- [ ] NumericUpDown
- [ ] RadioButton
- [ ] ScrollBar
- [ ] ScrollViewer
- [ ] Separator
- [ ] TabControl
- [ ] TabItem
- [ ] TabPane
- [ ] TagInput
- [ ] TextBlock
- [ ] TextBox
- [ ] ToggleSwitch
- [ ] ToolTip
- [ ] TreeView
- [ ] TreeViewItem
- [ ] Window
