# Popup Backdrop Blur Investigation

> **Branch:** `agents/liquid-glass-transparency-blur`  (worktree based on `LG/avalonia-12-transparency-blur`)
> **Status:** Cross-platform handoff — current macOS attempt does not give a satisfactory result; needs validation on Windows.

## Goal

Add a **frosted-glass / backdrop-blur effect to popup backgrounds** (ComboBox dropdowns,
ContextMenu, MenuFlyout, etc.) in the `Devolutions.AvaloniaTheme.MacOS` **LiquidGlass** accent.
The intent is that when a popup is open, the controls underneath shine through
*softened by a Gaussian blur*, instead of being either fully opaque or sharply visible
through semi-transparency.

In production, an opacity of ~0.9 on `PopupBackgroundBrush` was used to hide the distraction
of unblurred underlying content — which almost defeated the purpose of translucency.
A real backdrop blur is the correct visual.

## What's wired up on this branch

### Files added
- `src/Devolutions.AvaloniaTheme.MacOS/Controls/PopupRoot_LiquidGlass.axaml`
  — `Styles` file targeting `PopupRoot`. Sets
  `TransparencyLevelHint = "AcrylicBlur, Blur, Transparent"`,
  `Background = Transparent`,
  `WindowManagerAddShadowHint = True`.

### Files modified
- `src/Devolutions.AvaloniaTheme.MacOS/Internal/MacOsTheme.axaml.cs`
  — Conditionally loads `PopupRoot_LiquidGlass.axaml` alongside `ThemeResources_LiquidGlass.axaml`
  when `MacOSVersionDetector.IsLiquidGlassSupported()` returns true.
- `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources.axaml`
  — Added new base resource `ComboBoxPopupHorizontalOffset = -13` so `ComboBox.axaml`
  can use a `DynamicResource` instead of a hardcoded value.
- `src/Devolutions.AvaloniaTheme.MacOS/Controls/ComboBox.axaml`
  — `HorizontalOffset` of `PART_Popup` now uses `{DynamicResource ComboBoxPopupHorizontalOffset}`
  (was hardcoded `-13`).
- `src/Devolutions.AvaloniaTheme.MacOS/Accents/ThemeResources_LiquidGlass.axaml`
  — LiquidGlass overrides for the popup geometry:
    - `PopupMargin = "0"` (no shadow space — the OS draws the shadow)
    - `PopupSideMarginWidth = 0`
    - `InitialFirstItemDistance = 5`, `PopupTrimHeight = 10`
    - `ComboBoxPopupHorizontalOffset = 0`, `SubMenuPopupHorizontalOffset = -2`,
      `SubMenuPopupVerticalOffset = -7`, `MenuPopupHorizontalOffset = 0`
    - `PopupShadow → NoShadow` in both light and dark theme dicts
- `.vscode/tasks.json` — added `Build SampleApp` / `Run SampleApp` tasks for the session.

## Approach summary

`PopupRoot` is a separate **native OS sub-window** (NSPanel on macOS, top-level HWND on
Windows). It inherits from `WindowBase` and supports `TransparencyLevelHint`. Setting
`AcrylicBlur` asks the OS to put a backdrop-blur effect surface behind the popup window.

Because the OS blur surface covers the full rectangular native window, we zeroed out
`PopupMargin` so there is no shadow-accommodation bleed, and we ask the OS to draw the
drop shadow natively (`WindowManagerAddShadowHint = True`). The popup's
`PopupBackgroundBrush` (a semi-transparent tint) sits on top of the OS blur.

## Result on macOS

**Does not look right.** The popup renders as a fully opaque white box, with no rounded
corners and no shadow. Two known macOS-specific reasons:

1. `NSVisualEffectView` (the OS surface that Avalonia adds for `AcrylicBlur`) is an
   **opaque subview that fills the entire rectangular native window**. Avalonia's
   `CornerRadius` clipping happens *inside* the popup content — it doesn't clip the
   underlying `NSVisualEffectView`. Result: square corners.
2. In macOS light mode the default material renders near-white, so the popup looks opaque.
   The semi-transparent `PopupBackgroundBrush` on top of it further whitens the result.

Avalonia's macOS backend does not currently expose `NSVisualEffectView.material` or apply
a `CAShapeLayer` mask matching the popup's corner radius.

See **`avalonia-macos-popup-blur-feature-request.md`** in this folder for the full
feature request to send to the Avalonia team.

## What needs to be validated on Windows

When you run this branch on a Windows 11 VM:

1. **Expected good outcome:** The ComboBox popup (and other popups using
   `PopupBackgroundBrush`) should appear as a **frosted-glass rounded rectangle** sitting
   over the application's content, with a soft drop shadow. The content behind the popup
   should be visibly blurred through the popup background.
