# macOS Liquid Glass Theme Support - Implementation Plan

**Status:** In Progress - Infrastructure Complete
**Created:** 2025-11-03
**Last Updated:** 2025-11-12

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
2. Users on macOS ≥26 see enhanced Liquid Glass-inspired styling automatically
3. Zero breaking changes to existing API
4. Conservative approach prioritizing readability over visual gimmicks
5. SampleApp allows manual sub-theme selection for testing/development

## Current Status Summary

### ✅ Phase 1 Complete - Infrastructure
1. **`MacOSVersionDetector.cs`** - Fully implemented with:
   - OS version detection using `OperatingSystem.IsMacOSVersionAtLeast(26)`
   - Caching for performance
   - `SetTestOverride(bool?)` method for development/testing
   - `ResetCache()` method for test isolation
   - Moved to `Internal/` namespace (better organization)

2. **`ThemeResources_LiquidGlass.axaml`** - Stub created with:
   - Green text colors to visually confirm when LiquidGlass is active
   - Proper ThemeDictionaries structure (Light/Dark modes)
   - Ready to be replaced with real styling

3. **Conditional Resource Loading** - Implemented in:
   - `MacOsTheme.axaml.cs` - Loads LiquidGlass resources when OS ≥ 26
   - `MacOsThemeWithGlobalStyles.axaml.cs` - Same conditional loading

### ✅ Phase 2 Complete - Manual Sub-Theme Switching
1. **Three MacOS Theme Variants** - Created in SampleApp:
   - `MacOsTheme` - "MacOS - default theme" (auto-detects)
   - `MacOsClassicTheme` - "MacOS - classic" (forces classic)
   - `MacOsLiquidGlassTheme` - "MacOS - LiquidGlass" (forces LiquidGlass)

2. **Override Mechanism** - Fully functional:
   - `SetTheme()` calls `MacOSVersionDetector.SetTestOverride()` before loading styles
   - `CreateMacOsStyles()` generates fresh theme instances with current override
   - Manual switching successfully controls resource loading

3. **Testing Confirmed**:
   - Theme switcher shows all three MacOS variants
   - Switching works without errors
   - Green stub appears for LiquidGlass, normal colors for Classic
   - Auto-detection works for default theme

### 🔄 Next Steps
- All infrastructure for LiquidGlass theme development is complete and working in SampleApp.
- Manual theme switching and OS auto-detection are fully implemented.
- The SampleApp now includes a top-right wallpaper preview control (checkbox) for testing translucency effects.
- The next step is to work on individual controls, one at a time, to implement LiquidGlass-inspired visual effects.

## Technical Architecture

### 1. OS Version Detection ✅ COMPLETE

**Location:** `src/Devolutions.AvaloniaTheme.MacOS/Converters/MacOSVersionDetector.cs`

**Implementation Details:**
- Uses `OperatingSystem.IsMacOSVersionAtLeast(26)` API
- Result is cached for performance (OS version cannot change during runtime)
- `SetTestOverride(bool?)` allows manual override for testing
- `ResetCache()` clears both cached value and test override

**Version Mapping Reference:**
- macOS 26.x (Tahoe) = Darwin 25.x = Liquid Glass
- macOS 15.x (Sequoia) = Darwin 24.x = Classic
- macOS 14.x (Sonoma) = Darwin 23.x = Classic

### 2. Resource Organization Pattern ✅ COMPLETE

**Location:** `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml`

**Current Structure:**
```
Accents/
├── ThemeResources.axaml              # Base resources (all OS versions)
├── ThemeResources_LiquidGlass.axaml  # macOS 26+ overrides (STUB - green colors)
└── Icons.axaml                       # Icons
```

**Current Implementation (Stub):**
- Contains green colors to visually confirm LiquidGlass mode is active
- Has proper ThemeDictionaries structure for Light/Dark modes
- Will be replaced with real Liquid Glass styling in Phase 3

