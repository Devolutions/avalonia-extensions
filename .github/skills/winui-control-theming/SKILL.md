---
name: winui-control-theming
description: "Create or update Devolutions.AvaloniaTheme.WinUI control templates, ControlThemes, and ThemeResources. Use when adding a new WinUI-styled control, porting Fluent overrides from another app, choosing WinUI brush/resource names, or deciding whether a resource belongs in ThemeResources.axaml or a control file."
---

# WinUI Control Theming

Use this skill when working on `src/Devolutions.AvaloniaTheme.WinUI/` and the goal is to add or refine WinUI-styled control themes while staying consistent with the resource strategy already used for this theme.

## Core Principle

Prefer **official WinUI resource names** over made-up local names whenever they can be identified from WinUI source or docs.

This theme should follow the WinUI model as closely as practical:

- **Shared WinUI semantic tokens** go in `Accents/ThemeResources.axaml`
- **Control-specific WinUI resources** go in the relevant file under `Controls/`
- **New local resource names** should be introduced only when no real WinUI equivalent exists

## Why

Avalonia's Fluent theme is a Fluent-looking base theme, but its resource surface does not always match the newer WinUI token vocabulary. For this theme, the goal is not just to make controls "look good"; it is to make them look WinUI-like **using WinUI concepts and names where possible**.

That keeps future control work consistent:

- later controls can reuse the same token set
- resource names stay recognizable to people familiar with WinUI
- ad-hoc local naming does not spread across the theme

## Required Workflow

When adding or updating a WinUI control theme:

1. **Start from the WinUI control**
   - Find the WinUI default template and the theme resources it uses.
   - Extract the actual resource names referenced by the template.

2. **Separate shared tokens from control-specific resources**
   - Examples of shared tokens: `TextFillColorPrimaryBrush`, `AccentFillColorDefaultBrush`, `ControlFillColorDefaultBrush`
   - Examples of control resources: `ButtonBackground`, `ButtonForegroundPointerOver`, `ButtonBorderBrushPressed`

3. **Compare against Avalonia Fluent**
   - Check the existing Avalonia Fluent control template and resource model.
   - Decide whether the control can be adapted with targeted overrides or needs a fuller template port.

4. **Port only what is needed**
   - Do not dump the full WinUI token set into `ThemeResources.axaml`.
   - Add only the resources that are actually referenced by the new or updated control theme, unless there is a clear near-term reason to add a small shared set together.

5. **Place resources in the right layer**
   - Put reusable cross-control WinUI tokens in `src/Devolutions.AvaloniaTheme.WinUI/Accents/ThemeResources.axaml`
   - Put control-only resources in the relevant `src/Devolutions.AvaloniaTheme.WinUI/Controls/*.axaml`
     unless they alias theme-dictionary-scoped tokens (see "Learnings" below)
   - Keep `SampleAppBackground` and similar development-only resources clearly marked

6. **Prefer ControlTheme isolation**
   - Prefer `ControlTheme`-based overrides over broad global styles
   - Keep `GlobalStyles.axaml` empty or minimal unless a true bleed-out/global case is required

7. **Preserve fallbacks**
   - Keep Fluent as the fallback for anything not explicitly overridden
   - Reuse Avalonia/Fluent built-in resources when they already represent the right concept

## Inspecting the Target in WinUI 3 Gallery

On Windows, use the installed WinUI 3 Gallery as the visual reference instead of relying only on
screenshots supplied by the user. The Gallery is a native WinUI app, not an Avalonia app, so
Avalonia DevTools MCP cannot attach to it. Use Windows UI Automation to find controls and real
pointer input to inspect interaction states.

### Locate controls without fixed coordinates

1. Identify the Gallery process explicitly (normally `WinUIGallery.exe`) and keep its PID. Do not
   assume it is the only Gallery window or close an instance you did not start.
