# macOS Liquid Glass Theme Support - Implementation Plan

**Status:** Planning / In Progress
**Created:** 2025-11-03
**Last Updated:** 2025-11-03

## Overview

This document outlines the plan for adding macOS 26 (Tahoe) Liquid Glass visual design support to `Devolutions.AvaloniaTheme.MacOS` while maintaining full backward compatibility with earlier macOS versions.

## Background

macOS 26 (Tahoe), released September 2025, introduced "Liquid Glass" - a new design language featuring:
- Enhanced translucency and blur effects
- Vibrancy that samples colors from behind surfaces
- Lens-like distortion and depth effects
- Chromatic aberration at edges
- GPU-accelerated rendering (Core Animation + Metal)
- Adaptive behavior between light/dark modes

**Design Goals:**
1. Users on macOS <26 see the current classic theme (unchanged)
2. Users on macOS ≥26 see enhanced Liquid Glass-inspired styling
3. Zero breaking changes to existing API
4. Conservative approach prioritizing readability over visual gimmicks

## Technical Architecture

### 1. OS Version Detection

**Implementation:** Use built-in .NET API for version detection

```csharp
// Converters/MacOSVersionDetector.cs
public static class MacOSVersionDetector
{
    private static bool? _isLiquidGlassSupported;

    public static bool IsLiquidGlassSupported()
    {
        if (_isLiquidGlassSupported.HasValue)
            return _isLiquidGlassSupported.Value;

        // Check if running on macOS AND version 26+
        _isLiquidGlassSupported = OperatingSystem.IsMacOS()
            && OperatingSystem.IsMacOSVersionAtLeast(26);

        return _isLiquidGlassSupported.Value;
    }

    // For testing purposes
    internal static bool? _testOverride;
    public static void SetTestOverride(bool? value) => _testOverride = value;
}
```