**Benefits:**
- Only overrides what's different (minimal duplication)
- Follows existing ThemeDictionaries pattern
- Clear separation of base vs. enhanced resources
- Easy to see what's Liquid Glass-specific
- Both Light AND Dark modes can be enhanced

### 3. Theme Initialization ✅ COMPLETE

**Locations:** 
- `src/Devolutions.AvaloniaTheme.MacOS/Internal/MacOsTheme.axaml.cs`
- `src/Devolutions.AvaloniaTheme.MacOS/Internal/MacOsThemeWithGlobalStyles.axaml.cs`

**Implementation:**
Both files now:
1. Load base theme resources first (`ThemeResources.axaml`)
2. Conditionally load LiquidGlass overrides if `MacOSVersionDetector.IsLiquidGlassSupported()` returns true
3. Load debug theme previewer in DEBUG builds

**Code Pattern:**
```csharp
// 1) Always load the base theme
Uri baseUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml");
ResourceInclude baseInclude = new(baseUri) { Source = baseUri };
this.Resources.MergedDictionaries.Add(baseInclude);

// 2) Conditionally load LiquidGlass overrides
if (MacOSVersionDetector.IsLiquidGlassSupported())
{
    Uri liquidGlassUri = new("avares://Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml");
    ResourceInclude liquidGlassInclude = new(liquidGlassUri) { Source = liquidGlassUri };
    this.Resources.MergedDictionaries.Add(liquidGlassInclude);
}
```

### 4. SampleApp Theme Switching Architecture

**Current State:**
- Theme switcher ComboBox in MainWindow shows 5 themes
- Current MacOS theme option auto-detects OS version
- Switching themes recreates the main window to apply new styles

**Planned Enhancement (Phase 2):**

Add three MacOS theme variants:
1. **"MacOS - default theme"** - Auto-detects based on OS version (current behavior)
2. **"MacOS - classic"** - Forces classic theme (override OS version to 25)
3. **"MacOS - LiquidGlass"** - Forces LiquidGlass theme (override OS version to 26)

**Implementation Approach:**
- Create new Theme classes: `MacOsClassicTheme`, `MacOsLiquidGlassTheme`
- Each variant calls `MacOSVersionDetector.SetTestOverride()` before loading theme
- `MacOsTheme` (default) uses actual OS detection (override = null)

**Files to Modify:**
- `samples/SampleApp/App.axaml.cs` - Add new theme classes and switching logic
- `samples/SampleApp/ViewModels/MainWindowViewModel.cs` - Add new themes to available list

### 5. Avalonia Capabilities vs. Liquid Glass Features

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

### Phase 1: Foundation & Infrastructure ✅ COMPLETE

**Deliverables:**
- [x] Research and architecture design
- [x] Create `MacOSVersionDetector` class with test override support
- [x] Set up conditional resource loading in theme initialization
- [x] Create `ThemeResources_LiquidGlass.axaml` stub with ThemeDictionaries structure
- [x] Add test override mechanism for development (`SetTestOverride()`)
- [x] Verify version detection works correctly (green text visual confirmation)

**What Was Completed:**
- `MacOSVersionDetector.cs` - Full implementation with caching and override support
- `ThemeResources_LiquidGlass.axaml` - Stub with green colors for visual confirmation
- `MacOsTheme.axaml.cs` - Conditional loading of LiquidGlass resources
- `MacOsThemeWithGlobalStyles.axaml.cs` - Same conditional loading
- Visual confirmation: Green text appears when OS version > 26

**Duration:** Completed

### Phase 2: SampleApp Manual Sub-Theme Switching ✅ COMPLETE

**Goal:** Add manual theme variant selection in SampleApp for development and testing.

**What Was Implemented:**

1. **Created Three MacOS Theme Classes:**
   - `MacOsTheme` - "MacOS - default theme" (OsVersionOverride = null)
   - `MacOsClassicTheme` - "MacOS - classic" (OsVersionOverride = false)
   - `MacOsLiquidGlassTheme` - "MacOS - LiquidGlass" (OsVersionOverride = true)