2. Get the window's root `AutomationElement` from that PID.
3. Search descendants by stable automation properties such as `Name`, `AutomationId`, and
   `ControlType`. For example, the CheckBox page exposes controls named `Two-state` and
   `Three-state`.
4. Read the selected element's `BoundingRectangle` and derive the input point from that rectangle.
   Do not hard-code screen coordinates; window position, display scaling, and Gallery layout vary.

Prefer UI Automation patterns (`InvokePattern`, `SelectionItemPattern`, `TogglePattern`, and so on)
for navigation and discrete state changes. Reacquire elements after navigation because the page may
replace its automation subtree.

### Capture hover and held-press states

UI Automation's invoke/toggle operations do not expose a sustained pressed state. Use ordinary
pointer input for visual state inspection:

1. Move the pointer outside the control and capture the resting state.
2. Move it inside the target's `BoundingRectangle`, wait for the transition to settle, and capture
   `PointerOver`.
3. Send left-button down without releasing, wait briefly, and capture `Pressed`.
4. To avoid activating/toggling the control, move the pointer outside its bounds before sending
   left-button up. If activation is intentional, release over the control and restore its state
   afterward through the relevant UI Automation pattern.

This workflow was verified against the installed Gallery: moving the system pointer triggered the
hover visual, and holding the left button exposed the pressed visual without user assistance.

### Screenshot and measurement pitfalls

- WinUI uses animations. A capture taken immediately after changing state can show the previous or
  an intermediate frame; wait and confirm the state has settled.
- On mixed-DPI or multi-monitor systems, UI Automation bounds, pointer coordinates, and screen
  capture APIs may use different coordinate spaces. Make the automation process per-monitor
  DPI-aware where possible, and verify that the pointer/crop actually lands on the named element.
- `PrintWindow` can intermittently return incomplete DirectComposition content. Prefer a real
  screen capture for final comparisons; if using `PrintWindow`, check the result and retry rather
  than accepting a blank capture.
- Compare at the same effective display scale and measure composited pixels before changing theme
  tokens. Keep captures until animations have settled.

## Naming Rules

Use these rules in order:

1. **Use the real WinUI name** if it exists and is identifiable
2. **Reuse an existing WinUI-named resource already present in this theme** if it matches the concept
3. **Use a control-scoped WinUI-style name** if the concept is specific to one control
4. **Invent a local name only as a last resort**

Avoid generic names that hide intent, such as:

- `PrimaryBrush`
- `HoverBrush`
- `WinUiButtonBrush`
- `CustomAccentBrush`

Prefer names that encode the WinUI concept, such as:

- `TextFillColorPrimaryBrush`
- `ButtonBackgroundPointerOver`
- `ControlStrokeColorSecondaryBrush`

## Porting From Other Avalonia Apps

When the source is another Avalonia app with Fluent overrides, such as UniGetUI:

1. Find the exact override location
2. Identify whether it is:
   - a selector style
   - a local resource token
   - a template replacement
3. Preserve the **visual intent** and **resource naming strategy**
4. Convert selector-based app styles into isolated `ControlTheme` definitions where appropriate for this repository
5. Audit the imported resources afterward and remove any that are not actually referenced

Do not assume the app's entire resource file belongs in this repository. Import the minimum coherent subset.

## Windows 11 Mica Overlay

The theme supports two variants: classic (solid surfaces, the default `Accents/ThemeResources.axaml`)
and Windows 11 Mica (translucent surfaces). The Mica variant lives in
`Accents/ThemeResources.Windows11.axaml` and is merged conditionally at runtime by
`DevolutionsWinUiTheme` when `Windows11MicaDetector.IsMicaSupported()` is true.

Rules for the Mica overlay:

- **Grow it one control at a time.** When a new or updated control theme introduces a surface
  brush that should become translucent under Mica (card/panel/page/flyout/header backgrounds,
  borders), add that brush's Mica variant to `ThemeResources.Windows11.axaml` in the SAME change.
