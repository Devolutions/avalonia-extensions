# Planning Guide: Theme Control Styling

This specialized guide is for planning work on creating or updating ControlTheme styles for existing Avalonia controls within theme projects (MacOS, DevExpress, Linux themes).

For general planning guidance, see `docs/processes/planning_docs.md`.
For custom control creation, see `docs/processes/planning_custom_controls.md`.

## When to Use This Guide

Use this guide when planning to:
- Add a new control style to an existing theme (e.g., styling Slider for macOS theme)
- Update/improve an existing control theme
- Port a control theme across multiple themes (macOS → DevExpress → Linux)
- Fix visual bugs or alignment issues in control themes

## Goal & Context Template

When starting a control theme planning doc, clearly state:
- **Control name**: Which Avalonia control are you styling (e.g., ComboBox, Button, Slider)
- **Target theme(s)**: Which theme project(s) (MacOS, DevExpress, Linux, or all three)
- **Current state**: Is this a new style or updating existing? Link to existing AXAML if updating
- **Platform reference**: What native platform look are you targeting?
- **Scope**: Which control features/modes need styling (e.g., ComboBox dropdown, editable mode, disabled state)

## Visual Design Research Phase

Before writing AXAML, gather comprehensive visual references:

### 1. Platform Design Guidelines
- **macOS theme**: Reference [Apple Human Interface Guidelines](https://developer.apple.com/design/human-interface-guidelines/)
- **Linux theme**: Reference [Ubuntu Yaru design](https://github.com/ubuntu/yaru) or GTK documentation
- **DevExpress theme**: Reference DevExpress WinForms control screenshots/documentation

Document specific links to relevant guideline sections in your planning doc.

### 2. Native Screenshots
Gather screenshots showing:
- All visual states (normal, hover, pressed, disabled, focus)
- Light and dark mode variations
- Different sizes/variants of the control
- Edge cases (very long text, empty state, error state)

Store or link to these in the planning doc Appendix.

### 3. Existing Theme Code to Copy From

**Consider these three options for finding base code:**

1. **Same control in another of our themes** - Check if this control is already styled in MacOS/DevExpress/Linux themes
   - Location: `src/Devolutions.AvaloniaTheme.{ThemeName}/Controls/{ControlName}.axaml`
   - Advantage: Already adapted to our resource naming conventions

2. **Avalonia Fluent theme** - The fallback theme we use
   - Location: Avalonia source or NuGet package contents
   - Advantage: Official, well-tested implementation

3. **Similar control in same theme** - Find a related control (e.g., use ComboBox as base for Dropdown)
   - Advantage: Matches visual style and resource usage patterns

Evaluate each option based on how much overlap it has with the target behavior and look.

Document in planning doc (example):
```markdown
## Base Code Source
Starting from: `src/Devolutions.AvaloniaTheme.MacOS/Controls/{ControlName}.axaml`
Rationale: [Explain why this source is most appropriate for this specific control]
```

## AXAML Structure Planning

### ControlTheme Definition Pattern

Document the basic structure you'll follow:
```xml
<ResourceDictionary xmlns="..." xmlns:x="...">
  <!-- Design.PreviewWith for in-editor preview -->
  <Design.PreviewWith>
    <design:ThemePreviewer>
      <!-- Preview content showing different states -->
    </design:ThemePreviewer>
  </Design.PreviewWith>

  <!-- Supporting ControlThemes (e.g., for template parts) -->
  <ControlTheme x:Key="SupportingPartTheme" TargetType="...">
    <!-- ... -->
  </ControlTheme>

  <!-- Main ControlTheme using {x:Type} key -->
  <ControlTheme x:Key="{x:Type ControlName}" TargetType="ControlName">
    <!-- Setters for properties -->
    <!-- Template -->
    <!-- Pseudo-class styles -->
  </ControlTheme>
</ResourceDictionary>
```

### Resource Usage Planning

Plan which resources you'll need from `Accents/ThemeResources.axaml`:

**Colors & Brushes:**
- Background: `ControlBackgroundHighBrush`, `LayoutBackgroundLowBrush`, etc.
- Foreground: `ForegroundHighBrush`, `ForegroundMidBrush`, `ForegroundLowBrush`
- Borders: `ControlBorderBrush`, `ControlBorderAccentBrush`
- Accent colors: `SystemAccentColor`, `ControlBackgroundAccentHighBrush`

**Dimensions:**
- Corner radius: `ControlCornerRadius`, `LayoutCornerRadius`
- Thickness: `ControlBorderThickness`, `ButtonBorderThickness`
- Spacing/padding: Define control-specific or use existing

**Shadows:**
- `ControlBorderBoxShadow`, `PopupShadow`, `ButtonBorderBoxShadow`

**DynamicResource vs StaticResource:**
- Use `DynamicResource` for theme-variant-specific resources (colors defined in ThemeDictionaries)
- Use `StaticResource` for invariant resources (geometry, dimensions)

Document new resources needed:
```markdown
## New Resources Required
Add to `Accents/ThemeResources.axaml`:
- `SliderThumbWidth`: x:Double = 16
- `SliderTrackHeight`: x:Double = 4
- `SliderThumbBrush`: DynamicResource based on theme variant
```

## Visual State Specification

Define every visual state with precise descriptions:

### Template Structure
```markdown
## Template Parts
- `PART_Track`: The slider track background
- `PART_Thumb`: The draggable thumb button
- `PART_DecreaseButton`: Optional decrease button
```

### State Definitions

For each pseudo-class, document visual appearance:

```markdown
## Visual States

### :normal (default)
- Background: ControlBackgroundHighBrush
- Border: ControlBorderBrush, 0.6px thickness
- Foreground: ForegroundHighBrush
- Corner radius: ControlCornerRadius (5px)
- Shadow: ControlBorderBoxShadow

### :pointerover
- Background: ControlBackgroundActiveHighBrush (5% opacity overlay)
- Border: ControlBorderAccentBrush
- No change to foreground
- Transition: 100ms ease

### :pressed
- Background: ForegroundLowBrush (25% opacity)
- Border: Same as hover
- Slight visual compression (optional shadow change)

### :disabled
- Background: ControlBackgroundDisabledHighBrush
- Border: ControlBorderDisabledBrush
- Foreground: ForegroundLowBrush (25% opacity)
- Shadow: ComboBoxDisabledBoxShadow (reduced)

### :focus-visible
- Add focus border: FocusBorderBrush with FocusBorderThickness (3.5px)
- Focus border margin: FocusBorderMargin (-3px)
- Corner radius: Slightly larger to accommodate focus ring
```

### Light vs Dark Mode Variations

Document differences between theme variants:
```markdown
## Theme Variant Differences

### Light Mode (Default)
- Backgrounds use lower opacity overlays on white base
- Borders more subtle (29% opacity)
- Shadows more prominent

### Dark Mode
- Backgrounds use higher opacity overlays on black base
- Borders higher contrast (29% opacity but on dark background)
- Different shadow values (see ThemeResources.axaml Dark dictionary)
```

## Template Part Styling Details

For complex controls with multiple template parts, plan each part.

**Naming convention:** Template parts that are required and addressed by the control's code use the `PART_` prefix. Elements that are only named for reference within the template do not use the `PART_` prefix.

```markdown
## {ControlName} Template Parts Styling (example)

### Toggle Button (PART_ToggleButton)
- Nested ControlTheme: PopUpToggleButton
- Contains two chevron paths (up/down arrows)
- Size: 16x16px
- Responds to parent ComboBox.IsEnabled state
- Uses WindowActiveResourceToggler for active/inactive window states

### Popup (PART_Popup)
- Placement: AnchorAndGravity, Bottom anchor
- Min width: Binding to parent width + PopupSideMarginWidth
- Vertical offset: Calculated by SelectedIndexToPopupOffsetConverter
  - Positions dropdown so selected item aligns with control
- Background: PopupBackgroundBrush
- Shadow: PopupShadow
- Border: MenuFlyoutPresenterBorderBrush

### Items Presenter (PART_ItemsPresenter)
- Wrapped in ScrollViewer with MacOS_TransparentTrack class
- Items spacing handled by ComboBoxItem theme
```

## Pseudo-Class Selector Strategy

Plan your Style Selector hierarchy:

```markdown
## Pseudo-Class Implementation

### Basic State Selectors
```xml
<Style Selector="^:pointerover">
  <Style Selector="^ /template/ Border#PART_Border">
    <Setter Property="Background" Value="{DynamicResource ...}" />
  </Style>
</Style>
```

### Combined State Selectors
```xml
<Style Selector="^:pressed:enabled">
  <!-- Only when both pressed AND enabled -->
</Style>
```

### Template Part Targeting
```xml
<Style Selector="^:disabled /template/ ToggleButton#Toggle">
  <!-- Target specific template part in disabled state -->
</Style>
```

### Class-Based Variants
```xml
<Style Selector="^.accent">
  <!-- For accent-styled variant -->
</Style>
```

## Cross-Theme Consistency

When working on multiple themes, plan for consistency:

### Resource Key Naming
Use identical resource keys across all themes:
- ❌ `MacOSButtonBackground` vs `DevExpressButtonBg`
- ✅ `ControlBackgroundHighBrush` in all three themes

### Code Structure Similarity
Keep AXAML structure similar across themes:
- Same template part names
- Same pseudo-class organization
- Similar Setter order for readability

## Adding to Theme Index

Remember to add new control theme to `Controls/_index.axaml`:

```xml
<ResourceDictionary.MergedDictionaries>
  <!-- ... existing controls ... -->
  <MergeResourceInclude Source="YourNewControl.axaml" />
</ResourceDictionary.MergedDictionaries>
```

## Demo Page Planning

Plan demo content for `samples/SampleApp/DemoPages/`:

```markdown
## Demo Page Content

### YourControlDemo.axaml
Show examples of:
- Default state with typical content
- Disabled state
- Different sizes (if applicable)
- Error/validation state (if applicable)
- Light and dark backgrounds (using LayoutBackgroundLowBrush containers)
- Edge cases (long text, empty, etc.)

### Interactive Examples
- User can interact to see hover/pressed states
- Toggle between enabled/disabled
- Demonstrate special features (e.g., dropdown, selection)
```

## Testing Checklist for Actions

Include in your planning doc actions:

```markdown
### Phase X: Visual Testing
- [ ] Test in SampleApp with light theme
- [ ] Test in SampleApp with dark theme
- [ ] Verify all pseudo-classes (hover, pressed, disabled, focus)
- [ ] Test keyboard navigation and focus indicators
- [ ] Test on different window states (active/inactive)
- [ ] Compare side-by-side with platform native control (screenshot comparison)
- [ ] Test with varying content (long text, empty, special characters)
- [ ] Verify in context of real application layouts
- [ ] Check resource usage (no missing DynamicResources)
- [ ] Validate AXAML in IDE (no red squiggles)
```

## Common Pitfalls to Document

Note potential issues to watch for:

```markdown
## Known Challenges

### Focus Border Overlap
- Focus borders use negative margin to extend outside control
- May overlap with adjacent controls - use `ClipToBounds="False"` on parent
- Test in dense layouts

### Template Part Access
- Can only target template parts using `/template/` selector
- Template parts not accessible from outside control theme
- Plan proper pseudo-class propagation to parts

### Window Active State (macOS specific)
- macOS theme uses WindowActiveResourceToggler for inactive window styling
- Only applies to macOS theme
- Test by switching between app windows
```

## Example Planning Doc Structure

```markdown
# ComboBox Styling for macOS Theme

## Goal
Add native macOS AppKit-style ComboBox theme with dropdown positioning that aligns selected item with control.

## References
- Existing: `src/Devolutions.AvaloniaTheme.DevExpress/Controls/ComboBox.axaml` - Similar structure
- Base code: Avalonia Fluent ComboBox as starting point
- Platform: macOS AppKit NSComboBox ([screenshot link])

## Visual Design Research

### Platform References
- [Apple HIG - Combo Boxes](https://developer.apple.com/design/human-interface-guidelines/combo-boxes)
- Native screenshots: See Appendix

### Key Visual Characteristics
- Rounded corners (5px)
- Subtle gradient border (lighter top, darker bottom)
- Dropdown chevrons stack vertically
- Selected item aligns with control when dropdown opens
- Box shadow for depth

## Resource Planning

### Existing Resources to Use
- `ControlBackgroundHighBrush` - Main background
- `ControlBorderRaisedBrush` - Gradient border
- `ControlCornerRadius` - 5px rounded corners
- `PopupBackgroundBrush` - Dropdown background

### New Resources Needed
- `ComboBoxDropDownBorderBrush` - #bababa solid border for popup
- `ComboBoxItemHeight` - x:Int32 = 22 for item height calculations
- `PopupSideMarginWidth` - x:Int32 = 12 for width calculation

## Visual States

[... detailed state specifications ...]

## Template Structure

[... template part descriptions ...]

## Actions

### Phase 1: Setup and Resource Definition
- [ ] Create `ComboBox.axaml` in `src/Devolutions.AvaloniaTheme.MacOS/Controls/`
- [ ] Copy Fluent ComboBox theme as starting point
- [ ] Add new resources to `Accents/ThemeResources.axaml`
  - [ ] Light mode resources
  - [ ] Dark mode resources
- [ ] Add to `Controls/_index.axaml`

### Phase 2: Basic ControlTheme Implementation
[... detailed steps ...]

### Phase 3: Visual States and Pseudo-Classes
[... detailed steps ...]

### Phase 4: Demo Page and Testing
[... detailed steps ...]

### Phase 5: Cross-Theme Port (if applicable)
- [ ] Evaluate if DevExpress and Linux themes need same updates
- [ ] Port with theme-appropriate resource changes
- [ ] Test all three themes

## Appendix

### Platform Screenshots
[Links or embedded images]

### Existing Code Analysis
[Notes from reviewing similar controls]

### Design Decisions
[Rationale for specific choices]
```