2. **Updated SetTheme() Method:**
   - Detects MacOS theme variants via `theme is MacOsTheme` check
   - Calls `MacOSVersionDetector.SetTestOverride()` with appropriate value before loading styles
   - Creates fresh theme instances via `CreateMacOsStyles()` to ensure new resources are loaded

3. **Created CreateMacOsStyles() Method:**
   - Generates fresh `DevolutionsMacOsTheme` instances
   - Manually calls `BeginInit()` and `EndInit()` for proper initialization
   - Necessary because theme resource loading happens at initialization time
   - Reuses cached styles for non-MacOS themes

4. **Updated MainWindowViewModel:**
   - Added all three MacOS variants to available themes list
   - Theme switcher now shows 7 options total (was 5)

**Testing Results:**
- ✅ All three MacOS options appear in theme switcher
- ✅ Switching between variants works without errors
- ✅ "MacOS - LiquidGlass" shows green stub colors
- ✅ "MacOS - classic" shows normal colors
- ✅ "MacOS - default theme" uses actual OS version detection
- ✅ Window recreation works properly when switching themes
- ✅ Override mechanism successfully controls resource loading

**Duration:** Completed

### Phase 3: Per-Control LiquidGlass Visual Effects (Upcoming)

**Goal:** Implement and test LiquidGlass-inspired visual effects for individual controls in SampleApp.

**Status:** Infrastructure and preview tools are complete. Ready to begin per-control visual work.

**Approach:**
- Work on one control at a time (Window, Menu, ToolTip, DataGrid, ScrollViewer, etc.)
- Use the wallpaper preview control to test translucency and vibrancy effects
- Document changes and rationale for each control

### Phase 4: Selective Control Template Updates ⏸️ PENDING

**Prerequisites:** Phase 3 must be complete (resource-level styling done)

**Goal:** Enhance specific control templates if resource overrides prove insufficient.

**Approach:**
- Start with resource overrides only (Phase 3)
- Only modify control templates if absolutely necessary
- Create control-specific AXAML files in `ThemeResources_LiquidGlass.axaml` if needed
- Test each control in both light and dark modes
- Compare side-by-side with classic to ensure no regressions

**Duration:** 2-4 days (if needed)

### Phase 5: Documentation & Polish ⏸️ PENDING

**Prerequisites:** All visual work complete

**Deliverables:**
- [ ] Update theme README with OS version requirements
- [ ] Document manual sub-theme switching in SampleApp
- [ ] Update code comments explaining version detection logic
- [ ] Create visual examples/screenshots (classic vs LiquidGlass)
- [ ] Document design decisions and rationale
- [ ] Update CHANGELOG.md

**Duration:** 1 day

## Timeline

**Phase 1 (Foundation):** ✅ Complete (Nov 12)
**Phase 2 (SampleApp Switching):** ✅ Complete (Nov 12)
**Phase 3 (Visual Styling):** ⏸️ Pending - 2-3 days
**Phase 4 (Control Templates):** ⏸️ Pending - 2-4 days (if needed)
**Phase 5 (Documentation):** ⏸️ Pending - 1 day

**Total Remaining:** 5-8 days

## Decisions Made

### ✅ Resolved Decisions

1. **OS Version Detection Approach**
   - **Decision:** Use `OperatingSystem.IsMacOSVersionAtLeast(26)`
   - **Rationale:** Native .NET API, no external process spawning, cacheable
   - **Status:** Implemented and working

2. **Resource Organization**
   - **Decision:** Separate file `ThemeResources_LiquidGlass.axaml` with overrides
   - **Rationale:** Clear separation, follows ThemeDictionaries pattern, minimal duplication
   - **Status:** Implemented with stub

