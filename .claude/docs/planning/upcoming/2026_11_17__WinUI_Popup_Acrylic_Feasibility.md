# WinUI popup acrylic feasibility

## Goal and status

Upcoming, bounded feasibility investigation; production acrylic is not implemented.
Determine whether ComboBox popups and MenuFlyouts can retain subtle background
colours while blurring underlying text, without a rectangular halo or broken shadows.

This follows [PR #684](https://github.com/Devolutions/avalonia-extensions/pull/684).
The user accepted the opaque ComboBox fallback and soft shadow for now, and requested
warning status for both ComboBox and MenuFlyout in both WinUI variants. Do not block
that PR on acrylic or treat the warning status as approval of a native material match.

A shared helper would package a proven solution; it does not itself solve clipping.
First establish feasibility, then decide whether implementation is worth pursuing.

## Findings already verified

### WinUI reference

- The native ComboBox uses `AcrylicInAppFillColorDefaultBrush`, a real acrylic
  material rather than a translucent solid colour.
- Its Light material uses tint `#FCFCFC`, `TintOpacity=0`,
  `TintLuminosityOpacity=0.85`, and fallback `#F9F9F9`.
- Dark uses tint `#2C2C2C`, `TintOpacity=0.15`,
  `TintLuminosityOpacity=0.96`, and fallback `#2C2C2C`.
- Acrylic combines blur, tint/luminosity, and noise, independently of the main
  window's Mica backdrop. Copying luminosity opacity into `SolidColorBrush.Opacity`
  exposes readable controls and is not an acceptable replacement.

### Avalonia native-popup experiment

On Windows with the repository's Avalonia 12.1.2, a temporary native ComboBox
prototype obtained the opened popup's `PopupRoot` through
`TopLevel.GetTopLevel(popup.Child)` and requested
`TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur]`.
With the root background transparent and a translucent test fill on the surface:

- `ActualTransparencyLevel` reported `AcrylicBlur`.
- Parent-window text was blurred, while red/blue backgrounds remained visible.
  The effect was not merely wallpaper showing through.
- Light, Dark, editable, and above-opening editable cases obtained native blur.
  The experimental tint was illustrative, not calibrated WinUI material styling.
- Blur covered the whole popup root, including the transparent shadow gutter,
  producing an unacceptable rectangular blurred halo outside the visible surface.
- A border's `CornerRadius` did not clip the native backdrop. The global
  `Win32PlatformOptions.WinUICompositionBackdropCornerRadius=8` option rounded
  the outer root but did not inset blur to the surface or solve editable joins.

The Win32 implementation requests a host backdrop brush and a composition acrylic
effect. This establishes that native blur is possible, not that a production
solution is available through Avalonia's public APIs.

### Approaches that do not solve the problem

- `ExperimentalAcrylicBorder` alone adds material/tint/noise; its Skia material
  rendering does not independently sample and blur controls behind it.
  `AcrylicBackgroundSource.Digger` reveals the underlying window backdrop rather
  than supplying an in-app backdrop blur.
- A normal blur effect blurs the control and its descendants, not its backdrop.
- Clipping the popup's entire native window would also clip its custom shadow.
  The backdrop and the shadow need different effective bounds.
- `WindowManagerAddShadowHint=True` produced a hard rectangular native shadow in
  the experiment, not an acceptable replacement for the existing soft shadow.

## Current shipping approach

- ComboBox retains opaque fallback colours in Classic and Mica.
- Its soft shadow follows MenuFlyout: `0 4 12 0 #40000000`, with an 8-DIP gutter.
  Width, maximum height, and placement compensate for that gutter.
- Editable ComboBox uses different vertical compensation above and below the
  input. Its joined corners and animations depend on actual popup placement.
- MenuFlyout (including DropDownButton menus and submenus) also retains its opaque
  Desktop Acrylic fallback in Classic and Mica. The plain-alpha Mica approximation
  was removed because it exposed readable underlying content without blur.
- Approved Windows baselines protect the current appearance. They do not prove
  native blur correctness: ordinary page captures do not open these popups, and
  headless rendering does not validate the Windows compositor backdrop.

## Agreed boundaries

- Keep the working fallback; do not reintroduce translucent ComboBox or MenuFlyout fills
  before clean native blur has been demonstrated.
- Start with a bounded investigation, not a commitment to a large implementation.
  Agree a time budget before active work and stop with evidence at the decision gate.
- Native Windows compositor interop or an Avalonia enhancement may be required.
  Neither is assumed necessary or approved in advance.
- Gate any future translucent material on actual blur availability and applicable
  system policy, not just the Mica theme flag. Keep opaque rendering elsewhere.
- Do not use global compositor options that change unrelated application windows
  as a substitute for per-popup clipping.
- Do not generate replacement baselines for unfinished acrylic styling.

## Investigation

### Phase 1: Prove clipping without losing the shadow

- [ ] Recreate a minimal native popup over coloured stripes and readable text.
  Request blur on `PopupRoot`, record `ActualTransparencyLevel`, and retain a
  side-by-side opaque reference.
- [ ] Trace current Avalonia popup/composition APIs for a per-popup backdrop clip
  or inset that is independent of the content and shadow.
- [ ] If public APIs are insufficient, identify the smallest Windows composition
  interop or upstream API change that could expose separate backdrop bounds.
  Treat separate shadow hosting as an alternative to assess, not a chosen design.
- [ ] Demonstrate a rounded blurred surface with an unblurred surrounding gutter
  and intact soft shadow. Check pixels over text as well as solid backgrounds.
- [ ] Record findings and stop for a go/no-go review. If the bounded attempt cannot
  demonstrate this cleanly, keep the fallback and document an upstream repro or
  the specific missing capability instead of building a speculative helper.

### Phase 2: Validate interactions, only after the clipping proof

- [ ] Exercise selected-row ComboBox placement and editable popups above/below,
  including asymmetric joined corners, scrolling, screen edges, and constrained height.
- [ ] Verify opening/closing animations: native blur must not leave a stationary
  rectangle while only the content translates or fades.
- [ ] Exercise MenuFlyout and nested submenus, including light dismissal, keyboard
  navigation, repeated opening, and disposal.
- [ ] Check Light/Dark, Classic/Mica, DPI changes, and multiple monitors.
- [ ] Check unsupported or unavailable blur, transparency/accessibility settings,
  remote sessions, and renderer/platform differences. Preserve an opaque surface
  until the native backdrop is confirmed, and restore it if availability changes.
- [ ] Capture native screen evidence and record limitations for user review.

### Phase 3: Production integration, only if approved

- [ ] Share the proven lifecycle/clipping/material mechanism between ComboBox and
  MenuFlyout, keeping control-specific geometry in their templates.
- [ ] Add focused functional tests for lifecycle and fallback decisions, plus
  native compositor verification for blur and clipping. Do not claim headless
  brush or screenshot tests prove native backdrop correctness.
- [ ] Replace MenuFlyout's opaque fallback only with proven blur and tune real material
  tint/luminosity against the installed WinUI Gallery.
- [ ] Obtain appearance approval before changing catalog status or baselines.
- [ ] Update theme guidance, commit the approved implementation, and move this
  document to completed when delivered.

## References

- [WinUI ComboBox template](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/ComboBox/ComboBox_themeresources.xaml)
  identifies the acrylic brush used by the native dropdown.
- [WinUI acrylic resources](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/Materials/Acrylic/AcrylicBrush_themeresources.xaml)
  define the Light/Dark material properties and opaque fallback colours.
- Avalonia 12.1.2: [PopupRoot](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Primitives/PopupRoot.cs),
  [WindowImpl](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Windows/Avalonia.Win32/WindowImpl.cs),
  [WinUiCompositionUtils](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Windows/Avalonia.Win32/WinRT/Composition/WinUiCompositionUtils.cs),
  and [WinUiCompositedWindow](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Windows/Avalonia.Win32/WinRT/Composition/WinUiCompositedWindow.cs)
  show the native transparency and backdrop composition path.
- [Skia DrawingContextImpl](https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Skia/Avalonia.Skia/DrawingContextImpl.cs)
  contains `CreateAcrylicPaint`, relevant to the limits of standalone acrylic borders.
- [AvaloniaUI/Avalonia#15471](https://github.com/AvaloniaUI/Avalonia/discussions/15471)
  is historical clipping discussion, not proof of a current hard limitation.
- Repository [ComboBox template](../../../../src/Devolutions.AvaloniaTheme.WinUI/Controls/ComboBox.axaml),
  [MenuFlyout template](../../../../src/Devolutions.AvaloniaTheme.WinUI/Controls/MenuFlyout.axaml),
  [Mica overlay](../../../../src/Devolutions.AvaloniaTheme.WinUI/Accents/ThemeResources.Windows11.axaml),
  and [theming skill](../../../../.github/skills/winui-control-theming/SKILL.md)
  contain the current surfaces, geometry, and material guidance.