**Why this approach:**
- Uses native `libobjc.get_operatingSystemVersion` API
- No external process spawning (unlike sysctl approach)
- Official .NET API (available since .NET 5)
- Platform analyzer support
- Cached for performance (version won't change during process lifetime)

**Version Mapping Reference:**
- macOS 26.x (Tahoe) = Darwin 25.x = Liquid Glass
- macOS 15.x (Sequoia) = Darwin 24.x = Classic
- macOS 14.x (Sonoma) = Darwin 23.x = Classic

### 2. Resource Organization Pattern

**Strategy:** Conditional resource loading with override files

The theme currently uses a single `ThemeResources.axaml` file with `ThemeDictionaries` for Light/Dark modes. We'll extend this pattern with conditional loading for OS-specific enhancements.

**Structure:**
```
Accents/
├── ThemeResources.axaml           # Base resources (all OS versions, all themes)
├── LiquidGlassEffects.axaml      # macOS 26+ specific enhancements (NEW)
└── Icons.axaml                    # Icons
```

**LiquidGlassEffects.axaml pattern:**
```xml
<ResourceDictionary>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Default"> <!-- Light + Liquid Glass -->
      <!-- Override specific resources for Liquid Glass look -->
      <BoxShadows x:Key="PopupShadow">
        0 0 2 0 #80000000,
        0 2 8 0 #60000000,
        0 12 32 0 #50000000
      </BoxShadows>
      <x:Double x:Key="MenuBackgroundOpacity">0.7</x:Double>
    </ResourceDictionary>

    <ResourceDictionary x:Key="Dark"> <!-- Dark + Liquid Glass -->
      <!-- Same keys with dark-mode appropriate values -->
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

**Benefits:**
- Only overrides what's different (minimal duplication)
- Follows existing ThemeDictionaries pattern
- Clear separation of base vs. enhanced resources
- Easy to see what's Liquid Glass-specific
- Both Light AND Dark modes enhanced

### 3. Theme Initialization Changes

**Modify theme loading to conditionally merge Liquid Glass resources:**

```csharp
// Internal/MacOsTheme.axaml.cs
public MacOsTheme(IServiceProvider sp)
{
    AvaloniaXamlLoader.Load(sp, this);

    // Conditionally load Liquid Glass enhancements
    if (MacOSVersionDetector.IsLiquidGlassSupported())
    {
        this.Resources.MergedDictionaries.Add(new ResourceInclude
        {
            Source = new Uri("avares://Devolutions.AvaloniaTheme.MacOS/Accents/LiquidGlassEffects.axaml")
        });
    }
}
```

**Similarly for MacOsThemeWithGlobalStyles.axaml.cs**

### 4. Avalonia Capabilities vs. Liquid Glass Features

**What Avalonia Can Achieve:**
- ✅ Enhanced translucency via opacity values
- ✅ Vibrancy-inspired tinted backgrounds
- ✅ Deeper, more defined shadows (BoxShadows)
- ✅ ExperimentalAcrylicMaterial for blur-behind effects
- ✅ Dynamic resource switching at runtime
- ✅ Platform-specific rendering

**Limitations:**
- ❌ Native lens distortion effects (requires custom shaders)
- ❌ Chromatic aberration (too complex, minimal value)
- ❌ Specular highlights (not truly reactive)
- ❌ Direct NSVisualEffectView control (cross-platform framework)

**Design Philosophy:** Create "Liquid Glass-inspired" styling that captures the spirit of the design (enhanced depth, translucency, vibrancy) without attempting pixel-perfect reproduction of Apple's native effects.

## Implementation Phases

### Phase 1: Foundation & Infrastructure ✅ IN PROGRESS

**Deliverables:**
- [x] Research and architecture design
- [ ] Create `MacOSVersionDetector` class
- [ ] Set up conditional resource loading in theme initialization
- [ ] Create empty `LiquidGlassEffects.axaml` with ThemeDictionaries structure
- [ ] Add test override mechanism for development
- [ ] Verify version detection works correctly on various macOS versions

**Testing:**
- Create simple test override (e.g., set foreground to green) to verify switching works
- Test on macOS 26+ (Liquid Glass should apply)
- Test on macOS <26 (Classic theme should apply)

**Duration:** 1-2 days

### Phase 2: SampleApp Integration & Testing 🔄 NEXT

**Goal:** Verify theme switching infrastructure works before implementing visual effects.

**Deliverables:**
- [ ] Add version override mechanism to SampleApp (hardcode OS version 26 vs 27)
- [ ] Add simple visual differentiator to `LiquidGlassEffects.axaml` (e.g., green foreground color)
- [ ] Test automatic OS version detection:
  - Set override to 26 → should show Liquid Glass indicator
  - Set override to 25 → should show Classic (no indicator)
- [ ] Test manual theme switching in SampleApp
- [ ] Verify conditional resource loading works correctly
- [ ] Document testing approach for future development

**Testing Strategy:**
- Use obvious visual marker (green text or similar) to confirm Liquid Glass mode
- Toggle between OS version 26 and 27 using hardcoded override
- Verify fallback to classic theme when version < 26
- No actual visual effects needed yet - just infrastructure validation

**Duration:** 1 day

### Phase 3: Liquid Glass Visual Effects Definition

**Deliverables:**
- [ ] Define enhanced shadow resources (PopupShadow, ToolTipBorderShadow, etc.)
- [ ] Define translucency levels (conservative approach)
- [ ] Define blur radii (if using acrylic effects)
- [ ] Create complete `LiquidGlassEffects.axaml` with Light/Dark variants
- [ ] Document each override with rationale

**Key Resources to Override:**
```xml
<!-- Shadows - deeper, more defined -->
<BoxShadows x:Key="PopupShadow">...</BoxShadows>
<BoxShadows x:Key="ToolTipBorderShadow">...</BoxShadows>
<BoxShadows x:Key="ControlShadow">...</BoxShadows>

<!-- Translucency - conservative values -->
<x:Double x:Key="WindowBackgroundOpacity">0.90</x:Double>
<x:Double x:Key="MenuBackgroundOpacity">0.80</x:Double>
<x:Double x:Key="TooltipBackgroundOpacity">0.85</x:Double>

<!-- Vibrancy-inspired brushes -->
<SolidColorBrush x:Key="VibrancyBackgroundBrush"
                 Color="{DynamicResource BackgroundColor}"
                 Opacity="0.75" />
```

**Design Principles:**
- More conservative than Apple's Liquid Glass (prioritize readability)
- Enhance depth and polish without compromising usability
- Respect system "Reduce Transparency" setting (future enhancement)

**Duration:** 2-3 days

### Phase 4: Selective Control Updates

**Priority Controls (High Visual Impact):**

1. **Window** - Main application chrome
2. **Menu/ContextMenu** - Prominent glass effect
3. **ToolTip** - Small but frequent
4. **DataGrid** - Large surface area
5. **ScrollViewer** - Translucent scrollbars

**Medium Priority:**
6. ComboBox/ListBox dropdowns
7. TabControl backgrounds
8. Expander panels
9. TreeView sidebars

**Low Priority (minimal changes):**
10. Button/CheckBox/RadioButton - too small for complex effects
11. TextBox - should remain solid for readability
12. Slider/ToggleSwitch - functional, not decorative

**Approach:**
- Initially, try resource overrides only (no control template changes)
- If control templates need modification, do so sparingly
- Test each control in both light and dark modes
- Compare side-by-side with macOS <26 to ensure classic mode unchanged

**Duration:** 3-4 days

### Phase 5: Documentation & Polish

**Deliverables:**
- [ ] Update theme README with OS version requirements
- [ ] Document opt-out mechanisms (if needed)
- [ ] Create migration guide for users
- [ ] Add code comments explaining version detection logic
- [ ] Update control status tracking (README checkboxes)
- [ ] Create visual examples/screenshots

**Duration:** 1 day

## Total Estimated Timeline

**Note:** ToggleSwitch cleanup (originally planned as prerequisite) was completed separately in PR #XXX.

**Conservative estimate:** 8-10 days for complete implementation
**Aggressive estimate:** 6-8 days if everything goes smoothly

**Breakdown:**
- Phase 1 (Foundation): 1-2 days
- Phase 2 (SampleApp Testing): 1 day
- Phase 3 (Visual Effects): 2-3 days
- Phase 4 (Control Updates): 3-4 days
- Phase 5 (Documentation): 1 day

## Open Questions & Decisions

### Postponed for Later (After Infrastructure in Place)

1. **Specific Visual Values**
   - Q: What exact shadow, opacity, and blur values should we use?
   - Status: Will determine once switching infrastructure is working
   - Next: Create test overrides to preview different values

2. **Control Scope**
   - Q: Which controls get Liquid Glass treatment?
   - Status: Prioritized list created, but final scope TBD based on visual results
   - Next: Start with top 5 priority controls, expand if successful

3. **Acrylic/Blur-Behind**
   - Q: Should we enable window-level blur-behind effects?
   - Status: Deferred - would require app-level configuration
   - Decision: Start with stylistic translucency only (no true blur-behind)
   - Rationale: Simpler, no app changes required, better compatibility

4. **Performance Thresholds**
   - Q: What's acceptable GPU usage increase?
   - Status: Will measure after implementing visual effects
   - Next: Performance testing in Phase 5

### Outstanding Technical Questions

1. **Accessibility Integration**
   - Q: Should we respect macOS "Reduce Transparency" system setting?
   - Priority: Medium (can be added later)
   - Complexity: Requires system setting monitoring

2. **User Opt-Out Mechanism**
   - Q: Should apps be able to disable Liquid Glass even on macOS 26+?
   - Proposed API: `MacOSVersionDetector.DisableLiquidGlass = true;`
   - Priority: Low (nice-to-have)

3. **Testing on Pre-Release macOS**
   - Q: Do we have access to macOS 26 machines for testing?
   - Workaround: Use version override flag for development
   - Risk: Visual differences may exist between emulated and real Liquid Glass

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Visual inconsistencies between OS versions | Medium | Low | Extensive side-by-side testing |
| Performance degradation | High | Low | Conservative effect values, performance testing |
| Breaking changes for existing users | High | Low | Comprehensive testing, version detection fallbacks |
| Avalonia framework limitations | Medium | Medium | Set realistic expectations, document limitations |
| macOS API changes in future versions | Low | Medium | Use stable .NET APIs, cache version detection |

## Success Criteria

1. ✅ Zero breaking changes for existing users
2. ✅ macOS <26 users see identical theme as before
3. ✅ macOS ≥26 users see polished, modern Liquid Glass-inspired look
4. ✅ Readability maintained (no degradation)
5. ✅ Performance impact negligible (<5% render time increase)
6. ✅ All priority controls enhanced
7. ✅ Documentation complete
8. ✅ Sample app demonstrates both variants

## Dependencies

- .NET 9.0 (already in use)
- Avalonia 11.3.x (already in use)
- macOS 13+ for acrylic support (already required)

## References

**External:**
- [Apple Liquid Glass WWDC Video](https://developer.apple.com/videos/play/wwdc2025/219/)
- [Liquid Glass Wikipedia](https://en.wikipedia.org/wiki/Liquid_Glass)
- [OperatingSystem.IsMacOSVersionAtLeast Docs](https://learn.microsoft.com/en-us/dotnet/api/system.operatingsystem.ismacosversionatleast?view=net-9.0)
- [Avalonia Resources Documentation](https://docs.avaloniaui.net/docs/guides/styles-and-resources/resources)

**Internal:**
- Research findings in conversation history (2025-11-03)
- Current theme README: `src/Devolutions.AvaloniaTheme.MacOS/README.md`
- CLAUDE.md project instructions

## Notes

- This plan will be updated as implementation progresses
- Visual design decisions will evolve based on testing and feedback
- Conservative approach prioritizes stability and readability over visual flair
- Implementation can be done incrementally (each phase is independently valuable)