3. **Test/Development Override**
   - **Decision:** `SetTestOverride(bool?)` method in `MacOSVersionDetector`
   - **Rationale:** Allows testing on any OS version, flexible for development
   - **Status:** Implemented and ready for SampleApp integration

4. **Manual Sub-Theme Selection**
   - **Decision:** Add three MacOS variants in SampleApp theme switcher
   - **Rationale:** Essential for development/testing before OS 26 is widely available
   - **Status:** Planned for Phase 2 (current focus)

### ⏸️ Deferred for Later Phases

1. **Specific Visual Values**
   - **Status:** Will determine in Phase 3 after manual switching works
   - **Approach:** Start conservative, iterate based on visual results

2. **Control Template Modifications**
   - **Status:** May not be needed if resource overrides sufficient (Phase 3)
   - **Approach:** Evaluate necessity after resource-level styling complete

3. **Acrylic/Blur-Behind Effects**
   - **Status:** Deferred - would require app-level configuration
   - **Decision:** Start with stylistic translucency only (no true blur-behind)
   - **Rationale:** Simpler, no app changes required, better compatibility

4. **Performance Thresholds**
   - **Status:** Will measure in Phase 4/5 after visual effects implemented
   - **Approach:** Conservative defaults, measure impact, adjust if needed

### 🤔 Outstanding Questions (Low Priority)

1. **Accessibility Integration**
   - Q: Should we respect macOS "Reduce Transparency" system setting?
   - Priority: Medium (can be added in future enhancement)
   - Complexity: Requires system setting monitoring API

2. **User Opt-Out API**
   - Q: Should apps have API to disable Liquid Glass on macOS 26+?
   - Priority: Low (nice-to-have, not in current scope)
   - Note: `SetTestOverride(false)` already provides programmatic override

## Success Criteria

### Phase 1 (Complete) ✅
- [x] OS version detection working correctly
- [x] Conditional resource loading functional
- [x] Visual confirmation (stub) shows theme switching works
- [x] No breaking changes to existing code

### Phase 2 (Complete) ✅
- [x] Three MacOS theme variants available in SampleApp
- [x] Manual switching between classic/default/LiquidGlass works
- [x] Override correctly affects theme resource loading
- [x] Green stub colors confirm LiquidGlass mode active

### Phase 3 (Pending) ⏸️
- [ ] Real Liquid Glass styling replaces green stub
- [ ] Conservative, readable design (no usability degradation)
- [ ] Works in both Light and Dark system themes
- [ ] Side-by-side comparison shows clear visual enhancement

### Phase 4 (Pending) ⏸️
- [ ] Control templates updated (if needed)
- [ ] Priority controls enhanced
- [ ] No regressions in classic theme

### Phase 5 (Pending) ⏸️
- [ ] Documentation complete
- [ ] Screenshots/examples provided
- [ ] CHANGELOG updated

## Overall Project Success Metrics

1. ✅ Zero breaking changes for existing users
2. ✅ macOS <26 users see identical classic theme
3. ⏸️ macOS ≥26 users see polished, modern Liquid Glass-inspired look
4. ⏸️ Readability maintained (no degradation)
5. ⏸️ Performance impact negligible (<5% render time increase)
6. 🔄 SampleApp demonstrates manual theme variant switching
7. ⏸️ Documentation complete

## Dependencies

- .NET 9.0 (already in use)
- Avalonia 11.3.x (already in use)
- macOS 13+ for acrylic support (already required)

## Key Files

### Implementation Files
- `src/Devolutions.AvaloniaTheme.MacOS/Internal/MacOSVersionDetector.cs` - OS detection ✅
- `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml` - LiquidGlass overrides (stub) ✅
- `src/Devolutions.AvaloniaTheme.MacOS/Internal/MacOsTheme.axaml.cs` - Theme loading ✅
- `src/Devolutions.AvaloniaTheme.MacOS/Internal/MacOsThemeWithGlobalStyles.axaml.cs` - Theme loading with globals ✅

