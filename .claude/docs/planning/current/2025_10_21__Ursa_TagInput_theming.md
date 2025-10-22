# Ursa TagInput Control Theming

## Goal

Add theming support for Ursa's TagInput control to the DevExpress theme, following the established pattern used for ClosableTag. This will allow users of the DevExpress theme to have a consistent look when using TagInput controls.

## Context

The repository already uses Ursa controls (specifically ClosableTag) and has established a pattern for theming them:
1. AvaloniaControls has Ursa as a hard dependency
2. Theme packages get Ursa transitively through AvaloniaControls
3. Wrapper classes in AvaloniaControls inherit from Ursa controls
4. Theme AXAML files style the Ursa namespace types and create aliases for the wrapper types

This means we can add TagInput theming without any new architectural decisions - just follow the existing pattern.

## References

- `.claude/docs/processes/planning_docs.md` - Project planning guidelines
- `src/Devolutions.AvaloniaControls/Controls/ClosableTag.axaml.cs` - Example Ursa control wrapper
- `src/Devolutions.AvaloniaTheme.DevExpress/Controls/ClosableTag.axaml` - Example Ursa control theming
- `src/Devolutions.AvaloniaControls/Controls/MultiComboBox.axaml.cs` - Existing control that uses tags internally
- Ursa documentation: https://irihi.tech/ursa/ - Official Ursa UI library docs
- Ursa TagInput source: https://github.com/irihitech/Ursa.Avalonia - Reference implementation

## Principles & Key Decisions

1. **Follow the ClosableTag pattern exactly** - proven architecture, no need to reinvent
2. **DevExpress theme only initially** - can expand to macOS/Linux later if needed
3. **Production-ready quality** - this will be released as part of the theme package
4. **Match DevExpress design language** - consistent with existing input controls (TextBox, ComboBox)
5. **Create basic usage demo page** - focus on common scenarios like email recipient input
6. **Animation as polish (nice-to-have)** - graceful tag closing animation if time permits

## Theme Architecture

### Control Wrapper Pattern
```csharp
// In AvaloniaControls/Controls/TagInput.axaml.cs
public class TagInput : Ursa.Controls.TagInput { }
```

### Theme Resource Structure
- Define TagInput-specific brushes, dimensions, and spacing in `Accents/ThemeResources.axaml`
- Create ControlTheme in `Controls/TagInput.axaml` targeting `u:TagInput`
- Use StaticResource alias to map wrapper type to Ursa type theme
- Include in `Controls/_index.axaml` for theme inclusion

### Visual States to Style
- Normal (default appearance)
- Focus (keyboard focus indicator)
- Disabled (non-interactive state)
- PointerOver (hover on tags)
- Tag states (normal tag, hovered tag, tag close button)
- Input field states within the control

### DevExpress Design Goals
- Match existing input control styling (TextBox, ComboBox height and borders)
- Use consistent accent colors for focus states
- Match tag styling to existing ClosableTag appearance
- Ensure clear visual feedback for all interactive states

## Actions

### Phase 1: Research & Understanding ✅ COMPLETED
- [x] Research Ursa TagInput control
  - [x] Review Ursa documentation for TagInput features and API
  - [x] Examine Ursa source code to understand control structure and template parts
  - [x] Identify all styleable properties, template parts, and visual states
  - [x] Document key properties: Items, WaterMark, Delimiter, validation, etc.
  - [ ] Take screenshots of default Ursa TagInput appearance for comparison (will do in Phase 2)
- [x] Analyze existing ClosableTag implementation
  - [x] Review wrapper class pattern in AvaloniaControls
  - [x] Study theme resource organization in DevExpress
  - [x] Understand ControlTheme structure and pseudo-class selectors
  - [x] Document the StaticResource aliasing technique
- [x] Check MultiComboBox for tag-related patterns
  - [x] Review how MultiComboBox uses tags internally
  - [x] Identify reusable patterns or considerations

### Phase 2: Create SampleApp Demo Page ✅ COMPLETED
- [x] Research existing SampleApp structure
  - [x] Review existing demo pages in `samples/SampleApp/DemoPages/`
  - [x] Understand navigation and page organization