- **Only override keys this theme actually defines.** Every key in the Mica overlay must have a
  matching solid key in `Accents/ThemeResources.axaml` that a control theme consumes. Never add a
  Mica key for a control that has no WinUI theme yet.
- **Never import app-specific keys.** Keys like `PackageListBackground`, `AppDialog*`, or
  `MicaPageBackground` from source apps are layout concerns of those apps, not reusable theme
  tokens. Leave them out.
- **Keep both variants in sync.** A Mica key without a base key (or vice versa) is a smell.

### Critical: overlay placement and resource priority

The base `ThemeResources.axaml` is pulled into `ThemeRoot.axaml` via `MergeResourceInclude`, which
**flattens** it into the theme's own `ThemeDictionaries`. An owner dictionary's own
`ThemeDictionaries` take priority over anything in its `MergedDictionaries`, so an overlay appended
to the same dictionary as the base will NOT win. The Mica overlay must therefore be merged in a
Styles level ABOVE the base theme — this is why it lives in `DevolutionsWinUiTheme.EndInit()`
(above `WinUITheme`/`WinUIThemeWithGlobalStyles`), not inside `ThemeRoot.axaml`.

`tests/Devolutions.AvaloniaControls.Tests/WinUiMicaProbe.cs` (unit tests, not the visual tests)
guards this behaviour for both the `GlobalStyles=false` (SampleApp) and `GlobalStyles=true` (simple
consumer) paths. Keep it passing:
`dotnet test tests/Devolutions.AvaloniaControls.Tests --filter "FullyQualifiedName~WinUiMicaProbe"`.

### Testing the variants

`Windows11MicaDetector.SetTestOverride(bool?)` forces the variant on/off regardless of OS, mirroring
`MacOSVersionDetector`. The SampleApp exposes three dropdown entries — WinUI (automatic), WinUI
classic, WinUI (Win11 Mica) — so the translucent brushes can be previewed on macOS/Linux. The actual
native Mica backdrop is app-layer: `MainWindow.ApplyWindowsMicaBackdrop()` requests
`TransparencyLevelHint = WindowTransparencyLevel.Mica` when `App.IsWinUiMicaTheme` is true. Avalonia
drives the DWM system backdrop from that hint, so it lights up automatically on real Windows 11 (no
extra UI toggle) and is an inert no-op on Windows 10 / non-Windows. `MainWindow.OnPropertyChanged()`
makes the window `Background` transparent only once `ActualTransparencyLevel` confirms Mica was
granted. Forced previews and unsupported platforms deliberately keep the opaque fallback. On macOS
the visual approximation comes from the wallpaper preview layer, not a real compositor backdrop.

### What "classic" means (and where a style belongs)

Classic is **WinUI 3 on a solid backdrop** (Windows 10, Windows Server, or Win11 with transparency
effects off / RDP / battery saver) — not a "Windows 10 native" look. Real WinUI 3 ships the *same*
control resources on Win10 and Win11; control fills are translucent (alpha) on both, layered over
whatever surface they sit on. Only the window backdrop changes: solid on classic, Mica on Win11.
So classic and Mica are expected to differ only on backdrop-like surfaces (window/page
backgrounds, cards/layers, possibly acrylic flyouts/menus) — most controls render identically.

When porting a look (especially from a Win11 Gallery screenshot), **trace it to its source before
deciding where it goes**:

- The value comes from a `*_themeresources.xaml` / `Common_themeresources_any.xaml` entry → it is
  the same on Win10 and Win11 → base `Accents/ThemeResources.axaml`.
- The look only exists because the Mica backdrop shows through a surface → Mica overlay
  (`ThemeResources.Windows11.axaml`).

Don't guess: a Mica-only look placed in the base makes both variants wrong in the same way, and no
test can catch that (see `↔️` below).

### Catalog status: `WinUIClassic` / `WinUIMica` columns and `↔️`

