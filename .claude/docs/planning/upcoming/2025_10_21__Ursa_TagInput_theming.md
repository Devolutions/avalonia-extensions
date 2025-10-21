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

### Phase 1: Research & Understanding
- [ ] Research Ursa TagInput control
  - [ ] Review Ursa documentation for TagInput features and API
  - [ ] Examine Ursa source code to understand control structure and template parts
  - [ ] Identify all styleable properties, template parts, and visual states
  - [ ] Document key properties: Items, WaterMark, Delimiter, validation, etc.
  - [ ] Take screenshots of default Ursa TagInput appearance for comparison
- [ ] Analyze existing ClosableTag implementation
  - [ ] Review wrapper class pattern in AvaloniaControls
  - [ ] Study theme resource organization in DevExpress
  - [ ] Understand ControlTheme structure and pseudo-class selectors
  - [ ] Document the StaticResource aliasing technique
- [ ] Check MultiComboBox for tag-related patterns
  - [ ] Review how MultiComboBox uses tags internally
  - [ ] Identify reusable patterns or considerations

### Phase 2: Create SampleApp Demo Page
- [ ] Research existing SampleApp structure
  - [ ] Review existing demo pages in `samples/SampleApp/DemoPages/`
  - [ ] Understand navigation and page organization
- [ ] Create TagInputDemoPage with basic examples
  - [ ] Add new demo page AXAML and code-behind
  - [ ] Include in SampleApp navigation/menu system
  - [ ] Add email recipients example (primary use case)
  - [ ] Add basic examples: empty state, pre-populated tags, watermark text
  - [ ] Add state examples: disabled, error states
  - [ ] Keep examples simple and focused on common usage
  - [ ] Use `<u:TagInput>` directly (wrapper doesn't exist yet)
- [ ] Run SampleApp and verify TagInput displays with default Ursa styling
- [ ] Test basic functionality: adding tags, removing tags, input
- [ ] Stop and show user the control with default styling
- [ ] Update planning doc with progress and commit SampleApp changes
  - Follow `.claude/docs/processes/git_commits.md` guidelines
  - Commit message: "Add TagInput demo page to SampleApp"

### Phase 3: Set Up Basic Infrastructure
- [ ] Create TagInput wrapper class
  - [ ] Add `Controls/TagInput.axaml.cs` in AvaloniaControls
  - [ ] Inherit from `Ursa.Controls.TagInput`
  - [ ] Keep empty like ClosableTag wrapper (no custom logic needed)
- [ ] Create basic theme files
  - [ ] Add `Controls/TagInput.axaml` in DevExpress theme
  - [ ] Set up XML namespaces and Design.PreviewWith section
  - [ ] Create initial ControlTheme structure for `u:TagInput`
  - [ ] Add StaticResource alias for wrapper type
  - [ ] Include TagInput.axaml in `Controls/_index.axaml`
- [ ] Update demo page to use both `<u:TagInput>` and `<TagInput>` wrapper
- [ ] Build and verify no compilation errors
- [ ] Update planning doc with progress

### Phase 4: Design DevExpress Theme Styling
- [ ] Analyze DevExpress design patterns in existing controls
  - [ ] Review TextBox styling for input field consistency:113-221
  - [ ] Review ComboBox for dropdown-style controls
  - [ ] Review existing ClosableTag for tag appearance:1-63
  - [ ] Extract common dimensions: heights, paddings, border thickness
  - [ ] Extract common colors: borders, backgrounds, accents
- [ ] Define TagInput theme resources
  - [ ] Add color resources to `Accents/ThemeResources.axaml`
    - TagInputBackground, TagInputBorder, TagInputForeground
    - TagInputFocusBorder, TagInputDisabledBackground, etc.
  - [ ] Define dimension resources: MinHeight, Padding, TagSpacing
  - [ ] Consider light/dark theme variants
- [ ] Create complete visual design specification
  - [ ] Document all control states with expected appearance
  - [ ] Sketch or describe tag layout and spacing
  - [ ] Define focus indicator behavior
  - [ ] Plan animations/transitions if any
- [ ] Stop and review design with user before implementation

### Phase 5: Implement DevExpress Theme
- [ ] Implement base ControlTheme
  - [ ] Set up Template with all PART_ elements from Ursa control
  - [ ] Apply basic setters: Background, Foreground, BorderBrush, Padding
  - [ ] Structure internal layout: input field, tags container, decorations
- [ ] Style tag items within TagInput
  - [ ] Reuse/reference ClosableTag styling where appropriate
  - [ ] Ensure tag appearance matches standalone ClosableTag
  - [ ] Style tag close buttons consistently
- [ ] Implement visual states
  - [ ] Normal state baseline
  - [ ] :focus-within selector for focus state
  - [ ] :disabled selector with appropriate visual feedback
  - [ ] :pointerover states for interactive elements
  - [ ] :error state if TagInput supports validation
- [ ] Test in SampleApp
  - [ ] Run SampleApp with DevExpress theme
  - [ ] Verify all states render correctly
  - [ ] Test keyboard navigation and focus indicators
  - [ ] Test adding/removing tags interactively
  - [ ] Check both light and dark theme variants
  - [ ] Take screenshots for documentation
- [ ] Refine based on testing
  - [ ] Adjust spacing, sizing, colors as needed
  - [ ] Fix any visual glitches or alignment issues
  - [ ] Ensure accessibility (contrast ratios, focus visibility)
- [ ] Update planning doc with implementation notes and commit theme changes
  - Commit message: "Add DevExpress theme styling for TagInput control"

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

### Key Ursa TagInput Properties (to be filled during research)
- Items collection
- Delimiter character(s)
- WaterMark/placeholder text
- Max items limit
- Validation support
- Custom tag templates
- (Add more during Phase 1 research)

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
(Space for notes from Phase 1 research, web searches, discoveries)