2. **What to check:**
   - Are the corners of the blur layer **actually rounded** to match `PopupCornerRadius`
     (Radius12, from LiquidGlass)? On Windows 11 + DWM, per-pixel alpha should make
     transparent corners actually transparent in the acrylic layer.
   - Is the **tint colour** appropriate — does `PopupBackgroundBrush` (50% white in light
     mode, 50% near-black in dark) blend pleasantly with the acrylic?
   - Does the **drop shadow** look right (the BoxShadow has been removed in favour of
     `WindowManagerAddShadowHint = True` — on Windows this maps to a DWM shadow)?
   - Are the popup **dimensions / offsets** correct now that all margins are zero?
     Specifically: ComboBox popup should align flush with the textbox edge;
     menu submenus should still hang off correctly.

### If it works on Windows but not macOS

That's strong evidence that the AXAML / theme code is *correct* and the gap is in
Avalonia's macOS platform backend. The feature request document
(`avalonia-macos-popup-blur-feature-request.md`) is ready to send.

### If it doesn't work on Windows either

Possible reasons and things to try:
- On Windows 10 (not 11), `AcrylicBlur` falls back to a software acrylic which may not look
  great. Try `Mica` or `MicaAlt` instead of `AcrylicBlur` if Windows 11 22H2+.
- The transparency hint may need to be set differently — see Avalonia issue
  [#18620](https://github.com/AvaloniaUI/Avalonia/issues/18620) which discussed
  `TransparencyLevelHint` on `PopupRoot`.
- Verify the styles actually apply: in DevTools, inspect the open popup, check the
  `PopupRoot` element and confirm `TransparencyLevelHint` is `AcrylicBlur`.
- `Background = Transparent` may need to also be set on the immediate `Border` child of
  the popup template, not just `PopupRoot`.

## How to revert to "transparent only" (no blur)

If you need to fall back to the original behaviour (semi-transparent popup without blur,
which works on macOS as expected), change `PopupRoot_LiquidGlass.axaml`:

```xml
<Style Selector="PopupRoot">
  <Setter Property="TransparencyLevelHint" Value="Transparent" />
  <Setter Property="Background" Value="Transparent" />
</Style>
```

…and restore the popup-margin geometry in `ThemeResources_LiquidGlass.axaml` to:
```xml
<Thickness x:Key="PopupMargin">12 4 12 29</Thickness>
<x:Int32 x:Key="PopupSideMarginWidth">12</x:Int32>
<x:Int32 x:Key="InitialFirstItemDistance">9</x:Int32>
<x:Int32 x:Key="PopupTrimHeight">33</x:Int32>
<x:Double x:Key="ComboBoxPopupHorizontalOffset">-13</x:Double>
<x:Double x:Key="SubMenuPopupVerticalOffset">-11</x:Double>
```
…and remove the two `PopupShadow → NoShadow` overrides.

## Reference: Avalonia 12 transparency options explored

| Option | Where used | Verdict for popups |
|---|---|---|
| `BlurEffect` | `Effect` property on a control | Only blurs the element's *own* content. Useless as a backdrop blur. |
| `ExperimentalAcrylicBorder` (`BackgroundSource="Digger"`) | Inside the popup template | Samples the *same* Avalonia render tree. PopupRoot is a separate native window, so Digger sees nothing → falls back to solid colour. Doesn't cross native window boundaries. |
| `TransparencyLevelHint = "Transparent"` on `PopupRoot` | This branch (fallback) | Works on macOS: the native compositor shows the main window behind the popup. Semi-transparent only, no blur. |
| `TransparencyLevelHint = "AcrylicBlur"` on `PopupRoot` | **Current state of this branch** | Real OS-level backdrop blur. On Windows 11 expected to look correct. On macOS, broken (see above). |
| `WindowManagerAddShadowHint = True` on `PopupRoot` | **Current state** | Asks the OS to draw a native drop shadow. Replaces Avalonia BoxShadow (which needs margin space). |

## Build / run

```sh
dotnet build avalonia-extensions.sln -c Debug
dotnet run --project samples/SampleApp/SampleApp.csproj
```

The `Run SampleApp` task in `.vscode/tasks.json` does the same.

## Where popup blur should eventually be applied

`PopupBackgroundBrush` is the shared resource used by all popup-style controls in this theme:
- `ComboBox` (the main target so far)
- `ContextMenu` / `MenuFlyout` / `MenuItem` submenus
- `EditableComboBox`, `MultiComboBox`, `AutoCompleteBox`
- `CalendarDatePicker`

The `PopupRoot` style change is *global to the theme* — it affects every popup. Each
individual control template still needs its `PopupMargin` / `HorizontalOffset` / shadow
to be consistent with zero margins. Currently only `ComboBox.axaml` has been updated to
use `DynamicResource` for the horizontal offset. Other controls may have hardcoded
offsets that need similar treatment:
- `MultiComboBox.axaml` — `HorizontalOffset="0"` (probably fine)
- `CalendarDatePicker.axaml` — `HorizontalOffset="-10"` (hardcoded — would need a resource)
- Menu / submenu offsets — driven by `SubMenuPopupHorizontalOffset`,
  `MenuPopupHorizontalOffset` etc., already overridden in LiquidGlass.
