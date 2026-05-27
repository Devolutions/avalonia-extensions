# Feature Request: macOS Backdrop Blur for `PopupRoot` (and other `WindowBase` sub-windows)

> **Target:** Avalonia 12.x macOS platform backend
> **Audience:** Avalonia core team
> **Context:** This document is intended as a starting point for a feature
> request / discussion with the Avalonia team. We've identified a gap in the
> macOS backend that prevents a frosted-glass popup design that works well on
> Windows.

## Summary

`TransparencyLevelHint = "AcrylicBlur"` is documented and supported on `WindowBase`
(including `PopupRoot`). On **Windows 11** this gives a usable result for popups:
the DWM Acrylic layer respects the window's per-pixel alpha, so a control's
`CornerRadius` clipping produces a **rounded frosted popup with appropriate tint**.

On **macOS**, the current backend adds an `NSVisualEffectView` to the popup's
native window, but:

1. The `NSVisualEffectView` is a **rectangular opaque subview** with no corner
   masking. There is no way (from AXAML or app code) to make it follow the
   popup's `CornerRadius` — the popup ends up with a hard rectangular blurred
   background that does not match the rounded popup content on top.
2. The `NSVisualEffectMaterial` is **hardcoded** (or defaulted) in the backend.
   The default material renders near-white in macOS light mode, making popups
   look effectively opaque. App authors can't request a different material
   (e.g. `.popover`, `.menu`, `.hudWindow`) without going outside Avalonia.

The result is that a design that works on Windows with `AcrylicBlur` is not
achievable on macOS without writing native code.

## Concrete use case

We are building a "Liquid Glass" theme for macOS 26 (Tahoe) that styles popups
(ComboBox dropdowns, ContextMenu, MenuFlyout, etc.) as frosted-glass rounded
rectangles. The intended look is the standard macOS popover / menu material:
softly blurred content behind the popup, tinted, with rounded corners and a
subtle drop shadow.

The macOS HIG / system popovers achieve this with:
- `NSVisualEffectMaterial.popover` (or `.menu`)
- `NSVisualEffectView.maskImage` or a `CAShapeLayer` mask matching the corner radius
- `NSWindow.hasShadow = YES`

These are routine native APIs; the gap is that Avalonia's macOS backend does
not surface them.

## What works today and what doesn't

| Scenario | Windows 11 | macOS |
|---|---|---|
| `TransparencyLevelHint = "Transparent"` on `PopupRoot` | ✅ | ✅ — main window composites behind the popup |
| `TransparencyLevelHint = "AcrylicBlur"` on `PopupRoot` | ✅ rounded, tinted | ❌ rectangular, opaque-white in light mode |
| Per-pixel alpha through corner radius | ✅ via DWM | ❌ NSVisualEffectView ignores corner alpha |
| Choosing the material | ⚠️ `Mica` / `MicaAlt` exposed via hint | ❌ no way to pick `.popover` / `.menu` / etc. |
| Native drop shadow on popup | ✅ via `WindowManagerAddShadowHint` | ⚠️ works but the rectangular blur defeats it visually |

## Proposed changes to Avalonia

### 1. Mask `NSVisualEffectView` to the popup's `CornerRadius`

The `NSVisualEffectView` should be masked so its visible region matches the
rounded-rect shape of the popup. Two implementation options:

- **`maskImage`**: build an `NSImage` with the corner radius and assign it as
  `effectView.maskImage`. This is the simplest path and was Apple's
  recommendation for older macOS versions.
- **`CAShapeLayer` mask** on the layer-backed `NSVisualEffectView`. More
  flexible (e.g. asymmetric corner radii) but requires `wantsLayer = true`
  plumbing.

The popup's corner radius is known to Avalonia at popup-show time
(`Popup.CornerRadius` or the `PopupRoot` template `Border.CornerRadius`).
On resize the mask must be updated.

### 2. Expose `NSVisualEffectMaterial` as an AXAML-settable property

Currently `TransparencyLevelHint` is a flags enum mapped to a single material
internally. We propose either:

- A new `TransparencyMaterialHint` (or `MacOSTransparencyMaterial`) property on
  `WindowBase` that takes an enum mirroring `NSVisualEffectMaterial`:
  `Popover`, `Menu`, `HudWindow`, `Sidebar`, `Titlebar`, `UnderWindowBackground`,
  `WindowBackground`, etc. Default to a sensible value per window type
  (`Menu` for `PopupRoot`, `WindowBackground` for top-level windows).
- Alternatively, extend the `WindowTransparencyLevel` enum with macOS-specific
  values, though this conflates platform semantics.

The current Windows-side `Mica` / `MicaAlt` levels are precedent for
platform-specific values in the hint.

### 3. Default `PopupRoot` to a popup-appropriate material on macOS

Even without API surface changes, the default material chosen for `PopupRoot`
should be `.popover` or `.menu` rather than the current default. This alone
would substantially close the gap with the Windows behaviour.

### 4. Documentation

The interaction between `TransparencyLevelHint`, `WindowManagerAddShadowHint`,
and `PopupRoot` is currently underspecified. A doc page that shows the
recommended pattern for *"frosted-glass popup with rounded corners + OS shadow"*
on both Windows and macOS would help users discover the right approach.

## Pointers

- Avalonia issue [#18620](https://github.com/AvaloniaUI/Avalonia/issues/18620) —
  confirmed that `TransparencyLevelHint` *does* apply to `PopupRoot`.
- macOS source: `src/Avalonia.Native/PopupImpl.cs` and the matching
  Objective-C++ in `native/Avalonia.Native/src/OSX/`.
- Apple docs:
  [`NSVisualEffectView`](https://developer.apple.com/documentation/appkit/nsvisualeffectview),
  [`NSVisualEffectMaterial`](https://developer.apple.com/documentation/appkit/nsvisualeffectmaterial),
  [`NSVisualEffectView.maskImage`](https://developer.apple.com/documentation/appkit/nsvisualeffectview/maskimage).

## Repro / evidence

A reproducible test case is in the branch
`agents/liquid-glass-transparency-blur` of
[`Devolutions/avalonia-extensions`](https://github.com/Devolutions/avalonia-extensions).
The `Devolutions.AvaloniaTheme.MacOS` LiquidGlass accent sets
`TransparencyLevelHint = "AcrylicBlur"` on `PopupRoot`:

- On Windows 11: rounded frosted-glass popup as intended.
- On macOS 26 (Tahoe): opaque white rectangular box; rounded corners and the
  popup's drop shadow are not visible.

The reproduction uses `SampleApp` → any `ComboBox` dropdown.

## Workarounds we've considered

- **Software acrylic inside the popup** (`ExperimentalAcrylicBorder` with
  `BackgroundSource="Digger"`): doesn't cross the native popup-window boundary,
  so it samples nothing and falls back to a solid colour.
- **Native interop via `IPlatformHandle`**: technically possible (get
  `NSWindow*`, add a masked `NSVisualEffectView` ourselves) but requires
  `objc_msgSend` P/Invoke, custom mask updates on resize, and breaks the
  abstraction Avalonia is meant to provide. We'd rather not.
- **Reduce `PopupBackgroundBrush` opacity to ~0.9**: hides the unblurred
  bleed-through but defeats the design intent.

We'd really appreciate a first-class solution in Avalonia.
