# Ursa TagInput Control Theming - MacOS & Linux

## Goal

Add theming support for Ursa's TagInput control to both MacOS and Linux themes, following the established DevExpress implementation pattern. This will provide consistent TagInput styling across all three themes.

## Context

### What We've Already Done
- **DevExpress theme complete**: Full TagInput theming with two-layer border system, focus states, and validation support (PR #278)
- **Infrastructure exists**: TagInput wrapper class and default theme already in AvaloniaControls
- **Demo page exists**: `TagInputDemo.axaml` in SampleApp demonstrates all control features
- **ClosableTag status**:
  - MacOS: Has ClosableTag theming ✅
  - Linux: Uses default ClosableTag (looks fine, no custom theming needed) ✅

### Implementation Approach
Following user guidance: **Start with MacOS, then Linux** (unless parallel implementation is more efficient)

Since both themes:
1. Share the same wrapper class (already exists)
2. Can reference the same ItemTemplate pattern
3. Have different visual styles but similar structure
4. Are independent implementations

**Recommendation**: Implement sequentially (MacOS first) to ensure each theme's unique design language is properly captured.

## References

- `.claude/docs/planning/completed/2025_10_21__Ursa_TagInput_theming.md` - Complete DevExpress implementation process
- `src/Devolutions.AvaloniaControls/Controls/TagInput.axaml` - Wrapper class and default theme
- `src/Devolutions.AvaloniaTheme.DevExpress/Controls/TagInput.axaml` - DevExpress reference implementation
- `src/Devolutions.AvaloniaTheme.MacOS/Controls/TextBox.axaml` - MacOS input control styling patterns
- `src/Devolutions.AvaloniaTheme.MacOS/Controls/ClosableTag.axaml` - MacOS tag styling
- `src/Devolutions.AvaloniaTheme.Linux/Controls/TextBox.axaml` - Linux input control styling patterns
- Ursa documentation: https://irihi.tech/ursa/

## Principles & Key Decisions

1. **Follow DevExpress implementation structure** - proven pattern with focus states, validation, layout constraints
2. **Match each theme's design language** - MacOS (AppKit style), Linux (Yaru GTK style)
3. **Sequential implementation** - MacOS first, then Linux (cleaner testing and validation per theme)
4. **Leverage existing ClosableTag styling** - Tags within TagInput should match standalone ClosableTag appearance
5. **No new infrastructure needed** - Wrapper and default theme already exist
6. **Production-ready quality** - Will be released in theme packages
7. **Reuse existing demo page** - No need to modify `TagInputDemo.axaml`

## Theme Design Goals

### MacOS Theme
- **Match AppKit input styling**: Border thickness, corner radius, and colors from TextBox
- **Focus ring behavior**: Consistent with MacOS TextBox focus indication
- **Tag appearance**: Leverage existing ClosableTag theme (MinHeight: 16px)
- **Height consistency**: Match other MacOS input controls
- **Color system**: Use MacOS theme's Dynamic Resources (ForegroundHighBrush, TextBoxBackgroundBrush, etc.)

### Linux Theme
- **Match Yaru GTK styling**: Border, colors, and spacing from Linux TextBox
- **Focus behavior**: Consistent with Linux TextBox focus indication
- **Tag appearance**: Use default ClosableTag (no custom Linux styling needed)
- **Minimal approach**: Linux theme has fewer styled controls - maintain this philosophy
- **Color system**: Use Linux theme's Dynamic Resources

## Implementation Plan

### Phase 1: MacOS TagInput Theme Implementation

#### 1.1: Research MacOS Design Patterns
- [ ] Review MacOS TextBox styling in detail
  - [ ] Border structure (likely single border, not two-layer like DevExpress)
  - [ ] Color resources (background, border, focus colors)
  - [ ] Dimensions (MinHeight, Padding, CornerRadius)
  - [ ] Focus state implementation
  - [ ] Disabled state styling
- [ ] Review MacOS ClosableTag dimensions and spacing
  - [ ] MinHeight: 16px (vs DevExpress 21px)
  - [ ] Padding and margin values
  - [ ] Ensure TagInput internal padding accommodates tags

#### 1.2: Create MacOS TagInput Theme File
- [ ] Create `src/Devolutions.AvaloniaTheme.MacOS/Controls/TagInput.axaml`
- [ ] Set up basic file structure:
  - [ ] Add XML namespaces (including `u="https://irihi.tech/ursa"`)
  - [ ] Add Design.PreviewWith section with ThemePreviewer
  - [ ] Include preview examples (basic, with tags, disabled)
- [ ] Create ControlTheme for `u:TagInput`:
  - [ ] Copy DevExpress structure as starting point
  - [ ] Adapt template to match MacOS border/layout pattern
  - [ ] Reference MacOS color resources (not DevExpress colors)
  - [ ] Set appropriate MinHeight for MacOS
  - [ ] Configure ItemTemplate with ClosableTag
  - [ ] Set up PART_Watermark and PART_ItemsControl
- [ ] Create TagInputTextBoxTheme (if needed):
  - [ ] Style for embedded TextBox within TagInput
  - [ ] Remove focus adorner with `FocusAdorner="{x:Null}"`
  - [ ] Match MacOS TextBox input styling
- [ ] Implement visual states:
  - [ ] `:empty` - Show watermark when no tags
  - [ ] `:focus-within` - Apply MacOS focus styling
  - [ ] `:disabled` - Match MacOS disabled input appearance
- [ ] Add StaticResource alias for wrapper type at end

#### 1.3: Integrate into MacOS Theme
- [ ] Add `<MergeResourceInclude Source="TagInput.axaml" />` to `Controls/_index.axaml`
- [ ] Insert in alphabetical order (between TabPane and TextBlock)

#### 1.4: Test MacOS Implementation
- [ ] Build solution
  ```bash
  dotnet build src/Devolutions.AvaloniaTheme.MacOS
  ```
- [ ] Run SampleApp with MacOS theme
  ```bash
  /workon MacOS TagInputDemo
  ```
- [ ] Verify TagInput functionality:
  - [ ] Adding tags works (Enter, separators)
  - [ ] Removing tags works (X button)
  - [ ] Watermark shows/hides correctly
  - [ ] Focus state appears correctly
  - [ ] Disabled state looks appropriate
  - [ ] Height matches other MacOS input controls
  - [ ] Tags display with proper spacing and wrap correctly
- [ ] Test in ControlAlignment page:
  - [ ] No measurement errors in horizontal/vertical layouts
  - [ ] Control aligns properly with other inputs
- [ ] Visual inspection:
  - [ ] Compare side-by-side with TextBox, ComboBox
  - [ ] Verify colors match MacOS theme palette
  - [ ] Check tag spacing and sizing

#### 1.5: Update MacOS Documentation
- [ ] Update `src/Devolutions.AvaloniaTheme.MacOS/README.md`
  - [ ] Add TagInput to control list with ✅ status
  - [ ] Add description and key features in detailed section
- [ ] Verify Design.PreviewWith works in IDE

#### 1.6: Commit MacOS Implementation
- [ ] Review code quality:
  - [ ] Consistent formatting
  - [ ] No commented-out code
  - [ ] Proper resource naming
- [ ] Create commit:
  ```
  [MacOS] Add TagInput control theming

  Adds full theming support for Ursa's TagInput control to the MacOS theme,
  matching AppKit design patterns and integrating with existing ClosableTag styling.

  - Matches MacOS TextBox border and focus behavior
  - Leverages existing ClosableTag theme (MinHeight: 16px)
  - Includes watermark, focus, and disabled states
  - Integrates with form validation system

  🤖 Generated with [Claude Code](https://claude.com/claude-code)

  Co-Authored-By: Claude <noreply@anthropic.com>
  ```

---

### Phase 2: Linux TagInput Theme Implementation

#### 2.1: Research Linux Design Patterns
- [ ] Review Linux TextBox styling in detail
  - [ ] Border structure
  - [ ] Color resources (Yaru GTK color scheme)
  - [ ] Dimensions (MinHeight, Padding, CornerRadius)
  - [ ] Focus state implementation
  - [ ] Disabled state styling
- [ ] Confirm default ClosableTag appearance is acceptable
  - [ ] Run quick visual test to verify
  - [ ] No need for custom Linux ClosableTag theme

#### 2.2: Create Linux TagInput Theme File
- [ ] Create `src/Devolutions.AvaloniaTheme.Linux/Controls/TagInput.axaml`
- [ ] Set up basic file structure:
  - [ ] Add XML namespaces (including `u="https://irihi.tech/ursa"`)
  - [ ] Add Design.PreviewWith section with ThemePreviewer
  - [ ] Include preview examples (basic, with tags, disabled)
- [ ] Create ControlTheme for `u:TagInput`:
  - [ ] Use MacOS/DevExpress as structural reference
  - [ ] Adapt template to match Linux (Yaru GTK) patterns
  - [ ] Reference Linux color resources
  - [ ] Set appropriate MinHeight for Linux
  - [ ] Configure ItemTemplate with ClosableTag (uses default styling)
  - [ ] Set up PART_Watermark and PART_ItemsControl
- [ ] Create TagInputTextBoxTheme (if needed):
  - [ ] Style for embedded TextBox within TagInput
  - [ ] Remove focus adorner
  - [ ] Match Linux TextBox input styling
- [ ] Implement visual states:
  - [ ] `:empty` - Show watermark
  - [ ] `:focus-within` - Apply Linux focus styling
  - [ ] `:disabled` - Match Linux disabled input appearance
- [ ] Add StaticResource alias for wrapper type

#### 2.3: Integrate into Linux Theme
- [ ] Add `<MergeResourceInclude Source="TagInput.axaml" />` to `Controls/_index.axaml`
- [ ] Insert in alphabetical order (between TabItem and TextBox)

#### 2.4: Test Linux Implementation
- [ ] Build solution
  ```bash
  dotnet build src/Devolutions.AvaloniaTheme.Linux
  ```
- [ ] Run SampleApp with Linux theme
  ```bash
  /workon Linux TagInputDemo
  ```
- [ ] Verify TagInput functionality:
  - [ ] Adding tags works (Enter, separators)
  - [ ] Removing tags works (X button)
  - [ ] Watermark shows/hides correctly
  - [ ] Focus state appears correctly
  - [ ] Disabled state looks appropriate
  - [ ] Height matches other Linux input controls
  - [ ] Tags display with proper spacing (using default ClosableTag)
- [ ] Test in ControlAlignment page:
  - [ ] No measurement errors in horizontal/vertical layouts
  - [ ] Control aligns properly with other inputs
- [ ] Visual inspection:
  - [ ] Compare with Linux TextBox, ComboBox
  - [ ] Verify colors match Linux/Yaru theme palette
  - [ ] Verify default ClosableTag styling looks good

#### 2.5: Update Linux Documentation
- [ ] Update `src/Devolutions.AvaloniaTheme.Linux/README.md`
  - [ ] Add TagInput to control list with ✅ status
  - [ ] Add description and key features in detailed section
- [ ] Verify Design.PreviewWith works in IDE

#### 2.6: Commit Linux Implementation
- [ ] Review code quality:
  - [ ] Consistent formatting
  - [ ] No commented-out code
  - [ ] Proper resource naming
- [ ] Create commit:
  ```
  [Linux] Add TagInput control theming

  Adds theming support for Ursa's TagInput control to the Linux theme,
  following Yaru GTK design patterns. Uses default ClosableTag styling.

  - Matches Linux TextBox border and focus behavior
  - Uses default ClosableTag theme (no custom Linux styling needed)
  - Includes watermark, focus, and disabled states
  - Integrates with form validation system

  🤖 Generated with [Claude Code](https://claude.com/claude-code)

  Co-Authored-By: Claude <noreply@anthropic.com>
  ```

---

### Phase 3: Cross-Theme Testing & Documentation

#### 3.1: Comprehensive Testing
- [ ] Test theme switching functionality:
  ```bash
  /workon MacOS TagInputDemo
  /workon Linux TagInputDemo
  /workon DevExpress TagInputDemo
  ```
- [ ] Verify each theme has distinct visual appearance
- [ ] Verify functionality works consistently across all themes
- [ ] Test ControlAlignment page with each theme
- [ ] Check for any layout issues when switching themes

#### 3.2: Final Documentation Updates
- [ ] Update AvaloniaControls README (if not already done):
  - [ ] Confirm TagInput wrapper is documented
  - [ ] Add usage examples if helpful
- [ ] Review all three theme READMEs for consistency:
  - [ ] DevExpress TagInput documentation
  - [ ] MacOS TagInput documentation
  - [ ] Linux TagInput documentation

#### 3.3: Code Quality Review
- [ ] Review all TagInput.axaml files for consistency:
  - [ ] Proper XML namespaces
  - [ ] Consistent naming conventions
  - [ ] Appropriate use of StaticResource vs DynamicResource
  - [ ] Design.PreviewWith sections are comprehensive
- [ ] Verify no code duplication (structure is similar but styling differs)
- [ ] Check for any commented-out code or debug artifacts

#### 3.4: Final Build Verification
- [ ] Build entire solution in Debug mode:
  ```bash
  dotnet build avalonia-extensions.sln
  ```
- [ ] Verify no build errors or warnings related to TagInput
- [ ] Note: Release builds will work after AvaloniaControls package is published

---

### Phase 4: Wrap Up

#### 4.1: Update Planning Document
- [ ] Move this document to `.claude/docs/planning/completed/`
- [ ] Add final notes and lessons learned
- [ ] Document any deviations from the plan

#### 4.2: Prepare Pull Request
- [ ] Review all commits created
- [ ] Ensure commit messages follow project guidelines
- [ ] Create PR with comprehensive description:
  - [ ] Summary of changes (MacOS and Linux TagInput theming)
  - [ ] Reference to DevExpress implementation (PR #278)
  - [ ] Testing notes
  - [ ] Screenshots (user will add)
- [ ] Request user to add screenshots showing:
  - [ ] MacOS TagInput in various states
  - [ ] Linux TagInput in various states
  - [ ] Theme comparison showing all three themes

#### 4.3: Final Review with User
- [ ] Present completed implementation
- [ ] Demonstrate functionality in SampleApp
- [ ] Show theme switching behavior
- [ ] Discuss any improvements or polish needed

---

## Technical Notes

### Key Differences Between Themes

**DevExpress**:
- Two-layer border system (BorderThickness="1 1 1 0" + separate bottom border)
- Bottom border changes thickness on focus (1.6px)
- ClosableTag MinHeight: 21px
- Strong visual hierarchy with accent colors

**MacOS**:
- Likely single border system (typical of AppKit)
- Focus indication via border color/glow (needs research)
- ClosableTag MinHeight: 16px (smaller tags)
- Subtle, native macOS appearance

**Linux**:
- Yaru GTK styling patterns
- Focus indication (needs research)
- Uses default ClosableTag (no theme override)
- Clean, minimal Ubuntu/GTK aesthetic

### Common Template Structure
All three themes should share:
1. DataValidationErrors wrapper (for form validation)
2. PART_Watermark TextBlock
3. PART_ItemsControl with TagInputPanel
4. ItemTemplate with ClosableTag
5. Visual states (`:empty`, `:focus-within`, `:disabled`)
6. MaxWidth constraint to prevent measurement errors in horizontal layouts
7. HorizontalAlignment="Left" on ItemsControl
8. Embedded TextBox theme with FocusAdorner="{x:Null}"

### Measurement Error Prevention
Lessons learned from DevExpress implementation (Phase 5):
- Ursa's TagInputPanel returns invalid size with infinite horizontal constraint
- Fix: Set `HorizontalAlignment="Left"` on ItemsControl
- Fix: Set `MaxWidth` constraint (e.g., 600-800px depending on theme)
- Fix: Set `VerticalAlignment="Center"` for proper height control

### Resource Naming Pattern
Follow existing conventions:
- `TagInputTextBoxTheme` - Embedded TextBox theme
- Use theme-specific color resources (not hard-coded colors)
- StaticResource for fixed resources, DynamicResource for theme-switchable values

## Success Criteria

- [ ] MacOS TagInput matches MacOS design language and TextBox styling
- [ ] Linux TagInput matches Linux (Yaru GTK) design language and TextBox styling
- [ ] All TagInput functionality works in both themes (add, remove, watermark, focus, disabled)
- [ ] No measurement errors in horizontal or vertical layouts
- [ ] Height is consistent with other input controls in each theme
- [ ] Theme switching works smoothly without visual glitches
- [ ] Documentation is complete and accurate
- [ ] Code quality matches existing theme implementations
- [ ] Demo page works with all three themes
- [ ] Design.PreviewWith sections work in IDE for both themes

## Questions & Decisions Log

- ✅ Implementation order: MacOS first, then Linux (sequential approach)
- ✅ Linux ClosableTag: Use default styling (no custom theme needed per user guidance)
- ✅ Reuse existing demo page: No modifications needed to TagInputDemo.axaml
- ✅ Infrastructure: Wrapper class and default theme already exist (no new files in AvaloniaControls)
- ⏳ MacOS focus behavior: TBD - research in Phase 1.1
- ⏳ Linux focus behavior: TBD - research in Phase 2.1
- ⏳ Embedded TextBox theme needed?: Likely yes for both (to remove focus adorner)

## Risk Assessment

**Low Risk Areas**:
- Infrastructure already exists (wrapper, default theme)
- DevExpress implementation provides proven template structure
- ClosableTag themes already exist (MacOS) or not needed (Linux)
- Demo page already comprehensive

**Medium Risk Areas**:
- MacOS focus behavior might differ significantly from DevExpress two-layer border approach
- Linux theme has fewer styled controls - need to ensure consistency with existing patterns
- Height tuning might require iteration to match other inputs

**Mitigation**:
- Thorough research phase for each theme (Phase 1.1, 2.1)
- Test early and often with SampleApp
- Compare side-by-side with TextBox in each theme
- Follow established patterns from existing controls

## Estimated Complexity

- **Phase 1 (MacOS)**: Medium complexity
  - Similar to DevExpress but different visual design
  - Existing ClosableTag helps
  - 1-2 hours estimated

- **Phase 2 (Linux)**: Low-Medium complexity
  - Can leverage MacOS implementation structure
  - Simpler due to default ClosableTag
  - Fewer styled controls means simpler patterns
  - 1 hour estimated

- **Phase 3 (Testing)**: Low complexity
  - Demo page and test cases already exist
  - 30 minutes estimated

- **Phase 4 (Wrap up)**: Low complexity
  - Documentation and PR preparation
  - 30 minutes estimated

**Total Estimated Time**: 3-4 hours

## Appendix

### File Checklist

**MacOS Theme**:
- [ ] `src/Devolutions.AvaloniaTheme.MacOS/Controls/TagInput.axaml` (NEW)
- [ ] `src/Devolutions.AvaloniaTheme.MacOS/Controls/_index.axaml` (MODIFY - add TagInput include)
- [ ] `src/Devolutions.AvaloniaTheme.MacOS/README.md` (MODIFY - add TagInput to control list)

**Linux Theme**:
- [ ] `src/Devolutions.AvaloniaTheme.Linux/Controls/TagInput.axaml` (NEW)
- [ ] `src/Devolutions.AvaloniaTheme.Linux/Controls/_index.axaml` (MODIFY - add TagInput include)
- [ ] `src/Devolutions.AvaloniaTheme.Linux/README.md` (MODIFY - add TagInput to control list)

**No Changes Needed**:
- ✅ `src/Devolutions.AvaloniaControls/Controls/TagInput.axaml.cs` (already exists)
- ✅ `src/Devolutions.AvaloniaControls/Controls/TagInput.axaml` (already exists)
- ✅ `samples/SampleApp/DemoPages/TagInputDemo.axaml` (already exists)

### Key Code Patterns

**StaticResource Alias** (all themes):
```xml
<StaticResource x:Key="{x:Type TagInput}" ResourceKey="{x:Type u:TagInput}" />
```

**ItemTemplate Pattern** (all themes):
```xml
<Setter Property="ItemTemplate">
  <DataTemplate>
    <ClosableTag
      Content="{Binding}"
      Command="{Binding Close, RelativeSource={RelativeSource FindAncestor, AncestorType=u:TagInput}}" />
  </DataTemplate>
</Setter>
```

**Watermark Visibility** (all themes):
```xml
<Style Selector="^:empty /template/ TextBlock#PART_Watermark">
  <Setter Property="IsVisible" Value="True" />
</Style>
```

### Useful Commands

```bash
# Build individual themes
dotnet build src/Devolutions.AvaloniaTheme.MacOS
dotnet build src/Devolutions.AvaloniaTheme.Linux

# Run SampleApp with specific theme
/workon MacOS TagInputDemo
/workon Linux TagInputDemo

# Build entire solution
dotnet build avalonia-extensions.sln

# Run from bin directory (proper theme detection)
dotnet build samples/SampleApp/SampleApp.csproj && cd samples/SampleApp/bin/Debug/net9.0 && dotnet SampleApp.dll
```