### SampleApp Files (Phase 2 - Current Focus)
- `samples/SampleApp/App.axaml.cs` - Theme switching logic, needs new theme classes
- `samples/SampleApp/ViewModels/MainWindowViewModel.cs` - Available themes list, needs updates
- `samples/SampleApp/MainWindow.axaml.cs` - Theme selection event handling

## References

**External:**
- [Apple Liquid Glass WWDC Video](https://developer.apple.com/videos/play/wwdc2025/219/)
- [Liquid Glass Wikipedia](https://en.wikipedia.org/wiki/Liquid_Glass)
- [OperatingSystem.IsMacOSVersionAtLeast Docs](https://learn.microsoft.com/en-us/dotnet/api/system.operatingsystem.ismacosversionatleast?view=net-9.0)
- [Avalonia Resources Documentation](https://docs.avaloniaui.net/docs/guides/styles-and-resources/resources)

**Internal:**
- Research findings in conversation history (2025-11-03)
- Current theme README: `src/Devolutions.AvaloniaTheme.MacOS/README.md`
- Project instructions: `.claude/CLAUDE.md`

## Change Log

- **2025-11-12 (evening):** Phase 3 initial implementation - Liquid Glass styling
  - Replaced green color stub with actual shadow and opacity enhancements
  - Enhanced PopupShadow, ToolTipBorderShadow, control borders, text boxes, toggle switches
  - Increased scrollbar visibility (opacity +7-11%)
  - More pronounced layout backgrounds (opacity +3-6%)
  - Stronger effects in dark mode for better depth
  - Detailed implementation notes in `.claude/temp/liquidglass-phase3-notes.md`
  - **Status:** Awaiting user testing after reboot
  - **Feedback:** Effects may be too subtle, need side-by-side comparison
- **2025-11-12 (late afternoon):** SampleApp theme name updates for clarity
  - Renamed "MacOS - default theme" to "MacOS (automatic)" for better UX
  - Updated all `ConverterParameter=MacOS` to `ConverterParameter=MacOS - classic` throughout demo pages
  - Updated `ApplicableTo="MacOS"` to `ApplicableTo="MacOS - classic"` in MainWindow.axaml control headers
  - Modified SampleItemHeader to hide status indicator (🟢/🔴) when theme is "MacOS (automatic)"
  - Changed default ApplicableTo from "MacOS" to "" (empty) in SampleItemHeader (unrelated! should have been "" all 
    along)
  - Identified need for IsOneOfConverter (see upcoming project plan) to simplify multi-variant visibility
- **2025-11-12 (afternoon):** Phase 2 completed - Manual sub-theme switching working
  - Implemented three MacOS theme variant classes with OsVersionOverride property
  - Updated SetTheme() to call MacOSVersionDetector.SetTestOverride() before loading styles
  - Created CreateMacOsStyles() method to generate fresh theme instances
  - Updated MainWindowViewModel to show all three MacOS variants
  - Testing confirmed: switching works, green stub appears for LiquidGlass, normal colors for Classic
  - Ready to proceed to Phase 3 (actual visual styling)
- **2025-11-12 (morning):** Project plan rewritten to reflect completed Phase 1 and clarify Phase 2 requirements
  - Removed incorrect checkboxes from previous agent's confused state
  - Added clear current status summary
  - Reorganized phases to match actual progress
  - Added manual sub-theme switching as explicit Phase 2 goal
  - Deferred visual styling work to Phase 3 (after switching works)
  - Moved MacOSVersionDetector from Converters/ to Internal/ namespace
- **2025-11-03:** Initial project plan created

## Notes

- All infrastructure for LiquidGlass theme development is complete and working in SampleApp.
- Manual theme switching and OS auto-detection are fully implemented.
- The SampleApp now includes a top-right wallpaper preview control (checkbox) for testing translucency effects.
- The next step is to work on individual controls, one at a time, to implement LiquidGlass-inspired visual effects.
- Documentation and planning will be updated as each control is completed.