- [x] Create TagInputDemoPage with basic examples
  - [x] Add new demo page AXAML and code-behind
  - [x] Include in SampleApp navigation/menu system
  - [x] Add email recipients example (primary use case)
  - [x] Add basic examples: empty state, pre-populated tags, watermark text
  - [x] Add state examples: disabled, error states
  - [x] Keep examples simple and focused on common usage
  - [x] Use `<u:TagInput>` directly (wrapper doesn't exist yet)
- [x] Run SampleApp and verify TagInput displays with default Ursa styling
- [x] Test basic functionality: adding tags, removing tags, input
- [x] Stop and show user the control with default styling
- [x] Update planning doc with progress and commit SampleApp changes
  - Follow `.claude/docs/processes/git_commits.md` guidelines
  - Commit message: "Add TagInput demo page to SampleApp"

**Phase 2 Notes:**
- Created TagInputDemo.axaml with 8 different examples
- Demonstrated: email recipients, pre-populated tags, MaxCount, no duplicates, disabled states, separators
- Control works functionally: tags can be added (Enter + separator), removed (X button), MaxCount enforced, duplicates blocked
- **Known Issue**: Watermark exists but is hidden behind input panel - will be addressed in Phase 4 with proper theme layout

### Phase 3: Set Up Basic Infrastructure ✅ COMPLETED
- [x] Create TagInput wrapper class
  - [x] Add `Controls/TagInput.axaml.cs` in AvaloniaControls
  - [x] Inherit from `Ursa.Controls.TagInput`
  - [x] Keep empty like ClosableTag wrapper (no custom logic needed)
- [x] Create basic theme files
  - [x] Add `Controls/TagInput.axaml` in AvaloniaControls (default theme)
  - [x] Set up XML namespaces and Design.PreviewWith section
  - [x] Create initial ControlTheme structure for `u:TagInput`
  - [x] Add StaticResource alias for wrapper type
  - [x] Include TagInput.axaml in `DefaultControlTemplates.axaml`
- [x] Build and verify no compilation errors
- [x] Update planning doc with progress

**Phase 3 Notes:**
- Created wrapper class at `src/Devolutions.AvaloniaControls/Controls/TagInput.axaml.cs`
- Created default theme at `src/Devolutions.AvaloniaControls/Controls/TagInput.axaml`
- ItemTemplate properly wired with ClosableTag Command binding to TagInput.Close method
- Watermark visibility controlled by `:not(:empty)` pseudo-class selector
- Demo page uses `<u:TagInput>` directly (wrapper type available via StaticResource alias)

### Phase 4: Design & Implement DevExpress Theme Styling ✅ COMPLETED
- [x] Analyze DevExpress design patterns in existing controls
  - [x] Review TextBox styling for two-layer border pattern (BorderThickness="1 1 1 0" + separate bottom border)
  - [x] Review ClosableTag for tag appearance and styling
  - [x] Extract common dimensions from ThemeResources (TextBasedInputMinHeight, Padding, CornerRadius)
  - [x] Extract common colors (TextBoxBackgroundBrush, borders, focus accents)
- [x] Implement DevExpress TagInput theme
  - [x] Created `Controls/TagInput.axaml` in DevExpress theme
  - [x] Implemented two-layer border system matching TextBox pattern
  - [x] Added DataValidationErrors wrapper for form integration
  - [x] Created TagInputTextBoxTheme for embedded input styling
  - [x] Implemented visual states: empty, focus-within, disabled, error
  - [x] Added to `Controls/_index.axaml` for theme inclusion
- [x] Test basic functionality in ControlAlignment page
  - [x] Added TagInput to all four layout blocks (2 Grids, 2 StackPanels)
  - [x] Verified control works in horizontal and vertical layouts
  - [x] Confirmed no measurement crashes with current implementation
- [x] Commit working theme implementation

**Phase 4 Notes:**
- DevExpress theme successfully implemented with two-layer border system
- Control renders correctly and functions properly in all layout scenarios tested
- Focus state uses bottom border thickness change (1.6px) and accent color
- Disabled state shows transparent background with proper foreground dimming
- SmallScrollViewer removed due to "Invalid size returned for Measure" crashes in horizontal layouts
- Tags currently wrap instead of scrolling horizontally (to be addressed in Phase 5)

### Phase 5: Implement Horizontal Scrolling (TODO - Next Session)
- [ ] Investigate scrolling approaches
  - [ ] Research why SmallScrollViewer breaks measurement in horizontal layouts
    - Current symptom: "Invalid size returned for Measure" exception on app startup
    - Occurs specifically when TagInput is in horizontal StackPanel or Grid with Auto columns
    - Removing SmallScrollViewer resolves the crash (tags wrap instead)
  - [ ] Compare MultiComboBox SmallScrollViewer implementation
    - MultiComboBox successfully uses SmallScrollViewer for horizontal tag scrolling
    - Review property differences: MinHeight, MaxHeight, ScrollbarVisibility settings
    - Check if there are additional constraints or bindings needed
  - [ ] Analyze TagInputPanel measure/arrange behavior
    - Understand how TagInputPanel calculates desired size
    - Determine if panel needs size constraints when wrapped in ScrollViewer
  - [ ] Test different ScrollViewer configurations
    - Try standard ScrollViewer vs SmallScrollViewer
    - Experiment with explicit width/height constraints
    - Test MaxWidth binding to parent or container
    - Try ClipToBounds and other layout-affecting properties
  - [ ] Consider conditional scrolling based on layout context
    - Detect if control is in horizontal vs vertical container
    - Apply ScrollViewer only when width is constrained
    - Use attached properties or template selectors if needed
- [ ] Implement chosen solution
  - [ ] Apply changes to DevExpress TagInput.axaml
  - [ ] Test in all ControlAlignment scenarios (horizontal/vertical grids and stackpanels)
  - [ ] Verify no crashes and proper scrolling behavior
  - [ ] Test with many tags to confirm scroll functionality
- [ ] Commit scrolling implementation
  - Commit message: "Add horizontal scrolling support to TagInput"

**Investigation Context:**
- Current implementation: ItemsControl directly in Panel (no ScrollViewer)
- Works: No crashes, proper layout, tags wrap to multiple lines
- Doesn't work: Adding SmallScrollViewer causes immediate crash in horizontal layouts
- Reference: MultiComboBox has working SmallScrollViewer with HorizontalScrollBarVisibility
- Goal: Enable horizontal scrolling without breaking measurement system

### Phase 6: Documentation & Polish
- [ ] Update DevExpress theme README
  - [ ] Add TagInput to control list with ✅ status
  - [ ] Add screenshot or description if README includes examples
- [ ] Update AvaloniaControls README
  - [ ] Document TagInput wrapper if custom controls are listed
- [ ] Verify Design.PreviewWith works in IDE
  - [ ] Open TagInput.axaml in Rider/Visual Studio
  - [ ] Confirm preview renders correctly in design view
  - [ ] Adjust preview examples if needed
- [ ] (Nice-to-have) Add graceful tag closing animation
  - [ ] Research Avalonia animation approaches for tag removal
  - [ ] Implement smooth shrink/fade animation when tag is closed
  - [ ] Test animation performance and smoothness
  - [ ] Skip if too complex or time-consuming
- [ ] Code review checklist
  - [ ] Consistent code formatting
  - [ ] No commented-out code
  - [ ] Proper XML namespaces
  - [ ] Resources follow naming conventions
  - [ ] StaticResource vs DynamicResource used appropriately
- [ ] Final testing
  - [ ] Build entire solution in Release mode
  - [ ] Run SampleApp and test all TagInput scenarios
  - [ ] Test with both DevExpress theme and Fluent fallback
  - [ ] Verify no console errors or warnings
- [ ] Commit documentation updates
  - Commit message: "docs: add TagInput to DevExpress theme documentation"

### Phase 7: Wrap Up
- [ ] Stop and review final implementation with user
- [ ] Address any feedback or requested changes
- [ ] Move this planning doc to `docs/planning/completed/`
- [ ] Final commit with planning doc move
  - Commit message: "docs: complete TagInput theming project"

## Appendix

### Key Ursa TagInput Properties (filled from Phase 1 research)
- **Tags** (IList<string>) - Collection of current tag values (bindable)
- **Watermark** (string?) - Placeholder text displayed when empty
- **Separator** (string) - Character(s) that delimiter multiple tag input (e.g., ",", ";", " ")
- **MaxCount** (int) - Maximum number of tags allowed (default: int.MaxValue)
- **AllowDuplicates** (bool) - Whether repeated tag values are permitted (default: true)
- **LostFocusBehavior** (enum) - Action when focus is lost: Add current input as tag, or Clear input
- **AcceptsReturn** (bool) - Enables multi-line tag input via line breaks
- **ItemTemplate** (IDataTemplate?) - Custom rendering template for individual tags
- **InputTheme** (ControlTheme) - Styling applied to the embedded TextBox
- **InnerLeftContent** (object?) - Content positioned to the left of the input field
- **InnerRightContent** (object?) - Content positioned to the right of the input field
- **Items** (IList) - Internal collection that includes both tags and the TextBox control

### DevExpress Theme Color Keys (to be defined)
- (Will be populated during Phase 4)

### Questions & Decisions Log
- ✅ Dependency strategy: Following existing ClosableTag pattern (Ursa is hard dependency)
- ✅ Theme scope: DevExpress only initially
- ✅ Quality target: Production-ready
- ✅ Phase order: Demo page (Phase 2) before infrastructure (Phase 3) - allows user to see default styling first
- ✅ Use cases: Email recipients is primary use case for demo examples
- ✅ Visual design: Focus on consistency with existing DevExpress input controls, minimal animations
- ✅ Animation: Graceful tag closing animation as nice-to-have polish item
- ✅ Demo page complexity: Keep examples simple and focused on basic usage
- ✅ MultiComboBox: Review for tag-related patterns and considerations

### Research Notes

#### Ursa TagInput Control API (Completed)

**Source Files Identified:**
- `src/Ursa/Controls/TagInput/TagInput.cs` - Main control implementation
- `src/Ursa/Controls/TagInput/TagInputPanel.cs` - Layout panel for tags
- `src/Ursa/Controls/TagInput/ClosableTag.cs` - Individual tag component
- `src/Ursa/Controls/TagInput/LostFocusBehavior.cs` - Focus handling
- `src/Ursa.Themes.Semi/Controls/TagInput.axaml` - Default Semi theme styling

**Template Parts:**
- `PART_ItemsControl` - ItemsControl that houses the tag display
- `PART_Watermark` - TextBlock for placeholder text when empty

**Pseudo-Classes:**
- `:empty` - Applied when no tags exist and input is empty
- `:pointerover` - Hover state
- `:focus-within` - Keyboard focus state

**Key Public Properties:**
- `Tags` (IList<string>) - Collection of current tag values
- `Watermark` (string?) - Placeholder text
- `AcceptsReturn` (bool) - Enables multi-line tag input via line breaks
- `MaxCount` (int) - Maximum permissible tags (default: int.MaxValue)
- `Items` (IList) - Internal collection including TextBox control
- `InputTheme` (ControlTheme) - Styling for embedded TextBox
- `ItemTemplate` (IDataTemplate?) - Custom rendering template for tags
- `Separator` (string) - Character(s) delimiting multiple tag input
- `LostFocusBehavior` (LostFocusBehavior enum) - Action on focus loss: Add or Clear
- `AllowDuplicates` (bool) - Permits repeated tag values (default: true)
- `InnerLeftContent` (object?) - Content positioned left of input field
- `InnerRightContent` (object?) - Content positioned right of input field

**Default Theme Structure (Semi theme):**
- Root: Border with MinHeight 30px, Padding 8,4
- Layout: Panel containing TextBlock (watermark) + ItemsControl with TagInputPanel
- Uses custom `TagInputPanel` for horizontal tag layout
- ClosableTag is used as the ItemTemplate with 12x12 close button
- Resource references: TextBoxDefaultBackground, TextBoxDefaultBorderBrush, ClosableTag* colors

#### ClosableTag Wrapper Pattern Analysis (Completed)

**Wrapper Class Pattern:**
```csharp
// File: src/Devolutions.AvaloniaControls/Controls/ClosableTag.axaml.cs
public class ClosableTag : Ursa.Controls.ClosableTag { }
```
- Empty class that simply inherits from Ursa control
- No custom logic needed - wrapper just provides namespace for theme targeting

**Theme File Structure:**
```xml
<!-- File: src/Devolutions.AvaloniaTheme.DevExpress/Controls/ClosableTag.axaml -->
<ResourceDictionary xmlns:u="https://irihi.tech/ursa">
  <Design.PreviewWith>...</Design.PreviewWith>

  <!-- Theme-specific resources -->
  <SolidColorBrush x:Key="ClosableTagBorder" Color="{DynamicResource InputBorderColor}" />

  <!-- ControlTheme targeting Ursa type -->
  <ControlTheme x:Key="{x:Type u:ClosableTag}" TargetType="u:ClosableTag">
    <!-- Setters and Template -->
  </ControlTheme>

  <!-- StaticResource alias for wrapper type -->
  <StaticResource x:Key="{x:Type ClosableTag}" ResourceKey="{x:Type u:ClosableTag}" />
</ResourceDictionary>
```

**Key Pattern Elements:**
1. Use `xmlns:u="https://irihi.tech/ursa"` for Ursa namespace
2. Create ControlTheme with `TargetType="u:ClosableTag"` (Ursa type)
3. Add StaticResource alias at the end to map wrapper type to Ursa theme
4. Include Design.PreviewWith for IDE designer support

**ClosableTag Styling Details:**
- MinHeight: 21px
- BorderThickness: 1
- CornerRadius: 3
- Padding: 8,0,4,0
- Margin: 1
- Layout: Border > DockPanel with close button (right) + ContentPresenter
- Close button: Theme="{DynamicResource InnerIconButton}", Margin="4,0,0,0"
- FontSize: 12 for content
- Colors reference: InputBorderColor, ClosableTagBackgroundColor, TextForegroundColor

#### DevExpress Theme Resource Organization (Completed)

**Color Resources Structure:**
- Location: `src/Devolutions.AvaloniaTheme.DevExpress/Accents/ThemeResources.axaml`
- Uses ThemeDictionaries with "Light" and "Dark" keys for theme variants
- Colors are defined as Color resources (not brushes) in ThemeResources
- Controls create SolidColorBrush resources locally referencing these colors via DynamicResource

**Relevant Colors for TagInput:**
- `BackgroundColor` - #ffffff (light) / #202020 (dark)
- `BorderColor` - #dadada (light) / #404040 (dark)
- `ForegroundColor` - #000000 (light) / #f5f5f5 (dark)
- `TextBoxBackgroundColor` - #fff (light) / #202020 (dark)
- `TextBoxBorderBottomColor` - #9f9f9f (light) / #666666 (dark)
- `SystemAccentColor` - Used for focus states (via ControlBackgroundPointerOverBrush with opacity)

**Dimension Resources (from TextBox):**
- `TextBasedInputMinHeight` - Standard height for input controls
- `TextBoxPadding` - Standard padding for input fields
- `ControlCornerRadius` - Standard corner radius
- `ControlFontSize` - Standard font size

**TextBox Styling Patterns:**
- BorderThickness: "1 1 1 0" (no bottom border in main border)
- Separate BottomBorderElement with BorderThickness="0 0 0 1"
- Uses DataValidationErrors wrapper for validation support
- InnerLeftContent and InnerRightContent support for icons/decorations
- Focus indicated by changing BottomBorderElement brush

**Note on ClosableTag Colors:**
- ClosableTag.axaml references InputBorderColor, ClosableTagBackgroundColor, and TextForegroundColor
- These are NOT defined in ThemeResources.axaml
- They come from Ursa's default theme (via Fluent fallback)
- This is acceptable - means ClosableTag uses framework defaults

#### MultiComboBox Analysis (Completed)

**Finding:** MultiComboBox does NOT use ClosableTag or TagInput
- Uses its own `MultiComboBoxItem` for displaying selected items
- No special interaction patterns to consider
- TagInput will be independent - no conflicts or shared patterns

#### Phase 1 Summary

✅ **Research Complete** - All key information gathered:
1. TagInput has 12 public properties for customization
2. Control uses 2 template parts (PART_ItemsControl, PART_Watermark)
3. 3 main pseudo-classes for visual states (:empty, :pointerover, :focus-within)
4. Wrapper pattern is simple: empty class + theme file with StaticResource alias
5. DevExpress theme uses structured color system with Light/Dark variants
6. TextBox provides standard dimensions and styling patterns to follow
7. No conflicts with MultiComboBox - independent implementation

**Next Steps:** Proceed to Phase 2 - Create SampleApp demo page to see default Ursa styling