`page-catalog.jsonc` tracks the two variants as separate columns (like `MacClassic`/`LiquidGlass`).
Lifecycle for a control:

1. **While being styled:** both columns carry the same status. Agents set `🚧` after a first pass
   and stop there; any upgrade (`🚧` → `⚠️` → `✅`) is initiated by the user.
2. **Once the user considers Mica done**, they ask for a classic review: check the WinUI
   source/docs for anything on that page that should look different on a solid backdrop.
3. **Nothing should differ** → classic is set to `↔️` (only valid in `WinUIClassic`). It then
   inherits Mica's status, stores no baselines of its own, and the visual tests assert the classic
   render is pixel-identical to the Mica render (Light + Dark). If a later overlay change makes them
   diverge, that test fails — at which point classic gets its own status and baselines.
4. **Something should differ** → classic gets its own styling work, status, and baselines.

`↔️` is always a deliberate decision after that review — never a default, and never set by an
agent on its own.

Tip: on a Win11 machine, turning off Settings → Personalization → Colors → "Transparency effects"
makes WinUI apps (including the Gallery) fall back to a solid backdrop — a quick way to see the
classic look side by side (verify per surface; not guaranteed for every one).

## Resource Audit Checklist

After adding or changing a control:

- List all resources referenced by the new control theme
- Confirm each new resource is actually used
- Confirm each shared resource belongs in `ThemeResources.axaml` rather than the control file
- Remove speculative tokens that were added "for later"
- Keep comments short and factual, especially around development-only resources

## Learnings

- A root-level `<StaticResource x:Key="ButtonBackground" ResourceKey="ControlFillColorDefaultBrush"/>`
  in `Controls/Button.axaml` cannot resolve a token in the parent theme's Light/Dark
  `ThemeDictionaries` at runtime (`KeyNotFoundException`, not a build error). Define each
  control-specific alias **inside the corresponding Light and Dark dictionaries** in
  `Accents/ThemeResources.axaml` alongside its semantic token, as Button does; consume it
  with `{DynamicResource ButtonBackground}` from the control theme. Do not treat the
  general "control resources belong in Controls/" rule as overriding resource scope.
- WinUI's Light and Dark dictionaries are **not** always symmetric. Example: Light flips both
  `ControlElevationBorderBrush` and `AccentControlElevationBorderBrush` (strong edge at the bottom,
  a shadow), while Dark flips only the accent one (strong edge at the top, a highlight). Port each
  dictionary from its own source; never "fix" a difference for consistency without checking.
- When a colour looks off, measure the real composited pixels before touching opacities. WinUI fills
  are translucent, so a wrong **surface underneath** looks like a wrong token. When real Mica is
  granted, the window background must be transparent (`MainWindow.OnPropertyChanged()` handles this),
  or everything composites over the default opaque black `SystemRegionBrush`. DevTools screenshots
  don't include the DWM backdrop, so capture the screen instead.
- The installed WinUI 3 Gallery can be inspected autonomously with Windows UI Automation plus real
  pointer input. Use element names and bounds, not fixed coordinates, and hold mouse-down to capture
  the pressed state; move outside before release when the control must not be activated.

## Repository-Specific Notes

- The WinUI theme lives in `src/Devolutions.AvaloniaTheme.WinUI/`
- `ThemeRoot.axaml` should continue loading Fluent as fallback
- `Controls/_index.axaml` should include each committed control resource file
- `Accents/ThemeResources.axaml` should stay intentionally small and grow only as new control themes require it
- `Accents/ThemeResources.Windows11.axaml` is the Mica overlay; grow it in lockstep with the base file, never bulk-import
- `GlobalStyles.axaml` should remain empty unless a genuine global styling requirement appears

## Good Outcome

A good change in this theme does all of the following:

- makes the control more WinUI-like
- uses WinUI resource names where possible
- keeps shared and control-local resources separated correctly
- avoids unnecessary token bloat
- preserves Fluent fallback behavior
